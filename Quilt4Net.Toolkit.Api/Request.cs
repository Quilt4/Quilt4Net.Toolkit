namespace Quilt4Net.Toolkit.Api;

public record Request
{
    public required string Method { get; init; }
    public required string Path { get; init; }
    public required Dictionary<string, string> Headers { get; init; }
    public required Dictionary<string, string> Query { get; init; }
    public required string Body { get; init; }
    public required string ClientIp { get; init; }
}