// == namespaces == //
using System.Text.Json;
using System.Text.Json.Serialization;
using AssumptionChecker.Contracts;
using AssumptionChecker.Engine;
using AssumptionChecker.Engine.Services;

var builder = WebApplication.CreateBuilder(args); // initialize web application builder

builder.Services.AddSingleton<ILlmClient, OpenAILlmClient>(); // register OpenAILlmClient as the implementation for ILlmClient in the dependency injection container
builder.Services.ConfigureHttpJsonOptions(options =>          // configure JSON options for HTTP requests and responses
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)); // ensure enums are serialized in camelCase
});

var app = builder.Build(); // build the web application

// define endpoints for the web application
app.MapGet("/health", () => Results.Ok(new { status = "healthy"})); // define a GET endpoint for health checks
app.MapGet("/templates", () => Results.Ok(new[]
{
    new { Id = "general", Name = "General", Description = "Analyze any prompt for assumptions" }
}));

app.MapPost("/analyze", async (AnalyzeRequest request, ILlmClient client, CancellationToken ct) => // define a POST endpoint for analyzing prompts for assumptions
{
    // validate prompt input
    if (string.IsNullOrWhiteSpace(request.Prompt)) // validate the input prompt
    {
        return Results.BadRequest(new { error = "Prompt cannot be empty." });
    }

    var result = await client.AnalyzeAsync(request, ct); // call the AnalyzeAsync method of the ILlmClient to analyze the prompt for assumptions
    return Results.Ok(result);                           // return the analysis result as an HTTP response
});

app.Run(); // run the web application