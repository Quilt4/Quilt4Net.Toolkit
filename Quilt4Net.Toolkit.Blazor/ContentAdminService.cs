using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;

namespace Quilt4Net.Toolkit.Blazor;

internal class ContentAdminService : IContentAdminService
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly string[] _adminRoles;
    private readonly bool _assumeAdmin;

    public ContentAdminService(IOptions<ContentOptions> options, AuthenticationStateProvider authStateProvider = null)
    {
        _authStateProvider = authStateProvider;
        _adminRoles = options.Value.AdminRoles ?? [];
        _assumeAdmin = options.Value.AssumeAdmin;
    }

    public async Task<bool> IsContentAdminAsync()
    {
        if (_assumeAdmin)
            return true;

        if (_authStateProvider == null)
            return false;

        var state = await _authStateProvider.GetAuthenticationStateAsync();
        var user = state.User;

        if (user.Identity is not { IsAuthenticated: true })
            return false;

        return _adminRoles.Any(role => user.IsInRole(role));
    }
}
