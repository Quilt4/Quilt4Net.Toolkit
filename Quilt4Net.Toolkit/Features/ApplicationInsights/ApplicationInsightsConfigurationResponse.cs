namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

/// <summary>
/// Wire shape returned by <c>GET /Api/Monitoring/ApplicationInsights</c> on Quilt4Net.Server.
/// Implements <see cref="IApplicationInsightsContext"/> so it can be passed directly to
/// <see cref="IApplicationInsightsService"/> / <see cref="IVersionMatrixService"/> without a
/// shim record. Property names mirror the server DTO exactly so JSON round-trips one-to-one.
/// </summary>
public record ApplicationInsightsConfigurationResponse : IApplicationInsightsContext
{
    /// <summary>Server-side ObjectId, as a string. Used to identify a configuration in the in-component selector.</summary>
    public string Id { get; init; }

    /// <summary>Human-readable name shown in the selector dropdown.</summary>
    public string Name { get; init; }

    public string TenantId { get; init; }
    public string WorkspaceId { get; init; }
    public string ClientId { get; init; }
    public string ClientSecret { get; init; }

    public ApplicationInsightsAuthMode AuthMode { get; init; } = ApplicationInsightsAuthMode.ClientSecret;
}
