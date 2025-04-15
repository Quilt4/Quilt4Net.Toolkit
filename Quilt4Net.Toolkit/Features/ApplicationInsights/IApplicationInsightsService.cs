namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

public interface IApplicationInsightsService
{
    IAsyncEnumerable<SummaryData> GetSummaryAsync(string environment, TimeSpan timeSpan, SeverityLevel minSeverityLevel = SeverityLevel.Verbose);
    IAsyncEnumerable<LogDetails> GetDetails(string environment, string summaryIdentifier, TimeSpan timeSpan);
    Task<LogDetails> GetDetail(string environment, string id, TimeSpan timeSpan);
    IAsyncEnumerable<LogMeasurement> GetMeasurements(string environment, TimeSpan timeSpan);
    IAsyncEnumerable<LogItem> SearchAsync(string environment, string text, TimeSpan timeSpan);
}