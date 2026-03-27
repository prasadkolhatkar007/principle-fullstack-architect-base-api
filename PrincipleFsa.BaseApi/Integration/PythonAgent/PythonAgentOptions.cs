namespace PrincipleFsa.BaseApi.Integration.PythonAgent;

public sealed class PythonAgentOptions
{
    public const string SectionName = "PythonAgent";

    public string BaseUrl { get; init; } = "http://localhost:8000";

    public string AnalyzePath { get; init; } = "/analyze";

    public int TimeoutSeconds { get; init; } = 60;
}
