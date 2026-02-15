// == namespaces == //
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssumptionChecker.Contracts;
using OpenAI.Chat;
namespace AssumptionChecker.Engine.Services
{
    public class OpenAILlmClient : ILlmClient
    {
        private readonly ChatClient _chat;                   // controls chat client
        private readonly JsonSerializerOptions _jsonOptions; // controls Json config and initialization

        // == retrieve API key and model from configuration, with error handling for missing API key and default model == //
        public OpenAILlmClient(IConfiguration config)
        {
            var apiKey = config["OpenAI:ApiKey"]                                            // retrieve API key from configuration (in user secret)
                ?? throw new InvalidOperationException("OpenAI:ApiKey is not configured."); // error handling for missing API key
            var model  = config["OpenAI:Model"] ?? "gpt-4o-mini";                           // retrieve model from configuration, with default to gpt-4o-mini if not specified

            _chat        = new ChatClient(model, apiKey); // initialize chat client with model and API key
            _jsonOptions = new JsonSerializerOptions      // initialize JSON options for consistent parsing and serialization
            {
                // ensures camelCase in JSON output
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
            };
        }

        // == build & intake system prompt and user prompt == //
        public async Task<AnalyzeResponse> AnalyzeAsync(AnalyzeRequest request, CancellationToken cancellationToken = default)
        {
            var systemPrompt = BuildSystemPrompt(request.MaxAssumptions); // call function that specifies the system prompt
            var sw           = Stopwatch.StartNew();                      // start a stopwatch to track latency

            // == build the message list with the system prompt and user prompt == //
            // optional file context
            var userMessage = request.Prompt;
            if (request.FileContexts.Count > 0)
            {
                var contextBlock = string.Join("\n\n", request.FileContexts.Select(f =>
                    $"--- File: {f.FilePath} ---\n{f.Content}"));
                userMessage = $"{request.Prompt}\n\n[Open File Context]\n{contextBlock}";
            }

            // initialize the message list with the system prompt and user prompt
            List<ChatMessage> messages =
                [
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userMessage)
                ];

            var options = new ChatCompletionOptions { ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat() }; // forces JSON format

            // reattempt 3 times if JSON continues to come back as invalid
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                ChatCompletion completion = await _chat.CompleteChatAsync(messages, options, cancellationToken);
                var raw = completion.Content[0].Text;

                try
                {
                    var (assumptions, suggestedPrompts) = ParseResponse(raw);
                    sw.Stop();

                    // returns the assumptions along with metadata about the response (latency, model used, tokens used) == // 
                    return new AnalyzeResponse
                    {
                        Assumptions = assumptions,
                        SuggestedPrompts = suggestedPrompts,
                        Metadata = new ResponseMetadata
                        {
                            LatencyMs = sw.ElapsedMilliseconds,
                            ModelUsed = completion.Model,
                            TokensUsed = completion.Usage.TotalTokenCount
                        }
                    };
                }
                // error handling 
                catch (JsonException) when (attempt < 3)
                {
                    //reprompt
                    messages.Add(new AssistantChatMessage(raw));
                    messages.Add(new UserChatMessage(
                        "Your previous response was not a valid JSON. " +
                        "Return only a JSON object with an \"assumptions\" array. "));
                }
            }
            throw new InvalidOperationException("LLM failed to return valid JSON (includes reattempts)"); // broad stroke error handle for the function
        }

        // == parse the assumption JSON == //
        private (List<Assumption> assumptions, List<string> suggestedPrompts) ParseResponse(string raw)
        {
            using var jsonDoc = JsonDocument.Parse(raw); // parse the raw JSON response

            List<Assumption> assumptions;                // initialize the assumptions list

            // parse assumptions - if the response includes the full JSON structure, parse out the assumptions from the "assumptions" property.
            if (jsonDoc.RootElement.TryGetProperty("assumptions", out var assumptionsArray))
            {
                assumptions = JsonSerializer.Deserialize<List<Assumption>>(assumptionsArray.GetRawText(), _jsonOptions)
                    ?? throw new JsonException("Null assumptions");
            }
            else
            {
                assumptions = JsonSerializer.Deserialize<List<Assumption>>(raw, _jsonOptions) ?? throw new JsonException("Null assumptions");
            }

            // parse suggested prompts
            List<string> suggestedPrompts = new();
            if (jsonDoc.RootElement.TryGetProperty("suggestedPrompts", out var promptsArray))
            {
                suggestedPrompts = JsonSerializer.Deserialize<List<string>>(promptsArray.GetRawText(), _jsonOptions)
                    ?? throw new JsonException("Null suggested prompts");
            }

            return (assumptions, suggestedPrompts);
        }

        // == defines system prompt and instructions for model                                                           == //
        // == includes instruction to return only JSON, and to limit the number of assumptions based on the user request == //
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
            
            Return ONLY a JSON object with this structure: 
            { 
            "assumptions": [ ... ],
            "suggestedPrompts": [ ... improved prompt 1 ..., ... improved prompt 2 ..., ... improved prompt 3 ...]
            }
            """;
    }
}