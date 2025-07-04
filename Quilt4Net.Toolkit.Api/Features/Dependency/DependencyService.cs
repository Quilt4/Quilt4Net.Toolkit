using System.Net.Security;
using System.Runtime.CompilerServices;
using Quilt4Net.Toolkit.Features.Health;
using Quilt4Net.Toolkit.Framework;

namespace Quilt4Net.Toolkit.Api.Features.Dependency;

internal class DependencyService : IDependencyService
{
    private readonly Quilt4NetApiOptions _options;

    public DependencyService(Quilt4NetApiOptions options)
    {
        _options = options;
    }

    public async IAsyncEnumerable<KeyValuePair<string, DependencyComponent>> GetStatusAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var tasks = _options.Dependencies.Select(x => Task.Run(async () =>
        {
            var handler = new HttpClientHandler();

            var certificateStatus = HealthStatus.Healthy;
            handler.ServerCertificateCustomValidationCallback = (_, _, _, errors) =>
            {
                if (errors != SslPolicyErrors.None)
                {
                    certificateStatus = HealthStatus.Unhealthy;
                }

                return true;
            };

            using var httpClient = new HttpClient(handler);
            if (!Uri.TryCreate(x.Uri, "Health?noDependencies=true", out var uri)) throw new InvalidOperationException($"Cannot build uri from '{x.Uri}' and 'Health'.");
            using var response = await httpClient.GetAsync(uri, cancellationToken);
            var content = await response.Content.ReadFromJsonAsync<HealthResponse>(cancellationToken);

            content = await CheckCertificateAsync(x, certificateStatus, content);

            return (x.Name, x.Essential, content.Components);
        }, cancellationToken)).ToList();

        while (tasks.Any())
        {
            var task = await Task.WhenAny(tasks);

            var dependencyComponent = new DependencyComponent
            {
                Status = BuildStatus(task),
                DependencyComponents = task.Result.Components
            };
            yield return new KeyValuePair<string, DependencyComponent>(task.Result.Name, dependencyComponent);
            tasks.Remove(task);
        }
    }

    private async Task<HealthResponse> CheckCertificateAsync(Toolkit.Features.Health.Dependency x, HealthStatus certificateStatus, HealthResponse content)
    {
        if (!(_options.Certificate?.DependencyCheckEnabled ?? false)) return content;

        var certificateHealth = await Certificatehelper.GetCertificateHealthAsync(x.Uri, _options.Certificate, certificateStatus);

        content = content with
        {
            Status = EnumExtensions.MaxEnum<HealthStatus>(certificateStatus, content.Status),
            Components = content.Components
                .Concat(new Dictionary<string, HealthComponent> { { "Certificate", certificateHealth } })
                .ToUniqueDictionary()
        };

        return content;
    }

    private static HealthStatus BuildStatus(Task<(string Name, bool Essential, Dictionary<string, HealthComponent> Components)> task)
    {
        var status = task.Result.Components.Max(x => x.Value.Status);
        if (!task.Result.Essential && status == HealthStatus.Unhealthy)
        {
            status = HealthStatus.Degraded;
        }

        return status;
    }
}