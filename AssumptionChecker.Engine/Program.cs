// <summary>
// Engine entry point. Registers LLM clients (OpenAI + Anthropic), loads API keys
// from secure storage, and exposes /health, /templates, /analyze, and /settings endpoints.
// </summary>

// == namespaces == //
using AssumptionChecker.Core;
using AssumptionChecker.Contracts;
using AssumptionChecker.Engine;
using AssumptionChecker.Engine.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args); // initialize web application builder

builder.WebHost.UseUrls("http://localhost:5046"); // set the URL for the web application to listen on

// == secure settings == //
builder.Services.AddSingleton<ISecureSettingsManager, WindowsSecureSettingsManager>();

// == load API keys from secure storage into configuration == //
var secureSettings = new WindowsSecureSettingsManager();

var openAiKey    = secureSettings.GetApiKey("openai");
var anthropicKey = secureSettings.GetApiKey("anthropic");

if (!string.IsNullOrEmpty(openAiKey))
    builder.Configuration["OpenAI:ApiKey"] = openAiKey;

if (!string.IsNullOrEmpty(anthropicKey))
    builder.Configuration["Anthropic:ApiKey"] = anthropicKey;

// == register LLM clients and router == //
builder.Services.AddSingleton<OpenAILlmClient>();
builder.Services.AddSingleton<AnthropicLlmClient>();
builder.Services.AddSingleton<ILlmClient, LlmClientRouter>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

var app = builder.Build();

// == health check == //
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// == templates == //
app.MapGet("/templates", () => Results.Ok(new[]
{
    new { Id = "general", Name = "General", Description = "Analyze any prompt for assumptions" }
}));

// == analyze endpoint == //
app.MapPost("/analyze", async (AnalyzeRequest request, ILlmClient client, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Prompt))
        return Results.BadRequest(new { error = "Prompt cannot be empty." });

    try
    {
        var result = await client.AnalyzeAsync(request, ct);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, title: "Error analyzing prompt", statusCode: 500);
    }
});

// == settings: save API key for a provider == //
app.MapPost("/settings/apikey", (ApiKeySaveRequest request, ISecureSettingsManager settings, IConfiguration config) =>
{
    if (string.IsNullOrWhiteSpace(request.Provider) || string.IsNullOrWhiteSpace(request.ApiKey))
        return Results.BadRequest(new { error = "Provider and ApiKey are required." });

    var provider = request.Provider.ToLowerInvariant().Trim();
    if (provider != "openai" && provider != "anthropic")
        return Results.BadRequest(new { error = "Provider must be 'openai' or 'anthropic'." });

    settings.SaveApiKey(provider, request.ApiKey);

    // hot-reload into running config so the Engine picks it up without restart
    var configKey = provider == "openai" ? "OpenAI:ApiKey" : "Anthropic:ApiKey";
    config[configKey] = request.ApiKey;

    return Results.Ok(new { saved = true, provider });
});

// == settings: check which providers have keys configured == //
app.MapGet("/settings/providers", (ISecureSettingsManager settings) =>
{
    return Results.Ok(new
    {
        openai    = !string.IsNullOrEmpty(settings.GetApiKey("openai")),
        anthropic = !string.IsNullOrEmpty(settings.GetApiKey("anthropic"))
    });
});

app.Run();

// == DTO for the apikey save endpoint == //
public record ApiKeySaveRequest(string Provider, string ApiKey);
