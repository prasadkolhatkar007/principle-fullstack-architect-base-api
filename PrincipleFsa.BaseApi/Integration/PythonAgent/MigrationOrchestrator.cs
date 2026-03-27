using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace PrincipleFsa.BaseApi.Integration.PythonAgent;

public sealed class MigrationOrchestrator : IMigrationOrchestrator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly PythonAgentOptions _options;

    public MigrationOrchestrator(HttpClient httpClient, IOptions<PythonAgentOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<string> ProcessMigrationTask(string codeSnippet, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(codeSnippet))
        {
            throw new ArgumentException("Code snippet must be provided.", nameof(codeSnippet));
        }

        using var response = await _httpClient.PostAsJsonAsync(
            _options.AnalyzePath,
            new { code = codeSnippet },
            JsonOptions,
            ct
        );

        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Python agent call failed with status {(int)response.StatusCode} ({response.ReasonPhrase}). Body: {body}",
                inner: null,
                statusCode: response.StatusCode
            );
        }

        return TryExtractResult(body) ?? body;
    }

    private static string? TryExtractResult(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);

            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            return doc.RootElement.TryGetProperty("result", out var resultProp) && resultProp.ValueKind == JsonValueKind.String
                ? resultProp.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

