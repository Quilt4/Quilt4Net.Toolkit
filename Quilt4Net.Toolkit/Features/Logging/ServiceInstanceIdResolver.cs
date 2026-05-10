namespace Quilt4Net.Toolkit.Features.Logging;

/// <summary>
/// Resolves the <c>service.instance.id</c> OpenTelemetry resource attribute from a
/// precedence chain so multi-deployment binaries (same csproj, multiple branded /
/// tenanted hosts) can be told apart in telemetry without inventing per-deployment
/// resource keys.
///
/// Order of precedence:
/// <list type="number">
/// <item>The explicit <c>ServiceInstanceId</c> option value passed to <c>AddQuilt4NetLogging</c>.</item>
/// <item>The OTel-standard <c>OTEL_RESOURCE_ATTRIBUTES</c> env var, parsed for <c>service.instance.id=...</c>.</item>
/// <item>The Quilt4Net shorthand <c>QUILT4NET_SERVICE_INSTANCE_ID</c> env var.</item>
/// <item>Null — caller falls back to existing behaviour (e.g. MachineName for the OTel
///       resource, no per-record attribute).</item>
/// </list>
/// </summary>
internal static class ServiceInstanceIdResolver
{
    internal const string OtelResourceAttributesEnvVar = "OTEL_RESOURCE_ATTRIBUTES";
    internal const string Quilt4NetEnvVar = "QUILT4NET_SERVICE_INSTANCE_ID";
    internal const string OtelKey = "service.instance.id";

    public static string Resolve(string optionValue, Func<string, string> envReader = null)
    {
        envReader ??= System.Environment.GetEnvironmentVariable;

        if (!string.IsNullOrWhiteSpace(optionValue)) return optionValue.Trim();

        var fromOtel = ParseFromOtelResourceAttributes(envReader(OtelResourceAttributesEnvVar));
        if (!string.IsNullOrWhiteSpace(fromOtel)) return fromOtel;

        var fromShorthand = envReader(Quilt4NetEnvVar);
        if (!string.IsNullOrWhiteSpace(fromShorthand)) return fromShorthand.Trim();

        return null;
    }

    /// <summary>
    /// OTEL_RESOURCE_ATTRIBUTES is comma-separated key=value pairs per the OTel spec
    /// (https://opentelemetry.io/docs/specs/otel/configuration/sdk-environment-variables/#general-sdk-configuration).
    /// We only need the <c>service.instance.id</c> entry. URL-decoding is not applied —
    /// values containing reserved characters should use the option lambda or the
    /// QUILT4NET shorthand env var instead.
    /// </summary>
    private static string ParseFromOtelResourceAttributes(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        foreach (var rawPair in raw.Split(','))
        {
            var pair = rawPair.Trim();
            var eq = pair.IndexOf('=');
            if (eq <= 0 || eq == pair.Length - 1) continue;

            var key = pair.Substring(0, eq).Trim();
            if (!key.Equals(OtelKey, StringComparison.OrdinalIgnoreCase)) continue;

            var value = pair.Substring(eq + 1).Trim();
            return string.IsNullOrEmpty(value) ? null : value;
        }

        return null;
    }
}
