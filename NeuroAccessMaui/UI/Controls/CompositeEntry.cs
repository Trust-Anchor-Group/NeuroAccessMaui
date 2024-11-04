using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.Services;
using PathShape = Microsoft.Maui.Controls.Shapes.Path;

namespace NeuroAccessMaui.UI.Controls
{
	public class CompositeEntry : ContentView
	{
		private readonly Border innerBorder;
		private readonly Grid innerGrid;
		private readonly PathShape innerPath;
		private readonly PathShape clickablePath;
		private readonly Entry innerEntry;

		/// <summary>
		/// Bindable property for the command to be executed when the Entry gains focus.
		/// </summary>
		public static readonly BindableProperty FocusedCommandProperty = BindableProperty.Create(
			 nameof(FocusedCommand),
			 typeof(ICommand),
			 typeof(CompositeEntry));

		/// <summary>
		/// Gets or sets the command to be executed when the Entry gains focus.
		/// </summary>
		public ICommand FocusedCommand
		{
			get => (ICommand)this.GetValue(FocusedCommandProperty);
			set => this.SetValue(FocusedCommandProperty, value);
		}

		/// <summary>
		/// Bindable property for the command to be executed when the Entry loses focus.
		/// </summary>
		public static readonly BindableProperty UnfocusedCommandProperty = BindableProperty.Create(
			 nameof(UnfocusedCommand),
			 typeof(ICommand),
			 typeof(CompositeEntry));

		/// <summary>
		/// Gets or sets the command to be executed when the Entry loses focus.
		/// </summary>
		public ICommand UnfocusedCommand
		{
			get => (ICommand)this.GetValue(UnfocusedCommandProperty);
			set => this.SetValue(UnfocusedCommandProperty, value);
		}

		/// <summary>
		/// Bindable property for the command to be executed when the clickable PathData is tapped.
		/// </summary>
		public static readonly BindableProperty PathClickedCommandProperty = BindableProperty.Create(
			 nameof(PathClickedCommand),
			 typeof(ICommand),
			 typeof(CompositeEntry));

		/// <summary>
		/// Gets or sets the command to be executed when the clickable PathData is tapped.
		/// </summary>
		public ICommand PathClickedCommand
		{
			get => (ICommand)this.GetValue(PathClickedCommandProperty);
			set => this.SetValue(PathClickedCommandProperty, value);
		}

		/// <summary>
		/// Bindable property for the clickable PathData to be displayed.
		/// </summary>
		public static readonly BindableProperty ClickablePathDataProperty = BindableProperty.Create(
			 nameof(ClickablePathData),
			 typeof(Geometry),
			 typeof(CompositeEntry),
			 defaultValue: null,
			 propertyChanged: (bindable, oldValue, newValue) =>
			 {
				 ((CompositeEntry)bindable).OnClickablePathDataPropertyChanged((Geometry)oldValue, (Geometry)newValue);
			 });

		/// <summary>
		/// Gets or sets the clickable PathData to be displayed.
		/// </summary>
		public Geometry ClickablePathData
		{
			get => (Geometry)this.GetValue(ClickablePathDataProperty);
			set => this.SetValue(ClickablePathDataProperty, value);
		}

		/// <summary>
		/// Bindable property for the Keyboard type.
		/// </summary>
		public static readonly BindableProperty KeyboardProperty = BindableProperty.Create(
			 nameof(Keyboard),
			 typeof(Keyboard),
			 typeof(CompositeEntry),
			 defaultValue: Keyboard.Default,
			 propertyChanged: (bindable, oldValue, newValue) =>
			 {
				 ((CompositeEntry)bindable).OnKeyboardPropertyChanged((Keyboard)oldValue, (Keyboard)newValue);
			 });

		/// <summary>
		/// Gets or sets the Keyboard type for the Entry.
		/// </summary>
		public Keyboard Keyboard
		{
			get => (Keyboard)this.GetValue(KeyboardProperty);
			set => this.SetValue(KeyboardProperty, value);
		}

