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
using Path = Microsoft.Maui.Controls.Shapes.Path;

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
		#endregion

		#region Constructors
		public CompositeDurationPicker()
		{
			Grid MainGrid = new()
			{
				RowDefinitions =
				{
					new RowDefinition { Height = GridLength.Auto }, // Top Label
					new RowDefinition { Height = GridLength.Auto }, // Description Label
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
			TopTextGrid.Add(this.descriptionLabel, 0, 1);

			MainGrid.Add(TopTextGrid, 0, 0);

			// Date Pickers Vertical Stack Layout
			this.durationsContainer = [];
			this.durationsContainer.Spacing = AppStyles.SmallSpacing;
			MainGrid.Add(this.durationsContainer, 0, 2);

			//// Add new Duration Button
			//TextButton AddDurationButton = new()
			//{
			//	LabelData = "Add Duration",
			//	Style = AppStyles.TertiaryButton,
			//	HorizontalOptions = LayoutOptions.FillAndExpand,
			//	VerticalOptions = LayoutOptions.Center
			//};

			TemplatedButton AddDurationButton = new()
			{
				Content = new Border
				{
					Style = AppStyles.TransparentTemplateButtonBorder,
					HorizontalOptions = LayoutOptions.FillAndExpand,
					VerticalOptions = LayoutOptions.FillAndExpand,
					Margin = 0,
					Content = new Grid
					{
						ColumnDefinitions =
						{
							new ColumnDefinition { Width = GridLength.Auto },
							new ColumnDefinition { Width = GridLength.Auto }
						},
						Children =
						{
							new Label
							{
								Text = "Add Duration", //TODO LOCALIZE
								Style = AppStyles.TransparentTemplateButtonLabel,
								HorizontalOptions = LayoutOptions.Start
							},
							new Path
							{
								Data = Geometries.ArrowRightPath,
								Style = AppStyles.TransparentTemplateButtonPath,
								VerticalOptions = LayoutOptions.Center
							}
						}
					}
				}
			};

			AddDurationButton.Clicked += (sender, args) => this.AddDurationButton_Clicked();
			MainGrid.Add(AddDurationButton, 0, 3);

			// Button Grid
			Grid NegateGrid = new()
			{
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = GridLength.Auto },
					new ColumnDefinition { Width = GridLength.Auto },
				}
			};

			// Negate Label
			Label NegateLabel = new()
			{
				Text = "Negate duration:", // TODO: Localize
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center
			};
			NegateGrid.Add(NegateLabel, 0, 0);

			// Negate CheckBox
			CheckBox NegateCheckBox = new()
			{
				IsChecked = false,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center
			};
			NegateGrid.Add(NegateCheckBox, 1, 0);

			MainGrid.Add(NegateGrid, 0, 4);

			this.Content = MainGrid;
		}

		#endregion

		#region Methods

		async void AddDurationButton_Clicked()
		{
			if (this.durationUnits.Count == 0)
				return; // No unit selected

			DurationUnits? DurationUnit = await this.OpenDurationPopup();

			if (DurationUnit is not DurationUnits Unit)
				return; // No unit selected or popup canceled

			// Setup the inner CompositeInputView
			CompositeInputView DurationView = new()
			{
				Style = AppStyles.BaseCompositeInputView,
				Margin = 0,
				Padding = 0,
			};

			// Add the Unit Label to the left of the CompositeInputView
			DurationLabel UnitLabel = new()
			{
				Text = Unit.ToString() + ":", // TODO LOCALIZE
				HorizontalTextAlignment = TextAlignment.Center,
				VerticalTextAlignment = TextAlignment.Center,
				Style = AppStyles.SectionTitleLabel,
				LineBreakMode = LineBreakMode.NoWrap,
				WidthRequest = 60,
				Unit = Unit,
			};

			DurationView.LeftView = UnitLabel;

			this.durationUnits.Remove(Unit); // Remove used units

			// Add the time entry to the middle of the CompositeInputView
			CompositeEntry DurationEntry = new()
			{
				Style = AppStyles.RegularCompositeEntry,
				BorderStrokeShape = new Rectangle(),
				Padding = 0,
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
				Command = new RelayCommand(() => this.DeleteUnit(Unit, DurationView))
			};

			DurationView.RightView = DeleteButton;

			// Add the CompositeInputView to the Vertical Stack Layout
			this.durationsContainer.Add(DurationView);

			// Sort the entries by unit
			List<CompositeInputView> SortedDurationEntries = [.. this.durationsContainer.Children
				.OfType<CompositeInputView>()
				.OrderBy(x => (x.LeftView as DurationLabel)?.Unit)];

			this.durationsContainer.Clear();
			foreach (CompositeInputView Entry in SortedDurationEntries)
				this.durationsContainer.Add(Entry);
		}

		private void DeleteUnit(DurationUnits Unit, CompositeInputView DurationView)
		{
			if (!this.durationUnits.Contains(Unit)) // Prevent duplicates from being added
				this.durationUnits.Add(Unit); // Add back the removed unit

			this.durationUnits = this.durationUnits.OrderBy(x => x).ToObservableCollection<DurationUnits>();
			this.durationsContainer.Remove(DurationView);
		}

		/// <summary>
		/// Open QR Popup
		/// </summary>
		/// <returns></returns>
		[RelayCommand]
		public async Task<DurationUnits?> OpenDurationPopup()
		{
			DurationPopupViewModel ViewModel = new(this.durationUnits);
			DurationUnits? Result = await ServiceRef.UiService.PushAsync<DurationPopup, DurationPopupViewModel, DurationUnits?>(ViewModel);

			return Result;
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

	#region Helper classes
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
	#endregion
}
