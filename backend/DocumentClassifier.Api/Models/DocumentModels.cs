namespace DocumentClassifier.Api.Models;

public class ClassificationResult
{
    public string Category { get; set; } = "Unknown";
    public double Confidence { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class ClassifyResponse
{
    public string FileName { get; set; } = string.Empty;
    public string ExtractedText { get; set; } = string.Empty;
    public ClassificationResult Classification { get; set; } = new();
}

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
}
