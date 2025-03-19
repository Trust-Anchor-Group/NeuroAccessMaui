using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.Input;
using HarfBuzzSharp;
using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Popups.Duration;
using Waher.Script.Functions.Runtime;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NeuroAccessMaui.UI.Controls
{
	public partial class CompositeDurationPicker : ContentView
	{
		#region Fields
		private VerticalStackLayout durationsContainer;
		private Label titleLabel;
		private Label descriptionLabel;
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
				},
				RowSpacing = AppStyles.SmallSpacing
			};

			// Top Label
			Grid TopTextGrid = new()
			{
				RowDefinitions =
				{
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Auto }
				},
				RowSpacing = AppStyles.SmallSpacing
			};

			this.titleLabel = new();
			this.titleLabel.SetBinding(Label.StyleProperty, new Binding(nameof(this.TitleLabelStyle), source: this));
			this.titleLabel.SetBinding(Label.TextProperty, new Binding(nameof(this.TitleLabelText), source: this));
			TopTextGrid.Add(this.titleLabel, 0, 0);

			this.descriptionLabel = new();
			this.descriptionLabel.SetBinding(Label.StyleProperty, new Binding(nameof(this.DescriptionLabelStyle), source: this));
			this.descriptionLabel.SetBinding(Label.TextProperty, new Binding(nameof(this.DescriptionLabelText), source: this));
			TopTextGrid.Add(this.descriptionLabel, 0, 1);

			MainGrid.Add(TopTextGrid, 0, 0);

			// Date Pickers Vertical Stack Layout
			this.durationsContainer = [];
			this.durationsContainer.Spacing = AppStyles.SmallSpacing;
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
			ButtonGrid.Add(NegateCheckBox, 3, 0);

			MainGrid.Add(ButtonGrid, 0, 2);

			this.Content = MainGrid;
		}

		#endregion

		#region Methods

		async void AddDurationButton_Clicked()
		{
			if (this.unitPicker.Picker.SelectedIndex == -1)
				return; // No unit selected

			await this.OpenDurationPopup();

			// Setup the inner CompositeInputView
			CompositeInputView DurationView = new()
			{
				Style = AppStyles.BaseCompositeInputView,
				Margin = 0,
				Padding = 0,
			};

			DurationUnits Unit = (DurationUnits)this.unitPicker.Picker.SelectedItem;

			// Add the Unit Label to the left of the CompositeInputView
			DurationLabel UnitLabel = new()
			{
				Text = Unit.ToString(),
				HorizontalOptions = LayoutOptions.Start,
				VerticalOptions = LayoutOptions.Center,
				Style = AppStyles.SectionTitleLabel,
				WidthRequest = 50,
				Unit = Unit
			};

			DurationView.LeftView = UnitLabel;

			MainThread.BeginInvokeOnMainThread(() => this.durationUnits.Remove(Unit)); // Remove used units

			// Add the time entry to the middle of the CompositeInputView
			CompositeEntry DurationEntry = new()
			{
				Style = AppStyles.RegularCompositeEntry,
				BorderStrokeShape = new Rectangle(),
				BorderShadow = new Shadow
				{
					Opacity = 0,
					Radius = 0,
				}
			};

			DurationEntry.Keyboard = Keyboard.Numeric;

			DurationView.CenterView = DurationEntry;

			// Add delete button to the right of the CompositeInputView
			ImageButton DeleteButton = new()
			{
				Style = AppStyles.ImageOnlyButton,
				PathData = Geometries.CancelPath,
				VerticalOptions = LayoutOptions.Center,
				HorizontalOptions = LayoutOptions.End,
				Command = new AsyncRelayCommand(async () => await this.DeleteUnitAsync(Unit, DurationView), new AsyncRelayCommandOptions { })
			};

			DurationView.RightView = DeleteButton;

			// Add the CompositeInputView to the Vertical Stack Layout
			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.durationsContainer.Add(DurationView);
				List<CompositeInputView> SortedDurationEntries = [.. this.durationsContainer.Children
					.OfType<CompositeInputView>()
					.OrderBy(x => (x.LeftView as DurationLabel)?.Unit)];

				this.durationsContainer.Clear();
				foreach (CompositeInputView Entry in SortedDurationEntries)
					this.durationsContainer.Add(Entry);
			});
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

		/// <summary>
		/// Open QR Popup
		/// </summary>
		/// <returns></returns>
		[RelayCommand]
		public async Task OpenDurationPopup()
		{
			if (this.durationUnits.Count == 0) return;

			//DurationPopup DurationPopup = new(this.durationUnits);
			DurationPopupViewModel ViewModel = new(this.durationUnits);
			DurationUnits? Result = await ServiceRef.UiService.PushAsync<DurationPopup, DurationPopupViewModel, DurationUnits?>(ViewModel);
			//string? Result = await ServiceRef.UiService.PushAsync<CheckPasswordPopup, CheckPasswordViewModel, string>(ViewModel);
			//await ServiceRef.UiService.PushAsync(DurationPopup);

			Console.WriteLine($"Result: {Result}");
		}

		#endregion

		#region Bindable Properties

		/// <summary>
		/// Bindable property for the style applied to the top label.
		/// </summary>
		public static readonly BindableProperty TitleLabelStyleProperty =
			BindableProperty.Create(nameof(TitleLabelStyle), typeof(Style), typeof(CompositeInputView));

		/// <summary>
		/// Gets or sets the style applied to the label above the entry.
		/// </summary>
		public Style TitleLabelStyle
		{
			get => (Style)this.GetValue(TitleLabelStyleProperty);
			set => this.SetValue(TitleLabelStyleProperty, value);
		}

		/// <summary>
		/// Bindable property for the text of the top label.
		/// </summary>
		public static readonly BindableProperty TitleLabelTextProperty =
			BindableProperty.Create(nameof(TitleLabelText), typeof(string), typeof(CompositeInputView));

		/// <summary>
		/// Gets or sets the text applied to the top label.
		/// </summary>
		public string TitleLabelText
		{
			get => (string)this.GetValue(TitleLabelTextProperty);
			set => this.SetValue(TitleLabelTextProperty, value);
		}

		/// <summary>
		/// Bindable property for the style applied to the description label.
		/// </summary>
		public static readonly BindableProperty DescriptionLabelStyleProperty =
			BindableProperty.Create(nameof(DescriptionLabelStyle), typeof(Style), typeof(CompositeInputView));

		/// <summary>
		/// Gets or sets the style applied to the description label.
		/// </summary>
		public Style DescriptionLabelStyle
		{
			get => (Style)this.GetValue(DescriptionLabelStyleProperty);
			set => this.SetValue(DescriptionLabelStyleProperty, value);
		}

		/// <summary>
		/// Bindable Property for the text of the description label.
		/// </summary>
		public static readonly BindableProperty DescriptionLabelTextProperty =
			BindableProperty.Create(nameof(DescriptionLabelText), typeof(string), typeof(CompositeInputView));

		/// <summary>
		/// Gets or sets the text applied to the description label.
		/// </summary>
		public string DescriptionLabelText
		{
			get => (string)this.GetValue(DescriptionLabelTextProperty);
			set => this.SetValue(DescriptionLabelTextProperty, value);
		}
		#endregion
	}



	public enum DurationUnits
	{
		Years,
		Months,
		Weeks,
		Days,
		Hours,
		Minutes,
		Seconds,
	}

	class DurationLabel : Label, IComparable
	{
		private DurationUnits unit;

		public DurationLabel() : base()
		{
		}

		public DurationUnits Unit
		{
			get => this.unit;
			set => this.unit = value;
		}

		public int CompareTo(object? other)
		{
			return this.unit.CompareTo((other as DurationLabel)?.Unit);
		}
	}

}
