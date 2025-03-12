using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.Input;
using Waher.Script.Functions.Runtime;

namespace NeuroAccessMaui.UI.Controls
{
	public partial class CompositeDurationPicker : ContentView
	{
		#region Fields
		private VerticalStackLayout durationsContainer;
		private Label topLabel;
		private ObservableCollection<DurationUnits> durationUnits = new(){
			DurationUnits.Years,
			DurationUnits.Months,
			DurationUnits.Weeks,
			DurationUnits.Days,
			DurationUnits.Hours,
			DurationUnits.Minutes,
			DurationUnits.Seconds
		};
		private CompositePicker unitPicker;

		#endregion

		#region Constructors
		public CompositeDurationPicker()
		{
			Grid MainGrid = new()
			{
				RowDefinitions =
				{
					new RowDefinition { Height = GridLength.Auto }, // Top Label
					new RowDefinition { Height = GridLength.Auto }, // Date Pickers Vertical Stack Layout
					new RowDefinition { Height = GridLength.Auto }, // Add new Duration Button
				}
			};

			Border MainBorder = new()
			{
				Style = AppStyles.RoundedBorder,
				BackgroundColor = Color.FromHex("#AAAAAA"),
				Content = MainGrid
			};

			// Top Label
			this.topLabel = new();
			this.topLabel.SetBinding(Label.StyleProperty, new Binding(nameof(this.TopLabelStyle), source: this));
			this.topLabel.SetBinding(Label.TextProperty, new Binding(nameof(this.TopLabelText), source: this));
			MainGrid.Add(this.topLabel, 0, 0);

			// Date Pickers Vertical Stack Layout
			this.durationsContainer = [];
			MainGrid.Add(this.durationsContainer, 0, 1);

			// Button Grid
			Grid ButtonGrid = new()
			{
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = GridLength.Auto },
					new ColumnDefinition { Width = GridLength.Auto },
					new ColumnDefinition { Width = GridLength.Auto },
				}
			};

			// Duration Unit Selection
			this.unitPicker = new()
			{
				ItemsSource = this.durationUnits,
				WidthRequest = 100,
				HorizontalOptions = LayoutOptions.Start,
				VerticalOptions = LayoutOptions.Center,
				Style = AppStyles.RegularCompositePicker
			};

			this.unitPicker.Picker.SelectedIndex = 0;

			ButtonGrid.Add(this.unitPicker, 0, 0);

			// Add new Duration Button
			TextButton AddDurationButton = new()
			{
				LabelData = "Add Duration",
				Style = AppStyles.FilledTextButton,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center
			};
			AddDurationButton.Clicked += (sender, args) => this.AddDurationButton_Clicked();
			ButtonGrid.Add(AddDurationButton, 1, 0);

			// Negate Label
			Label NegateLabel = new()
			{
				Text = "Negate",
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center
			};
			ButtonGrid.Add(NegateLabel, 2, 0);

			// Negate CheckBox
			CheckBox NegateCheckBox = new()
			{
				IsChecked = false,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center
			};
			ButtonGrid.Add(NegateCheckBox, 2, 0);

			MainGrid.Add(ButtonGrid, 0, 2);

			this.Content = MainBorder;
		}

		#endregion

		#region Methods

		void AddDurationButton_Clicked()
		{
			if (this.unitPicker.Picker.SelectedIndex == -1)
				return; // No unit selected

			// Setup the inner CompositeInputView
			CompositeInputView DurationView = new()
			{
				Style = AppStyles.BaseCompositeInputView,
				Margin = AppStyles.SmallLeftMargins + AppStyles.SmallRightMargins
			};

			DurationUnits Unit = (DurationUnits)this.unitPicker.Picker.SelectedItem;

			// Add the Unit Label to the left of the CompositeInputView
			Label UnitLabel = new()
			{
				Text = Unit.ToString(),
				HorizontalOptions = LayoutOptions.Start,
				VerticalOptions = LayoutOptions.Center,
			};

			DurationView.LeftView = UnitLabel;

			MainThread.BeginInvokeOnMainThread(() => this.durationUnits.Remove(Unit)); // Remove used units

			// Add the time entry to the middle of the CompositeInputView
			CompositeEntry DurationEntry = new()
			{
				Style = AppStyles.RegularCompositeEntry
			};

			DurationView.CenterView = DurationEntry;

			// Add delete button to the right of the CompositeInputView
			TextButton DeleteButton = new()
			{
				LabelData = "X",
				Style = AppStyles.FilledTextButton,
				HorizontalOptions = LayoutOptions.End,
				VerticalOptions = LayoutOptions.Center,
				Command = new AsyncRelayCommand(async () => await this.DeleteUnitAsync(Unit, DurationView), new AsyncRelayCommandOptions { })
			};

			DurationView.RightView = DeleteButton;

			// Add the CompositeInputView to the Vertical Stack Layout
			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.durationsContainer.Add(DurationView);
				List<CompositeInputView> SortedDurationEntries = [.. this.durationsContainer.Children
					.OfType<CompositeInputView>()
					.OrderBy(x => LabelToUnit((Label)x.LeftView))];

				this.durationsContainer.Clear();
				foreach (CompositeInputView Entry in SortedDurationEntries)
					this.durationsContainer.Add(Entry);
			});
		}

		private static DurationUnits LabelToUnit(Label Label)
		{
			return (DurationUnits)Enum.Parse(typeof(DurationUnits), Label.Text);
		}


		private async Task DeleteUnitAsync(DurationUnits Unit, CompositeInputView DurationView)
		{
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				if (!this.durationUnits.Contains(Unit)) // Prevent duplicates from being added
					this.durationUnits.Add(Unit); // Add back the removed unit
				if (this.durationUnits.Count == 1) // If there were 0 available units, before adding a unit back
					this.unitPicker.Picker.SelectedIndex = 0; // Make picker show the added Unit
				this.durationUnits = this.durationUnits.OrderBy(x => x).ToObservableCollection<DurationUnits>(); // Sort the values
				this.unitPicker.ItemsSource = this.durationUnits;
				this.durationsContainer.Remove(DurationView);
			});
		}

		#endregion

		#region Bindable Properties

		/// <summary>
		/// Bindable property for the style applied to the top label.
		/// </summary>
		public static readonly BindableProperty TopLabelStyleProperty =
			BindableProperty.Create(nameof(TopLabelStyle), typeof(Style), typeof(CompositeInputView));

		/// <summary>
		/// Gets or sets the style applied to the label above the entry.
		/// </summary>
		public Style TopLabelStyle
		{
			get => (Style)this.GetValue(TopLabelStyleProperty);
			set => this.SetValue(TopLabelStyleProperty, value);
		}

		/// <summary>
		/// Bindable property for the text of the top label.
		/// </summary>
		public static readonly BindableProperty TopLabelTextProperty =
			BindableProperty.Create(nameof(TopLabelText), typeof(string), typeof(CompositeInputView));

		/// <summary>
		/// Gets or sets the text applied to the top label.
		/// </summary>
		public string TopLabelText
		{
			get => (string)this.GetValue(TopLabelTextProperty);
			set => this.SetValue(TopLabelTextProperty, value);
		}

		#endregion
	}

	enum DurationUnits
	{
		Years,
		Months,
		Weeks,
		Days,
		Hours,
		Minutes,
		Seconds,
	}

}
