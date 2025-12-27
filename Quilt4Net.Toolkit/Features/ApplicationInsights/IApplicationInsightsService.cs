namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

public interface IApplicationInsightsService
{
    IAsyncEnumerable<LogItem> SearchAsync(IApplicationInsightsContext context, string environment, string text, TimeSpan timeSpan, SeverityLevel minSeverityLevel = SeverityLevel.Verbose);
    IAsyncEnumerable<MeasureData> GetMeasureAsync(IApplicationInsightsContext context, string environment, TimeSpan timeSpan);
    IAsyncEnumerable<CountData> GetCountAsync(IApplicationInsightsContext context, string environment, TimeSpan timeSpan);

    //TODO: --> Revisit

    IAsyncEnumerable<SummaryData> GetSummaryAsync(IApplicationInsightsContext context, string environment, TimeSpan timeSpan, SeverityLevel minSeverityLevel = SeverityLevel.Verbose);
    IAsyncEnumerable<LogDetails> GetDetails(string environment, string summaryIdentifier, TimeSpan timeSpan);
    Task<LogDetails> GetDetail(string environment, string id, TimeSpan timeSpan);
    IAsyncEnumerable<LogMeasurement> GetMeasurements(string environment, TimeSpan timeSpan);
    Task<bool> CanConnectAsync(IApplicationInsightsContext context);
}