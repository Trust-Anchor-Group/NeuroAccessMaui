using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Popups.Duration;
using Duration = Waher.Content.Duration;
using Path = Microsoft.Maui.Controls.Shapes.Path;

namespace NeuroAccessMaui.UI.Controls
{
	public partial class CompositeDurationPicker : ContentView
	{
		#region Fields
		private readonly VerticalStackLayout durationsContainer;
		private readonly Label titleLabel;
		private readonly Label descriptionLabel;
		private readonly CheckBox negateCheckBox;
		private ObservableCollection<DurationUnits> durationUnits = [
			DurationUnits.Years,
			DurationUnits.Months,
			DurationUnits.Days,
			DurationUnits.Hours,
			DurationUnits.Minutes,
			DurationUnits.Seconds
		];
		#endregion

		#region Constructors
		/// <summary>
		/// Constructor for the CompositeDurationPicker. Initializes the layout and the components needed to add additional duration units.
		/// </summary>
		public CompositeDurationPicker()
		{
			Grid MainGrid = new()
			{
				RowDefinitions =
				{
					new RowDefinition { Height = GridLength.Auto }, // Top Label
					new RowDefinition { Height = GridLength.Auto }, // Date Pickers Vertical Stack Layout
					new RowDefinition { Height = GridLength.Auto }, // Add new Duration Button
					new RowDefinition { Height = GridLength.Auto }  // Negate Field
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
			this.descriptionLabel.Margin = 0;
			TopTextGrid.Add(this.descriptionLabel, 0, 1);

			MainGrid.Add(TopTextGrid, 0, 0);

			// Date Pickers Vertical Stack Layout
			this.durationsContainer = [];
			this.durationsContainer.Spacing = AppStyles.SmallSpacing;
			MainGrid.Add(this.durationsContainer, 0, 1);

			//// Add new Duration Button
			//TextButton AddDurationButton = new()
			//{
			//	LabelData = "Add Duration",
			//	Style = AppStyles.TertiaryButton,
			//	HorizontalOptions = LayoutOptions.Fill,
			//	VerticalOptions = LayoutOptions.Center
			//};

			TemplatedButton AddDurationButton = new()
			{
				Content = new Border
				{
					Style = AppStyles.TransparentTemplateButtonBorder,
					HorizontalOptions = LayoutOptions.Fill,
					VerticalOptions = LayoutOptions.Fill,
					Margin = 0,
					Content = new Grid
					{
						ColumnDefinitions =
						{
							new ColumnDefinition { Width = GridLength.Star },
							new ColumnDefinition { Width = GridLength.Auto }
						},
						Children =
						{
							new Label
							{
								Text = ServiceRef.Localizer[nameof(AppResources.AddDuration)],
								Style = AppStyles.TransparentTemplateButtonLabel,
								HorizontalOptions = LayoutOptions.Start
							},
							new Path
							{
								Data = Geometries.ArrowRightPath,
								Style = AppStyles.TransparentTemplateButtonPath,
								VerticalOptions = LayoutOptions.Center,
								HorizontalOptions = LayoutOptions.End,
								WidthRequest = 18,
								HeightRequest = 18,
								Aspect = Stretch.Uniform
							}
						}
					}
				}
			};

			AddDurationButton.Clicked += (sender, args) => this.AddDurationButton_Clicked();
			MainGrid.Add(AddDurationButton, 0, 2);

			// Button Grid
			Grid NegateGrid = new()
			{
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = GridLength.Auto },
					new ColumnDefinition { Width = GridLength.Auto },
				},
				HorizontalOptions = LayoutOptions.Center
			};

			// Negate Label
			Label NegateLabel = new()
			{
				Text = ServiceRef.Localizer[nameof(AppResources.NegateDuration)],
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				Style = AppStyles.InfoLabel
			};
			NegateGrid.Add(NegateLabel, 0, 0);

			// Negate CheckBox
			this.negateCheckBox = new()
			{
				IsChecked = false,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				Color = AppColors.ButtonAccessPrimarybg
			};

			this.negateCheckBox.CheckedChanged += (sender, args) => this.UpdateDuration();

			NegateGrid.Add(this.negateCheckBox, 1, 0);

			MainGrid.Add(NegateGrid, 0, 3);

			this.Content = MainGrid;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Fill the durations container with the duration entries based on the Duration Binding
		/// </summary>
		void PopulateDurationsContainer(List<DurationUnits?> PreviousUnits)
		{
			List<DurationUnits> AvaliableUnits = [
				DurationUnits.Years,
				DurationUnits.Months,
				DurationUnits.Days,
				DurationUnits.Hours,
				DurationUnits.Minutes,
				DurationUnits.Seconds
			];

			if (this.DurationValue.Years != 0)
			{
				if (!PreviousUnits.Contains(DurationUnits.Years))
					this.AddUnit(DurationUnits.Years, this.DurationValue.Years.ToString(CultureInfo.InvariantCulture));

				AvaliableUnits.Remove(DurationUnits.Years);
			}

			if (this.DurationValue.Months != 0)
			{
				if (!PreviousUnits.Contains(DurationUnits.Months))
					this.AddUnit(DurationUnits.Months, this.DurationValue.Months.ToString(CultureInfo.InvariantCulture));

				AvaliableUnits.Remove(DurationUnits.Months);
			}

			if (this.DurationValue.Days != 0)
			{
				if (!PreviousUnits.Contains(DurationUnits.Days))
					this.AddUnit(DurationUnits.Days, this.DurationValue.Days.ToString(CultureInfo.InvariantCulture));

				AvaliableUnits.Remove(DurationUnits.Days);
			}

			if (this.DurationValue.Hours != 0)
			{
				if (!PreviousUnits.Contains(DurationUnits.Hours))
					this.AddUnit(DurationUnits.Hours, this.DurationValue.Hours.ToString(CultureInfo.InvariantCulture));

				AvaliableUnits.Remove(DurationUnits.Hours);
			}

			if (this.DurationValue.Minutes != 0)
			{
				if (!PreviousUnits.Contains(DurationUnits.Minutes))
					this.AddUnit(DurationUnits.Minutes, this.DurationValue.Minutes.ToString(CultureInfo.InvariantCulture));

				AvaliableUnits.Remove(DurationUnits.Minutes);
			}

			if (this.DurationValue.Seconds != 0)
			{
				if (!PreviousUnits.Contains(DurationUnits.Seconds))
					this.AddUnit(DurationUnits.Seconds, this.DurationValue.Seconds.ToString(CultureInfo.InvariantCulture));

				AvaliableUnits.Remove(DurationUnits.Seconds);
			}

			this.negateCheckBox.IsChecked = this.DurationValue.Negation;

			this.durationUnits = AvaliableUnits.ToObservableCollection<DurationUnits>();
		}

		/// <summary>
		/// Add a duration entry to the durations container
		/// </summary>
		/// <param name="Unit"></param>
		private void AddUnit(DurationUnits Unit, string Value)
		{
			// Setup the Composite Entry
			CompositeEntry DurationEntry = new()
			{
				Style = AppStyles.RegularCompositeEntry,
				Margin = 0,
				Padding = 0,
				EntryData = (Value == "0") ? "" : Value
			};

			 // Add the Unit Label to the left of the CompositeEntry
			DurationLabel UnitLabel = new()
			{
				Text = ServiceRef.Localizer[Unit.ToString() + "Capitalized"],
				HorizontalTextAlignment = TextAlignment.Start,
				VerticalTextAlignment = TextAlignment.Center,
				Style = AppStyles.SectionTitleLabel,
				LineBreakMode = LineBreakMode.NoWrap,
				Unit = Unit,
			};

			DurationEntry.LeftView = UnitLabel;

			this.durationUnits.Remove(Unit); // Remove used units

			DurationEntry.SetBinding(CompositeEntry.IsValidProperty, new Binding(nameof(this.IsValid), source: this));

			DurationEntry.TextChanged += (sender, args) => this.UpdateDuration();

			DurationEntry.Keyboard = Keyboard.Numeric;


			// Add delete button to the right of the CompositeEntry
			ImageButton DeleteButton = new()
			{
				Style = AppStyles.ImageOnlyButton,
				PathData = Geometries.CancelPath,
				VerticalOptions = LayoutOptions.Center,
				HorizontalOptions = LayoutOptions.End,
				Command = new RelayCommand(() => this.DeleteUnit(Unit, DurationEntry))
			};

			DurationEntry.RightView = DeleteButton;

			// Add the CompositeEntry to the Vertical Stack Layout
			this.durationsContainer.Add(DurationEntry);

			// Sort the entries by unit
			List<CompositeEntry> SortedDurationEntries = [.. this.durationsContainer.Children
										.OfType<CompositeEntry>()
										.OrderBy(x => (x.LeftView as DurationLabel)?.Unit)];

			this.durationsContainer.Clear();
			foreach (CompositeEntry Entry in SortedDurationEntries)
				MainThread.BeginInvokeOnMainThread(() => this.durationsContainer.Add(Entry));

			MainThread.BeginInvokeOnMainThread(async () => {
				await Task.Delay(500);
				DurationEntry.Focus();
			});
		  }

		/// <summary>
		/// Delete a duration entry from the durations container and add the unit back to the available units
		/// </summary>
		private void DeleteUnit(DurationUnits Unit, CompositeEntry DurationView)
		{
			if (!this.durationUnits.Contains(Unit)) // Prevent duplicates from being added
				this.durationUnits.Add(Unit); // Add back the removed unit
			this.durationUnits = this.durationUnits.OrderBy(x => x).ToObservableCollection<DurationUnits>();

			this.durationsContainer.Remove(DurationView);

			this.UpdateDuration();
		}

		/// <summary>
		/// Open QR Popup
		/// </summary>
		/// <returns></returns>
		[RelayCommand]
		public async Task<DurationUnits?> OpenDurationPopup()
		{
			DurationPopupViewModel ViewModel = new(this.durationUnits);
			DurationUnits? Result = await ServiceRef.PopupService.PushAsync<DurationPopup, DurationPopupViewModel, DurationUnits?>(ViewModel);

			return Result;
		}

		/// <summary>
		/// Update the duration value based on the entries in the durations container
		/// </summary>
		void UpdateDuration()
		{
			Duration NewDuration = new();
			CultureInfo Culture = CultureInfo.InvariantCulture;

			foreach (CompositeEntry DurationEntry in this.durationsContainer.Children.Cast<CompositeEntry>())
			{
				DurationLabel UnitLabel = (DurationLabel)DurationEntry.LeftView;

				string Data = DurationEntry.EntryData;

				string FilteredText = new(Data.Where(char.IsDigit).ToArray());
				if (Data != FilteredText)
				{
					DurationEntry.EntryData = FilteredText;
					Data = FilteredText; // Remove invalid characters
				}

				if (string.IsNullOrEmpty(Data))
					Data = "0";

				switch (UnitLabel.Unit)
				{
					case DurationUnits.Years:
						NewDuration.Years = int.Parse(Data, Culture);
						break;
					case DurationUnits.Months:
						NewDuration.Months = int.Parse(Data, Culture);
						break;
					case DurationUnits.Days:
						NewDuration.Days = int.Parse(Data, Culture);
						break;
					case DurationUnits.Hours:
						NewDuration.Hours = int.Parse(Data, Culture);
						break;
					case DurationUnits.Minutes:
						NewDuration.Minutes = int.Parse(Data, Culture);
						break;
					case DurationUnits.Seconds:
						NewDuration.Seconds = int.Parse(Data, Culture);
						break;
				}
			}

			NewDuration.Negation = this.negateCheckBox.IsChecked;

			this.DurationValue = NewDuration;
		}

		#endregion

		#region Events

		/// <summary>
		/// Event handler for the Add Duration Button.
		/// </summary>
		async void AddDurationButton_Clicked()
		{
			if (this.durationUnits.Count == 0)
				return; // No unit available

			DurationUnits? DurationUnit = await this.OpenDurationPopup();

			if (DurationUnit is not DurationUnits Unit)
				return; // No unit selected or popup canceled

			this.AddUnit(Unit, "");
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

		/// <summary>
		/// Bindable property for the text displayed in the entry.
		/// </summary>
		public static readonly BindableProperty DurationValueProperty = BindableProperty.Create(
			nameof(DurationValue),
			typeof(Duration),
			typeof(CompositeDurationPicker),
			default(Duration),
			BindingMode.TwoWay,
			propertyChanged: OnDurationChanged);

		// IsValid Property
		public static readonly BindableProperty IsValidProperty =
			 BindableProperty.Create(nameof(IsValid), typeof(bool), typeof(CompositeDurationPicker), true);

		/// <summary>
		/// Gets or sets the validity of the DurationPicker
		/// </summary>
		public bool IsValid
		{
			get => (bool)this.GetValue(IsValidProperty);
			set => this.SetValue(IsValidProperty, value);
		}

		/// <summary>
		/// Event handler for the DurationValue property changed event.
		/// </summary>
		/// <param name="bindable"></param>
		/// <param name="oldValue"></param>
		/// <param name="newValue"></param>
		static void OnDurationChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is CompositeDurationPicker Picker)
			{
				List<DurationUnits?> PreviousUnits = [.. Picker.durationsContainer.Children
					.OfType<CompositeEntry>()
					.Select(x => (x.LeftView as DurationLabel)?.Unit)];

				Picker.PopulateDurationsContainer(PreviousUnits);
			}
		}

		/// <summary>
		/// Gets or sets the text displayed in the entry.
		/// </summary>
		public Duration DurationValue
		{
			get => (Duration)this.GetValue(DurationValueProperty);
			set => this.SetValue(DurationValueProperty, value);
		}
		#endregion
	}

	#region Helper classes

	/// <summary>
	/// Duration units enumeration
	/// </summary>
	public enum DurationUnits
	{
		Years,
		Months,
		Days,
		Hours,
		Minutes,
		Seconds,
	}

	/// <summary>
	/// Custom class to be able to access the unit used in and entry
	/// </summary>
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
	#endregion
}