		/// <summary>
		/// Bindable property for the border style.
		/// </summary>
		public static readonly BindableProperty BorderStyleProperty = BindableProperty.Create(
			 nameof(BorderStyle),
			 typeof(Style),
			 typeof(CompositeEntry),
			 defaultValue: null,
			 propertyChanged: (bindable, oldValue, newValue) =>
			 {
				 ((CompositeEntry)bindable).OnBorderStylePropertyChanged((Style)oldValue, (Style)newValue);
			 });

		/// <summary>
		/// Gets or sets the style for the Border.
		/// </summary>
		public Style BorderStyle
		{
			get => (Style)this.GetValue(BorderStyleProperty);
			set => this.SetValue(BorderStyleProperty, value);
		}

		/// <summary>
		/// Bindable property for the spacing between elements in the stack.
		/// </summary>
		public static readonly BindableProperty StackSpacingProperty = BindableProperty.Create(
			 nameof(StackSpacing),
			 typeof(double),
			 typeof(CompositeEntry),
			 defaultValue: 0.0,
			 propertyChanged: (bindable, oldValue, newValue) =>
			 {
				 ((CompositeEntry)bindable).OnStackSpacingPropertyChanged((double)oldValue, (double)newValue);
			 });

		/// <summary>
		/// Gets or sets the spacing between elements in the stack.
		/// </summary>
		public double StackSpacing
		{
			get => (double)this.GetValue(StackSpacingProperty);
			set => this.SetValue(StackSpacingProperty, value);
		}

		/// <summary>
		/// Bindable property for the PathData to be displayed.
		/// </summary>
		public static readonly BindableProperty PathDataProperty = BindableProperty.Create(
			 nameof(PathData),
			 typeof(Geometry),
			 typeof(CompositeEntry),
			 defaultValue: null,
			 propertyChanged: (bindable, oldValue, newValue) =>
			 {
				 ((CompositeEntry)bindable).OnPathDataPropertyChanged((Geometry)oldValue, (Geometry)newValue);
			 });

		/// <summary>
		/// Gets or sets the PathData to be displayed.
		/// </summary>
		public Geometry PathData
		{
			get => (Geometry)this.GetValue(PathDataProperty);
			set => this.SetValue(PathDataProperty, value);
		}

		/// <summary>
		/// Bindable property for the Path style.
		/// </summary>
		public static readonly BindableProperty PathStyleProperty = BindableProperty.Create(
			 nameof(PathStyle),
			 typeof(Style),
			 typeof(CompositeEntry),
			 defaultValue: null,
			 propertyChanged: (bindable, oldValue, newValue) =>
			 {
				 ((CompositeEntry)bindable).OnPathStylePropertyChanged((Style)oldValue, (Style)newValue);
			 });

		/// <summary>
		/// Gets or sets the style for the Path.
		/// </summary>
		public Style PathStyle
		{
			get => (Style)this.GetValue(PathStyleProperty);
			set => this.SetValue(PathStyleProperty, value);
		}

		/// <summary>
		/// Bindable property for the Entry data.
		/// </summary>
		public static readonly BindableProperty EntryDataProperty = BindableProperty.Create(
			 nameof(EntryData),
			 typeof(string),
			 typeof(CompositeEntry),
			 defaultValue: string.Empty,
			 defaultBindingMode: BindingMode.TwoWay);

		/// <summary>
		/// Gets or sets the data for the Entry.
		/// </summary>
		public string EntryData
		{
			get => (string)this.GetValue(EntryDataProperty);
			set => this.SetValue(EntryDataProperty, value);
		}

		/// <summary>
		/// Bindable property for the Entry hint (placeholder).
		/// </summary>
		public static readonly BindableProperty EntryHintProperty = BindableProperty.Create(
			 nameof(EntryHint),
			 typeof(string),
			 typeof(CompositeEntry),
			 defaultValue: string.Empty,
			 propertyChanged: (bindable, oldValue, newValue) =>
			 {
				 ((CompositeEntry)bindable).OnEntryHintPropertyChanged((string)oldValue, (string)newValue);
			 });

