namespace PrincipleFsa.BaseApi.Integration.RagBridge;

public sealed record RagUpsertItem(string Id, string Text, IReadOnlyDictionary<string, object>? Metadata = null);

public sealed record RagUpsertRequest(string Collection, IReadOnlyList<RagUpsertItem> Items);

public sealed record RagUpsertResponse(string Collection, int Upserted);

public sealed record RagQueryRequest(
    string Collection,
    string Query,
    int TopK = 5,
    IReadOnlyDictionary<string, object>? Where = null
);

public sealed record RagMatch(
    string Id,
    string Text,
    IReadOnlyDictionary<string, object> Metadata,
    double Distance
);

public sealed record RagQueryResponse(string Collection, IReadOnlyList<RagMatch> Matches);

