using Azure.Core;
using Azure.Identity;

namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

/// <summary>
/// Builds the appropriate Azure <see cref="TokenCredential"/> for the configured <see cref="ApplicationInsightsAuthMode"/>.
/// </summary>
internal static class CredentialFactory
{
    public static TokenCredential Create(ApplicationInsightsAuthMode authMode, string tenantId, string clientId, string clientSecret)
    {
        switch (authMode)
        {
            case ApplicationInsightsAuthMode.ManagedIdentity:
                // Empty ClientId -> system-assigned MI; value -> user-assigned MI.
                return string.IsNullOrEmpty(clientId)
                    ? new ManagedIdentityCredential()
                    : new ManagedIdentityCredential(clientId);

            case ApplicationInsightsAuthMode.DefaultAzureCredential:
                // Forward TenantId + ClientId as hints. Empty values become null so the SDK's
                // own discovery (env vars / az login / Visual Studio / etc.) kicks in.
                return new DefaultAzureCredential(new DefaultAzureCredentialOptions
                {
                    TenantId = string.IsNullOrEmpty(tenantId) ? null : tenantId,
                    ManagedIdentityClientId = string.IsNullOrEmpty(clientId) ? null : clientId,
                });

            case ApplicationInsightsAuthMode.ClientSecret:
            default:
                if (string.IsNullOrEmpty(clientSecret))
                    throw new InvalidOperationException(
                        $"No {nameof(ApplicationInsightsOptions.ClientSecret)} has been configured. " +
                        $"Set {nameof(ApplicationInsightsOptions.AuthMode)} = {nameof(ApplicationInsightsAuthMode)}.{nameof(ApplicationInsightsAuthMode.ManagedIdentity)} " +
                        $"or {nameof(ApplicationInsightsAuthMode)}.{nameof(ApplicationInsightsAuthMode.DefaultAzureCredential)} to skip the secret.");
                return new ClientSecretCredential(tenantId, clientId, clientSecret);
        }
    }
}
