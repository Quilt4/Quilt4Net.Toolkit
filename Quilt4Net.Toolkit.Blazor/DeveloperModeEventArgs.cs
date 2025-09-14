namespace Quilt4Net.Toolkit.Blazor;

public class DeveloperModeEventArgs : EventArgs
{
    public bool Enabled { get; }

    public DeveloperModeEventArgs(bool enabled)
    {
        Enabled = enabled;
    }
}