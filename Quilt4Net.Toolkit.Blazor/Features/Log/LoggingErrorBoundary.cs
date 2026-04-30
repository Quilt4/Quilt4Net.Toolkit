using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace Quilt4Net.Toolkit.Blazor.Features.Log;

/// <summary>
/// <see cref="ErrorBoundary"/> that mints an <see cref="Quilt4Net.Toolkit.Features.Diagnostics.IncidentId"/>
/// once per caught exception and writes a structured error log entry. The id is exposed via
/// <see cref="IncidentId"/> so the rendered error message can include it.
/// </summary>
public class LoggingErrorBoundary : ErrorBoundary
{
    [Inject]
    public ILogger<LoggingErrorBoundary> Logger { get; set; } = default!;

    public string IncidentId { get; private set; }

    protected override Task OnErrorAsync(Exception exception)
    {
        IncidentId = Quilt4Net.Toolkit.Features.Diagnostics.IncidentId.New();
        Logger.LogError(exception,
            "AI call failed. Incident={IncidentId} Component=LogView",
            IncidentId);
        return base.OnErrorAsync(exception);
    }
}
