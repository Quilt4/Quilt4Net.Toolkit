using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using Quilt4Net.Toolkit.Features.Logging;
using Xunit;

namespace Quilt4Net.Toolkit.Tests;

public class TelemetryIdentityEnrichmentTests
{
    private static readonly TelemetryIdentity FullIdentity = new(
        Environment: "Production",
        ApplicationName: "MyApp",
        Version: "1.2.3",
        MachineName: "host-42",
        MonitorName: "Quilt4Net");

    [Fact]
    public void LogProcessor_attaches_all_five_attributes_to_a_record()
    {
        var record = BuildLogRecord();

        new TelemetryIdentityLogProcessor(FullIdentity).OnEnd(record);

        record.Attributes.Should().Contain(new KeyValuePair<string, object>("deployment.environment", "Production"));
        record.Attributes.Should().Contain(new KeyValuePair<string, object>("service.name", "MyApp"));
        record.Attributes.Should().Contain(new KeyValuePair<string, object>("service.version", "1.2.3"));
        record.Attributes.Should().Contain(new KeyValuePair<string, object>("host.name", "host-42"));
        record.Attributes.Should().Contain(new KeyValuePair<string, object>("quilt4net.monitor", "Quilt4Net"));
    }

    [Fact]
    public void LogProcessor_skips_attributes_whose_identity_value_is_empty()
    {
        var partial = FullIdentity with { Environment = null, MonitorName = "" };
        var record = BuildLogRecord();

        new TelemetryIdentityLogProcessor(partial).OnEnd(record);

        record.Attributes.Should().NotContain(kv => kv.Key == "deployment.environment");
        record.Attributes.Should().NotContain(kv => kv.Key == "quilt4net.monitor");
        record.Attributes.Should().Contain(kv => kv.Key == "service.name" && kv.Value!.Equals("MyApp"));
    }

    [Fact]
    public void LogProcessor_preserves_pre_existing_attributes()
    {
        var seed = new List<KeyValuePair<string, object>> { new("custom-key", "custom-value") };
        var record = BuildLogRecord(seed);

        new TelemetryIdentityLogProcessor(FullIdentity).OnEnd(record);

        record.Attributes.Should().Contain(kv => kv.Key == "custom-key" && kv.Value!.Equals("custom-value"));
        record.Attributes.Should().Contain(kv => kv.Key == "deployment.environment");
    }

    [Fact]
    public void ActivityProcessor_attaches_all_five_tags_to_an_activity()
    {
        using var source = new ActivitySource(nameof(TelemetryIdentityEnrichmentTests));
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        using var activity = source.StartActivity("test")!;

        new TelemetryIdentityActivityProcessor(FullIdentity).OnEnd(activity);

        activity.GetTagItem("deployment.environment").Should().Be("Production");
        activity.GetTagItem("service.name").Should().Be("MyApp");
        activity.GetTagItem("service.version").Should().Be("1.2.3");
        activity.GetTagItem("host.name").Should().Be("host-42");
        activity.GetTagItem("quilt4net.monitor").Should().Be("Quilt4Net");
    }

    [Fact]
    public void ActivityProcessor_skips_tags_whose_identity_value_is_empty()
    {
        using var source = new ActivitySource(nameof(TelemetryIdentityEnrichmentTests));
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        using var activity = source.StartActivity("test")!;

        var partial = FullIdentity with { Environment = null, MonitorName = "" };
        new TelemetryIdentityActivityProcessor(partial).OnEnd(activity);

        activity.GetTagItem("deployment.environment").Should().BeNull();
        activity.GetTagItem("quilt4net.monitor").Should().BeNull();
        activity.GetTagItem("service.name").Should().Be("MyApp");
    }

    /// <summary>
    /// LogRecord has no public constructor — emit a real one through the OTel SDK pipeline
    /// and capture the post-enrichment state so the test can assert on attributes.
    /// </summary>
    private static LogRecord BuildLogRecord(IList<KeyValuePair<string, object>> seedAttributes = null)
    {
        LogRecord captured = null!;
        using var loggerFactory = LoggerFactory.Create(b =>
        {
            b.AddOpenTelemetry(o =>
            {
                if (seedAttributes != null) o.AddProcessor(new SeedProcessor(seedAttributes));
                o.AddProcessor(new CaptureProcessor(r => captured = r));
            });
        });
        loggerFactory.CreateLogger("test").LogInformation("hello");
        return captured;
    }

    private sealed class CaptureProcessor : BaseProcessor<LogRecord>
    {
        private readonly Action<LogRecord> _capture;
        public CaptureProcessor(Action<LogRecord> capture) => _capture = capture;
        public override void OnEnd(LogRecord data) => _capture(data);
    }

    private sealed class SeedProcessor : BaseProcessor<LogRecord>
    {
        private readonly IReadOnlyList<KeyValuePair<string, object?>> _attrs;
        public SeedProcessor(IList<KeyValuePair<string, object>> attrs) =>
            _attrs = attrs.Select(kv => new KeyValuePair<string, object?>(kv.Key, kv.Value)).ToList();
        public override void OnEnd(LogRecord data) => data.Attributes = _attrs;
    }
}
