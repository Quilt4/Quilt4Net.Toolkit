namespace Quilt4Net.Toolkit;

/// <summary>
/// This attributes uses the default of LoggingAttribute but sets the ResponseBody=false to support output streams.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class LoggingStreamAttribute : Attribute;