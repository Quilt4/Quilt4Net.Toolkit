using System.Reflection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Quilt4Net.Toolkit.Api;

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
            .Where(m => m.DeclaringType == typeof(HealthController) && !m.IsSpecialName);

        foreach (var method in methods)
        {
            var information = GetInformation(method);

            swaggerDoc.Paths.Add($"{_options.Pattern}{_options.ControllerName}/{method.Name.ToLower()}",
                new OpenApiPathItem
                {
                    Operations = new Dictionary<OperationType, OpenApiOperation>
                    {
                        [OperationType.Get] = new()
                        {
                            Summary = information.Summary,
                            Description = information.Description,
                            Responses = information.Responses,
                            Tags = [new OpenApiTag { Name = "Health" }]
                        }
                    },
                });
        }
    }

    private static (string Summary, string Description, OpenApiResponses Responses) GetInformation(MethodInfo method)
    {
        var responses = new OpenApiResponses
        {
            ["200"] = new() { Description = "Success" },
            //["503"] = new() { Description = "Service Unavailable" }
        };

        switch (method.Name)
        {
            case nameof(HealthController.Live):
                return ("Liveness", "Checks if the service is running.", responses);
            //case nameof(HealthController.Ready):
            //    summary = "Readiness";
            //    description = "Checks if the service is ready for traffic.";
            //    break;
            case nameof(HealthController.Health):
                return (
                    "Health",
                    "Comprehensive health check of the service and dependencies.",
                    new OpenApiResponses
                    {
                        ["200"] = new() { Description = "Success" },
                        ["503"] = new() { Description = "Service Unavailable" }
                    });
            //case nameof(HealthController.Startup):
            //    summary = "Startup";
            //    description = "Indicates whether the service has started successfully.";
            //    break;
            //case nameof(HealthController.Metrics):
            //    summary = "Metrics";
            //    description = "Provides performance and system metrics.";
            //    break;
            //case nameof(HealthController.Version):
            //    summary = "Version";
            //    description = "Shows the application version.";
            //    break;
            //case nameof(HealthController.Dependencies):
            //    summary = "Dependencies";
            //    description = "Lists the health of critical dependencies.";
            //    break;
            default:
                return (null, null, responses);
        }
    }
}