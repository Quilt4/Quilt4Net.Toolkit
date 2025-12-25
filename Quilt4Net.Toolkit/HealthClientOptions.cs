namespace Quilt4Net.Toolkit;

/// <summary>
/// This option can be configured by code or with appsettings.json on location "Quilt4Net/HealthClient"
/// </summary>
public record HealthClientOptions
{
    /// <summary>
    /// HealthAddress to the health API.
    /// </summary>
    public string HealthAddress { get; set; }
}