// <summary>
// Routes LLM requests to the correct provider based on the model name.
// Models starting with "claude" go to Anthropic, everything else to OpenAI.
// Falls back to DPAPI secure storage if IConfiguration is missing a key.
// </summary>

using AssumptionChecker.Contracts;
using AssumptionChecker.Core;
using AssumptionChecker.Engine.Services;

namespace AssumptionChecker.Engine
{
    // == LLM client router (strategy pattern) == //
    public class LlmClientRouter : ILlmClient
    {
        private readonly IConfiguration _config;
        private readonly IServiceProvider _services;
        private readonly ISecureSettingsManager _secureSettings;

        public LlmClientRouter(IConfiguration config, IServiceProvider services, ISecureSettingsManager secureSettings)
        {
            _config = config;
            _services = services;
            _secureSettings = secureSettings;
        }

        // == route to correct provider based on model prefix == //
        public Task<AnalyzeResponse> AnalyzeAsync(AnalyzeRequest request, CancellationToken ct = default)
        {
            var model = request.Model?.ToLowerInvariant() ?? "";
            var isAnthropic = model.StartsWith("claude");

            if (isAnthropic)
            {
                var key = ResolveApiKey("anthropic", "Anthropic:ApiKey");
                if (string.IsNullOrEmpty(key))
                    throw new InvalidOperationException("Anthropic API key is not configured. Please add your key in Settings.");

                var client = _services.GetRequiredService<AnthropicLlmClient>();
                return client.AnalyzeAsync(request, ct);
            }
            else
            {
                var key = ResolveApiKey("openai", "OpenAI:ApiKey");
                if (string.IsNullOrEmpty(key))
                    throw new InvalidOperationException("OpenAI API key is not configured. Please add your key in Settings.");

                var client = _services.GetRequiredService<OpenAILlmClient>();
                return client.AnalyzeAsync(request, ct);
            }
        }

        // == check IConfiguration first, fall back to DPAPI, and hot-load into config == //
        private string? ResolveApiKey(string provider, string configKey)
        {
            var key = _config[configKey];
            if (!string.IsNullOrEmpty(key)) return key;

            key = _secureSettings.GetApiKey(provider); // DPAPI fallback
            if (!string.IsNullOrEmpty(key))
                _config[configKey] = key;              // hot-load so LLM clients find it

            return key;
        }
    }
}
