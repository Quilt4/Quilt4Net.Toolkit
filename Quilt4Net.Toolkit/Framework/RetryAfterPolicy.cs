using System.Net;
using System.Net.Http.Headers;

namespace Quilt4Net.Toolkit.Framework;

/// <summary>
/// Reads the server's <c>Retry-After</c> from a <c>429 Too Many Requests</c>.
/// </summary>
/// <remarks>
/// <para>
/// Shared by the content and configuration clients so the two cannot disagree about when a shed
/// request may be tried again. It lived privately in the content client until the configuration
/// client needed it too — and a second copy is how the two would drift.
/// </para>
/// <para>
/// The server is stating when it will be ready. Ignoring that and falling back to a local back-off
/// either retries far too early and deepens the overload that caused the rejection, or sits out an
/// interval far longer than the server asked for.
/// </para>
/// </remarks>
internal static class RetryAfterPolicy
{
    /// <summary>
    /// How long the server asked the caller to wait, or <c>null</c> when the response is not a 429 or
    /// carries no usable header — in which case the caller's own failure interval applies.
    /// </summary>
    /// <remarks>
    /// RFC 9110 allows either delta-seconds or an HTTP-date; <see cref="HttpResponseHeaders.RetryAfter"/>
    /// surfaces both, so both are handled. A date already in the past yields <c>null</c> — treated as
    /// "no advice" rather than "retry immediately", because retrying instantly into a server that just
    /// shed the request is the one response guaranteed to make things worse.
    /// </remarks>
    public static TimeSpan? Of(HttpResponseMessage response)
    {
        if (response.StatusCode != HttpStatusCode.TooManyRequests) return null;

        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter == null) return null;

        if (retryAfter.Delta is { } delta && delta > TimeSpan.Zero) return delta;

        if (retryAfter.Date is { } date)
        {
            var wait = date - DateTimeOffset.UtcNow;
            if (wait > TimeSpan.Zero) return wait;
        }

        return null;
    }
}
