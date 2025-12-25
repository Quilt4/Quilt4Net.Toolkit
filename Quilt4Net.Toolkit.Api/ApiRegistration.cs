using Quilt4Net.Toolkit.Features.Api;

namespace Quilt4Net.Toolkit.Api;

public static class ApiRegistration
{
    [Obsolete($"Use {nameof(Health.HealthRegistration.AddQuilt4NetHealthApi)} instead.")]
    public static void AddQuilt4NetApi(this WebApplicationBuilder builder, Action<Quilt4NetHealthApiOptions> options = null)
    {
        Health.HealthRegistration.AddQuilt4NetHealthApi(builder.Services, options);
    }

    [Obsolete($"Use {nameof(Health.HealthRegistration.AddQuilt4NetHealthApi)} instead.")]
    public static void AddQuilt4NetApi(this IServiceCollection services, Action<Quilt4NetHealthApiOptions> options = null)
    {
        Health.HealthRegistration.AddQuilt4NetHealthApi(services, options);
    }

    [Obsolete($"Use {nameof(Health.HealthRegistration.UseQuilt4NetHealthApi)} instead.")]
    public static void UseQuilt4NetApi(this WebApplication app)
    {
        Health.HealthRegistration.UseQuilt4NetHealthApi(app);
    }
}