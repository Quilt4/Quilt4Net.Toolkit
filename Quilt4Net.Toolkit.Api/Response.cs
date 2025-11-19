namespace Quilt4Net.Toolkit.Api;

public record Response
{
    public required int StatusCode { get; init; }
    public required Dictionary<string, string> Headers { get; init; }
    public required string Body { get; init; }
}