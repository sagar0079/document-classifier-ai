using System.Diagnostics;

namespace DocumentClassifier.Api.Services;

/// <summary>
/// Extracts text from images and PDFs using the Tesseract CLI (installed at the OS level
/// in the Docker image) plus poppler-utils for PDF-to-image conversion. Shelling out avoids
/// the native-binding headaches of the Tesseract NuGet wrapper and keeps the Docker image simple.
/// </summary>
public class TesseractOcrService : IOcrService
{
    private readonly ILogger<TesseractOcrService> _logger;

    public TesseractOcrService(ILogger<TesseractOcrService> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExtractTextAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        var workDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(workDir);

        try
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var inputPath = Path.Combine(workDir, "input" + extension);

            await using (var fs = File.Create(inputPath))
            {
                await fileStream.CopyToAsync(fs, ct);
            }

            var imagePath = inputPath;

            if (extension == ".pdf")
            {
                var pngPrefix = Path.Combine(workDir, "page");
                await RunProcessAsync("pdftoppm", $"-png -r 300 -f 1 -l 1 \"{inputPath}\" \"{pngPrefix}\"", ct);

                var firstPng = Directory.GetFiles(workDir, "page*.png").FirstOrDefault();
                imagePath = firstPng
                    ?? throw new InvalidOperationException("Could not convert the PDF's first page to an image for OCR.");
            }

            var outputPrefix = Path.Combine(workDir, "ocr-output");
            await RunProcessAsync("tesseract", $"\"{imagePath}\" \"{outputPrefix}\"", ct);

            var textPath = outputPrefix + ".txt";
            var text = File.Exists(textPath) ? await File.ReadAllTextAsync(textPath, ct) : string.Empty;

            return text.Trim();
        }
        finally
        {
            try { Directory.Delete(workDir, recursive: true); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to clean up temp OCR directory {Dir}", workDir); }
        }
    }

    private async Task RunProcessAsync(string fileName, string arguments, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start process '{fileName}'. Is it installed in the container?");

        var stderrTask = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        var stderr = await stderrTask;

        if (process.ExitCode != 0)
        {
            _logger.LogError("{FileName} exited with code {Code}: {Error}", fileName, process.ExitCode, stderr);
            throw new InvalidOperationException($"'{fileName}' failed: {stderr}");
        }
    }
}
