// <summary>
// Routes LLM requests to the correct provider based on the model name.
// Models starting with "claude" go to Anthropic, everything else to OpenAI.
// Handles cases where only one provider has a key configured.
// </summary>

using AssumptionChecker.Contracts;
using AssumptionChecker.Engine.Services;

namespace AssumptionChecker.Engine
{
    // == LLM client router (strategy pattern) == //
    public class LlmClientRouter : ILlmClient
    {
        private readonly IConfiguration _config;
        private readonly IServiceProvider _services;

        public LlmClientRouter(IConfiguration config, IServiceProvider services)
        {
            _config = config;
            _services = services;
        }

        // == route to correct provider based on model prefix == //
        public Task<AnalyzeResponse> AnalyzeAsync(AnalyzeRequest request, CancellationToken ct = default)
        {
            var model = request.Model?.ToLowerInvariant() ?? "";
            var isAnthropic = model.StartsWith("claude");

            if (isAnthropic)
            {
                if (string.IsNullOrEmpty(_config["Anthropic:ApiKey"]))
                    throw new InvalidOperationException("Anthropic API key is not configured. Please add your key in Settings.");

                var client = _services.GetRequiredService<AnthropicLlmClient>();
                return client.AnalyzeAsync(request, ct);
            }
            else
            {
                if (string.IsNullOrEmpty(_config["OpenAI:ApiKey"]))
                    throw new InvalidOperationException("OpenAI API key is not configured. Please add your key in Settings.");

                var client = _services.GetRequiredService<OpenAILlmClient>();
                return client.AnalyzeAsync(request, ct);
            }
        }
    }
}
