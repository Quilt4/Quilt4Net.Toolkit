namespace Quilt4Net.Toolkit.Api;

public record FeatureToggle
{
    public string ApiKey { get; set; }
    public string Address { get; set; }
    public TimeSpan? Ttl { get; set; } = TimeSpan.FromMinutes(10);
    public Func<IServiceProvider, string> InstanceLoader { get; set; } = _ => null;
}