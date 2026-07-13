namespace Quilt4Net.Toolkit.Blazor;

/// <summary>
/// A menu item for <see cref="Quilt4SplitButton"/>. The label is content-localised from
/// <see cref="TextKey"/> with <see cref="DefaultText"/> as the fallback — the same
/// resolution <c>Quilt4Button</c> and <c>Quilt4Text</c> use. <see cref="Value"/> is handed
/// back to the split button's item-click handler to identify which item was chosen.
/// </summary>
public sealed class Quilt4SplitButtonItem
{
    /// <summary>Content key for the item label.</summary>
    public string TextKey { get; init; }

    /// <summary>Fallback label used when <see cref="TextKey"/> is unset or the lookup misses.</summary>
    public string DefaultText { get; init; }

    /// <summary>Opaque value passed to the split button's item-click handler when this item is chosen.</summary>
    public string Value { get; init; }

    /// <summary>Optional Radzen icon name shown before the label.</summary>
    public string Icon { get; init; }

    /// <summary>When <c>true</c> the item renders disabled and cannot be chosen.</summary>
    public bool Disabled { get; init; }

    /// <summary>
    /// Optional exact translations for this item's label, keyed by <b>language name</b> (e.g.
    /// <c>{ ["Swedish"] = "Ta bort" }</c>). Applied only the first time the key is created on the
    /// server: stored as authoritative for the matching language, AI skipped (issue #141). Optional.
    /// </summary>
    public IReadOnlyDictionary<string, string> Translations { get; init; }
}
