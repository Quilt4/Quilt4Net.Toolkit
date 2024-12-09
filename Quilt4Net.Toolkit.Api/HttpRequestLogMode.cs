namespace Quilt4Net.Toolkit.Api;

[Flags]
public enum HttpRequestLogMode
{
    None = 0,
    ApplicationInsights = 1,
    Logger = 2,
}