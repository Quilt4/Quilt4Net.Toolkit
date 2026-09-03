namespace Quilt4Net.Toolkit;

/// <summary>
/// Options for the Quilt4Net issue tracker client and the roadmap view component.
/// </summary>
public record IssueOptions
{
    /// <summary>
    /// Address of the Quilt4Net server. Defaulted on the type so an unbound
    /// <c>IOptions&lt;IssueOptions&gt;</c> still carries a usable URL.
    /// </summary>
    public string Quilt4NetAddress { get; set; } = "https://quilt4net.com/";

    /// <summary>
    /// Team API key used for every call. Reading needs <c>issue:read</c>; the write methods
    /// additionally need <c>issue:write</c>, which is granted per key rather than by access level —
    /// an ordinary application key will not have it.
    /// </summary>
    public string ApiKey { get; set; }
}
