namespace Quilt4Net.Toolkit.Api.Features.FeatureToggle;

public interface IKeyContext
{
    string Key { get; }
    string Application { get; }
    string Environment { get; }
    string Instance { get; }
    string Version { get; }
}