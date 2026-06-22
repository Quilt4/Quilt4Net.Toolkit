using System.Net.Http.Json;
using System.Net.Security;
using Microsoft.Extensions.Logging;
using Quilt4Net.Toolkit.Features.Api;
using Quilt4Net.Toolkit.Framework;

namespace Quilt4Net.Toolkit.Features.Health.Dependency;

internal class DefaultDependencyProbe : IDependencyProbe
{
    private const string ProbeComponentName = "Probe";
    private const string Parameters = "Health?noDependencies=true&noCertSelfCheck=true";

    private readonly Quilt4NetHealthApiOptions _apiOptions;
    private readonly ILogger<DefaultDependencyProbe> _logger;

    /// <summary>
    /// Test seam to override the HTTP transport. When null the real <see cref="HttpClientHandler"/>
    /// (with the certificate-observing validation callback) is used.
    /// </summary>
    internal Func<HttpMessageHandler> HandlerFactory { get; set; }

    public DefaultDependencyProbe(Quilt4NetHealthApiOptions apiOptions, ILogger<DefaultDependencyProbe> logger)
    {
        _apiOptions = apiOptions;
        _logger = logger;
    }

    public async Task<HealthResponse> ProbeAsync(Dependency dependency, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(dependency.Uri, Parameters, out var uri))
        {
            _logger.LogWarning("Dependency '{DependencyName}' has an invalid uri '{Uri}'.", dependency.Name, dependency.Uri);
            return BuildFailed(HealthStatus.Unhealthy, new Dictionary<string, string> { { "Message", $"Cannot build uri from '{dependency.Uri}' and '{Parameters}'." } });
        }

        var certificateStatus = HealthStatus.Healthy;
        string certificateMessage = null;

        try
        {
            HttpMessageHandler handler;
            if (HandlerFactory != null)
            {
                handler = HandlerFactory();
            }
            else
            {
                var httpClientHandler = new HttpClientHandler();
                httpClientHandler.ServerCertificateCustomValidationCallback = (_, _, _, errors) =>
                {
                    if (errors != SslPolicyErrors.None)
                    {
                        certificateStatus = HealthStatus.Degraded;
                        certificateMessage = $"{errors}";
                    }

                    return true;
                };
                handler = httpClientHandler;
            }

            using var httpClient = new HttpClient(handler);
            using var response = await httpClient.GetAsync(uri, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Dependency '{DependencyName}' probe to '{Uri}' returned non-success status {StatusCode} ({ReasonPhrase}).", dependency.Name, uri, (int)response.StatusCode, response.ReasonPhrase);
                return BuildFailed(HealthStatus.Unhealthy, new Dictionary<string, string>
                {
                    { "StatusCode", $"{(int)response.StatusCode}" },
                    { "Reason", response.ReasonPhrase ?? $"{response.StatusCode}" }
                });
            }

            var content = await response.Content.ReadFromJsonAsync<HealthResponse>(cancellationToken);
            if (content == null)
            {
                _logger.LogWarning("Dependency '{DependencyName}' probe to '{Uri}' returned an empty body.", dependency.Name, uri);
                return BuildFailed(HealthStatus.Degraded, new Dictionary<string, string> { { "Message", "Dependency returned an empty body." } });
            }

            return await CheckCertificateAsync(dependency, certificateStatus, certificateMessage, content);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Dependency '{DependencyName}' probe to '{Uri}' failed.", dependency.Name, uri);
            return BuildFailed(HealthStatus.Unhealthy, new Dictionary<string, string> { { "Message", e.Message } });
        }
    }

    private static HealthResponse BuildFailed(HealthStatus status, Dictionary<string, string> details) => new()
    {
        Status = status,
        Components = new Dictionary<string, HealthComponent>
        {
            { ProbeComponentName, new HealthComponent { Status = status, Details = details } }
        }
    };

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
}
