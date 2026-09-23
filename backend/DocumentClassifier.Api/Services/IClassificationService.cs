using DocumentClassifier.Api.Models;

namespace DocumentClassifier.Api.Services;

public interface IClassificationService
{
    Task<ClassificationResult> ClassifyAsync(string extractedText, CancellationToken ct = default);
}
