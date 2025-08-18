namespace Quilt4Net.Toolkit;

public record Quilt4NetServerOptions
{
    public string ApiKey { get; set; }
    public string Address { get; set; } = "https://quilt4net.com/";
    public TimeSpan? Ttl { get; set; }
    //public Func<IServiceProvider, string> InstanceLoader { get; set; } = _ => null;
}