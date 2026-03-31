using System.Text.Json;
using Microsoft.Extensions.Options;

namespace PrincipleFsa.BaseApi.Integration.RagBridge;

public sealed class RagBridgeClient : IRagBridgeClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly RagBridgeOptions _options;

    public RagBridgeClient(HttpClient httpClient, IOptions<RagBridgeOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<RagQueryResponse> QueryAsync(RagQueryRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            throw new ArgumentException("Query must be provided.", nameof(request));
        }

        using var response = await _httpClient.PostAsJsonAsync(_options.QueryPath, new
        {
            collection = request.Collection,
            query = request.Query,
            top_k = request.TopK,
            where = request.Where
        }, JsonOptions, ct);

        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"RAG query failed with status {(int)response.StatusCode} ({response.ReasonPhrase}). Body: {body}",
                inner: null,
                statusCode: response.StatusCode
            );
        }

        var parsed = JsonSerializer.Deserialize<PythonRagQueryResponse>(body, JsonOptions);
        if (parsed is null)
        {
            throw new InvalidOperationException("RAG query returned an unreadable response.");
        }

        return new RagQueryResponse(
            parsed.Collection ?? request.Collection,
            (parsed.Matches ?? []).Select(m => new RagMatch(
                m.Id ?? "",
                m.Text ?? "",
                m.Metadata ?? new Dictionary<string, object>(),
                m.Distance
            )).ToList()
        );
    }

    public async Task<RagUpsertResponse> UpsertAsync(RagUpsertRequest request, CancellationToken ct = default)
    {
        if (request.Items is null || request.Items.Count == 0)
        {
            throw new ArgumentException("At least one item is required.", nameof(request));
        }

        using var response = await _httpClient.PostAsJsonAsync(_options.UpsertPath, new
        {
            collection = request.Collection,
            items = request.Items.Select(i => new
            {
                id = i.Id,
                text = i.Text,
                metadata = i.Metadata
            })
        }, JsonOptions, ct);

        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"RAG upsert failed with status {(int)response.StatusCode} ({response.ReasonPhrase}). Body: {body}",
                inner: null,
                statusCode: response.StatusCode
            );
        }

        var parsed = JsonSerializer.Deserialize<PythonRagUpsertResponse>(body, JsonOptions);
        if (parsed is null)
        {
            throw new InvalidOperationException("RAG upsert returned an unreadable response.");
        }

        return new RagUpsertResponse(parsed.Collection ?? request.Collection, parsed.Upserted);
    }

    private sealed record PythonRagUpsertResponse(string? Collection, int Upserted);

    private sealed record PythonRagQueryResponse(string? Collection, List<PythonRagMatch>? Matches);

    private sealed record PythonRagMatch(string? Id, string? Text, Dictionary<string, object>? Metadata, double Distance);
}

