using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Quilt4Net.Toolkit.Api.Sample;

public class DummyAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string ApiKeyHeaderName = "X-API-KEY";

    public DummyAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check for the X-API-KEY header. Any value will allow the call to pass.
        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyValue) ||
            string.IsNullOrWhiteSpace(apiKeyValue) ||
            apiKeyValue == "0")
        {
            // Return failure if header is missing.
            return Task.FromResult(AuthenticateResult.Fail("Missing or invalid API key."));
        }

        // Header is present and valid – succeed
        var claims = new[] { new Claim(ClaimTypes.Name, "ApiKeyUser") };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
