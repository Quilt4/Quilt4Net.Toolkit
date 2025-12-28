using Quilt4Net.Toolkit.Features.Api;

namespace Quilt4Net.Toolkit.Api;

public static class ApiRegistration
{
    [Obsolete($"Use AddQuilt4NetHealthApi in Quilt4Net.Toolkit.Health nuget package instead.")]
    public static void AddQuilt4NetApi(this WebApplicationBuilder builder, Action<Quilt4NetHealthApiOptions> options = null)
    {
        throw new NotSupportedException("Use AddQuilt4NetHealthApi in Quilt4Net.Toolkit.Health nuget package instead.");
    }

    [Obsolete($"Use AddQuilt4NetHealthApi in Quilt4Net.Toolkit.Health nuget package instead.")]
    public static void AddQuilt4NetApi(this IServiceCollection services, Action<Quilt4NetHealthApiOptions> options = null)
    {
        throw new NotSupportedException("Use AddQuilt4NetHealthApi in Quilt4Net.Toolkit.Health nuget package instead.");
    }

    [Obsolete($"Use UseQuilt4NetHealthApi in Quilt4Net.Toolkit.Health nuget package instead.")]
    public static void UseQuilt4NetApi(this WebApplication app)
    {
        throw new NotSupportedException("Use UseQuilt4NetHealthApi in Quilt4Net.Toolkit.Health nuget package instead.");
    }
}