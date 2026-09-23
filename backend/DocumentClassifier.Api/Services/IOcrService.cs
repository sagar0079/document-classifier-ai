namespace DocumentClassifier.Api.Services;

public interface IOcrService
{
    Task<string> ExtractTextAsync(Stream fileStream, string fileName, CancellationToken ct = default);
}
