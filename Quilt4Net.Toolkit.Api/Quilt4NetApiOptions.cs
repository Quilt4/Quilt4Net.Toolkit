namespace Quilt4Net.Toolkit.Api;

public record Quilt4NetApiOptions
{
    public bool ShowInSwagger { get; set; } = true;
    public string Pattern { get; set; } = "api";
    public string ControllerName { get; set; } = "health";
}