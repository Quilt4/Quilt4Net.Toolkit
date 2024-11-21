namespace Quilt4Net.Toolkit.Live;

/// <summary>
/// Status for Health.
/// </summary>
public enum LiveStatus
{
    /// <summary>
    /// The application process is running.
    /// </summary>
    Alive,

    /// <summary>
    /// The application process is not running or completely unresponsive (usually, this results in an HTTP 503 response instead of a JSON body).
    /// </summary>
    Dead
}