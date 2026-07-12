using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Features.FeatureToggle;

public record GetContentRequest : ILanguageKeyContext
{
    public required string Key { get; init; }
    public required Guid LanguageKey { get; init; }
    public required string Application { get; init; }
    public required string Environment { get; init; }
    public required string Instance { get; init; }
    public required string DefaultValue { get; init; }
    public required ContentFormat? ContentFormat { get; init; }

    /// <summary>
    /// Optional developer-supplied exact translations for this key, keyed by <b>language name</b>
    /// (as entered on the server), e.g. <c>{ ["Swedish"] = "Ärende" }</c>. When the key is first
    /// materialized the server stores each of these verbatim as authoritative content for the
    /// matching language and skips AI translation for it; the remaining AI-enabled languages are
    /// translated as usual. A name matching no configured language is ignored. Null/empty (the
    /// default) means "no direct translations" — fully backward compatible with older servers,
    /// which simply ignore the field.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, string> Translations { get; init; }
}