		/// <summary>
		/// Gets or sets the hint (placeholder) for the Entry.
		/// </summary>
		public string EntryHint
		{
			get => (string)this.GetValue(EntryHintProperty);
			set => this.SetValue(EntryHintProperty, value);
		}

		/// <summary>
		/// Bindable property for the Entry style.
		/// </summary>
		public static readonly BindableProperty EntryStyleProperty = BindableProperty.Create(
			 nameof(EntryStyle),
			 typeof(Style),
			 typeof(CompositeEntry),
			 defaultValue: null,
			 propertyChanged: (bindable, oldValue, newValue) =>
			 {
				 ((CompositeEntry)bindable).OnEntryStylePropertyChanged((Style)oldValue, (Style)newValue);
			 });

		/// <summary>
		/// Gets or sets the style for the Entry.
		/// </summary>
		public Style EntryStyle
		{
			get => (Style)this.GetValue(EntryStyleProperty);
			set => this.SetValue(EntryStyleProperty, value);
		}

		/// <summary>
		/// Bindable property for the return command.
		/// </summary>
		public static readonly BindableProperty ReturnCommandProperty = BindableProperty.Create(
			 nameof(ReturnCommand),
			 typeof(ICommand),
			 typeof(CompositeEntry),
			 defaultValue: null,
			 propertyChanged: (bindable, oldValue, newValue) =>
			 {
				 ((CompositeEntry)bindable).OnReturnCommandPropertyChanged((ICommand)oldValue, (ICommand)newValue);
			 });

		/// <summary>
		/// Gets or sets the command executed when the return key is pressed.
		/// </summary>
		public ICommand ReturnCommand
		{
			get => (ICommand)this.GetValue(ReturnCommandProperty);
			set => this.SetValue(ReturnCommandProperty, value);
		}

		/// <summary>
		/// Bindable property for IsPassword.
		/// </summary>
		public static readonly BindableProperty IsPasswordProperty = BindableProperty.Create(
			 nameof(IsPassword),
			 typeof(bool),
			 typeof(CompositeEntry),
			 defaultValue: false,
			 propertyChanged: (bindable, oldValue, newValue) =>
			 {
				 ((CompositeEntry)bindable).OnIsPasswordPropertyChanged((bool)oldValue, (bool)newValue);
			 });

		/// <summary>
		/// Gets or sets whether the Entry should mask the input.
		/// </summary>
		public bool IsPassword
		{
			get => (bool)this.GetValue(IsPasswordProperty);
			set => this.SetValue(IsPasswordProperty, value);
		}

		/// <summary>
		/// Bindable property for IsReadOnly.
		/// </summary>
		public static readonly BindableProperty IsReadOnlyProperty = BindableProperty.Create(
			 nameof(IsReadOnly),
			 typeof(bool),
			 typeof(CompositeEntry),
			 defaultValue: false,
			 propertyChanged: (bindable, oldValue, newValue) =>
			 {
				 ((CompositeEntry)bindable).OnIsReadOnlyPropertyChanged((bool)oldValue, (bool)newValue);
			 });

		/// <summary>
		/// Gets or sets whether the Entry is read-only.
		/// </summary>
		public bool IsReadOnly
		{
			get => (bool)this.GetValue(IsReadOnlyProperty);
			set => this.SetValue(IsReadOnlyProperty, value);
		}

		/// <summary>
		/// Bindable property for the Entry placeholder.
		/// </summary>
		public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(
			 nameof(Placeholder),
			 typeof(string),
			 typeof(CompositeEntry),
			 defaultValue: string.Empty,
			 propertyChanged: (bindable, oldValue, newValue) =>
			 {
				 ((CompositeEntry)bindable).OnPlaceholderPropertyChanged((string)oldValue, (string)newValue);
			 });

