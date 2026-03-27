namespace PrincipleFsa.BaseApi.Integration.PythonAgent;

public interface IMigrationOrchestrator
{
    Task<string> ProcessMigrationTask(string codeSnippet, CancellationToken ct = default);
}

