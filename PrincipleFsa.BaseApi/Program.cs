#nullable enable

using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Adds basic routing services
builder.Services.AddRouting();

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

app.Run();