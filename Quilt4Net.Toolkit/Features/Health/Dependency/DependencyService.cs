using System.Net.Http.Json;
using System.Net.Security;
using System.Runtime.CompilerServices;
using Quilt4Net.Toolkit.Features.Api;
using Quilt4Net.Toolkit.Framework;

namespace Quilt4Net.Toolkit.Features.Health.Dependency;

internal class DependencyService : IDependencyService
{
    private readonly Quilt4NetHealthApiOptions _apiOptions;

    public DependencyService(Quilt4NetHealthApiOptions apiOptions)
    {
        _apiOptions = apiOptions;
    }

    public async IAsyncEnumerable<KeyValuePair<string, DependencyComponent>> GetStatusAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var tasks = _apiOptions.DependencyRegistrations.Select(x => Task.Run<(string Name, bool Essential, Dictionary<string, HealthComponent> Components, Uri Uri)>(async () =>
        {
            var handler = new HttpClientHandler();

            var certificateStatus = HealthStatus.Healthy;
            string message = null;
            handler.ServerCertificateCustomValidationCallback = (_, _, _, errors) =>
            {
                if (errors != SslPolicyErrors.None)
                {
                    certificateStatus = HealthStatus.Degraded;
                    message = $"{errors}";
                }

                return true;
            };

            using var httpClient = new HttpClient(handler);
            var parameters = "Health?noDependencies=true&noCertSelfCheck=true";
            if (!Uri.TryCreate(x.Uri, parameters, out var uri)) throw new InvalidOperationException($"Cannot build uri from '{x.Uri}' and '{parameters}'.");
            using var response = await httpClient.GetAsync(uri, cancellationToken);
            var content = await response.Content.ReadFromJsonAsync<HealthResponse>(cancellationToken);

            content = await CheckCertificateAsync(x, certificateStatus, message, content);

            return (x.Name, x.Essential, content.Components, x.Uri);
        }, cancellationToken)).ToList();

        while (tasks.Any())
        {
            var task = await Task.WhenAny(tasks);

            var dependencyComponent = new DependencyComponent
            {
                Status = BuildStatus(task),
                Uri = task.Result.Uri,
                DependencyComponents = task.Result.Components
            };
            yield return new KeyValuePair<string, DependencyComponent>(task.Result.Name, dependencyComponent);
            tasks.Remove(task);
        }
    }

    private async Task<HealthResponse> CheckCertificateAsync(Dependency x, HealthStatus certificateStatus, string message, HealthResponse content)
    {
        if (!(_apiOptions.Certificate?.DependencyCheckEnabled ?? false)) return content;

        var certificateHealth = await Certificatehelper.GetCertificateHealthAsync(x.Uri, _apiOptions.Certificate, certificateStatus, message);

        content = content with
        {
            Status = EnumExtensions.MaxEnum<HealthStatus>(certificateStatus, content.Status),
            Components = content.Components
                .Concat(new Dictionary<string, HealthComponent> { { "Certificate", certificateHealth } })
                .ToUniqueDictionary()
        };

        content.Components.Remove("CertificateSelf");

        return content;
    }

    private static HealthStatus BuildStatus(Task<(string Name, bool Essential, Dictionary<string, HealthComponent> Components, Uri _)> task)
    {
        var status = task.Result.Components.Max(x => x.Value.Status);
        if (!task.Result.Essential && status == HealthStatus.Unhealthy)
        {
            status = HealthStatus.Degraded;
        }

        return status;
    }
}