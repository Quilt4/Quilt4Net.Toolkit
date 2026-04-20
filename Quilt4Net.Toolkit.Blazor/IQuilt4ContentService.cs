namespace Quilt4Net.Toolkit.Blazor;

/// <summary>
/// Simplified content service that handles language selection automatically.
/// </summary>
public interface IQuilt4ContentService
{
    /// <summary>
    /// Get content by key using the currently selected language.
    /// </summary>
    /// <param name="key">Content key.</param>
    /// <param name="defaultValue">Value to return if the server is unreachable or the key is unknown.</param>
    /// <param name="application">
    /// Application scope. Convention:
    /// <list type="bullet">
    /// <item><c>null</c> — default: resolve the current application name (ContentOptions.Application or the entry assembly name).</item>
    /// <item><c>""</c> — shared (cross-application).</item>
    /// <item>any other value — that exact application.</item>
    /// </list>
    /// </param>
    Task<string> GetAsync(string key, string defaultValue, string application = null);
}
