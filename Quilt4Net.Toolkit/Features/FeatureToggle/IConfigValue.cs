namespace Quilt4Net.Toolkit.Features.FeatureToggle;

public interface IConfigValue
{
    public string Key { get; }
    public string Application { get; }
    public string Environment { get; }
    public string Instance { get; }
    public string Value { get; }
}