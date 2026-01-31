namespace Quilt4Net.Toolkit.Features.Api;

/// <summary>
/// Level of information depending on if the user is authenticated or not.
/// </summary>
public enum DetailsLevel
{
    /// <summary>
    /// Show details for everyone.
    /// </summary>
    Everyone,

    /// <summary>
    /// Show details for authenticated calls only.
    /// </summary>
    AuthenticatedOnly,

    /// <summary>
    /// Never show details.
    /// </summary>
    NoOne
}