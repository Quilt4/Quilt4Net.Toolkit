namespace Quilt4Net.Toolkit.Features.Api;

public record GetMethodOptions : MethodOptions
{
    public DetailsLevel? Details { get; set; }
}