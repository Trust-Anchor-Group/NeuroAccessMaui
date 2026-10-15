using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.UI.Controls
{
    public class SelectableOption<T> : BindableObject, NeuroAccessMaui.Core.ISelectable
    {
        public T Item { get; }
        private bool isSelectedBacking;
        public bool IsSelected
        {
            get => this.isSelectedBacking;
            set
            {
                if (this.isSelectedBacking != value)
                {
                    this.isSelectedBacking = value;
                    this.OnPropertyChanged();
                    this.OnIsSelectedChanged();
                }
            }
        }

        private void OnIsSelectedChanged()
        {
            // Notify parent selector if available (for single selection enforcement)
            this.IsSelectedChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? IsSelectedChanged;

        public Command ToggleSelectionCommand { get; }
        private readonly Action<SelectableOption<T>>? toggleAction;

        public SelectableOption(T Item, bool IsSelected = false, Action<SelectableOption<T>>? ToggleAction = null)
        {
            this.Item = Item;
            this.isSelectedBacking = IsSelected;
            this.toggleAction = ToggleAction;
            this.ToggleSelectionCommand = new Command(() => this.toggleAction?.Invoke(this));
        }
    }
}
