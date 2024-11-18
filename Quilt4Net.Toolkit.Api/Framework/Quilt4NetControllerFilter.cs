using System.Reflection;
using Microsoft.OpenApi.Models;
using Quilt4Net.Toolkit.Api.Features.Health;
using Quilt4Net.Toolkit.Api.Features.Live;
using Quilt4Net.Toolkit.Api.Features.Ready;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Quilt4Net.Toolkit.Api.Framework;

internal class Quilt4NetControllerFilter : IDocumentFilter
{
    private readonly Quilt4NetApiOptions _options;

    public Quilt4NetControllerFilter(Quilt4NetApiOptions options)
    {
        _options = options;
    }

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var methods = typeof(HealthController).GetMethods()
            .Where(m => m.DeclaringType == typeof(HealthController) && !m.IsSpecialName)
            .ToArray();

        //NOTE: Also add the default endpoint
        var defaultMethd = methods.FirstOrDefault(x => x.Name.Equals(_options.DefaultAction, StringComparison.InvariantCultureIgnoreCase));
        if (defaultMethd != null)
        {
            swaggerDoc.Paths.Add($"{_options.Pattern}{_options.ControllerName}", BuildOpenApiPathItem(defaultMethd));
        }

        foreach (var method in methods)
        {
            swaggerDoc.Paths.Add($"{_options.Pattern}{_options.ControllerName}/{method.Name.ToLower()}", BuildOpenApiPathItem(method));
        }
    }

    private OpenApiPathItem BuildOpenApiPathItem(MethodInfo method)
    {
        var information = GetInformation(method);

        return new OpenApiPathItem
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [OperationType.Get] = new()
                {
                    Summary = information.Summary,
                    Description = information.Description,
                    Responses = information.Responses,
                    Tags = [new OpenApiTag { Name = _options.ControllerName }]
                },
                [OperationType.Head] = new()
                {
                    Summary = information.Summary,
                    Description = information.Description,
                    Responses = information.Responses,
                    Tags = [new OpenApiTag { Name = _options.ControllerName }]
                }
            },
        };
    }

    private static (string Summary, string Description, OpenApiResponses Responses) GetInformation(MethodInfo method)
    {
        switch (method.Name)
        {
            case nameof(HealthController.Live):
                return ("Liveness",
                    $"Checks if the service is running.\n\nStatus values\n- {string.Join("\n- ", Enum.GetValues<LiveStatus>())}",
                    new OpenApiResponses
                    {
                        ["200"] = new() { Description = "Success" },
                    });
            case nameof(HealthController.Ready):
                return ("Readiness",
                    $"Checks if the service is ready for traffic.\n\nStatus values\n- {string.Join("\n- ", Enum.GetValues<ReadyStatus>())}",
                    new OpenApiResponses
                    {
                        ["200"] = new() { Description = "Success" },
                        ["503"] = new() { Description = "Service Unavailable" }
                    });
            case nameof(HealthController.Health):
                return ("Health",
                    $"Comprehensive health check of the service and dependencies.\n\nStatus values\n- {string.Join("\n- ", Enum.GetValues<HealthStatus>())}",
                    new OpenApiResponses
                    {
                        ["200"] = new() { Description = "Success" },
                        ["503"] = new() { Description = "Service Unavailable" }
                    });
            //case nameof(HealthController.Startup):
            //    summary = "Startup";
            //    description = "Indicates whether the service has started successfully.";
            //    break;
            case nameof(HealthController.Metrics):
                return ("Metrics",
                    "Provides performance and system metrics.",
                    new OpenApiResponses
                    {
                        ["200"] = new() { Description = "Success" }
                    });
            case nameof(HealthController.Version):
                return ("Version",
                    "Shows the application version and environment information.",
                    new OpenApiResponses
                    {
                        ["200"] = new() { Description = "Success" }
                    });
            //case nameof(HealthController.Dependencies):
            //    summary = "Dependencies";
            //    description = "Lists the health of critical dependencies.";
            //    break;
            default:
                return (null, null, new OpenApiResponses { ["200"] = new() { Description = "Success" } });
        }
    }
}