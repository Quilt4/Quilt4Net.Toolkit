using Quilt4Net.Toolkit.Features.Content;

namespace Quilt4Net.Toolkit.Blazor;

public interface ILanguageStateService
{
    event EventHandler<LanguageLoadedEventArgs> LanguageLoadedEvent;
    event EventHandler<LanguageChangedEventArgs> LanguageChangedEvent;
    event EventHandler<DeveloperModeEventArgs> DeveloperModeEvent;
    Language Selected { get; set; }
    Language[] Languages { get; set; }
    bool DeveloperMode { get; set; }
    Task<Language[]> ReloadAsync();

    /// <summary>
    /// Select the language whose <see cref="Language.Code"/> matches <paramref name="code"/>
    /// (case-insensitive). Returns <c>false</c> and leaves <see cref="Selected"/> untouched when the
    /// team has no language with that code.
    /// </summary>
    /// <remarks>
    /// The point of the code (issue #144): a host holding its own language identity — a team's
    /// working language stored as <c>"sv"</c> — can follow it without matching on a display name or
    /// hardcoding a per-tenant <see cref="Language.Key"/>. A miss is an ordinary outcome, not an
    /// error: a team simply may not have the language the host asked for, and the caller decides
    /// whether to fall back or offer to add it.
    /// </remarks>
    /// <remarks>
    /// A <b>default interface member</b>, so a host with its own <see cref="ILanguageStateService"/>
    /// keeps compiling — adding an abstract member to a shipped public interface would break every
    /// implementor, including test stubs, for a convenience method.
    /// </remarks>
    bool SelectByCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;

        // Languages with no code are skipped rather than compared: a null Code means "the server
        // could not determine one", and matching those on anything would reintroduce the guessing
        // this field exists to remove.
        var match = Languages?.FirstOrDefault(x =>
            !string.IsNullOrWhiteSpace(x?.Code) && string.Equals(x.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));

        if (match == null) return false;
        Selected = match;
        return true;
    }

    /// <summary>
    /// Select the language whose <see cref="Language.Name"/> matches <paramref name="name"/>
    /// (case-insensitive). Returns <c>false</c> and leaves <see cref="Selected"/> untouched on a miss.
    /// </summary>
    /// <remarks>
    /// Prefer <see cref="SelectByCode"/>. The display name is what a code exists to stop hosts
    /// matching on — it can be renamed or localised out from under the caller. This overload is here
    /// for a host that genuinely has only a name to work from.
    /// </remarks>
    bool SelectByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        var match = Languages?.FirstOrDefault(x =>
            !string.IsNullOrWhiteSpace(x?.Name) && string.Equals(x.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));

        // A miss must leave the current selection alone — swapping the user's language because a
        // host asked for one the team does not have is worse than doing nothing.
        if (match == null) return false;
        Selected = match;
        return true;
    }
}