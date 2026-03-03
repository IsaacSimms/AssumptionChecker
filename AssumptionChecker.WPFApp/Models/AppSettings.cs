///// persisted application settings (non-secret) /////

namespace AssumptionChecker.WPFApp.Models
{
    public class AppSettings
    {
        // == Properties == //
        public int    MaxAssumptions { get; set; } = 10;             // default max assumptions per analysis
        public string OpenAiModel   { get; set; } = "gpt-4o-mini";  // default OpenAI model for assumption generation
    }
}