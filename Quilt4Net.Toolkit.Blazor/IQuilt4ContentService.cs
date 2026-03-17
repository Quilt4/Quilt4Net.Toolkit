namespace Quilt4Net.Toolkit.Blazor;

/// <summary>
/// Simplified content service that handles language selection automatically.
/// </summary>
public interface IQuilt4ContentService
{
    /// <summary>
    /// Get content by key using the currently selected language.
    /// </summary>
    Task<string> GetAsync(string key, string defaultValue);
}
