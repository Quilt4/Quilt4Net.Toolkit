namespace Quilt4Net.Toolkit.Features.ValueGroup;

/// <summary>
/// Options for <see cref="IValueGroupClient"/>. Configuration path: <c>Quilt4Net:ValueGroup</c>.
/// </summary>
public record ValueGroupClientOptions
{
    /// <summary>Quilt4Net server base URL. Default is <c>https://quilt4net.com/</c>.</summary>
    public string Quilt4NetAddress { get; set; } = "https://quilt4net.com/";

    /// <summary>The API key minted for this Value Group on the server. Required.</summary>
    public string ApiKey { get; set; }

    /// <summary>The id of the Value Group this client fetches. Required.</summary>
    public string GroupId { get; set; }

    /// <summary>How long the cached bundle is considered fresh. Default 5 minutes.</summary>
    public TimeSpan? Ttl { get; set; }

    /// <summary>Timeout for HTTP calls to the server. Default 5 seconds.</summary>
    public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(5);
}
