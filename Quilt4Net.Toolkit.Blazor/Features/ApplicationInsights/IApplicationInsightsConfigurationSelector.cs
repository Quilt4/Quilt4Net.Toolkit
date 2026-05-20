using Quilt4Net.Toolkit.Features.ApplicationInsights;

namespace Quilt4Net.Toolkit.Blazor.Features.ApplicationInsights;

/// <summary>
/// Holds the currently selected Application Insights configuration for a Blazor circuit.
/// LogView and VersionMatrixDisplay both consume this in remote mode so they pick the
/// same configuration when rendered side-by-side on one page. Selection optionally
/// persists in localStorage when a storage scope is supplied.
/// </summary>
public interface IApplicationInsightsConfigurationSelector
{
    ApplicationInsightsConfigurationResponse Selected { get; }
    IReadOnlyList<ApplicationInsightsConfigurationResponse> Available { get; }
    bool IsLoaded { get; }

    Task LoadAsync(string storageScope = null, CancellationToken cancellationToken = default);
    Task SelectAsync(string id);

    event Action OnChanged;
}
