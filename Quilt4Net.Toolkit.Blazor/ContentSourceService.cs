namespace Quilt4Net.Toolkit.Blazor;

/// <inheritdoc cref="IContentSourceService"/>
public class ContentSourceService : IContentSourceService
{
    private bool _enabled;

    /// <inheritdoc />
    public event EventHandler<SourceModeEventArgs> SourceModeEvent;

    /// <inheritdoc />
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value) return;
            _enabled = value;
            SourceModeEvent?.Invoke(this, new SourceModeEventArgs(value));
        }
    }
}
