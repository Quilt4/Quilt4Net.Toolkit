using Blazored.LocalStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
        services.AddBlazoredLocalStorage();
        services.AddQuilt4NetContent(configuration, options);

        return services;
    }
}