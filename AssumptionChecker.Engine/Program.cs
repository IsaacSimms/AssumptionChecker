// == namespaces == //
using AssumptionChecker.Core;
using AssumptionChecker.Contracts;
using AssumptionChecker.Engine;
using AssumptionChecker.Engine.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args); // initialize web application builder

builder.WebHost.UseUrls("http://localhost:5046"); // set the URL for the web application to listen on

// Register secure settings manager
builder.Services.AddSingleton<ISecureSettingsManager, WindowsSecureSettingsManager>();

// Load API key from secure storage and add to configuration
var tempServiceProvider = builder.Services.BuildServiceProvider();
var secureSettings = tempServiceProvider.GetRequiredService<ISecureSettingsManager>();
var apiKey = secureSettings.GetApiKey();

if (!string.IsNullOrEmpty(apiKey))
{
    builder.Configuration["OpenAI:ApiKey"] = apiKey;
}

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

    try
    {
        var result = await client.AnalyzeAsync(request, ct); // call the AnalyzeAsync method of the ILlmClient to analyze the prompt for assumptions
        return Results.Ok(result);                           // return the analysis result as an HTTP response
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, title: "Error analyzing prompt", statusCode: 500); // return an error response if the analysis fails
    }
});

app.Run(); // run the web application