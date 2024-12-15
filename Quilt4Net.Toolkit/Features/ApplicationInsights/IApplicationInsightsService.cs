namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

public interface IApplicationInsightsService
{
    IAsyncEnumerable<SummaryData> GetSummaryAsync(string environment);
    Task<LogDetails> GetDetails(string environment, string summaryIdentifier);
    IAsyncEnumerable<LogMeasurement> GetMeasurements(string environment);
    IAsyncEnumerable<LogItem> SearchAsync(string environment, string correlationId);
}