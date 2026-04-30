using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quilt4Net.Toolkit.Blazor.Features.Log;
using Quilt4Net.Toolkit.Features.ApplicationInsights;
using Xunit;

namespace Quilt4Net.Toolkit.Blazor.Tests;

public class LogEnvironmentSelectorIncidentTests : BunitContext
{
    public LogEnvironmentSelectorIncidentTests()
    {
        Services.AddLogging();
        Services.AddSingleton<IHostEnvironment>(new TestHostEnvironment());
    }

    [Fact]
    public void Renders_alert_with_Incident_id_when_GetEnvironments_throws()
    {
        Services.AddSingleton<IApplicationInsightsService>(new ThrowingApplicationInsightsService(new InvalidOperationException("boom")));

        var cut = Render<LogEnvironmentSelector>();

        cut.Markup.Should().Contain("Could not load Application Insights environments.");
        cut.Markup.Should().MatchRegex(@"\[Incident:\s+[2-9A-HJ-NP-Z]{6}\]");
    }

    private sealed class ThrowingApplicationInsightsService : IApplicationInsightsService
    {
        private readonly Exception _ex;
        public ThrowingApplicationInsightsService(Exception ex) => _ex = ex;

        public Task<bool> CanConnectAsync(IApplicationInsightsContext context) => Task.FromResult(false);

        public IAsyncEnumerable<EnvironmentOption> GetEnvironments(IApplicationInsightsContext context) => Throw<EnvironmentOption>();
        public IAsyncEnumerable<LogItem> SearchAsync(IApplicationInsightsContext context, string environment, string text, TimeSpan timeSpan, SeverityLevel minSeverityLevel = SeverityLevel.Verbose) => Throw<LogItem>();
        public IAsyncEnumerable<MeasureData> GetMeasureAsync(IApplicationInsightsContext context, string environment, TimeSpan timeSpan) => Throw<MeasureData>();
        public IAsyncEnumerable<CountData> GetCountAsync(IApplicationInsightsContext context, string environment, TimeSpan timeSpan) => Throw<CountData>();
        public Task<LogDetails> GetDetail(IApplicationInsightsContext context, string id, LogSource source, string environment, TimeSpan timeSpan) => Task.FromException<LogDetails>(_ex);
        public Task<SummaryData> GetSummary(IApplicationInsightsContext context, string fingerprint, LogSource source, string environment, TimeSpan timeSpan) => Task.FromException<SummaryData>(_ex);
        public IAsyncEnumerable<SummarySubset> GetSummaries(IApplicationInsightsContext context, string environment, TimeSpan timeSpan) => Throw<SummarySubset>();

#pragma warning disable CS1998
        private async IAsyncEnumerable<T> Throw<T>()
#pragma warning restore CS1998
        {
            throw _ex;
            yield break;
        }
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
