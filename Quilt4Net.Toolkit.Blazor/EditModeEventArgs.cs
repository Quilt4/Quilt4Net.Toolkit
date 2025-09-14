namespace Quilt4Net.Toolkit.Blazor;

public class EditModeEventArgs : EventArgs
{
    public EditModeEventArgs(bool enabled)
    {
        Enabled = enabled;
    }

    public bool Enabled { get; }
}