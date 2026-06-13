using Blazored.LocalStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Quilt4Net.Toolkit.Blazor.Framework;

namespace Quilt4Net.Toolkit.Blazor;

public static class Quilt4ContentRegistration
{
    /// <summary>
    /// Register blazor usages of content from Quilt4Net.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static IServiceCollection AddQuilt4NetBlazorContent(this IHostApplicationBuilder builder, Action<ContentOptions> options = null)
    {
        return builder.Services.AddQuilt4NetBlazorContent(builder.Configuration, options);
    }

    /// <summary>
    /// Register blazor usages of content from Quilt4Net.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static IServiceCollection AddQuilt4NetBlazorContent(this IServiceCollection services, IConfiguration configuration, Action<ContentOptions> options = null)
    {
        services.AddScoped<IEditContentService, EditContentService>();
        services.AddScoped<ILanguageStateService, LanguageStateService>();
        services.AddScoped<IQuilt4ContentService, Quilt4ContentService>();
        services.AddScoped<IContentAdminService, ContentAdminService>();
        // Content-aware Radzen DialogService / NotificationService wrappers. Both depend on
        // the already-registered Radzen services (host registers Radzen via AddRadzen* or
        // AddScoped<DialogService>() / AddScoped<NotificationService>()).
        services.AddScoped<IQuilt4DialogService, Quilt4DialogService>();
        services.AddScoped<IQuilt4NotificationService, Quilt4NotificationService>();
        // Default adapter forwards to Tharga.Blazor's BreadCrumbService when registered, silently
        // no-ops otherwise. TryAdd so a host can swap in their own adapter ahead of this call.
        services.TryAddScoped<Features.Content.Pages.IPageBreadcrumbAdapter, Features.Content.Pages.TharBlazorBreadcrumbAdapter>();
        // TryAdd so AddQuilt4NetBlazor*-AI registrations can register it too without conflict.
        services.TryAddScoped<IBrowserTimeZoneAccessor, BrowserTimeZoneAccessor>();
        services.AddBlazoredLocalStorage();
        services.AddQuilt4NetContent(configuration, options);

        // Startup warm-up: bulk-load the default language into the (singleton) content cache so the
        // first render avoids per-key fan-out. Honors ContentOptions.WarmUpEnabled; selected-language
        // warming is handled per-circuit by LanguageStateService.
        services.AddHostedService<ContentWarmupHostedService>();

        return services;
    }
}