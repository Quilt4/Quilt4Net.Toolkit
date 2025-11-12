//using System.Security.Claims;

//namespace Quilt4Net.Toolkit;

//public static class ClaimsExtensions
//{
//    private static readonly string[] _keyClaimTypes =
//    {
//        ClaimTypes.NameIdentifier, // WS-Fed / ASP.NET Identity
//        "sub", // OpenID Connect
//        "oid", // Azure AD
//        "nameid", // SAML / ADFS
//        "uid" // Custom / LDAP
//    };

//    [Obsolete($"Use Tharga.Toolkit.ClaimsExtensions.GetKey instead.")]
//    public static string GetKey(this ClaimsPrincipal claimsPrincipal)
//    {
//        if (claimsPrincipal == null) throw new ArgumentNullException(nameof(claimsPrincipal));
//        return claimsPrincipal.Claims.GetKey();
//    }

//    [Obsolete($"Use Tharga.Toolkit.ClaimsExtensions.GetKey instead.")]
//    public static string GetKey(this ClaimsIdentity claimsIdentity)
//    {
//        if (claimsIdentity == null) throw new ArgumentNullException(nameof(claimsIdentity));
//        return claimsIdentity.Claims.GetKey();
//    }

//    [Obsolete($"Use Tharga.Toolkit.ClaimsExtensions.GetKey instead.")]
//    public static string GetKey(this IEnumerable<Claim> claims)
//    {
//        if (claims == null) throw new ArgumentNullException(nameof(claims));

//        var arr = claims as Claim[] ?? claims.ToArray();

//        foreach (var type in _keyClaimTypes)
//        {
//            var value = arr.FirstOrDefault(c => string.Equals(c.Type, type, StringComparison.OrdinalIgnoreCase))?.Value;
//            if (!string.IsNullOrWhiteSpace(value)) return value;
//        }

//        return null;
//    }
//}