

using System.Collections;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.UI.Controls
{
    public enum SelectorSelectionMode
    {
        Single,
        Multiple
    }

    public class SelectableTemplateSelector : StackLayout
    {
        public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
            nameof(ItemsSource), typeof(IEnumerable), typeof(SelectableTemplateSelector), propertyChanged: OnItemsSourceChanged);

        public static readonly BindableProperty ItemTemplateProperty = BindableProperty.Create(
            nameof(ItemTemplate), typeof(DataTemplate), typeof(SelectableTemplateSelector));

        public static readonly BindableProperty SelectionModeProperty = BindableProperty.Create(
            nameof(SelectionMode), typeof(SelectorSelectionMode), typeof(SelectableTemplateSelector), SelectorSelectionMode.Single);

        public static readonly BindableProperty SelectedItemProperty = BindableProperty.Create(
            nameof(SelectedItem), typeof(object), typeof(SelectableTemplateSelector), defaultBindingMode: BindingMode.TwoWay);

        public static readonly BindableProperty SelectedItemsProperty = BindableProperty.Create(
            nameof(SelectedItems), typeof(IList), typeof(SelectableTemplateSelector), defaultValue: new ObservableCollection<object>(), defaultBindingMode: BindingMode.TwoWay);
        public static readonly BindableProperty SelectOnTapProperty = BindableProperty.Create(
            nameof(SelectOnTap), typeof(bool), typeof(SelectableTemplateSelector), false);

        public bool SelectOnTap
        {
            get => (bool)this.GetValue(SelectOnTapProperty);
            set => this.SetValue(SelectOnTapProperty, value);
        }
        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public DataTemplate ItemTemplate
        {
            get => (DataTemplate)GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }

        public SelectorSelectionMode SelectionMode
        {
            get => (SelectorSelectionMode)GetValue(SelectionModeProperty);
            set => SetValue(SelectionModeProperty, value);
        }

        public object SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public IList SelectedItems
        {
            get => (IList)GetValue(SelectedItemsProperty);
            set => SetValue(SelectedItemsProperty, value);
        }

        public SelectableTemplateSelector()
        {
            Orientation = StackOrientation.Vertical;
        }

        private static void OnItemsSourceChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is SelectableTemplateSelector selector)
            {
                selector.BuildItems();
            }
        }

        private void BuildItems()
        {
            this.Children.Clear();
            if (this.ItemsSource == null || this.ItemTemplate == null)
                return;
            foreach (object Item in this.ItemsSource)
            {
                NeuroAccessMaui.Core.ISelectable Selectable;
                if (Item is NeuroAccessMaui.Core.ISelectable AlreadySelectable)
                {
                    Selectable = AlreadySelectable;
                }
                else
                {
                    SelectableOption<object> Option = new SelectableOption<object>(Item, this.SelectedItems?.Contains(Item) == true, o => this.ToggleSelection(o));
                    Option.IsSelectedChanged += this.OnChildIsSelectedChanged;
                    Selectable = Option;
                }
                if (Selectable is SelectableOption<object> Option2)
                {
                    Option2.IsSelectedChanged += this.OnChildIsSelectedChanged;
                }
                View Content = (View)this.ItemTemplate.CreateContent();
                Content.BindingContext = Selectable;
                if (this.SelectOnTap)
                {
                    TapGestureRecognizer TapGesture = new TapGestureRecognizer();
                    TapGesture.Tapped += (s, e) => this.ToggleSelection(Selectable);
                    Content.GestureRecognizers.Add(TapGesture);
                }
                this.Children.Add(Content);
            }
        }

        private void OnChildIsSelectedChanged(object? sender, EventArgs e)
        {
            if (this.SelectionMode == SelectorSelectionMode.Single && sender is NeuroAccessMaui.Core.ISelectable ChangedSelectable && ChangedSelectable.IsSelected)
            {
                foreach (View ChildView in this.Children.Cast<View>())
                {
                    if (ChildView.BindingContext is NeuroAccessMaui.Core.ISelectable OtherSelectable && !object.ReferenceEquals(OtherSelectable, ChangedSelectable))
                    {
                        OtherSelectable.IsSelected = false;
                    }
                }
                this.SelectedItem = (ChangedSelectable is SelectableOption<object> So) ? So.Item : ChangedSelectable;
                if (this.SelectedItems != null)
                {
                    this.SelectedItems.Clear();
                    this.SelectedItems.Add(this.SelectedItem);
                }
            }
        }

        private object CreateSelectableOption(object item, bool isSelected, Action<object>? toggleAction = null)
        {
            Type OptionType = typeof(SelectableOption<>).MakeGenericType(item.GetType());
            object? Instance = Activator.CreateInstance(OptionType, item, isSelected, toggleAction);
            if (Instance == null)
            {
                throw new InvalidOperationException($"Could not create SelectableOption for type {OptionType}.");
            }
            return Instance;
        }

        private void ToggleSelection(object selectableOptionObj)
        {
            NeuroAccessMaui.Core.ISelectable? Selectable = selectableOptionObj as NeuroAccessMaui.Core.ISelectable;
            if (Selectable == null)
            {
                return;
            }
            object Item = (Selectable is SelectableOption<object> So) ? So.Item : selectableOptionObj;
            if (this.SelectionMode == SelectorSelectionMode.Single)
            {
                // Deselect all others
                foreach (View ChildView in this.Children.Cast<View>())
                {
                    if (ChildView.BindingContext is NeuroAccessMaui.Core.ISelectable OtherSelectable && !object.ReferenceEquals(OtherSelectable, Selectable))
                    {
                        OtherSelectable.IsSelected = false;
                    }
                }
                // Select this one
                Selectable.IsSelected = true;
                this.SelectedItem = Item;
                // Ensure only one in SelectedItems
                if (this.SelectedItems != null)
                {
                    this.SelectedItems.Clear();
                    this.SelectedItems.Add(Item);
                }
            }
            else
            {
                Selectable.IsSelected = !Selectable.IsSelected;
                if (Selectable.IsSelected)
                {
                    if (!this.SelectedItems.Contains(Item))
                    {
                        this.SelectedItems.Add(Item);
                    }
                }
                else
                {
                    if (this.SelectedItems.Contains(Item))
                    {
                        this.SelectedItems.Remove(Item);
                    }
                }
            }
        }

        // SelectableOption<T> is now used for item context
    }
}
