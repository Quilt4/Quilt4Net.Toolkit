using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Quilt4Net.Toolkit.Api.Features.Live;

namespace Quilt4Net.Toolkit.Api;

public static class Quilt4NetRegistration
{
    private static Quilt4NetApiOptions _options;
    internal static Quilt4NetApiOptions Options => _options ?? throw new InvalidOperationException($"Register Quilt4Net.Toolkit by using {nameof(Quilt4NetRegistration)}.{nameof(AddQuilt4Net)} before starting to use it.");

    public static void AddQuilt4Net(this IServiceCollection services, Quilt4NetApiOptions options = default)
    {
        _options = options ?? new Quilt4NetApiOptions();

        if (!Options.Pattern.EndsWith('/')) Options.Pattern = $"{Options.Pattern}/";
        if (!Options.Pattern.StartsWith('/')) Options.Pattern = $"/{Options.Pattern}";

        if (Options.ShowInSwagger)
        {
            services.AddSwaggerGen(c => { c.DocumentFilter<Quilt4NetControllerFilter>(); });
        }

        services.AddSingleton<IActionDescriptorProvider, CustomRouteDescriptorProvider>();

        services.AddTransient<ILiveService, LiveService>();
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
                    pattern: $"{Options.Pattern}{Options.ControllerName}/{routeName}",
                    defaults: new { controller = Options.ControllerName, action = method.Name }
                );
            }
        });
    }
}