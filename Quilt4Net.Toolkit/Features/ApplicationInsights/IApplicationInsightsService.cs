namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

public interface IApplicationInsightsService
{
    IAsyncEnumerable<SummaryData> GetSummaryAsync(string environment, TimeSpan timeSpan);
    Task<LogDetails> GetDetails(string environment, string summaryIdentifier);
    IAsyncEnumerable<LogMeasurement> GetMeasurements(string environment, TimeSpan timeSpan);
    IAsyncEnumerable<LogItem> SearchAsync(string environment, string correlationId, TimeSpan timeSpan);
}