namespace Quilt4Net.Toolkit.Api.Features.Ready;

/// <summary>
/// Status for Ready.
/// </summary>
public enum ReadyStatus
{
    /// <summary>
    /// The application is fully prepared to handle incoming traffic.
    /// </summary>
    Ready,

    /// <summary>
    /// The application can handle requests, but some components are operating at reduced performance or reliability.
    /// </summary>
    Degraded,

    /// <summary>
    /// The application cannot handle traffic, typically due to critical dependencies being unavailable.
    /// </summary>
    Unready
}