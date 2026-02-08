namespace AssumptionChecker.Engine
{
    using AssumptionChecker.Contracts;
    public interface ILlmClient
    {
        Task<AnalyzeResponse> AnalyzeAsync(AnalyzeRequest request, CancellationToken ct = default);
    }
}
