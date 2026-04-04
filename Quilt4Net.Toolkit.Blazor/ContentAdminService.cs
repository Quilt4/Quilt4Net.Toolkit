using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;

namespace Quilt4Net.Toolkit.Blazor;

internal class ContentAdminService : IContentAdminService
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly string[] _adminRoles;

    public ContentAdminService(IOptions<ContentOptions> options, AuthenticationStateProvider authStateProvider = null)
    {
        _authStateProvider = authStateProvider;
        _adminRoles = options.Value.AdminRoles ?? [];
    }

    public async Task<bool> IsContentAdminAsync()
    {
        if (_authStateProvider == null)
            return true;

        var state = await _authStateProvider.GetAuthenticationStateAsync();
        var user = state.User;

        if (user.Identity is not { IsAuthenticated: true })
            return false;

        return _adminRoles.Any(role => user.IsInRole(role));
    }
}
