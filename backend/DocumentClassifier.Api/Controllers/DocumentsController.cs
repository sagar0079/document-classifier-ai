using DocumentClassifier.Api.Models;
using DocumentClassifier.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DocumentClassifier.Api.Controllers;

[ApiController]
[Route("api/documents")]
public class DocumentsController : ControllerBase
{
    private static readonly string[] AllowedExtensions = { ".png", ".jpg", ".jpeg", ".pdf", ".tiff", ".bmp" };
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    private readonly IOcrService _ocrService;
    private readonly IClassificationService _classificationService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IOcrService ocrService,
        IClassificationService classificationService,
        ILogger<DocumentsController> logger)
    {
        _ocrService = ocrService;
        _classificationService = classificationService;
        _logger = logger;
    }

    [HttpPost("classify")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    public async Task<ActionResult<ClassifyResponse>> Classify(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new ErrorResponse { Message = "No file was uploaded." });

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return BadRequest(new ErrorResponse
            {
                Message = $"Unsupported file type '{extension}'. Allowed: {string.Join(", ", AllowedExtensions)}"
            });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var extractedText = await _ocrService.ExtractTextAsync(stream, file.FileName, ct);

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                return Ok(new ClassifyResponse
                {
                    FileName = file.FileName,
                    ExtractedText = string.Empty,
                    Classification = new ClassificationResult
                    {
                        Category = "Unknown",
                        Confidence = 0,
                        Reason = "No readable text was found in the document."
                    }
                });
            }

            var classification = await _classificationService.ClassifyAsync(extractedText, ct);

            return Ok(new ClassifyResponse
            {
                FileName = file.FileName,
                ExtractedText = extractedText,
                Classification = classification
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to classify document {FileName}", file.FileName);
            return StatusCode(500, new ErrorResponse { Message = "Failed to process the document. Please try again." });
        }
    }

    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "ok" });
}
