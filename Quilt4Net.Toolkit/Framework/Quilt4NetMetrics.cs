using System.Diagnostics.Metrics;

namespace Quilt4Net.Toolkit.Framework;

/// <summary>
/// Meter names and instrument names for the toolkit's own metrics, in one place so a host can
/// subscribe to them by name without reading the source.
/// </summary>
/// <remarks>
/// Issue #170: content and configuration resolution were instrumented with timings and a source, but
/// only as `Debug` logs — so a consumer could see *that* one call was slow and never *how many* calls
/// there are, nor what fraction hit the cache. Counting log rows is not a viable substitute: one
/// screen can resolve dozens of keys, and the interesting number (the cache-hit ratio) is only
/// knowable inside the library, because the public read returns a bare value.
/// <para>
/// Deliberately near-zero cost when nobody is listening — an unsubscribed <see cref="Counter{T}"/>
/// records nothing.
/// </para>
/// </remarks>
public static class Quilt4NetMetrics
{
    /// <summary>Meter carrying the content-resolution instruments.</summary>
    public const string ContentMeterName = "Quilt4Net.Toolkit.Content";

    /// <summary>Meter carrying the remote-configuration instruments.</summary>
    public const string ConfigurationMeterName = "Quilt4Net.Toolkit.Configuration";

    /// <summary>Count of content resolutions, tagged by <c>source</c>.</summary>
    public const string ContentResolutions = "quilt4net.content.resolutions";

    /// <summary>Duration of a content resolution in milliseconds, tagged by <c>source</c>.</summary>
    public const string ContentResolutionDuration = "quilt4net.content.resolution.duration";

    /// <summary>Number of content keys currently held off by the failure back-off.</summary>
    public const string ContentBackoffKeys = "quilt4net.content.backoff.keys";

    /// <summary>Count of configuration resolutions, tagged by <c>source</c> and <c>key</c>.</summary>
    public const string ConfigurationResolutions = "quilt4net.configuration.resolutions";

    /// <summary>Duration of a configuration resolution in milliseconds, tagged by <c>source</c> and <c>key</c>.</summary>
    public const string ConfigurationResolutionDuration = "quilt4net.configuration.resolution.duration";

    /// <summary>Number of configuration keys currently held off by the failure back-off.</summary>
    public const string ConfigurationBackoffKeys = "quilt4net.configuration.backoff.keys";
}
