using System.Globalization;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using Duration = Waher.Content.Duration;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// An input for a duration. Each row holds a number between buttons for less and more, and a unit. "Add unit" adds a row for durations such
	/// as "1 year 6 months", and "Backwards in time" makes the duration negative where the contract allows that.
	/// </summary>
	public class DurationInput : ContentView
	{
		private const int maxNumber = 9999;

		private static readonly DurationUnits[] allUnits =
		[
			DurationUnits.Years,
			DurationUnits.Months,
			DurationUnits.Days,
			DurationUnits.Hours,
			DurationUnits.Minutes,
			DurationUnits.Seconds
		];

		private readonly Label titleLabel;
		private readonly VerticalStackLayout rowsLayout;
		private readonly Label addUnitLabel;
		private readonly Grid negativeGrid;
		private readonly Switch negativeSwitch;
		private readonly List<UnitRow> rows = [];
		private bool isUpdating;

		/// <summary>
		/// The duration, or null while no number has been entered.
		/// </summary>
		public static readonly BindableProperty DurationValueProperty =
			BindableProperty.Create(nameof(DurationValue), typeof(Duration?), typeof(DurationInput), null, BindingMode.TwoWay,
				propertyChanged: OnDurationValueChanged);

		/// <summary>
		/// The label shown above the input.
		/// </summary>
		public static readonly BindableProperty LabelTextProperty =
			BindableProperty.Create(nameof(LabelText), typeof(string), typeof(DurationInput), string.Empty,
				propertyChanged: (Bindable, OldValue, NewValue) =>
					((DurationInput)Bindable).titleLabel.IsVisible = !string.IsNullOrEmpty(NewValue as string));

		/// <summary>
		/// If the duration may go backwards in time.
		/// </summary>
		public static readonly BindableProperty AllowNegativeProperty =
			BindableProperty.Create(nameof(AllowNegative), typeof(bool), typeof(DurationInput), false,
				propertyChanged: (Bindable, OldValue, NewValue) => ((DurationInput)Bindable).UpdateNegativeVisibility());

		/// <summary>
		/// If the value is accepted. When it isn't, the inputs are marked.
		/// </summary>
		public static readonly BindableProperty IsValidProperty =
			BindableProperty.Create(nameof(IsValid), typeof(bool), typeof(DurationInput), true,
				propertyChanged: (Bindable, OldValue, NewValue) => ((DurationInput)Bindable).UpdateValidity());

		/// <summary>
		/// Creates an input for a duration.
		/// </summary>
		public DurationInput()
		{
			this.titleLabel = new Label
			{
				Style = AppStyles.SectionTitleLabel
			};
			this.titleLabel.SetBinding(Label.TextProperty, new Binding(nameof(this.LabelText), source: this));
			this.titleLabel.IsVisible = false;

			this.rowsLayout = new VerticalStackLayout
			{
				Spacing = AppStyles.SmallSpacing
			};

			this.addUnitLabel = new Label
			{
				Text = ServiceRef.Localizer[nameof(AppResources.AddDuration)],
				FontFamily = "HaasGroteskMedium",
				FontSize = 13,
				MinimumHeightRequest = 48,
				VerticalTextAlignment = TextAlignment.Center,
				HorizontalOptions = LayoutOptions.Start
			};
			this.addUnitLabel.SetDynamicResource(Label.TextColorProperty, "ContentAccessWL");
			TapGestureRecognizer AddUnitTap = new();
			AddUnitTap.Tapped += (Sender, Args) => this.AddRow(null, null);
			this.addUnitLabel.GestureRecognizers.Add(AddUnitTap);

			this.negativeSwitch = new Switch
			{
				VerticalOptions = LayoutOptions.Center
			};
			this.negativeSwitch.Toggled += (Sender, Args) => this.UpdateDuration();

			this.negativeGrid = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = GridLength.Star },
					new ColumnDefinition { Width = GridLength.Auto }
				},
				IsVisible = false
			};
			this.negativeGrid.Add(new Label
			{
				Text = ServiceRef.Localizer[nameof(AppResources.NegateDuration)],
				Style = AppStyles.InfoLabel,
				VerticalOptions = LayoutOptions.Center
			}, 0, 0);
			this.negativeGrid.Add(this.negativeSwitch, 1, 0);

			this.Content = new VerticalStackLayout
			{
				Spacing = AppStyles.SmallSpacing,
				Children =
				{
					this.titleLabel,
					this.rowsLayout,
					this.addUnitLabel,
					this.negativeGrid
				}
			};

			this.AddRow(null, null);
		}

		/// <summary>
		/// The duration, or null while no number has been entered.
		/// </summary>
		public Duration? DurationValue
		{
			get => (Duration?)this.GetValue(DurationValueProperty);
			set => this.SetValue(DurationValueProperty, value);
		}

		/// <summary>
		/// The label shown above the input.
		/// </summary>
		public string LabelText
		{
			get => (string)this.GetValue(LabelTextProperty);
			set => this.SetValue(LabelTextProperty, value);
		}

		/// <summary>
		/// If the duration may go backwards in time.
		/// </summary>
		public bool AllowNegative
		{
			get => (bool)this.GetValue(AllowNegativeProperty);
			set => this.SetValue(AllowNegativeProperty, value);
		}

		/// <summary>
		/// If the value is accepted. When it isn't, the inputs are marked.
		/// </summary>
		public bool IsValid
		{
			get => (bool)this.GetValue(IsValidProperty);
			set => this.SetValue(IsValidProperty, value);
		}

		/// <summary>
		/// Shows a duration set from outside, unless it came from the input itself.
		/// </summary>
		private static void OnDurationValueChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			DurationInput Input = (DurationInput)Bindable;

			if (!Input.isUpdating)
				Input.ShowDuration(NewValue as Duration?);
		}

		/// <summary>
		/// Fills the rows from a duration: one row for each unit that isn't zero.
		/// </summary>
		/// <param name="Value">The duration, or null to show one empty row.</param>
		private void ShowDuration(Duration? Value)
		{
			this.isUpdating = true;
			try
			{
				this.rows.Clear();
				this.rowsLayout.Clear();

				if (Value is Duration D)
				{
					foreach (DurationUnits Unit in allUnits)
					{
						int Number = GetNumber(D, Unit);
						if (Number != 0)
							this.AddRow(Unit, Number);
					}

					this.negativeSwitch.IsToggled = D.Negation;
				}
				else
					this.negativeSwitch.IsToggled = false;

				if (this.rows.Count == 0)
					this.AddRow(null, null);

				this.UpdateNegativeVisibility();
			}
			finally
			{
				this.isUpdating = false;
			}
		}

		/// <summary>
		/// Adds a row. Without a unit, it gets the first unit no other row uses.
		/// </summary>
		/// <param name="Unit">The unit, or null for the first free one.</param>
		/// <param name="Number">The number, or null for an empty row.</param>
		private void AddRow(DurationUnits? Unit, int? Number)
		{
			DurationUnits[] Free = this.GetFreeUnits(null);
			if (Free.Length == 0)
				return;

			UnitRow Row = new(this, Unit ?? (this.rows.Count == 0 ? DurationUnits.Days : Free[0]), Number);
			Row.SetValid(this.IsValid);
			this.rows.Add(Row);
			this.rowsLayout.Add(Row.View);

			this.UpdateRows();
		}

		/// <summary>
		/// Removes a row, and updates the duration.
		/// </summary>
		/// <param name="Row">The row to remove.</param>
		private void RemoveRow(UnitRow Row)
		{
			this.rows.Remove(Row);
			this.rowsLayout.Remove(Row.View);

			if (this.rows.Count == 0)
				this.AddRow(null, null);

			this.UpdateRows();
			this.UpdateDuration();
		}

		/// <summary>
		/// Updates what each row offers: the units no other row uses, and the remove button when there is more than one row.
		/// </summary>
		private void UpdateRows()
		{
			foreach (UnitRow Row in this.rows)
				Row.Update(this.GetFreeUnits(Row), this.rows.Count > 1);

			this.addUnitLabel.IsVisible = this.rows.Count < allUnits.Length;
		}

		/// <summary>
		/// Gets the units no row uses, apart from a given row.
		/// </summary>
		/// <param name="Except">A row whose unit counts as free, or null.</param>
		/// <returns>The free units, in order.</returns>
		private DurationUnits[] GetFreeUnits(UnitRow? Except)
		{
			return [.. allUnits.Where(Unit => !this.rows.Any(Row => Row != Except && Row.Unit == Unit))];
		}

		/// <summary>
		/// Marks the rows when the duration isn't accepted.
		/// </summary>
		private void UpdateValidity()
		{
			foreach (UnitRow Row in this.rows)
				Row.SetValid(this.IsValid);
		}

		/// <summary>
		/// Shows "Backwards in time" when allowed, or when the duration already is negative.
		/// </summary>
		private void UpdateNegativeVisibility()
		{
			this.negativeGrid.IsVisible = this.AllowNegative || this.negativeSwitch.IsToggled;
		}

		/// <summary>
		/// Works out the duration from the rows. It is null while no number has been entered.
		/// </summary>
		private void UpdateDuration()
		{
			if (this.isUpdating)
				return;

			Duration Result = Duration.Zero;
			bool HasNumber = false;

			foreach (UnitRow Row in this.rows)
			{
				if (Row.Number is int Number)
				{
					Result = SetNumber(Result, Row.Unit, Number);
					HasNumber = true;
				}
			}

			Result.Negation = HasNumber && this.negativeSwitch.IsToggled;

			this.isUpdating = true;
			try
			{
				this.DurationValue = HasNumber ? Result : null;
			}
			finally
			{
				this.isUpdating = false;
			}
		}

		/// <summary>
		/// Gets the number of a unit in a duration.
		/// </summary>
		private static int GetNumber(Duration Value, DurationUnits Unit)
		{
			return Unit switch
			{
				DurationUnits.Years => Value.Years,
				DurationUnits.Months => Value.Months,
				DurationUnits.Days => Value.Days,
				DurationUnits.Hours => Value.Hours,
				DurationUnits.Minutes => Value.Minutes,
				_ => (int)Value.Seconds
			};
		}

		/// <summary>
		/// Sets the number of a unit in a duration.
		/// </summary>
		private static Duration SetNumber(Duration Value, DurationUnits Unit, int Number)
		{
			switch (Unit)
			{
				case DurationUnits.Years:
					Value.Years = Number;
					break;
				case DurationUnits.Months:
					Value.Months = Number;
					break;
				case DurationUnits.Days:
					Value.Days = Number;
					break;
				case DurationUnits.Hours:
					Value.Hours = Number;
					break;
				case DurationUnits.Minutes:
					Value.Minutes = Number;
					break;
				default:
					Value.Seconds = Number;
					break;
			}

			return Value;
		}

		/// <summary>
		/// Gets the name of a unit, for the unit picker.
		/// </summary>
		private static string GetUnitName(DurationUnits Unit)
		{
			return ServiceRef.Localizer[Unit.ToString() + "Capitalized"];
		}

		/// <summary>
		/// One row of the input: buttons for less and more around a number, and a unit.
		/// </summary>
		private sealed class UnitRow
		{
			private readonly DurationInput owner;
			private readonly Entry entry;
			private readonly Border numberField;
			private readonly Border unitField;
			private readonly Picker unitPicker;
			private readonly Label removeLabel;
			private DurationUnits[] units = [];
			private bool isSyncing;

			/// <summary>
			/// Creates a row.
			/// </summary>
			/// <param name="Owner">The input the row belongs to.</param>
			/// <param name="Unit">The unit.</param>
			/// <param name="Number">The number, or null for an empty row.</param>
			public UnitRow(DurationInput Owner, DurationUnits Unit, int? Number)
			{
				this.owner = Owner;
				this.Unit = Unit;
				this.Number = Number;

				this.entry = new Entry
				{
					Keyboard = Keyboard.Numeric,
					Text = Number?.ToString(CultureInfo.CurrentCulture) ?? string.Empty,
					Placeholder = "0",
					VerticalOptions = LayoutOptions.Center
				};
				this.entry.TextChanged += this.Entry_TextChanged;

				this.entry.HorizontalTextAlignment = TextAlignment.Center;
				this.numberField = new Border
				{
					Style = AppStyles.RegularCompositeEntryBorder,
					Padding = new Thickness(4, 0),
					WidthRequest = 64,
					Content = this.entry
				};

				this.unitPicker = new Picker
				{
					VerticalOptions = LayoutOptions.Center
				};
				this.unitPicker.SelectedIndexChanged += this.UnitPicker_SelectedIndexChanged;
				this.unitField = new Border
				{
					Style = AppStyles.RegularCompositeEntryBorder,
					Content = this.unitPicker
				};

				this.removeLabel = new Label
				{
					Text = "✕",
					Style = AppStyles.InfoLabel,
					VerticalOptions = LayoutOptions.Center,
					HorizontalTextAlignment = TextAlignment.Center,
					VerticalTextAlignment = TextAlignment.Center,
					MinimumWidthRequest = 48,
					MinimumHeightRequest = 48
				};
				TapGestureRecognizer RemoveTap = new();
				RemoveTap.Tapped += (Sender, Args) => this.owner.RemoveRow(this);
				this.removeLabel.GestureRecognizers.Add(RemoveTap);

				// Less, the number, more, and the unit, as one row.
				Grid Row = new()
				{
					ColumnDefinitions =
					{
						new ColumnDefinition { Width = GridLength.Auto },
						new ColumnDefinition { Width = GridLength.Auto },
						new ColumnDefinition { Width = GridLength.Auto },
						new ColumnDefinition { Width = GridLength.Star },
						new ColumnDefinition { Width = GridLength.Auto }
					},
					ColumnSpacing = AppStyles.SmallSpacing
				};
				Row.Add(CreateStepButton("−", () => this.Step(-1)), 0, 0);
				Row.Add(this.numberField, 1, 0);
				Row.Add(CreateStepButton("+", () => this.Step(1)), 2, 0);
				Row.Add(this.unitField, 3, 0);
				Row.Add(this.removeLabel, 4, 0);

				this.View = Row;
			}

			/// <summary>
			/// Creates a square button that makes the number one less or one more.
			/// </summary>
			/// <param name="Text">The button's sign.</param>
			/// <param name="OnTap">What the button does.</param>
			/// <returns>The button.</returns>
			private static Border CreateStepButton(string Text, Action OnTap)
			{
				Label Sign = new()
				{
					Text = Text,
					FontFamily = "HaasGroteskMedium",
					FontSize = 20,
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center
				};
				Sign.SetDynamicResource(Label.TextColorProperty, "ContentPrimaryWL");

				Border Button = new()
				{
					WidthRequest = 48,
					HeightRequest = 48,
					Padding = 0,
					StrokeThickness = 1,
					StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
					VerticalOptions = LayoutOptions.Center,
					Content = Sign
				};
				Button.SetDynamicResource(Border.StrokeProperty, "InputFieldsSurfaceStroke");
				Button.SetDynamicResource(VisualElement.BackgroundColorProperty, "SurfaceElevation1WL");

				TapGestureRecognizer Tap = new();
				Tap.Tapped += (Sender, Args) => OnTap();
				Button.GestureRecognizers.Add(Tap);

				return Button;
			}

			/// <summary>
			/// Makes the number one less or one more, never below zero.
			/// </summary>
			/// <param name="Change">-1 or 1.</param>
			private void Step(int Change)
			{
				this.Number = Math.Clamp((this.Number ?? 0) + Change, 0, maxNumber);

				this.isSyncing = true;
				this.entry.Text = this.Number.Value.ToString(CultureInfo.CurrentCulture);
				this.isSyncing = false;

				this.owner.UpdateDuration();
			}

			/// <summary>
			/// Marks the row when the duration isn't accepted.
			/// </summary>
			/// <param name="IsValid">If the duration is accepted.</param>
			public void SetValid(bool IsValid)
			{
				string Stroke = IsValid ? "InputFieldsSurfaceStroke" : "InputFieldsContentDangerv500";
				this.numberField.SetDynamicResource(Border.StrokeProperty, Stroke);
				this.unitField.SetDynamicResource(Border.StrokeProperty, Stroke);
			}

			/// <summary>
			/// The view of the row.
			/// </summary>
			public Grid View { get; }

			/// <summary>
			/// The row's unit.
			/// </summary>
			public DurationUnits Unit { get; private set; }

			/// <summary>
			/// The row's number, or null while it is empty.
			/// </summary>
			public int? Number { get; private set; }

			/// <summary>
			/// Updates the units the row offers, and if it can be removed.
			/// </summary>
			/// <param name="Units">The units the row may use.</param>
			/// <param name="CanRemove">If the row can be removed.</param>
			public void Update(DurationUnits[] Units, bool CanRemove)
			{
				this.isSyncing = true;
				try
				{
					this.units = Units;
					this.unitPicker.ItemsSource = Units.Select(GetUnitName).ToList();
					this.unitPicker.SelectedIndex = Array.IndexOf(Units, this.Unit);
					this.removeLabel.IsVisible = CanRemove;
				}
				finally
				{
					this.isSyncing = false;
				}
			}

			private void Entry_TextChanged(object? Sender, TextChangedEventArgs e)
			{
				if (this.isSyncing)
					return;

				string Digits = new([.. (e.NewTextValue ?? string.Empty).Where(char.IsDigit)]);
				if (Digits != (e.NewTextValue ?? string.Empty))
				{
					this.entry.Text = Digits;
					return;
				}

				this.Number = int.TryParse(Digits, NumberStyles.None, CultureInfo.InvariantCulture, out int Number)
					? Math.Min(Number, maxNumber)
					: null;

				this.owner.UpdateDuration();
			}

			private void UnitPicker_SelectedIndexChanged(object? Sender, EventArgs e)
			{
				if (this.isSyncing || this.unitPicker.SelectedIndex < 0 || this.unitPicker.SelectedIndex >= this.units.Length)
					return;

				this.Unit = this.units[this.unitPicker.SelectedIndex];
				this.owner.UpdateRows();
				this.owner.UpdateDuration();
			}
		}
	}
}
