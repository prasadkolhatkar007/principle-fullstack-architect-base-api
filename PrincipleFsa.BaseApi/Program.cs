#nullable enable

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PrincipleFsa.BaseApi.Integration.PythonAgent;
using PrincipleFsa.BaseApi.Integration.RagBridge;
using PrincipleFsa.BaseApi.Infrastructure.Http;
using PrincipleFsa.BaseApi.Security;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Adds basic routing services
builder.Services.AddRouting();

builder.Services.AddHttpContextAccessor();

builder.Services.Configure<PythonAgentOptions>(
    builder.Configuration.GetSection(PythonAgentOptions.SectionName)
);

builder.Services.Configure<RagBridgeOptions>(
    builder.Configuration.GetSection(RagBridgeOptions.SectionName)
);

builder.Services.Configure<IdentityOptions>(
    builder.Configuration.GetSection(IdentityOptions.SectionName)
);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var identity = builder.Configuration.GetSection(IdentityOptions.SectionName).Get<IdentityOptions>() ?? new IdentityOptions();

        options.Authority = identity.Authority;
        options.Audience = identity.Audience;
        options.RequireHttpsMetadata = identity.RequireHttpsMetadata;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(rateLimiterOptions =>
{
    rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    rateLimiterOptions.AddPolicy("bff", httpContext =>
    {
        var partitionKey =
            httpContext.User.Identity?.IsAuthenticated == true
                ? (httpContext.User.FindFirst("sub")?.Value ?? httpContext.User.Identity?.Name ?? "auth")
                : (httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon");

        return RateLimitPartition.GetTokenBucketLimiter(
            partitionKey,
            _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 60,
                TokensPerPeriod = 30,
                ReplenishmentPeriod = TimeSpan.FromSeconds(30),
                AutoReplenishment = true,
                QueueLimit = 0
            });
    });
});

builder.Services.AddTransient<UserContextHeadersHandler>();

builder.Services.AddHttpClient<IMigrationOrchestrator, MigrationOrchestrator>((sp, httpClient) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PythonAgentOptions>>().Value;

    httpClient.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
    httpClient.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
})
    .AddHttpMessageHandler<UserContextHeadersHandler>()
    .AddStandardResilienceHandler(resilienceOptions =>
    {
        resilienceOptions.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
        resilienceOptions.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);

        resilienceOptions.Retry.MaxRetryAttempts = 2;
        resilienceOptions.CircuitBreaker.MinimumThroughput = 20;
        resilienceOptions.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
        resilienceOptions.CircuitBreaker.FailureRatio = 0.5;
        resilienceOptions.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(20);
    });

builder.Services.AddHttpClient<IRagBridgeClient, RagBridgeClient>((sp, httpClient) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RagBridgeOptions>>().Value;

    httpClient.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
    httpClient.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
})
    .AddHttpMessageHandler<UserContextHeadersHandler>()
    .AddStandardResilienceHandler(resilienceOptions =>
    {
        resilienceOptions.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
        resilienceOptions.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);

        resilienceOptions.Retry.MaxRetryAttempts = 2;
        resilienceOptions.CircuitBreaker.MinimumThroughput = 20;
        resilienceOptions.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
        resilienceOptions.CircuitBreaker.FailureRatio = 0.5;
        resilienceOptions.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(20);
    });

var app = builder.Build();

app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

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

// BFF endpoint for the UI: authenticated, rate-limited, resilient downstream call
app.MapPost("/bff/migration/analyze",
        async (AnalyzeRequest request, IMigrationOrchestrator orchestrator, CancellationToken ct) =>
        {
            var result = await orchestrator.ProcessMigrationTask(request.Code, ct);
            return Results.Ok(new { result });
        })
    .RequireAuthorization()
    .RequireRateLimiting("bff");

app.MapPost("/bff/rag/search",
        async (RagSearchRequest request, IRagBridgeClient rag, CancellationToken ct) =>
        {
            var result = await rag.QueryAsync(
                new RagQueryRequest(
                    Collection: request.Collection ?? "default",
                    Query: request.Query,
                    TopK: request.TopK ?? 5,
                    Where: request.Where
                ),
                ct
            );

            return Results.Ok(result);
        })
    .RequireAuthorization()
    .RequireRateLimiting("bff");

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

    app.MapPost("/debug/rag/search",
        async (RagSearchRequest request, IRagBridgeClient rag, CancellationToken ct) =>
        {
            var result = await rag.QueryAsync(
                new RagQueryRequest(
                    Collection: request.Collection ?? "default",
                    Query: request.Query,
                    TopK: request.TopK ?? 5,
                    Where: request.Where
                ),
                ct
            );

            return Results.Ok(result);
        });
}

app.Run();

internal sealed record AnalyzeRequest(string Code);

internal sealed record RagSearchRequest(
    string Query,
    string? Collection,
    int? TopK,
    IReadOnlyDictionary<string, object>? Where
);