namespace Quilt4Net.Toolkit.Framework;

public interface IConnectionService
{
    Task<ConnectionResult> CanConnectAsync(Service service);
}

public record ConnectionResult
{
    public bool Success { get; init; }
    public string Message { get; init; }
    public Uri Address { get; init; }
    public WhoAmIResponse Capabilities { get; init; }
}