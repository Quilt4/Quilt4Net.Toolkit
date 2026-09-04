namespace Quilt4Net.Toolkit.Features.Issue;

/// <summary>
/// Thrown when the Quilt4Net server rejects an issue-tracker call.
/// </summary>
/// <remarks>
/// The issue client deliberately throws rather than degrading to a default. A tracker read that
/// swallowed a failure and returned an empty set would look exactly like a team with no issues,
/// which is the one answer a caller must not be given wrongly.
/// </remarks>
public class IssueServiceException : Exception
{
    /// <summary>
    /// Creates the exception.
    /// </summary>
    /// <param name="message">What the server said.</param>
    /// <param name="statusCode">The HTTP status code returned.</param>
    /// <param name="retryAfter">How long the server asked the caller to wait, when it said so.</param>
    public IssueServiceException(string message, int statusCode, TimeSpan? retryAfter = null)
        : base(message)
    {
        StatusCode = statusCode;
        RetryAfter = retryAfter;
    }

    /// <summary>The HTTP status code the server returned.</summary>
    public int StatusCode { get; }

    /// <summary>
    /// How long to wait before trying again, taken from the server's <c>Retry-After</c> on a
    /// <c>429</c>. <c>null</c> when the server gave no advice, in which case the caller's own
    /// back-off applies — never an immediate retry.
    /// </summary>
    public TimeSpan? RetryAfter { get; }
}
