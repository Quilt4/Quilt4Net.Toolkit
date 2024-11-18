namespace Quilt4Net.Toolkit.Api.Framework;

public abstract record ResponseBase<TStatus> where TStatus : Enum
{
    public abstract required TStatus Status { get; init; }
}