namespace Quilt4Net.Toolkit.Api;

public static class RemoteConfigurationRegistration
{
    [Obsolete($"Use {nameof(AddQuilt4NetRemoteConfiguration)} with {nameof(IServiceCollection)} instead.")]
    public static void AddQuilt4NetRemoteConfiguration(this WebApplicationBuilder builder, Action<RemoteConfigurationOptions> options = null)
    {
        builder.Services.AddQuilt4NetRemoteConfiguration(options);
    }
}