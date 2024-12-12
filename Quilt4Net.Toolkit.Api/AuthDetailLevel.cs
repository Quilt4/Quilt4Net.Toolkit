namespace Quilt4Net.Toolkit.Api;

/// <summary>
/// Level of information depending on if the user is authenticated or not.
/// </summary>
public enum AuthDetailLevel
{
    /// <summary>
    /// Show details for everyone.
    /// </summary>
    EveryOne,

    /// <summary>
    /// Show details for authenticated calls only.
    /// </summary>
    AuthenticatedOnly,

    /// <summary>
    /// Never show details.
    /// </summary>
    NoOne
}