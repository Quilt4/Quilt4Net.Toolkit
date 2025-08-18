namespace Quilt4Net.Toolkit;

public record Quilt4NetServerOptions
{
    public string ApiKey { get; set; }
    public string Address { get; set; }
    public TimeSpan? Ttl { get; set; }
    public Func<IServiceProvider, string> InstanceLoader { get; set; } = _ => null;
}

//public record RemoteConfigurationOptions
//{
//    public FeatureToggle FeatureToggle { get; set; } = new();
//}

public record Quilt4NetHealthOptions
{
    /// <summary>
    /// Address to the health API.
    /// </summary>
    public Uri HealthAddress { get; set; }
}

//public record FeatureToggle
//{
//    public string ApiKey { get; set; }
//    public string Address { get; set; }
//    public TimeSpan? Ttl { get; set; } = TimeSpan.FromMinutes(10);
//    public Func<IServiceProvider, string> InstanceLoader { get; set; } = _ => null;
//}