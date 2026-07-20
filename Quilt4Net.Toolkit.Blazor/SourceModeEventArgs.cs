namespace Quilt4Net.Toolkit.Blazor;

/// <summary>Carries the new state of the content source overlay.</summary>
public class SourceModeEventArgs : EventArgs
{
    /// <summary>Creates the event args.</summary>
    public SourceModeEventArgs(bool enabled)
    {
        Enabled = enabled;
    }

    /// <summary>Whether the source overlay is now enabled.</summary>
    public bool Enabled { get; }
}
