namespace Quilt4Net.Toolkit.Client;

public interface IApplicationInsightsClient
{
    IAsyncEnumerable<SummaryData> GetSummaryAsync(string environment);
    Task<LogDetails> GetDetails(string environment, string appRoleName, string problemId);
}