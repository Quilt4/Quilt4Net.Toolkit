namespace Quilt4Net.Toolkit.Features.Health;

public abstract record ResponseBase<TStatus> where TStatus : Enum
{
    public abstract required TStatus Status { get; init; }
}