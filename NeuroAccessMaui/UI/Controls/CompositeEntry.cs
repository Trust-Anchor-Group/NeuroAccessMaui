using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Path = Microsoft.Maui.Controls.Shapes.Path;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// A customizable entry control that allows setting views on the left and right sides of the entry.
	/// </summary>
	public class CompositeEntry : ContentView
	{
		#region Bindable Properties

		/// <summary>
		/// Bindable property for the view displayed on the left side of the entry.
		/// </summary>
		public static readonly BindableProperty LeftViewProperty = BindableProperty.Create(
			 nameof(LeftView),
			 typeof(View),
			 typeof(CompositeEntry),
			 propertyChanged: OnLeftViewPropertyChanged);

		/// <summary>
		/// Gets or sets the view displayed on the left side of the entry.
		/// </summary>
		public View LeftView
		{
			get => (View)this.GetValue(LeftViewProperty);
			set => this.SetValue(LeftViewProperty, value);
		}

		/// <summary>
		/// Bindable property for the view displayed on the right side of the entry.
		/// </summary>
		public static readonly BindableProperty RightViewProperty = BindableProperty.Create(
			 nameof(RightView),
			 typeof(View),
			 typeof(CompositeEntry),
			 propertyChanged: OnRightViewPropertyChanged);

		/// <summary>
		/// Gets or sets the view displayed on the right side of the entry.
		/// </summary>
		public View RightView
		{
			get => (View)this.GetValue(RightViewProperty);
			set => this.SetValue(RightViewProperty, value);
		}

		/// <summary>
		/// Bindable property for the text displayed in the entry.
		/// </summary>
		public static readonly BindableProperty EntryDataProperty = BindableProperty.Create(
			 nameof(EntryData),
			 typeof(string),
			 typeof(CompositeEntry),
			 default(string),
			 BindingMode.TwoWay);

		/// <summary>
		/// Gets or sets the text displayed in the entry.
		/// </summary>
		public string EntryData
		{
			get => (string)this.GetValue(EntryDataProperty);
			set => this.SetValue(EntryDataProperty, value);
		}

		/// <summary>
		/// Bindable property for the placeholder text displayed when the entry is empty.
		/// </summary>
		public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(
			 nameof(Placeholder),
			 typeof(string),
			 typeof(CompositeEntry),
			 default(string));

		/// <summary>
		/// Gets or sets the placeholder text displayed when the entry is empty.
		/// </summary>
		public string Placeholder
		{
			get => (string)this.GetValue(PlaceholderProperty);
			set => this.SetValue(PlaceholderProperty, value);
		}

		/// <summary>
		/// Bindable property indicating whether the entry is used for password input.
		/// </summary>
		public static readonly BindableProperty IsPasswordProperty = BindableProperty.Create(
			 nameof(IsPassword),
			 typeof(bool),
			 typeof(CompositeEntry),
			 false);

		/// <summary>
		/// Gets or sets a value indicating whether the entry is used for password input.
		/// </summary>
		public bool IsPassword
		{
			get => (bool)this.GetValue(IsPasswordProperty);
			set => this.SetValue(IsPasswordProperty, value);
		}

		/// <summary>
		/// Bindable property for the style applied to the entry.
		/// </summary>
		public static readonly BindableProperty EntryStyleProperty = BindableProperty.Create(
			 nameof(EntryStyle),
			 typeof(Style),
			 typeof(CompositeEntry));

		/// <summary>
		/// Gets or sets the style applied to the entry.
		/// </summary>
		public Style EntryStyle
		{
			get => (Style)this.GetValue(EntryStyleProperty);
			set => this.SetValue(EntryStyleProperty, value);
		}

		/// <summary>
		/// Bindable property for the keyboard type used by the entry.
		/// </summary>
		public static readonly BindableProperty KeyboardProperty = BindableProperty.Create(
			 nameof(Keyboard),
			 typeof(Keyboard),
			 typeof(CompositeEntry),
			 Keyboard.Default);

		/// <summary>
		/// Gets or sets the keyboard type used by the entry.
		/// </summary>
		public Keyboard Keyboard
		{
			get => (Keyboard)this.GetValue(KeyboardProperty);
			set => this.SetValue(KeyboardProperty, value);
		}

		/// <summary>
		/// Bindable property for the text color of the entry.
		/// </summary>
		public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
			 nameof(TextColor),
			 typeof(Color),
			 typeof(CompositeEntry),
			 Colors.Black);

		/// <summary>
		/// Gets or sets the text color of the entry.
		/// </summary>
		public Color TextColor
		{
			get => (Color)this.GetValue(TextColorProperty);
			set => this.SetValue(TextColorProperty, value);
		}

		/// <summary>
		/// Bindable property for the style applied to the border.
		/// </summary>
		public static readonly BindableProperty BorderStyleProperty = BindableProperty.Create(
			  nameof(BorderStyle),
			  typeof(Style),
			  typeof(CompositeEntry),
			  default(Style));

		/// <summary>
		/// Gets or sets the style applied to the border.
		/// </summary>
		public Style BorderStyle
		{
			get => (Style)this.GetValue(BorderStyleProperty);
			set => this.SetValue(BorderStyleProperty, value);
		}

		/// <summary>
		/// Bindable property indicating whether the entry data is valid.
		/// </summary>
		public static readonly BindableProperty IsValidProperty = BindableProperty.Create(
			 nameof(IsValid),
			 typeof(bool),
			 typeof(CompositeEntry),
			 true,
			 propertyChanged: (bindable, oldValue, newValue) =>
			 {
				 CompositeEntry control = (CompositeEntry)bindable;
				 control.OnPropertyChanged(nameof(control.ShowValidationLabel));
			 });

		/// <summary>
		/// Gets or sets a value indicating whether the entry data is valid.
		/// </summary>
		public bool IsValid
		{
			get => (bool)this.GetValue(IsValidProperty);
			set => this.SetValue(IsValidProperty, value);
		}

		/// <summary>
		/// Bindable property indicating whether spell check is enabled.
		/// </summary>
		public static readonly BindableProperty IsSpellCheckEnabledProperty = BindableProperty.Create(
			 nameof(IsSpellCheckEnabled),
			 typeof(bool),
			 typeof(CompositeEntry),
			 true);

		/// <summary>
		/// Gets or sets a value indicating whether spell check is enabled.
		/// </summary>
		public bool IsSpellCheckEnabled
		{
			get => (bool)this.GetValue(IsSpellCheckEnabledProperty);
			set => this.SetValue(IsSpellCheckEnabledProperty, value);
		}

		/// <summary>
		/// Bindable property indicating whether text prediction is enabled.
		/// </summary>
		public static readonly BindableProperty IsTextPredictionEnabledProperty = BindableProperty.Create(
			 nameof(IsTextPredictionEnabled),
			 typeof(bool),
			 typeof(CompositeEntry),
			 true);

		/// <summary>
		/// Gets or sets a value indicating whether text prediction is enabled.
		/// </summary>
		public bool IsTextPredictionEnabled
		{
			get => (bool)this.GetValue(IsTextPredictionEnabledProperty);
			set => this.SetValue(IsTextPredictionEnabledProperty, value);
		}

		/// <summary>
		/// Bindable property indicating whether Read Only is enabled.
		/// </summary>
		public static readonly BindableProperty IsReadOnlyProperty = BindableProperty.Create(
			 nameof(IsReadOnly),
			 typeof(bool),
			 typeof(CompositeEntry),
			 false);

		/// <summary>
		/// Gets or sets a value indicating whether text prediction is enabled.
		/// </summary>
		public bool IsReadOnly
		{
			get => (bool)this.GetValue(IsReadOnlyProperty);
			set => this.SetValue(IsReadOnlyProperty, value);
		}

		// Define the new BackgroundColor property
		public static new readonly BindableProperty BackgroundColorProperty = BindableProperty.Create(
			 nameof(BackgroundColor),
			 typeof(Color),
			 typeof(CompositeEntry),
			 Colors.Transparent);

		public new Color BackgroundColor
		{
			get => (Color)this.GetValue(BackgroundColorProperty);
			set => this.SetValue(BackgroundColorProperty, value);
		}

		/// <summary>
		/// Bindable property for the label above the entry
		/// </summary>
		public static readonly BindableProperty LabelDataProperty = BindableProperty.Create(
			 nameof(LabelData),
			 typeof(string),
			 typeof(CompositeEntry),
			 default(string),
			 BindingMode.TwoWay);

		/// <summary>
		/// Gets or sets the text displayed in the entry.
		/// </summary>
		public string LabelData
		{
			get => (string)this.GetValue(LabelDataProperty);
			set => this.SetValue(LabelDataProperty, value);
		}

		/// <summary>
		/// Bindable property for the label above the entry
		/// </summary>
		public static readonly BindableProperty ValidationDataProperty = BindableProperty.Create(
			 nameof(ValidationData),
			 typeof(string),
			 typeof(CompositeEntry),
			 default(string),
			 BindingMode.TwoWay);

		/// <summary>
		/// Gets or sets the text displayed in the entry.
		/// </summary>
		public string ValidationData
		{
			get => (string)this.GetValue(ValidationDataProperty);
			set => this.SetValue(ValidationDataProperty, value);
		}


		/// <summary>
		/// Bindable property for the style applied to the label above the entry.
		/// </summary>
		public static readonly BindableProperty LabelStyleProperty = BindableProperty.Create(
			 nameof(LabelStyle),
			 typeof(Style),
			 typeof(CompositeEntry));

		/// <summary>
		/// Gets or sets the style applied to the label above the entry.
		/// </summary>
		public Style LabelStyle
		{
			get => (Style)this.GetValue(LabelStyleProperty);
			set => this.SetValue(LabelStyleProperty, value);
		}

		#endregion

		#region Fields

		private readonly Entry innerEntry;

		private readonly Grid outerGrid;
		private readonly Grid validationGrid;
		private readonly Grid entryGrid;
		private readonly Border border;
		private readonly Label topLabel;
		private readonly Label validationLabel;

		/// <summary>
		/// If the validation label should be shown.
		/// </summary>
		public bool ShowValidationLabel => !this.IsValid && !string.IsNullOrEmpty(this.validationLabel.Text);

		/// <summary>
		/// If the top label should be shown.
		/// </summary>
		public bool ShowTopLabel => !string.IsNullOrEmpty(this.topLabel.Text);
		#endregion

		#region Events

		/// <summary>
		/// Occurs when the user finalizes the text in an entry with the return key.
		/// </summary>
		public event EventHandler? Completed;

		/// <summary>
		/// Occurs when the text of the entry changes.
		/// </summary>
		public event EventHandler<TextChangedEventArgs>? TextChanged;

		/// <summary>
		/// Occurs when the entry gains focus.
		/// </summary>
		public new event EventHandler<FocusEventArgs>? Focused;

		/// <summary>
		/// Occurs when the entry loses focus.
		/// </summary>
		public new event EventHandler<FocusEventArgs>? Unfocused;

		#endregion

		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="CompositeEntry"/> class.
		/// </summary>
		public CompositeEntry()
		{
			// Initialize the inner Entry
			this.innerEntry = new Entry
			{
				VerticalOptions = LayoutOptions.Center,
				HorizontalOptions = LayoutOptions.Fill,
				BackgroundColor = Colors.Transparent
			};

			// Set up bindings for the Entry
			this.innerEntry.SetBinding(Entry.TextProperty, new Binding(nameof(this.EntryData), source: this, mode: BindingMode.TwoWay));
			this.innerEntry.SetBinding(Entry.PlaceholderProperty, new Binding(nameof(this.Placeholder), source: this));
			this.innerEntry.SetBinding(Entry.IsPasswordProperty, new Binding(nameof(this.IsPassword), source: this));
			this.innerEntry.SetBinding(Entry.KeyboardProperty, new Binding(nameof(this.Keyboard), source: this));
			this.innerEntry.SetBinding(Entry.StyleProperty, new Binding(nameof(this.EntryStyle), source: this));
			this.innerEntry.SetBinding(Entry.TextColorProperty, new Binding(nameof(this.TextColor), source: this));
			this.innerEntry.SetBinding(InputView.IsSpellCheckEnabledProperty, new Binding(nameof(this.IsSpellCheckEnabled), source: this));
			this.innerEntry.SetBinding(InputView.IsTextPredictionEnabledProperty, new Binding(nameof(this.IsTextPredictionEnabled), source: this));
			this.innerEntry.SetBinding(Entry.IsReadOnlyProperty, new Binding(nameof(this.IsReadOnly), source: this));

			// Handle events
			this.innerEntry.Completed += this.OnEntryCompleted;
			this.innerEntry.TextChanged += this.OnEntryTextChanged;
			this.innerEntry.Focused += this.OnEntryFocused;
			this.innerEntry.Unfocused += this.OnEntryUnfocused;

			// Initialize the top label
			this.topLabel = new Label
			{
				HorizontalOptions = LayoutOptions.Fill,
				VerticalTextAlignment = TextAlignment.Center,
				BackgroundColor = Colors.Transparent,
				IsVisible = false
			};

			this.topLabel.SetBinding(Label.TextProperty, new Binding(nameof(this.LabelData), source: this));
			this.topLabel.SetBinding(Label.StyleProperty, new Binding(nameof(this.LabelStyle), source: this));
			this.topLabel.SetBinding(Label.IsVisibleProperty, new Binding(nameof(this.ShowTopLabel), source: this));

			this.topLabel.PropertyChanged += (sender, e) =>
			{
				if (e.PropertyName == nameof(Label.Text))
				{
					this.OnPropertyChanged(nameof(this.ShowTopLabel));
				}
			};

			// Initialize the entry Grid
			this.entryGrid = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = GridLength.Auto },    // Left view
					new ColumnDefinition { Width = GridLength.Star },    // Entry
					new ColumnDefinition { Width = GridLength.Auto }     // Right view
                },
				ColumnSpacing = AppStyles.SmallSpacing
			};
			// Add the Entry to the Grid
			this.entryGrid.Add(this.innerEntry, 1, 0);

			// Set Left and Right Views if they are not null
			if (this.LeftView != null)
			{
				this.entryGrid.Add(this.LeftView, 0, 0);
			}
			if (this.RightView != null)
			{
				this.entryGrid.Add(this.RightView, 2, 0);
			}

			// Initialize the Border
			this.border = new Border
			{
				Content = this.entryGrid,
				BackgroundColor = Colors.Transparent
			};

			this.border.SetBinding(Border.StyleProperty, new Binding(nameof(this.BorderStyle), source: this));
			this.border.SetBinding(Border.BackgroundColorProperty, new Binding(nameof(this.BackgroundColor), source: this));
			// Initialize the validation label
			this.validationLabel = new Label
			{
				VerticalTextAlignment = TextAlignment.Center,
				BackgroundColor = Colors.Transparent,
			};

			this.validationLabel.PropertyChanged += (sender, e) =>
			{
				if (e.PropertyName == nameof(Label.Text))
				{
					this.OnPropertyChanged(nameof(this.ShowValidationLabel));
				}
			};

			this.validationLabel.SetBinding(Label.TextProperty, new Binding(nameof(this.ValidationData), source: this));
			this.validationLabel.SetBinding(Label.TextColorProperty, new Binding(nameof(this.TextColor), source: this));

			Path ValidationPath = new Path()
			{
				Data = Geometries.ErrorPath,
				WidthRequest = 20,
				HeightRequest = 20,
				VerticalOptions = LayoutOptions.Center,
				HorizontalOptions = LayoutOptions.Center,
				Aspect = Microsoft.Maui.Controls.Stretch.Fill
			};
			ValidationPath.SetBinding(Path.FillProperty, new Binding(nameof(this.TextColor), source: this));

			// Initialize the validation Grid
			this.validationGrid = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = GridLength.Auto },
					new ColumnDefinition { Width = GridLength.Star }
				},
				ColumnSpacing = AppStyles.SmallSpacing,
				HorizontalOptions = LayoutOptions.Center,
			};

			this.validationGrid.Add(ValidationPath, 0, 0);
			this.validationGrid.Add(this.validationLabel, 1, 0);
			this.validationGrid.SetBinding(Grid.IsVisibleProperty, new Binding(nameof(this.ShowValidationLabel), source: this));

			VerticalStackLayout outerLayout = new VerticalStackLayout
			{
				Spacing = AppStyles.SmallSpacing,
				Children =
				{
					this.topLabel,
					this.border,
					this.validationGrid
				}
			};
			// Initialize the outer Grid
			this.outerGrid = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Auto }
				},
			};

			this.outerGrid.RowSpacing = AppStyles.SmallSpacing;

			this.outerGrid.Add(this.topLabel, 0, 0);
			this.outerGrid.Add(this.border, 0, 1);
			this.outerGrid.Add(this.validationGrid, 0, 2);

			// Set the Content of the control
			this.Content = outerLayout;
		}

		#endregion

		#region Property Overrides

		/// <summary>
		/// Gets or sets a user-defined value to uniquely identify the element.
		/// This is forwarded to the inner Entry control.
		/// </summary>
		public new string StyleId
		{
			get => this.innerEntry.StyleId;
			set => this.innerEntry.StyleId = value;
		}

		public new string AutomationId
		{
			get => this.innerEntry.AutomationId;
			set => this.innerEntry.AutomationId = value;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Focuses the Entry.
		/// </summary>
		public new bool Focus()
		{
			return this.innerEntry.Focus();
		}

		/// <summary>
		/// Unfocuses the Entry.
		/// </summary>
		public new void Unfocus()
		{
			this.innerEntry.Unfocus();
		}

		#endregion

		#region Property Changed Callbacks

		/// <summary>
		/// Called when the <see cref="LeftView"/> property changes.
		/// </summary>
		/// <param name="bindable">The bindable object.</param>
		/// <param name="oldValue">The old value of the property.</param>
		/// <param name="newValue">The new value of the property.</param>
		protected static void OnLeftViewPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			CompositeEntry control = (CompositeEntry)bindable;
			control.UpdateLeftView((View)oldValue, (View)newValue);
		}

		/// <summary>
		/// Updates the left view in the grid.
		/// </summary>
		/// <param name="oldView">The old view.</param>
		/// <param name="newView">The new view.</param>
		private void UpdateLeftView(View oldView, View newView)
		{
			if (oldView != null)
			{
				this.entryGrid.Remove(oldView);
			}
			if (newView != null)
			{
				this.entryGrid.Add(newView, 0, 0);
			}
		}

		/// <summary>
		/// Called when the <see cref="RightView"/> property changes.
		/// </summary>
		/// <param name="bindable">The bindable object.</param>
		/// <param name="oldValue">The old value of the property.</param>
		/// <param name="newValue">The new value of the property.</param>
		private static void OnRightViewPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			CompositeEntry control = (CompositeEntry)bindable;
			control.UpdateRightView((View)oldValue, (View)newValue);
		}

		/// <summary>
		/// Updates the right view in the grid.
		/// </summary>
		/// <param name="oldView">The old view.</param>
		/// <param name="newView">The new view.</param>
		private void UpdateRightView(View oldView, View newView)
		{
			if (oldView != null)
			{
				this.entryGrid.Remove(oldView);
			}
			if (newView != null)
			{
				this.entryGrid.Add(newView, 2, 0);
			}
		}
		#endregion

		#region Event Handlers

		private void OnEntryTextChanged(object? sender, TextChangedEventArgs e)
		{
			TextChanged?.Invoke(this, e);
		}

		/// <summary>
		/// Handles the <see cref="Entry.Completed"/> event.
		/// Executes the <see cref="Completed"/> event.
		/// </summary>
		private void OnEntryCompleted(object? sender, EventArgs e)
		{
			Completed?.Invoke(this, e);
		}

		/// <summary>
		/// Handles the <see cref="VisualElement.Focused"/> event.
		/// Executes the <see cref="FocusedCommand"/> if it can execute.
		/// </summary>
		private void OnEntryFocused(object? sender, FocusEventArgs e)
		{
			Focused?.Invoke(this, e);
		}

		/// <summary>
		/// Handles the <see cref="VisualElement.Unfocused"/> event.
		/// Executes the <see cref="UnfocusedCommand"/> if it can execute.
		/// </summary>
		private void OnEntryUnfocused(object? sender, FocusEventArgs e)
		{
			Unfocused?.Invoke(this, e);
		}

		#endregion
	}
}
