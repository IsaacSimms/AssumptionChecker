// <summary>
// Anthropic (Claude) LLM client implementing ILlmClient.
// Calls the Anthropic Messages API, parses JSON response into AnalyzeResponse.
// Same system prompt and retry logic as OpenAILlmClient.
// Key is resolved at call time so hot-reload from /settings/apikey works.
// </summary>

// == namespaces == //
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Anthropic;
using Anthropic.Core;
using Anthropic.Models.Messages;
using AssumptionChecker.Contracts;

namespace AssumptionChecker.Engine.Services
{
    // == Anthropic LLM client == //
    public class AnthropicLlmClient : ILlmClient
    {
        private readonly IConfiguration _config;
        private readonly JsonSerializerOptions _jsonOptions;

        // == store config ref; key is resolved at call time so hot-reload works == //
        public AnthropicLlmClient(IConfiguration config)
        {
            _config = config;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
            };
        }

        // == analyze prompt using Anthropic Messages API == //
        public async Task<AnalyzeResponse> AnalyzeAsync(AnalyzeRequest request, CancellationToken cancellationToken = default)
        {
            var systemPrompt = BuildSystemPrompt(request.MaxAssumptions);
            var sw = Stopwatch.StartNew();

            // == build user message with optional file context == //
            var userMessage = request.Prompt;
            if (request.FileContexts.Count > 0)
            {
                var contextBlock = string.Join("\n\n", request.FileContexts.Select(f =>
                    $"--- File: {f.FilePath} ---\n{f.Content}"));
                userMessage = $"{request.Prompt}\n\n[Open File Context]\n{contextBlock}";
            }

            // == initialize conversation messages == //
            var messages = new List<MessageParam>
            {
                new() { Role = Role.User, Content = userMessage }
            };

            var apiKey = _config["Anthropic:ApiKey"]
                ?? throw new InvalidOperationException("Anthropic:ApiKey is not configured.");
            var model = string.IsNullOrWhiteSpace(request.Model) ? "claude-sonnet-4-6" : request.Model;
            var client = new AnthropicClient(new ClientOptions { ApiKey = apiKey });

            // == retry loop (up to 3 attempts on JSON parse failure) == //
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                var createParams = new MessageCreateParams
                {
                    Model = model,
                    MaxTokens = 4096,
                    System = systemPrompt,
                    Messages = messages
                };

                var completion = await client.Messages.Create(createParams, cancellationToken: cancellationToken);

                // == extract text content from response == //
                var textParts = new List<string>();
                foreach (var block in completion.Content)
                {
                    if (block.TryPickText(out var textBlock))
                        textParts.Add(textBlock.Text);
                }
                var raw = string.Join("", textParts);

                try
                {
                    var (assumptions, suggestedPrompts) = ParseResponse(raw);
                    sw.Stop();

                    return new AnalyzeResponse
                    {
                        Assumptions = assumptions,
                        SuggestedPrompts = suggestedPrompts,
                        Metadata = new ResponseMetadata
                        {
                            LatencyMs = sw.ElapsedMilliseconds,
                            ModelUsed = completion.Model.ToString(),
                            TokensUsed = (int)(completion.Usage.InputTokens + completion.Usage.OutputTokens)
                        }
                    };
                }
                catch (JsonException) when (attempt < 3)
                {
                    // reprompt: add assistant response and retry instruction
                    messages.Add(new MessageParam { Role = Role.Assistant, Content = raw });
                    messages.Add(new MessageParam { Role = Role.User, Content =
                        "Your previous response was not valid JSON. " +
                        "Return ONLY a JSON object with an \"assumptions\" array and a \"suggestedPrompts\" array." });
                }
            }

            throw new InvalidOperationException("Anthropic LLM failed to return valid JSON (includes reattempts)");
        }

        // == parse JSON response into assumptions and suggested prompts == //
        private (List<Assumption> assumptions, List<string> suggestedPrompts) ParseResponse(string raw)
        {
            using var jsonDoc = JsonDocument.Parse(raw);
            List<Assumption> assumptions;

            if (jsonDoc.RootElement.TryGetProperty("assumptions", out var assumptionsArray))
            {
                assumptions = JsonSerializer.Deserialize<List<Assumption>>(assumptionsArray.GetRawText(), _jsonOptions)
                    ?? throw new JsonException("Null assumptions");
            }
            else
            {
                assumptions = JsonSerializer.Deserialize<List<Assumption>>(raw, _jsonOptions)
                    ?? throw new JsonException("Null assumptions");
            }

            List<string> suggestedPrompts = new();
            if (jsonDoc.RootElement.TryGetProperty("suggestedPrompts", out var promptsArray))
            {
                suggestedPrompts = JsonSerializer.Deserialize<List<string>>(promptsArray.GetRawText(), _jsonOptions)
                    ?? throw new JsonException("Null suggested prompts");
            }

            return (assumptions, suggestedPrompts);
        }

        // == system prompt (same as OpenAI version, with stronger JSON instruction since Claude lacks response_format) == //
        private static string BuildSystemPrompt(int maxAssumptions) => $$"""
            You are a prompt analysis engine that identifies the most critical assumptions in user prompts.
            You will be given a user's prompt that will later be sent to a separate AI system for processing.
            An assumption is any explicit or implicit condition that is required for the success or validity of the plan, is not fully verified or guaranteed by the text itself, and would materially affect outcomes if false.

            Your task is to identify ONLY the most critical and impactful assumptions - prioritize quality over quantity.
            Focus on assumptions that would significantly change the outcome if they were incorrect.
            Skip minor, obvious, or low-impact assumptions.

            For each assumption, return a JSON object with the following content:
            - id: a unique identifier for the assumption (e.g., "assumption-1")
            - assumptionText: a brief, clear statement of the assumption (max 15 words)
            - rationale: a concise explanation of why this matters (max 25 words)
            - category: "userContext", "domainContext", "constraints", "outputFormat", "ambiguity", or "other"
            - riskLevel: one of "low", "medium", "high" (prioritize medium and high risk assumptions)
            - clarifyingQuestion: a short, direct question to verify the assumption (max 20 words). Only include if absolutely necessary.
            - confidence: a number between 0 and 1 indicating confidence this is a critical assumption

            Additionally, generate 2-3 improved versions of the original prompt to reduce assumptions and ambiguity.
            Each improved prompt must be:
            - Complete, specific, and immediately copy-pastable
            - NO placeholders like [specific date], [insert details], [your experience level], etc.
            - Concrete and actionable with reasonable defaults or specific examples
            - Only append "Additional Information: [what is needed]" if context is absolutely critical
            - Maintain the original intent while being immediately usable

            Return at most {{maxAssumptions}} assumptions, ordered by riskLevel (high to low) and confidence (high to low).
            Be concise and direct - brevity improves readability.

            IMPORTANT: Return ONLY a valid JSON object. No markdown code fences, no explanation text.
            The response must be parseable JSON with this exact structure:
            {
            "assumptions": [ ... ],
            "suggestedPrompts": [ ... improved prompt 1 ..., ... improved prompt 2 ..., ... improved prompt 3 ...]
            }
            """;
    }
}
