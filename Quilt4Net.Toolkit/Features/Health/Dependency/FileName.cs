using Quilt4Net.Toolkit.Features.Health;
using Quilt4Net.Toolkit.Framework;
using System.Net.Http.Json;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Quilt4Net.Toolkit.Api.Features.Dependency;

public interface IDependencyService
{
    IAsyncEnumerable<KeyValuePair<string, DependencyComponent>> GetStatusAsync(CancellationToken cancellationToken);
}

internal static class EnumExtensions
{
    public static TEnum MaxEnum<TEnum>(params TEnum?[] values) where TEnum : struct, Enum
    {
        if (values == null || values.Length == 0) throw new ArgumentException("At least one enum value must be provided.", nameof(values));

        var nonNullValues = values
            .Where(v => v.HasValue)
            .Select(v => v.Value)
            .ToList();

        if (nonNullValues.Count == 0) throw new ArgumentException("All provided enum values are null.", nameof(values));

        return nonNullValues
            .OrderByDescending(v => Convert.ToInt64(v))
            .First();
    }
}

internal static class Certificatehelper
{
    public static async Task<HealthComponent> GetCertificateHealthAsync(Uri uri, CertificateCheckOptions optionsCertificate, HealthStatus? certificateStatus = default, string message = null)
    {
        var certInfo = await GetCertificateInfoAsync(uri);
        var sb = new StringBuilder();
        sb.Append($"Certificate for '{certInfo.Host}' with {certInfo.TlsVersion}");
        if (!string.IsNullOrEmpty(message)) sb.Append($"has issue '{message}', ");

        var exp = HealthStatus.Degraded;
        int? daysLeft = null;
        if (certInfo.CertExpiry.HasValue)
        {
            daysLeft = (int)(certInfo.CertExpiry.Value - DateTime.UtcNow).TotalDays;
            if (daysLeft <= (optionsCertificate?.CertExpiryUnhealthyLimitDays ?? 3))
            {
                exp = HealthStatus.Unhealthy;
            }
            else if (daysLeft <= (optionsCertificate?.CertExpiryDegradedLimitDays ?? 30))
            {
                exp = HealthStatus.Degraded;
            }
            else
            {
                exp = HealthStatus.Healthy;
            }
            sb.Append($" expires in {daysLeft} days");
        }
        sb.Append(".");

        var status = EnumExtensions.MaxEnum(certificateStatus, exp);

        return new HealthComponent
        {
            Status = status,
            Details = new Dictionary<string, string>
            {
                { "host", $"{certInfo.Host}" },
                { "tlsVersion", $"{certInfo.TlsVersion}" },
                { "certificateExpiryDate", $"{certInfo.CertExpiry}" },
                { "certificateExpiryDays", $"{daysLeft}" },
                { "message", sb.ToString() },
            }
        };
    }

    public static async Task<(SslProtocols TlsVersion, DateTime? CertExpiry, string Host)> GetCertificateInfoAsync(Uri uri)
    {
        var host = uri.Host;
        var port = uri.Port == -1 ? 443 : uri.Port;

        if (port == 80) return (SslProtocols.None, null, host);

        using var client = new TcpClient();
        await client.ConnectAsync(host, port);

        await using var sslStream = new SslStream(client.GetStream(), false, (_, _, _, _) => true); // Accept invalid certs

        await sslStream.AuthenticateAsClientAsync(host); // SNI

        var cert = sslStream.RemoteCertificate as X509Certificate2;

        var expiry = cert?.NotAfter;

        return (sslStream.SslProtocol, expiry, host);
    }
}

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

    private async Task<HealthResponse> CheckCertificateAsync(Toolkit.Features.Health.Dependency x, HealthStatus certificateStatus, string message, HealthResponse content)
    {
        if (!(_options.Certificate?.DependencyCheckEnabled ?? false)) return content;

        var certificateHealth = await Certificatehelper.GetCertificateHealthAsync(x.Uri, _options.Certificate, certificateStatus, message);

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