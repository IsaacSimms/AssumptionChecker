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

            // build the message list with the system prompt and user prompt
            List<ChatMessage> messages =
                [
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(request.Prompt)
                ];

            var options = new ChatCompletionOptions { ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat() }; // forces JSON format

            // reattempt 3 times if JSON continues to come back as invalid
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                ChatCompletion completion = await _chat.CompleteChatAsync(messages, options, cancellationToken);
                var raw = completion.Content[0].Text;

                try
                {
                    var assumptions = ParseAssumptions(raw);
                    sw.Stop();

                    // returns the assumptions along with metadata about the response (latency, model used, tokens used) == // 
                    return new AnalyzeResponse
                    {
                        Assumptions = assumptions,
                        Metadata = new ResponseMetadata
                        {
                            LatencyMs = sw.ElapsedMilliseconds,
                            ModelUsed = completion.Model,
                            TokensUsed = completion.Usage.TotalTokenCount
                        }
                    };
                }
                // error handling 
                catch (JsonException) when (attempt == 0)
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
        private List<Assumption> ParseAssumptions(string raw)
        {
            using var jsonDoc = JsonDocument.Parse(raw);

            // handle { "assumptions": [...] } and bare [...]
            if (jsonDoc.RootElement.TryGetProperty("assumptions", out var arr))
            {
                return JsonSerializer.Deserialize<List<Assumption>>(arr.GetRawText(), _jsonOptions)
                    ?? throw new JsonException("Null assumptions");
            }

            return JsonSerializer.Deserialize<List<Assumption>>(raw, _jsonOptions)
                ?? throw new JsonException("Failed to deserialize assumptions.");
        }

        // == defines system prompt and instructions for model                                                           == //
        // == includes instruction to return only JSON, and to limit the number of assumptions based on the user request == //
        private static string BuildSystemPrompt(int maxAssumptions) => $"""
            You are a prompt analysis engine that identifies assumptions in user prompts.
            You will be given a user's prompt that will later be sent to a seperate AI system for processing. 
            An assumption is any explicit or implicit condition that is required for the success or validity of the plan, is not fully verified or guaranteed by the text itself, and would materially affect outcomes if false.
            Your task is to identify and list the assumptions an AI system may make in relation to the user's prompt and any provided context.

            For each assumption, return a JSON object with the following content:
            - id: a unique identifier for the assumption (e.g., "assumption-1")
            - assumption: a concise statement of the assumption
            - rationale: a brief explanation of why this is an assumption, how it relates to the user's prompt, and why it is important.
            - category: "userContext", "domainContext", "constraintsDefaults", "outputFormat", "ambiguity"
            - riskLevel: one of "low", "medium", "high"
            - clarificationQuestion: a question that could be asked to the user to clarify or verify the assumption. Asking a clarifying question is OPTIONAL and should only be included if absolutely necessary to clarify the assumption. The question should be concise and directly related to the assumption.
            - confidence: a number between 0 and 1 indicating how confident you are that this is an assumption relevant to the prompt, where 0 means not confident at all and 1 means extremely confident.
            
            Return at most { maxAssumptions} assumptions, orderd by riskLevel (high to low) and confidence (high to low).
            Return ONLY a JSON object: "assumptions": [ ... ] 
            """;
    }
}