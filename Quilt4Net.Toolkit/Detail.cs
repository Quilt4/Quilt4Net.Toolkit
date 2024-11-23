namespace Quilt4Net.Toolkit.Client;

public record Detail
{
    public string SeverityLevel { get; init; }
    public string OuterId { get; init; }
    public string Message { get; init; }
    public string Type { get; init; }
    public string Id { get; init; }
    public Parsedstack[] ParsedStack { get; init; }
}