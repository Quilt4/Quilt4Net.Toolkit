namespace Quilt4Net.Toolkit.Api;

public static class HealthRegistration
{
    [Obsolete($"Use {nameof(AddQuilt4NetHealthClient)} with {nameof(IServiceCollection)} instead.")]
    public static void AddQuilt4NetHealthClient(this WebApplicationBuilder builder, Action<HealthClientOptions> options = null)
    {
        builder.Services.AddQuilt4NetHealthClient(options);
    }
}