namespace Quilt4Net.Toolkit.Api;

public static class RemoteConfigurationRegistration
{
    public static void AddQuilt4NetRemoteConfiguration(this WebApplicationBuilder builder, Action<RemoteConfigurationOptions> options = null)
    {
        builder.Services.AddQuilt4NetRemoteConfiguration(options);
    }
}