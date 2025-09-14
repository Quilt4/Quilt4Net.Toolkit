namespace Quilt4Net.Toolkit.Blazor;

public class EditContentService : IEditContentService
{
    private bool _enabled;

    public event EventHandler<EditModeEventArgs> EditModeEvent;

    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value) return;
            _enabled = value;
            EditModeEvent?.Invoke(this, new EditModeEventArgs(value));
        }
    }
}