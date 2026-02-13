///// service interface for assumption checker /////

using AssumptionChecker.Contracts;
namespace AssumptionChecker.Core
{
    // == takes interaction from .NET front ends and defines the contract for analyzing prompts for assumptions == // 
    public interface IAssumptionCheckerService
    {
        // == summary == //
        // service contract consumed by .NET front ends (CLI, VS Copilot extension, VS code extension, etc.)
        // Non-.NET clients need to call the API endpoint using JSON contracts

        // == AnalyzeAsync == //
        Task<AnalyzeResponse> AnalyzeAsync(string prompt, int maxAssumptions, CancellationToken cancellationToken = default);
    }
}
