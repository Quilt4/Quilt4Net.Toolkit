using Blazored.LocalStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Quilt4Net.Toolkit.Blazor;

public static class Quilt4ContentRegistration
{
    private static AddQuilt4NetOptions _options;

    public static IServiceCollection AddQuilt4Net(this IHostApplicationBuilder builder, Action<AddQuilt4NetOptions> options = null)
    {
        return builder.Services.AddQuilt4Net(options);
    }

    //public static IServiceCollection AddQuilt4Net(this IServiceCollection services, Func<IServiceProvider, string> environmentNameLoader, Action<AddQuilt4NetOptions> options = null)
    //{
    //    return AddQuilt4Net(services, null, environmentNameLoader, options);
    //}

    //public static IServiceCollection AddQuilt4Net(this IServiceCollection services, IConfiguration configuration, Func<IServiceProvider, string> environmentNameLoader, Action<AddQuilt4NetOptions> options = null)
    public static IServiceCollection AddQuilt4Net(this IServiceCollection services, Action<AddQuilt4NetOptions> options = null)
    {
        var configuration = services.BuildServiceProvider().GetService<IConfiguration>();

        _options = BuildOptions(configuration, options);
        //services.AddSingleton(_ => _options);
        services.AddSingleton(Options.Create(_options));

        services.AddScoped<IEditContentService, EditContentService>();
        services.AddScoped<ILanguageStateService, LanguageStateService>();
        services.AddBlazoredLocalStorage();
        services.AddQuilt4NetContent();

        return services;
    }

    private static AddQuilt4NetOptions BuildOptions(IConfiguration configuration, Action<AddQuilt4NetOptions> options)
    {
        //var o = configuration?.GetSection("Quilt4Net:Service").Get<AddQuilt4NetOptions>() ?? new AddQuilt4NetOptions();

        //var oRoot = configuration?.GetSection("Quilt4Net").Get<AddQuilt4NetOptions>();
        //o.ApiKey ??= oRoot?.ApiKey;
        //o.Address ??= oRoot?.Address;
        //o.Ttl ??= oRoot?.Ttl;
        //o.Application ??= oRoot?.Application;

        //options?.Invoke(o);

        //return o;

        throw new NotImplementedException();
    }
}

public record AddQuilt4NetOptions
{
    //public string ApiKey { get; set; }
    //public string Address { get; set; } = "https://quilt4net.com/";
    //public TimeSpan? Ttl { get; set; }
    //public string Application { get; set; }
    ////public Func<IServiceProvider, string> InstanceLoader { get; set; } = _ => null;
}