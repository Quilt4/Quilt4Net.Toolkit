namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

public interface IApplicationInsightsService
{
    Task<bool> CanConnectAsync(IApplicationInsightsContext context);
    IAsyncEnumerable<LogItem> SearchAsync(IApplicationInsightsContext context, string environment, string text, TimeSpan timeSpan, SeverityLevel minSeverityLevel = SeverityLevel.Verbose);
    IAsyncEnumerable<MeasureData> GetMeasureAsync(IApplicationInsightsContext context, string environment, TimeSpan timeSpan);
    IAsyncEnumerable<CountData> GetCountAsync(IApplicationInsightsContext context, string environment, TimeSpan timeSpan);
    Task<LogDetails> GetDetail(IApplicationInsightsContext context, string id, LogSource source, TimeSpan timeSpan);
    Task<SummaryData> GetSummary(IApplicationInsightsContext context, string fingerprint, LogSource source, TimeSpan timeSpan);
    IAsyncEnumerable<SummarySubset> GetSummaries(IApplicationInsightsContext context, TimeSpan timeSpan);
}