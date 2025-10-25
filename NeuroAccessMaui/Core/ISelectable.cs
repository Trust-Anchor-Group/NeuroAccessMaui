namespace NeuroAccessMaui.Core
{
    public interface ISelectable
    {
        bool IsSelected { get; set; }
        Microsoft.Maui.Controls.Command ToggleSelectionCommand { get; }
    }
}
