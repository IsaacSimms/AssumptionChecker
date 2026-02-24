///// persisted application settings (non-secret) /////

namespace AssumptionChecker.WPFApp.Models
{
    public class AppSettings
    {
        // == Properties == //
        public string EngineUrl    { get; set; } = "http://localhost:5046"; // base URL of the AssumptionChecker Engine API
        public int MaxAssumptions  { get; set; } = 10;                      // default max assumptions per analysis
    }
}