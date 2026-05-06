namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

public interface IApplicationInsightsService
{
    Task<bool> CanConnectAsync(IApplicationInsightsContext context);
    IAsyncEnumerable<EnvironmentOption> GetEnvironments(IApplicationInsightsContext context);
    IAsyncEnumerable<LogItem> SearchAsync(IApplicationInsightsContext context, string environment, string text, TimeSpan timeSpan, SeverityLevel minSeverityLevel = SeverityLevel.Verbose);
    IAsyncEnumerable<MeasureData> GetMeasureAsync(IApplicationInsightsContext context, string environment, TimeSpan timeSpan);
    IAsyncEnumerable<CountData> GetCountAsync(IApplicationInsightsContext context, string environment, TimeSpan timeSpan);
    Task<LogDetails> GetDetail(IApplicationInsightsContext context, string id, LogSource source, string environment, TimeSpan timeSpan);
    Task<SummaryData> GetSummary(IApplicationInsightsContext context, string fingerprint, LogSource source, string environment, TimeSpan timeSpan);
    IAsyncEnumerable<SummarySubset> GetSummaries(IApplicationInsightsContext context, string environment, TimeSpan timeSpan);

    /// <summary>
    /// Get the latest version of each (application, environment) combination
    /// seen in the workspace. Tries the Quilt4NetStartup-tagged fast path
    /// first, then falls back to a general scan for cells the fast path
    /// did not cover. Pass <c>null</c> for <paramref name="lookback"/> to
    /// query the workspace's full retention.
    /// </summary>
    IAsyncEnumerable<VersionMatrixCell> GetVersionMatrixAsync(IApplicationInsightsContext context, TimeSpan? lookback = null);
}