namespace Quilt4Net.Toolkit.Api;

public static class ApplicationInsightsRegistration
{
    [Obsolete($"Use {nameof(AddQuilt4NetApplicationInsightsClient)} with {nameof(IServiceCollection)} instead")]
    public static void AddQuilt4NetApplicationInsightsClient(this WebApplicationBuilder builder, Action<ApplicationInsightsOptions> options = null)
    {
        builder.Services.AddQuilt4NetApplicationInsightsClient(options);
    }
}