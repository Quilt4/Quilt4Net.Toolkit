namespace Quilt4Net.Toolkit;

public record RemoteConfigurationOptions
{
    public TimeSpan? Ttl { get; set; }
    public string Quilt4NetAddress { get; set; }
    public string ApiKey { get; set; }
}