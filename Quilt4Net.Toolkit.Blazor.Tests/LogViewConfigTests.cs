using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Quilt4Net.Toolkit.Blazor.Features.Log;
using Quilt4Net.Toolkit.Features.ApplicationInsights;
using Xunit;

namespace Quilt4Net.Toolkit.Blazor.Tests;

public class LogViewConfigTests : BunitContext
{
    public LogViewConfigTests()
    {
        // Register a stub service so the LogView's service-missing guard passes — these tests
        // exercise the *options*-missing guard, which runs after the service guard.
        Services.AddSingleton<IApplicationInsightsService>(new StubApplicationInsightsService());
    }

    [Fact]
    public void Shows_Error_When_ApplicationInsights_Not_Configured()
    {
        Services.AddSingleton(Options.Create(new ApplicationInsightsOptions()));

        var cut = Render<LogView>();

        cut.Markup.Should().Contain("Application Insights is not configured");
    }

    [Fact]
    public void Shows_Error_When_ClientSecret_Is_Missing()
    {
        Services.AddSingleton(Options.Create(new ApplicationInsightsOptions
        {
            TenantId = "tenant",
            WorkspaceId = "workspace",
            ClientId = "client",
            ClientSecret = null
        }));

        var cut = Render<LogView>();

        cut.Markup.Should().Contain("Application Insights is not configured");
    }

    [Fact]
    public void Shows_Error_When_Options_Not_Registered()
    {
        var cut = Render<LogView>();

        cut.Markup.Should().Contain("Application Insights is not configured");
    }

    private sealed class StubApplicationInsightsService : IApplicationInsightsService
    {
        public Task<bool> CanConnectAsync(IApplicationInsightsContext context) => Task.FromResult(false);
        public IAsyncEnumerable<EnvironmentOption> GetEnvironments(IApplicationInsightsContext context) => Empty<EnvironmentOption>();
        public IAsyncEnumerable<LogItem> SearchAsync(IApplicationInsightsContext context, string environment, string text, TimeSpan timeSpan, SeverityLevel minSeverityLevel = SeverityLevel.Verbose) => Empty<LogItem>();
        public IAsyncEnumerable<MeasureData> GetMeasureAsync(IApplicationInsightsContext context, string environment, TimeSpan timeSpan) => Empty<MeasureData>();
        public IAsyncEnumerable<CountData> GetCountAsync(IApplicationInsightsContext context, string environment, TimeSpan timeSpan) => Empty<CountData>();
        public Task<LogDetails> GetDetail(IApplicationInsightsContext context, string id, LogSource source, string environment, TimeSpan timeSpan) => Task.FromResult<LogDetails>(null);
        public Task<SummaryData> GetSummary(IApplicationInsightsContext context, string fingerprint, LogSource source, string environment, TimeSpan timeSpan) => Task.FromResult<SummaryData>(null);
        public IAsyncEnumerable<SummarySubset> GetSummaries(IApplicationInsightsContext context, string environment, TimeSpan timeSpan) => Empty<SummarySubset>();

        private static async IAsyncEnumerable<T> Empty<T>()
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
