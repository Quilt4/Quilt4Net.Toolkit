namespace Quilt4Net.Toolkit;

/// <summary>
/// By default, all endpoinds that starts with Api is logged, not other endpoinds.
/// If this attributes is added, logging will be performed on that endpoind independent on the path.
/// To disable logging for an endpoint add the attribute and set the property Enabled=false.
/// For streamed output to work, set the property ResponseBody=false or Enabled=false.
/// Logged request data: Method, Path, Headers (optional), Query (optional), Body (optional), ClientIp
/// Logged response data: StatusCode, Headers (optional), Body (optional)
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class LoggingAttribute : Attribute
{
    /// <summary>
    /// Enable or disable all logging disregarding other properties.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Enable or disable logging of the request body.
    /// </summary>
    public bool RequestBody { get; set; } = true;

    /// <summary>
    /// Enable or disable logging of the request body.
    /// This needs to be set to false when the output is streamed.
    /// </summary>
    public bool ResponseBody { get; set; } = true;
}