		/// <summary>
		/// Gets or sets the placeholder text for the Entry.
		/// </summary>
		public string Placeholder
		{
			get => (string)this.GetValue(PlaceholderProperty);
			set => this.SetValue(PlaceholderProperty, value);
		}

		/// <summary>
		/// Bindable property for the Entry text color.
		/// </summary>
		public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
			 nameof(TextColor),
			 typeof(Color),
			 typeof(CompositeEntry),
			 defaultValue: Colors.Black,
			 propertyChanged: (bindable, oldValue, newValue) =>
			 {
				 ((CompositeEntry)bindable).OnTextColorPropertyChanged((Color)oldValue, (Color)newValue);
			 });

		/// <summary>
		/// Gets or sets the text color for the Entry.
		/// </summary>
		public Color TextColor
		{
			get => (Color)this.GetValue(TextColorProperty);
			set => this.SetValue(TextColorProperty, value);
		}

		/// <summary>
		/// Occurs when the user finalizes the text in an entry with the return key.
		/// </summary>
		public event EventHandler? Completed;

		/// <summary>
		/// Embedded Entry control.
		/// </summary>
		public Entry Entry => this.innerEntry;

		/// <summary>
		/// Embedded Border encapsulating the Entry.
		/// </summary>
		public Border Border => this.innerBorder;

		/// <summary>
		/// Initializes a new instance of the <see cref="CompositeEntry"/> class.
		/// </summary>
		public CompositeEntry()
		{
			this.innerPath = new PathShape
			{
				VerticalOptions = LayoutOptions.Center,
				IsVisible = this.PathData != null,
				HeightRequest = 24,
				WidthRequest = 24,
				Aspect = Stretch.Uniform
			};

			this.clickablePath = new PathShape
			{
				VerticalOptions = LayoutOptions.Center,
				IsVisible = this.ClickablePathData != null,
				HeightRequest = 24,
				WidthRequest = 24,
				Aspect = Stretch.Uniform
			};

			TapGestureRecognizer tapGestureRecognizer = new TapGestureRecognizer();
			tapGestureRecognizer.Tapped += (s, e) =>
			{
				if (this.PathClickedCommand?.CanExecute(null) ?? false)
					this.PathClickedCommand.Execute(null);
			};
			this.clickablePath.GestureRecognizers.Add(tapGestureRecognizer);

			this.innerEntry = new Entry
			{
				VerticalOptions = LayoutOptions.Center,
				HorizontalOptions = LayoutOptions.Fill,
				BackgroundColor = this.BackgroundColor,
				TextColor = this.TextColor
			};

			this.innerEntry.Completed += this.InnerEntry_Completed;

			this.innerGrid = new Grid
			{
				HorizontalOptions = LayoutOptions.Fill,
				ColumnDefinitions =
					 {
						  new ColumnDefinition { Width = GridLength.Auto },
						  new ColumnDefinition { Width = GridLength.Star },
						  new ColumnDefinition { Width = GridLength.Auto },
					 },
				RowDefinitions =
					 {
						  new RowDefinition { Height = GridLength.Auto },
					 }
			};

			this.innerGrid.Add(this.innerPath, 0, 0);
			this.innerGrid.Add(this.innerEntry, 1, 0);
			this.innerGrid.Add(this.clickablePath, 2, 0);

			this.innerBorder = new Border
			{
				StrokeThickness = 2,
				Content = this.innerGrid,
				BackgroundColor = this.BackgroundColor
			};

			this.Content = this.innerBorder;

			// Bind Entry properties to the CompositeEntry properties
			this.innerEntry.SetBinding(Entry.TextProperty, new Binding(nameof(this.EntryData), source: this, mode: BindingMode.TwoWay));
			this.innerEntry.SetBinding(Entry.KeyboardProperty, new Binding(nameof(this.Keyboard), source: this));
			this.innerEntry.SetBinding(Entry.PlaceholderProperty, new Binding(nameof(this.Placeholder), source: this));
			this.innerEntry.SetBinding(Entry.IsPasswordProperty, new Binding(nameof(this.IsPassword), source: this));
			this.innerEntry.SetBinding(Entry.IsReadOnlyProperty, new Binding(nameof(this.IsReadOnly), source: this));
			this.innerEntry.SetBinding(Entry.ReturnCommandProperty, new Binding(nameof(this.ReturnCommand), source: this));
			this.innerEntry.SetBinding(Entry.StyleProperty, new Binding(nameof(this.EntryStyle), source: this));
			this.innerEntry.SetBinding(Entry.TextColorProperty, new Binding(nameof(this.TextColor), source: this));

			// Handle focus events
			this.innerEntry.Focused += this.OnEntryFocused;
			this.innerEntry.Unfocused += this.OnEntryUnfocused;
		}

