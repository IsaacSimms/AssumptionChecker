///// persisted application settings (non-secret) /////

using System.Text.Json.Serialization;

namespace AssumptionChecker.WPFApp.Models
{
    public class AppSettings
    {
        // == Properties == //
        public int    MaxAssumptions { get; set; } = 10;                  // default max assumptions per analysis

        [JsonPropertyName("OpenAiModel")]                                 // backward compat with existing settings files
        public string Model          { get; set; } = "claude-haiku-4-5";  // default LLM model for assumption generation
    }
}