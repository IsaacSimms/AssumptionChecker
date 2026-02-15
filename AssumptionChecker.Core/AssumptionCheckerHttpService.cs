///// HTTP client implementation that calls the AssumptionChecker Engine API /////


// == namespaces == //
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssumptionChecker.Contracts;

namespace AssumptionChecker.Core
{
    // == implementation of the IAssumptionCheckerService interface that uses HttpClient to call the AssumptionChecker API == //
    public class AssumptionCheckerHttpService : IAssumptionCheckerService
    {
        private readonly HttpClient _httpClient;             // HttpClient instance for making API calls
        private readonly JsonSerializerOptions _jsonOptions; // JSON serialization options

        // == constructor that initializes the HttpClient and JSON options == //
        public AssumptionCheckerHttpService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
                WriteIndented = true
            };
        }

        // == implements AnalyzeResponse method that connects to AssumptionChecker API== //
        public async Task<AnalyzeResponse> AnalyzeAsync(string prompt, int maxAssumptions, List<FileContext>? fileContexts = null, CancellationToken cancellationToken = default)
        {
            var request = new AnalyzeRequest
            {
                Prompt         = prompt,                // the user made prompt to analyze
                MaxAssumptions = maxAssumptions,        // the maximum number of assumptions to return
                FileContexts   = fileContexts ?? new()  // optional file context from the IDE
            };

            // Send the request to the AssumptionChecker API and ensure a successful response
            var response = await _httpClient.PostAsJsonAsync("/analyze", request, _jsonOptions, cancellationToken); 
            response.EnsureSuccessStatusCode();

            // Read and deserialize the response content into an AnalyzeResponse object, throwing an exception if the response is null
            return await response.Content.ReadFromJsonAsync<AnalyzeResponse>(_jsonOptions, cancellationToken)
                ?? throw new InvalidOperationException("Received null response from the AssumptionChecker API.");
        }
    }
}
