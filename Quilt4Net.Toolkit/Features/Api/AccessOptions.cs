namespace Quilt4Net.Toolkit.Features.Api;

public sealed record AccessOptions
{
    public AccessLevel? Level { get; set; }

    /// <summary>
    /// Optional: if provided, the caller must be in at least one of these roles.
    /// Only meaningful when Level == AuthenticatedOnly.
    /// </summary>
    public string[] Roles { get; set; } = Array.Empty<string>();
}