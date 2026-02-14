namespace Quilt4Net.Toolkit.Features.Api;

public record HeartbeatOptions
{
    public bool Enabled { get; set; } = false;
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(5);
    public string ConnectionString { get; set; }
}