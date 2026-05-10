namespace Quilt4Net.Toolkit.Blazor.Features.Log;

/// <summary>
/// Controls how log detail and summary views are opened when a user clicks a row in a log component.
/// </summary>
/// <remarks>
/// When <see cref="DetailPath"/> or <see cref="SummaryPath"/> is set, the component navigates to
/// that path with a URL-safe base64-encoded <c>p</c> query parameter containing all navigation params.
/// When null (the default), a Radzen dialog is opened inline instead.
/// </remarks>
public record LogNavigationOptions
{
    /// <summary>
    /// Optional path for the log detail page, e.g. "/log/detail".
    /// When null, clicking a detail link opens a dialog.
    /// </summary>
    public string DetailPath { get; init; }

    /// <summary>
    /// Optional path for the log summary page, e.g. "/log/summary".
    /// When null, clicking a summary link opens a dialog.
    /// </summary>
    public string SummaryPath { get; init; }

    /// <summary>
    /// Optional path for the host's Application Insights configuration page, e.g. "/monitor/configuration".
    /// When set, log views render an "Edit configuration" link next to authentication-failure
    /// alerts so operators can jump straight to the broken AI configuration instead of
    /// hunting through the configuration list.
    /// </summary>
    public string ConfigurationPath { get; init; }
}
