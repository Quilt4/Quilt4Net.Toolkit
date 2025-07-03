using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Quilt4Net.Toolkit.Features.Health;

namespace Quilt4Net.Toolkit.Api.Features.Dependency;

internal static class Certificatehelper
{
    public static async Task<HealthComponent> GetCertificateHealthAsync(Uri uri, CertificateCheckOptions optionsCertificate, HealthStatus? certificateStatus = default)
    {
        var certInfo = await GetCertificateInfoAsync(uri);

        var exp = HealthStatus.Unhealthy;
        if (certInfo.CertExpiry.HasValue)
        {
            var ds  = (int)(certInfo.CertExpiry.Value - DateTime.UtcNow).TotalDays;
            if (ds <= (optionsCertificate?.CertExpiryUnhealthyLimitDays ?? 3))
            {
                exp = HealthStatus.Unhealthy;
            }
            else if (ds <= (optionsCertificate?.CertExpiryDegradedLimitDays ?? 3))
            {
                exp = HealthStatus.Degraded;
            }
            else
            {
                exp = HealthStatus.Healthy;
            }
        }

        var cs = EnumExtensions.MaxEnum(certificateStatus, exp);

        return new HealthComponent
        {
            Status = cs,
            Details = new Dictionary<string, string>
            {
                { "host", $"{certInfo.Host}" },
                { "tlsVersion", $"{certInfo.TlsVersion}" },
                { "certificateExpiry", $"{certInfo.CertExpiry}" },
            }
        };
    }

    public static async Task<(SslProtocols TlsVersion, DateTime? CertExpiry, string Host)> GetCertificateInfoAsync(Uri uri)
    {
        var host = uri.Host;
        var port = uri.Port == -1 ? 443 : uri.Port;

        using var client = new TcpClient();
        await client.ConnectAsync(host, port);

        await using var sslStream = new SslStream(client.GetStream(), false, (_, _, _, _) => true); // Accept invalid certs

        await sslStream.AuthenticateAsClientAsync(host); // SNI

        var cert = sslStream.RemoteCertificate as X509Certificate2;

        var expiry = cert?.NotAfter;

        return (sslStream.SslProtocol, expiry, host);
    }
}