///// represents a single message in the chat conversation /////

namespace AssumptionChecker.WPFApp.Models
{
    public class ChatMessage
    {
        // == Properties == //
        public required string Role    { get; set; }                    // "User" or "Assistant"
        public required string Content { get; set; }                    // the message text
        public DateTime Timestamp      { get; set; } = DateTime.Now;    // when the message was created
        public bool IsUser             => Role == "User";               // convenience flag for UI binding
        public bool IsThinking         { get; set; }                    // true while waiting for engine response
        public List<string> SuggestedPrompts { get; set; } = new();     // suggested improved prompts from the engine
        public bool HasSuggestedPrompts      => SuggestedPrompts.Count > 0;
    }
}