namespace AssumptionChecker.Engine
{
    using AssumptionChecker.Contracts;
    public class ILlmClient
    {
        Task<AnalyzeResponse> AnalyzeAsync(AnalyzeRequest request, CancellationToken ct = default);
    }
}
