namespace Quilt4Net.Toolkit.Features.FeatureToggle;

public interface ILanguageKeyContext
{
    string Key { get; }
    string Language { get; }
    string Application { get; }
    string Environment { get; }
    string Instance { get; }
}