using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuroAccessMaui.UI.Controls
{
	public partial class CompositeDurationPicker : ContentView
	{
		#region Fields
		private VerticalStackLayout durationsContainer;
		private Label topLabel;

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
			this.durationsContainer = new();
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

			// Add new Duration Button
			TextButton AddDurationButton = new()
			{
				LabelData = "Add Duration",
				Style = AppStyles.FilledTextButton,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center
			};
			AddDurationButton.Clicked += async (sender, args) => await Task.Run(() => this.AddDurationButton_Clicked());
			ButtonGrid.Add(AddDurationButton, 0, 0);

			// Negate Label
			Label NegateLabel = new()
			{
				Text = "Negate",
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center
			};
			ButtonGrid.Add(NegateLabel, 1, 0);

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
			CompositeInputView DurationView = new();

			TextButton DeleteButton = new()
			{
				LabelData = "Delete",
				Style = AppStyles.FilledTextButton,
				HorizontalOptions = LayoutOptions.End,
				VerticalOptions = LayoutOptions.Center
			};

			DurationView.RightView = DeleteButton;

			DeleteButton.Clicked += (sender, args) =>
				MainThread.BeginInvokeOnMainThread(() =>
					this.durationsContainer.Remove(DurationView)
				);
			
			MainThread.BeginInvokeOnMainThread(() =>
				this.durationsContainer.Add(DurationView)
			);
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
}
