using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quilt4Net.Toolkit.Api.Features.Health;
using Quilt4Net.Toolkit.Api.Features.Live;

namespace Quilt4Net.Toolkit.Api;

public static class Quilt4NetRegistration
{
    private static Quilt4NetApiOptions _options;

    public static void AddQuilt4Net(this WebApplicationBuilder builder, Action<Quilt4NetApiOptions> options = default)
    {
        AddQuilt4Net(builder.Services, builder.Configuration, options);
    }

    public static void AddQuilt4Net(this IServiceCollection services, IConfiguration configuration = default, Action<Quilt4NetApiOptions> options = default)
    {
        _options = BuildOptions(configuration, options);
        services.AddSingleton(_ => _options);

        if (_options.ShowInSwagger)
        {
            services.AddSwaggerGen(c => { c.DocumentFilter<Quilt4NetControllerFilter>(); });
        }

        services.AddSingleton<IActionDescriptorProvider, CustomRouteDescriptorProvider>();

        services.AddTransient<ILiveService, LiveService>();
        services.AddTransient<IHealthService, HealthService>();
    }

    private static Quilt4NetApiOptions BuildOptions(IConfiguration configuration, Action<Quilt4NetApiOptions> options)
    {
        var o = configuration?.GetSection("Quilt4Net").Get<Quilt4NetApiOptions>() ?? new Quilt4NetApiOptions();
        options?.Invoke(o);

        if (!o.Pattern.EndsWith('/')) o.Pattern = $"{o.Pattern}/";
        if (!o.Pattern.StartsWith('/')) o.Pattern = $"/{o.Pattern}";

        return o;
    }

    public static void UseQuilt4Net(this WebApplication app)
    {
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            var methods = typeof(HealthController).GetMethods()
                .Where(m => m.DeclaringType == typeof(HealthController) && !m.IsSpecialName);

            foreach (var method in methods)
            {
                var routeName = method.Name.ToLower();
                endpoints.MapControllerRoute(
                    name: $"Quilt4Net{routeName}Route",
                    pattern: $"{_options.Pattern}{_options.ControllerName}/{routeName}",
                    defaults: new { controller = _options.ControllerName, action = method.Name }
                );
            }
        });
    }
}