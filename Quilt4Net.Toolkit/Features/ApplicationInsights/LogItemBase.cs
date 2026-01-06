namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

public abstract record LogItemBase
{
    public required string Id { get; init; }
    public required string Fingerprint { get; init; }
    public required DateTime TimeGenerated { get; init; }

    public required string Message { get; init; }
    public required string Environment { get; init; }
    public required string Application { get; init; }
}