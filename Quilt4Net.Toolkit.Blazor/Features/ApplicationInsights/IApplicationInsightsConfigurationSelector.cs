using Quilt4Net.Toolkit.Features.ApplicationInsights;

namespace Quilt4Net.Toolkit.Blazor.Features.ApplicationInsights;

/// <summary>
/// Holds the currently selected Application Insights configuration for a Blazor circuit.
/// LogView, MetricsView, and VersionMatrixDisplay all consume this in remote mode so they pick
/// the same configuration when rendered side-by-side on one page. Selection persists in
/// localStorage by default — pages remember the operator's choice across reloads under a shared
/// key (<c>Quilt4Net.Monitor.SelectedConfig.default</c>). Hosts that need per-team or per-tenant
/// isolation can pass an explicit <c>storageScope</c> to <see cref="LoadAsync"/> to get a
/// separate key.
/// </summary>
public interface IApplicationInsightsConfigurationSelector
{
    ApplicationInsightsConfigurationResponse Selected { get; }
    IReadOnlyList<ApplicationInsightsConfigurationResponse> Available { get; }
    bool IsLoaded { get; }

    /// <summary>
    /// Loads the available configurations and restores the previously selected one (if any) from
    /// localStorage. <paramref name="storageScope"/> picks the localStorage key suffix; pass null
    /// or empty to use the shared default scope. Idempotent — only the first call per circuit
    /// hits the network; subsequent calls return immediately.
    /// </summary>
    Task LoadAsync(string storageScope = null, CancellationToken cancellationToken = default);
    Task SelectAsync(string id);

    event Action OnChanged;
}
