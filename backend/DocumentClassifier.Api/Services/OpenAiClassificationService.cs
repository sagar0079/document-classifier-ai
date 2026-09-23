using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DocumentClassifier.Api.Models;

namespace DocumentClassifier.Api.Services;

/// <summary>
/// Sends OCR-extracted text to OpenAI (or an Azure OpenAI-compatible endpoint) and asks it to
/// classify the document and explain why. Calls the REST API directly rather than pulling in
/// the official SDK, to keep the dependency footprint small.
/// </summary>
public class OpenAiClassificationService : IClassificationService
{
    private static readonly string[] Categories =
        { "Invoice", "Contract", "ID Document", "Resume", "Receipt", "Other" };

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<OpenAiClassificationService> _logger;

    public OpenAiClassificationService(
        HttpClient httpClient,
        IConfiguration config,
        ILogger<OpenAiClassificationService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public async Task<ClassificationResult> ClassifyAsync(string extractedText, CancellationToken ct = default)
    {
        var apiKey = _config["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OpenAI:ApiKey is not configured. Set it via appsettings or the OpenAI__ApiKey environment variable.");

        var model = _config["OpenAI:Model"] ?? "gpt-4o-mini";

        var truncatedText = extractedText.Length > 6000
            ? extractedText[..6000]
            : extractedText;

        var systemPrompt =
            $"You classify scanned business documents based on their OCR-extracted text. " +
            $"Choose exactly one category from: {string.Join(", ", Categories)}. " +
            "Respond ONLY with minified JSON in this shape: " +
            "{\"category\": string, \"confidence\": number between 0 and 1, \"reason\": short string}.";

        var requestBody = new
        {
            model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = $"OCR text:\n\n{truncatedText}" }
            },
            temperature = 0,
            response_format = new { type = "json_object" }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await _httpClient.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("OpenAI API error {Status}: {Body}", response.StatusCode, responseBody);
            throw new InvalidOperationException("The classification request to OpenAI failed.");
        }

        using var doc = JsonDocument.Parse(responseBody);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "{}";

        using var resultDoc = JsonDocument.Parse(content);
        var root = resultDoc.RootElement;

        return new ClassificationResult
        {
            Category = root.TryGetProperty("category", out var cat) ? cat.GetString() ?? "Unknown" : "Unknown",
            Confidence = root.TryGetProperty("confidence", out var conf) ? conf.GetDouble() : 0,
            Reason = root.TryGetProperty("reason", out var reason) ? reason.GetString() ?? "" : ""
        };
    }
}
