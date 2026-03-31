namespace PrincipleFsa.BaseApi.Integration.RagBridge;

public interface IRagBridgeClient
{
    Task<RagQueryResponse> QueryAsync(RagQueryRequest request, CancellationToken ct = default);

    Task<RagUpsertResponse> UpsertAsync(RagUpsertRequest request, CancellationToken ct = default);
}

