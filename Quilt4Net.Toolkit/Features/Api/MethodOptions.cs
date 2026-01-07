namespace Quilt4Net.Toolkit.Features.Api;

public record MethodOptions
{
    public EndpointState State { get; set; } = EndpointState.Visible;
    public AccessOptions Access { get; set; } = new();
}