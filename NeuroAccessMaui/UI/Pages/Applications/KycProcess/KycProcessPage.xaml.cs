using Microsoft.Maui.Controls;
using NeuroAccessMaui.UI.Controls;
using NeuroAccessMaui.UI;

namespace NeuroAccessMaui.UI.Pages.Applications.KycProcess
{

	public partial class KycProcessPage
	{
		public KycProcessPage(KycProcessViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentPageModel = ViewModel;

			ViewModel.PropertyChanged += this.ViewModel_PropertyChanged;
		}

		protected override void OnLoaded()
		{
			this.PopulateFields();
			base.OnLoaded();
		}

		private void ViewModel_PropertyChanged(object? Sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(KycProcessViewModel.Fields))
				this.PopulateFields();
		}

		private void PopulateFields()
		{
			if (this.BindingContext is not KycProcessViewModel Vm)
				return;

			this.FieldsLayout.Clear();

			foreach (KycField Field in Vm.Fields)
			{
				View Input = this.CreateInput(Field);
				this.FieldsLayout.Add(Input);

				if (!string.IsNullOrEmpty(Field.Hint))
				{
					Label Hint = new()
					{
						Text = Field.Hint,
						Style = AppStyles.InfoLabel
					};

					Hint.SetBinding(IsVisibleProperty, new Binding(nameof(KycField.IsVisible), source: Field));
					this.FieldsLayout.Add(Hint);
				}
			}
		}

		private View CreateInput(KycField Field)
		{
			View Input;

			switch (Field.Type)
			{
				case "date":
					CompositeDatePicker Date = new()
					{
						Style = AppStyles.RegularCompositeDatePicker,
						Placeholder = Field.Placeholder ?? string.Empty,
						LabelText = Field.Label,
						Required = Field.Validation?.Required ?? false
					};
					Date.BindingContext = Field;
					Date.SetBinding(CompositeDatePicker.NullableDateProperty, new Binding(nameof(KycField.DateValue), BindingMode.TwoWay));
					Date.SetBinding(IsVisibleProperty, new Binding(nameof(KycField.IsVisible)));
					Input = Date;
					break;

				case "picker":
					CompositePicker Picker = new()
					{
						Style = AppStyles.RegularCompositePicker,
						Placeholder = Field.Placeholder ?? string.Empty,
						LabelText = Field.Label,
						Required = Field.Validation?.Required ?? false,
						ItemsSource = Field.Options,
						ItemDisplayBinding = new Binding(nameof(KycOption.Label))
					};
					Picker.BindingContext = Field;
					Picker.SetBinding(CompositePicker.SelectedItemProperty, new Binding(nameof(KycField.SelectedOption), BindingMode.TwoWay));
					Picker.SetBinding(IsVisibleProperty, new Binding(nameof(KycField.IsVisible)));
					Input = Picker;
					break;

				case "boolean":
					Switch Boolean = new();
					Boolean.BindingContext = Field;
					Boolean.SetBinding(Switch.IsToggledProperty, new Binding(nameof(KycField.BoolValue), BindingMode.TwoWay));
					Boolean.SetBinding(IsVisibleProperty, new Binding(nameof(KycField.IsVisible)));
					Input = new StackLayout
					{
						Children = { new Label { Text = Field.Label }, Boolean }
					};
					Input.BindingContext = Field;
					break;

				default:
					CompositeEntry Entry = new()
					{
						Style = AppStyles.RegularCompositeEntry,
						Placeholder = Field.Placeholder ?? string.Empty,
						LabelText = Field.Label,
						Required = Field.Validation?.Required ?? false
					};
					Entry.BindingContext = Field;
					Entry.SetBinding(CompositeEntry.EntryDataProperty, new Binding(nameof(KycField.Value), BindingMode.TwoWay));
					Entry.SetBinding(IsVisibleProperty, new Binding(nameof(KycField.IsVisible)));
					Input = Entry;
					break;
			}

			return Input;
		}
	}
}
