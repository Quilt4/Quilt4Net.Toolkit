using System.Reflection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Quilt4Net.Toolkit.Api;

internal class Quilt4NetControllerFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var methods = typeof(HealthController).GetMethods()
            .Where(m => m.DeclaringType == typeof(HealthController) && !m.IsSpecialName);

        foreach (var method in methods)
        {
            var information = GetInformation(method);

            swaggerDoc.Paths.Add($"{Quilt4NetRegistration.Options.Pattern}{Quilt4NetRegistration.Options.ControllerName}/{method.Name.ToLower()}", new OpenApiPathItem
            {
                Operations = new Dictionary<OperationType, OpenApiOperation>
                {
                    [OperationType.Get] = new()
                    {
                        Summary = information.Summary,
                        Description = information.Description,
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

    private static (string Summary, string Description) GetInformation(MethodInfo method)
    {
        switch (method.Name)
        {
            case nameof(HealthController.Live):
                return ("Liveness", "Checks if the service is running.");
            //case nameof(HealthController.Ready):
            //    summary = "Readiness";
            //    description = "Checks if the service is ready for traffic.";
            //    break;
            //case nameof(HealthController.Health):
            //    summary = "Health";
            //    description = "Comprehensive health check of the service and dependencies.";
            //    break;
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
                return (null, null);
        }
    }
}