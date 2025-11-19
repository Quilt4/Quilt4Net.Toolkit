using Blazored.LocalStorage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Quilt4Net.Toolkit.Blazor;

public static class Quilt4ContentRegistration
{
    public static IServiceCollection AddQuilt4NetBlazorContent(this IHostApplicationBuilder builder, Action<ContentOptions> options = null)
    {
        return builder.Services.AddQuilt4NetBlazorContent(options);
    }

    public static IServiceCollection AddQuilt4NetBlazorContent(this IServiceCollection services, Action<ContentOptions> options = null)
    {
        services.AddScoped<IEditContentService, EditContentService>();
        services.AddScoped<ILanguageStateService, LanguageStateService>();
        services.AddBlazoredLocalStorage();
        services.AddQuilt4NetContent(options);

        return services;
    }
}