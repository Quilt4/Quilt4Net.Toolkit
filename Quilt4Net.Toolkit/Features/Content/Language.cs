namespace Quilt4Net.Toolkit.Features.Content;

public record Language
{
    public static readonly Guid DeveloperLanguageKey = Guid.Parse("8C12E829-318E-40DA-86E9-6B37A68EFFD1");
    public static readonly Guid NoApiKeyLanguageKey = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public Guid Key { get; set; }
    public string Name { get; set; }
    public bool Developer { get; set; }
}