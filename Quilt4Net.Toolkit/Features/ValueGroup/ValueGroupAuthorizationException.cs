namespace Quilt4Net.Toolkit.Features.ValueGroup;

/// <summary>
/// Thrown by <see cref="IValueGroupClient.GetAsync"/> when the server returns 401 or 403.
/// Differs from <see cref="Features.FeatureToggle.IRemoteConfigurationService"/>'s silent-fallback
/// behaviour by design: a revoked agent must learn it has been revoked rather than continue
/// serving cached secret-bearing data.
/// </summary>
public class ValueGroupAuthorizationException : Exception
{
    public ValueGroupAuthorizationException(string message) : base(message) { }
    public ValueGroupAuthorizationException(string message, Exception innerException) : base(message, innerException) { }
}
