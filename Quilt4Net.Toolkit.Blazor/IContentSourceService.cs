namespace Quilt4Net.Toolkit.Blazor;

/// <summary>
/// Toggles the content source overlay: when enabled, content components annotate each rendered
/// value with where it came from — server, cache, stale cache or fallback default.
/// </summary>
/// <remarks>
/// Distinct from <c>ILanguageStateService.DeveloperMode</c>, which the language selector labels
/// "Debug mode" — that one swaps in the developer pseudo-language so every key renders as a
/// placeholder. This one leaves the rendered text alone and only annotates its provenance.
/// </remarks>
public interface IContentSourceService
{
    /// <summary>Raised when <see cref="Enabled"/> changes, so rendered components can restyle.</summary>
    event EventHandler<SourceModeEventArgs> SourceModeEvent;

    /// <summary>Whether the source overlay is currently shown.</summary>
    bool Enabled { get; set; }
}
