namespace Quilt4Net.Toolkit.Features.FeatureToggle;

public interface ILanguageKeyContext
{
    string Key { get; }
    Guid LanguageKey { get; }
    string Application { get; }
    string Environment { get; }
    string Instance { get; }
}