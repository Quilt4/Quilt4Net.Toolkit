namespace Quilt4Net.Toolkit;

public record Quilt4NetHealthOptions
{
    /// <summary>
    /// Address to the health API.
    /// </summary>
    public Uri HealthAddress { get; set; }
}