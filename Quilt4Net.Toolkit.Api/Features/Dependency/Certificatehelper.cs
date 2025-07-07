using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Quilt4Net.Toolkit.Features.Health;

namespace Quilt4Net.Toolkit.Api.Features.Dependency;

internal static class Certificatehelper
{
    public static async Task<HealthComponent> GetCertificateHealthAsync(Uri uri, CertificateCheckOptions optionsCertificate, HealthStatus? certificateStatus = default)
    {
        var certInfo = await GetCertificateInfoAsync(uri);
        var sb = new StringBuilder();
        sb.Append($"Certificate for '{certInfo.Host}' with {certInfo.TlsVersion}");

        var exp = HealthStatus.Unhealthy;
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

        using var client = new TcpClient();
        await client.ConnectAsync(host, port);

        await using var sslStream = new SslStream(client.GetStream(), false, (_, _, _, _) => true); // Accept invalid certs

        await sslStream.AuthenticateAsClientAsync(host); // SNI

        var cert = sslStream.RemoteCertificate as X509Certificate2;

        var expiry = cert?.NotAfter;

        return (sslStream.SslProtocol, expiry, host);
    }
}