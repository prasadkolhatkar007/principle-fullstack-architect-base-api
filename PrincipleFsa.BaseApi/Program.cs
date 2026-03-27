#nullable enable

using Microsoft.AspNetCore.Diagnostics;
using PrincipleFsa.BaseApi.Integration.PythonAgent;

var builder = WebApplication.CreateBuilder(args);

// Adds basic routing services
builder.Services.AddRouting();

builder.Services.Configure<PythonAgentOptions>(
    builder.Configuration.GetSection(PythonAgentOptions.SectionName)
);

builder.Services.AddHttpClient<IMigrationOrchestrator, MigrationOrchestrator>((sp, httpClient) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PythonAgentOptions>>().Value;

    httpClient.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
    httpClient.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});

var app = builder.Build();

app.UseHttpsRedirection();

// Robust Error Handling - Essential for Legacy Modernization
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var traceId = context.TraceIdentifier;

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        // In a production ADR, we would specify logging this to a sink 
        await context.Response.WriteAsJsonAsync(new
        {
            Title = "An unexpected error occurred.",
            Status = 500,
            TraceId = traceId,
            Message = app.Environment.IsDevelopment() ? exception?.Message : "Internal Server Error"
        });
    });
});



// Root Endpoint
app.MapGet("/", () => Results.Ok(new 
{ 
    Service = "Principle Fullstack Architect Base API",
    WorkItem = "Repo setup & Architecture Design (Minimal APIs)" 
}));

// Health Check
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));

// Dev-only endpoint to quickly verify Python-agent integration
if (app.Environment.IsDevelopment())
{
    app.MapPost("/debug/migration/analyze", async (
        AnalyzeRequest request,
        IMigrationOrchestrator orchestrator,
        CancellationToken ct) =>
    {
        var result = await orchestrator.ProcessMigrationTask(request.Code, ct);
        return Results.Ok(new { result });
    });
}

app.Run();

internal sealed record AnalyzeRequest(string Code);