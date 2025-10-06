using System.Collections;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.UI.Controls
{
    public class CompositeCheckboxGroup : StackLayout
    {
        public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
            nameof(ItemsSource), typeof(IEnumerable), typeof(CompositeCheckboxGroup), propertyChanged: OnItemsSourceChanged);

        public static readonly BindableProperty SelectedItemsProperty = BindableProperty.Create(
            nameof(SelectedItems), typeof(IList), typeof(CompositeCheckboxGroup), defaultValue: new ObservableCollection<object>(),
            defaultBindingMode: BindingMode.TwoWay);

        public static readonly BindableProperty LabelTextProperty = BindableProperty.Create(
            nameof(LabelText), typeof(string), typeof(CompositeCheckboxGroup), default(string));

        public static readonly BindableProperty RequiredProperty = BindableProperty.Create(
            nameof(Required), typeof(bool), typeof(CompositeCheckboxGroup), default(bool));

        public static readonly BindableProperty IsValidProperty = BindableProperty.Create(
            nameof(IsValid), typeof(bool), typeof(CompositeCheckboxGroup), default(bool));

        public static readonly BindableProperty ValidationTextProperty = BindableProperty.Create(
            nameof(ValidationText), typeof(string), typeof(CompositeCheckboxGroup), default(string));

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)this.GetValue(ItemsSourceProperty);
            set => this.SetValue(ItemsSourceProperty, value);
        }

        public IList SelectedItems
        {
            get => (IList)this.GetValue(SelectedItemsProperty);
            set => this.SetValue(SelectedItemsProperty, value);
        }

        public string LabelText
        {
            get => (string)this.GetValue(LabelTextProperty);
            set => this.SetValue(LabelTextProperty, value);
        }

        public bool Required
        {
            get => (bool)this.GetValue(RequiredProperty);
            set => this.SetValue(RequiredProperty, value);
        }

        public bool IsValid
        {
            get => (bool)this.GetValue(IsValidProperty);
            set => this.SetValue(IsValidProperty, value);
        }

        public string ValidationText
        {
            get => (string)this.GetValue(ValidationTextProperty);
            set => this.SetValue(ValidationTextProperty, value);
        }

        public CompositeCheckboxGroup()
        {
            this.Orientation = StackOrientation.Vertical;
        }

        private static void OnItemsSourceChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is CompositeCheckboxGroup Group)
            {
                Group.BuildCheckboxes();
            }
        }

        private void BuildCheckboxes()
        {
            this.Children.Clear();
            if (!string.IsNullOrEmpty(this.LabelText))
            {
                Label HeaderLabel = new Label
                {
                    Text = this.LabelText,
                    FontAttributes = FontAttributes.Bold
                };
                this.Children.Add(HeaderLabel);
            }
            if (this.ItemsSource == null)
            {
                return;
            }
            foreach (object Item in this.ItemsSource)
            {
                CheckBox CheckBox = new CheckBox
                {
                    BindingContext = Item,
                    IsChecked = this.SelectedItems?.Contains(Item) == true
                };
                CheckBox.CheckedChanged += (object? sender, CheckedChangedEventArgs e) =>
                {
                    if (this.SelectedItems == null)
                    {
                        return;
                    }
                    if (e.Value)
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
                };

                Label OptionLabel = new Label
                {
                    VerticalTextAlignment = TextAlignment.Center
                };
                OptionLabel.SetBinding(Label.TextProperty, "Label.Text");

                HorizontalStackLayout Row = new HorizontalStackLayout
                {
                    Spacing = 8
                };
                Row.Children.Add(CheckBox);
                Row.Children.Add(OptionLabel);
                this.Children.Add(Row);
            }
            if (!this.IsValid && !string.IsNullOrEmpty(this.ValidationText))
            {
                Label ValidationLabel = new Label
                {
                    Text = this.ValidationText,
                    TextColor = Colors.Red,
                    FontSize = 12
                };
                this.Children.Add(ValidationLabel);
            }
        }
    }
}
