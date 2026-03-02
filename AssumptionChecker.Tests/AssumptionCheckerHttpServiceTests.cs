///// handles tests for AssumptionCheckerHttpService, which is responsible for sending Analyze requests to the backend API and processing responses /////

// == namespaces == //
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AssumptionChecker.Contracts;
using AssumptionChecker.Core;
using Xunit;

namespace AssumptionChecker.Tests
{
    public class AssumptionCheckerHttpServiceTests
    {
        private static readonly JsonSerializerOptions _jsonOptions = new() 
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        [Theory]
        [InlineData("gpt-4.1")]
        [InlineData("o3-mini")]
        [InlineData("gpt-4o")]
        public async Task AnalyzeAsyncSendsModelInRequestBody(string expectedModel)
        {
            // Arrange
            AnalyzeRequest? receivedRequest = null; // Capture the request body sent to the mock server

            var fakeResponse = new AnalyzeResponse
            {
                Assumptions      = [],
                SuggestedPrompts = [],
                Metadata         = new ResponseMetadata { ModelUsed = expectedModel, LatencyMs = 1, TokensUsed = 1 }
            };

            // intercept the HTTP request and capture the body
            var handler = new FakeHttpHandler(async (request) =>
            {
                var body = await request.Content!.ReadAsStringAsync();
                receivedRequest = JsonSerializer.Deserialize<AnalyzeRequest>(body, _jsonOptions);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(fakeResponse, _jsonOptions),
                        Encoding.UTF8,
                        "application/json")
                };
            });

            var httpClient = new HttpClient(handler) { BaseAddress = new System.Uri("http://localhost:5046") }; // BaseAddress is required but won't be used since we're intercepting the request
            var service    = new AssumptionCheckerHttpService(httpClient);                                      // System under test

            // Act
            await service.AnalyzeAsync("Test prompt",maxAssumptions: 5, model: expectedModel);

            // Assert
            Assert.NotNull(receivedRequest);
            Assert.Equal(expectedModel, receivedRequest.Model);
        }

        // == minimal fake HttpMessageHandler that intercepts requests without hitting the network == //
        private class FakeHttpHandler : HttpMessageHandler
        {
            private readonly System.Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

            public FakeHttpHandler(System.Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
                => _handler = handler;

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
                => _handler(request);
        }
    }
}
