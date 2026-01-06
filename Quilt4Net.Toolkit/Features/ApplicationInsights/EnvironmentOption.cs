namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

public record EnvironmentOption
{
    public string Label { get; set; }
    public string Value { get; set; }

    public EnvironmentOption(string value, string label = null)
    {
        Value = value;
        Label = label ?? value ?? "Any";
    }

    public static implicit operator string(EnvironmentOption option) => option?.Value;
}