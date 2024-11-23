namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

public interface IApplicationInsightsClient
{
    IAsyncEnumerable<SummaryData> GetSummaryAsync(string environment);
    Task<LogDetails> GetDetails(string environment, string appRoleName, string problemId);
}