// == namespaces == //
using System.Text.Json;
using System.Text.Json.Serialization;
using AssumptionChecker.Contracts;
using AssumptionChecker.Engine.Services;

var builder = WebApplication.CreateBuilder(args); // initialize web application builder

builder.Services.AddSingleton<ILlmClient, OpenAILlmClient>(); // register OpenAILlmClient as the implementation for ILlmClient in the dependency injection container
builder.Services.ConfigureHttpJsonOptions(options => // configure JSON options for HTTP requests and responses
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)); // ensure enums are serialized in camelCase
});

var app = builder.Build(); // build the web application