namespace Quilt4Net.Toolkit.Api;

public static class HealthRegistration
{
    public static void AddQuilt4NetHealthClient(this WebApplicationBuilder builder, Action<HealthOptions> options = null)
    {
        builder.Services.AddQuilt4NetHealthClient(options);
    }
}