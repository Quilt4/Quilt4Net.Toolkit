using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Quilt4Net.Toolkit.Api;

public class HealthController : ControllerBase
{
    public IActionResult Live()
    {
        return Ok(new { status = "alive" });
    }
}

public record Quilt4NetApiOptions
{
    public bool ShowInSwagger { get; set; } = true;
    public string Pattern { get; set; } = "api";
    public string ControllerName { get; set; } = "health";
}

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
            services.AddSwaggerGen(c =>
            {
                c.DocumentFilter<Quilt4NetControllerFilter>();
            });
        }

        services.AddSingleton<IActionDescriptorProvider, CustomRouteDescriptorProvider>();
    }

    public static void UseQuilt4Net(this WebApplication app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                name: "Quilt4NetRoute",
                pattern: $"{Options.Pattern}{Options.ControllerName}/live",
                defaults: new { controller = Options.ControllerName, action = "live" }
            );
        });
    }
}

internal class Quilt4NetControllerFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Paths.Add($"{Quilt4NetRegistration.Options.Pattern}{Quilt4NetRegistration.Options.ControllerName}/live", new OpenApiPathItem
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [OperationType.Get] = new()
                {
                    Summary = "Liveness",
                    Description = "Checks if the service is running.",
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new() { Description = "Success" }
                    },
                    Tags = new[] { new OpenApiTag { Name = "Health" } }
                }
            },
        });
    }
}

internal class CustomRouteDescriptorProvider : IActionDescriptorProvider
{
    public int Order => -1000; // Ensure it runs early

    public void OnProvidersExecuted(ActionDescriptorProviderContext context)
    {
        foreach (var descriptor in context.Results)
        {
            if (descriptor.RouteValues["controller"] == "Health")
            {
                descriptor.RouteValues["controller"] = Quilt4NetRegistration.Options.ControllerName; // Replace route value
            }
        }
    }

    public void OnProvidersExecuting(ActionDescriptorProviderContext context) { }
}