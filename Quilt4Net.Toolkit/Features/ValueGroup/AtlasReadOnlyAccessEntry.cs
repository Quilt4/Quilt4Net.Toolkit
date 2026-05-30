namespace Quilt4Net.Toolkit.Features.ValueGroup;

/// <summary>
/// A read-only MongoDB Atlas database user delivered as part of a <see cref="ValueGroupBundle"/>.
/// <see cref="ConnectionString"/> is the cluster's SRV connection string with
/// <see cref="Username"/>/<see cref="Password"/> already embedded — consumers can pass it straight
/// to a <c>MongoClient</c>. To revoke access, the team admin removes the underlying access in the
/// Quilt4Net.Server admin UI, which deletes the Atlas database user.
/// </summary>
public record AtlasReadOnlyAccessEntry
{
    public string Name { get; init; }
    public string ClusterName { get; init; }
    public string Username { get; init; }
    public string Password { get; init; }

    /// <summary>SRV connection string with username:password embedded; ready to hand to a MongoClient.</summary>
    public string ConnectionString { get; init; }
}
