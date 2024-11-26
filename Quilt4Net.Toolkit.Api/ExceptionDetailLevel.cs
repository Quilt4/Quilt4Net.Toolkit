namespace Quilt4Net.Toolkit.Api;

/// <summary>
/// Level of information for exceptions.
/// </summary>
public enum ExceptionDetailLevel
{
    /// <summary>
    /// Message is not displayed at all, just a correlationId if logger is enabled.
    /// </summary>
    Hidden,

    /// <summary>
    /// Message of the exception, together with the correlationId.
    /// </summary>
    Message,

    /// <summary>
    /// Message of the exception and the stack trace is returned, together with the correlationId.
    /// </summary>
    StackTrace
}