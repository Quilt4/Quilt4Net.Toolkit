namespace Quilt4Net.Toolkit.Blazor;

public interface IEditContentService
{
    event EventHandler<EditModeEventArgs> EditModeEvent;
    bool Enabled { get; set; }
}