		private void OnKeyboardPropertyChanged(Keyboard oldValue, Keyboard newValue)
		{
			this.innerEntry.Keyboard = newValue;
		}

		private void OnBorderStylePropertyChanged(Style oldValue, Style newValue)
		{
			this.innerBorder.Style = newValue;
		}

		private void OnStackSpacingPropertyChanged(double oldValue, double newValue)
		{
			this.innerGrid.ColumnSpacing = (this.innerPath.IsVisible || this.clickablePath.IsVisible) ? newValue : 0;
		}

		private void OnPathDataPropertyChanged(Geometry oldValue, Geometry newValue)
		{
			this.innerPath.Data = newValue;
			this.innerPath.IsVisible = newValue != null;
			this.innerGrid.ColumnSpacing = (this.innerPath.IsVisible || this.clickablePath.IsVisible) ? this.StackSpacing : 0;
		}

		private void OnClickablePathDataPropertyChanged(Geometry oldValue, Geometry newValue)
		{
			this.clickablePath.Data = newValue;
			this.clickablePath.IsVisible = newValue != null;
			this.innerGrid.ColumnSpacing = (this.innerPath.IsVisible || this.clickablePath.IsVisible) ? this.StackSpacing : 0;
		}

		private void OnPathStylePropertyChanged(Style oldValue, Style newValue)
		{
			this.innerPath.Style = newValue;
			this.clickablePath.Style = newValue;
		}

		private void OnEntryHintPropertyChanged(string oldValue, string newValue)
		{
			this.innerEntry.Placeholder = newValue;
		}

		private void OnEntryStylePropertyChanged(Style oldValue, Style newValue)
		{
			this.innerEntry.Style = newValue;
		}

		private void OnReturnCommandPropertyChanged(ICommand oldValue, ICommand newValue)
		{
			this.innerEntry.ReturnCommand = newValue;
		}

		private void OnIsPasswordPropertyChanged(bool oldValue, bool newValue)
		{
			this.innerEntry.IsPassword = newValue;
		}

		private void OnIsReadOnlyPropertyChanged(bool oldValue, bool newValue)
		{
			this.innerEntry.IsReadOnly = newValue;
		}

		private void OnPlaceholderPropertyChanged(string oldValue, string newValue)
		{
			this.innerEntry.Placeholder = newValue;
		}

		private void OnTextColorPropertyChanged(Color oldValue, Color newValue)
		{
			this.innerEntry.TextColor = newValue;
		}

		private void OnEntryFocused(object? sender, FocusEventArgs e)
		{
			if (this.FocusedCommand?.CanExecute(e) ?? false)
			{
				this.FocusedCommand.Execute(e);
			}
		}

		private void OnEntryUnfocused(object? sender, FocusEventArgs e)
		{
			if (this.UnfocusedCommand?.CanExecute(e) ?? false)
			{
				this.UnfocusedCommand.Execute(e);
			}
		}

		private void InnerEntry_Completed(object? sender, EventArgs e)
		{
			try
			{
				this.Completed?.Invoke(sender, e);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		public override string ToString()
		{
			return this.innerEntry.Text;
		}
	}
}
