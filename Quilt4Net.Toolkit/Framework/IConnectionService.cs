namespace Quilt4Net.Toolkit.Framework;

public interface IConnectionService
{
    Task<(bool Success, string Message, Uri Address)> CanConnectAsync(Service service);
}