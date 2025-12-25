namespace Quilt4Net.Toolkit;

/// <summary>
/// This option can be configured by code or with appsettings.json on location "Quilt4Net/Content"
/// </summary>
public record ContentOptions
{
    public string Application { get; set; }
    public string Quilt4NetAddress { get; set; }
    public string ApiKey { get; set; }
}