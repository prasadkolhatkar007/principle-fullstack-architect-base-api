namespace PrincipleFsa.BaseApi.Integration.RagBridge;

public sealed class RagBridgeOptions
{
    public const string SectionName = "RagBridge";

    public string BaseUrl { get; init; } = "http://localhost:8000";

    public string QueryPath { get; init; } = "/rag/query";

    public string UpsertPath { get; init; } = "/rag/upsert";

    public int TimeoutSeconds { get; init; } = 60;
}

