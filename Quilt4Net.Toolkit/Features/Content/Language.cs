namespace Quilt4Net.Toolkit.Features.Content;

public record Language
{
    public Guid Key { get; set; }
    public string Name { get; set; }
    public bool Developer { get; set; }
}