namespace Quilt4Net.Toolkit.Features.Api;

public record HealthEndpointOptions
{
    public MethodOptions Head { get; set; } = new();
    public GetMethodOptions Get { get; set; } = new();
}