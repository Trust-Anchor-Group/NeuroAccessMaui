using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.UI.Core;
using PathShape = Microsoft.Maui.Controls.Shapes.Path;

namespace NeuroAccessMaui.UI.Controls;

class CompositeEntry : ContentView, IBorderDataElement, IStackElement, IPathDataElement, IEntryDataElement
{
	private readonly Border innerBorder;
	private readonly HorizontalStackLayout innerStack;
	private readonly PathShape innerPath;
	private readonly Entry innerEntry;

	/// <summary>Bindable property for <see cref="BorderStyle"/>.</summary>
	public static readonly BindableProperty BorderStyleProperty = BorderDataElement.BorderStyleProperty;

	/// <summary>Bindable property for <see cref="StackSpacing"/>.</summary>
	public static readonly BindableProperty StackSpacingProperty = StackElement.StackSpacingProperty;

	/// <summary>Bindable property for <see cref="PathData"/>.</summary>
	public static readonly BindableProperty PathDataProperty = PathDataElement.PathDataProperty;

	/// <summary>Bindable property for <see cref="PathStyle"/>.</summary>
	public static readonly BindableProperty PathStyleProperty = PathDataElement.PathStyleProperty;

	/// <summary>Bindable property for <see cref="EntryData"/>.</summary>
	public static readonly BindableProperty EntryDataProperty = EntryDataElement.EntryDataProperty;

	/// <summary>Bindable property for <see cref="EntryHint"/>.</summary>
	public static readonly BindableProperty EntryHintProperty = EntryDataElement.EntryHintProperty;

	/// <summary>Bindable property for <see cref="EntryStyle"/>.</summary>
	public static readonly BindableProperty EntryStyleProperty = EntryDataElement.EntryStyleProperty;

	public void OnBorderStylePropertyChanged(Style OldValue, Style NewValue)
	{
		this.innerBorder.Style = NewValue;
	}

	public void OnStackSpacingPropertyChanged(double OldValue, double NewValue)
	{
		this.innerStack.Spacing = NewValue;
	}

	public void OnPathDataPropertyChanged(Geometry OldValue, Geometry NewValue)
	{
		this.innerPath.Data = NewValue;
		this.innerPath.IsVisible = NewValue is not null;
	}

	public void OnPathStylePropertyChanged(Style OldValue, Style NewValue)
	{
		this.innerPath.Style = NewValue;
	}

	public void OnEntryDataPropertyChanged(string OldValue, string NewValue)
	{
		this.innerEntry.Text = NewValue;
	}

	public void OnEntryHintPropertyChanged(string OldValue, string NewValue)
	{
		this.innerEntry.Placeholder = NewValue;
	}

	public void OnEntryStylePropertyChanged(Style OldValue, Style NewValue)
	{
		this.innerEntry.Style = NewValue;
	}

	public Style BorderStyle
	{
		get => (Style)this.GetValue(BorderDataElement.BorderStyleProperty);
		set => this.SetValue(BorderDataElement.BorderStyleProperty, value);
	}

	public double StackSpacing
	{
		get => (double)this.GetValue(StackElement.StackSpacingProperty);
		set => this.SetValue(StackElement.StackSpacingProperty, value);
	}

	public Geometry PathData
	{
		get => (Geometry)this.GetValue(PathDataElement.PathDataProperty);
		set => this.SetValue(PathDataElement.PathDataProperty, value);
	}

	public Style PathStyle
	{
		get => (Style)this.GetValue(PathDataElement.PathStyleProperty);
		set => this.SetValue(PathDataElement.PathStyleProperty, value);
	}

	public string EntryData
	{
		get => (string)this.GetValue(EntryDataElement.EntryDataProperty);
		set => this.SetValue(EntryDataElement.EntryDataProperty, value);
	}

	public string EntryHint
	{
		get => (string)this.GetValue(EntryDataElement.EntryHintProperty);
		set => this.SetValue(EntryDataElement.EntryHintProperty, value);
	}

	public Style EntryStyle
	{
		get => (Style)this.GetValue(EntryDataElement.EntryStyleProperty);
		set => this.SetValue(EntryDataElement.EntryStyleProperty, value);
	}

	public CompositeEntry() : base()
	{
		this.innerPath = new()
		{
			IsVisible = this.PathData is not null,
			HeightRequest = 24,
			WidthRequest = 24,
			Aspect = Stretch.Uniform
		};

		this.innerEntry = new()
		{
			HorizontalOptions = LayoutOptions.Fill,
		};

		this.innerStack = [this.innerPath, this.innerEntry];

		this.innerBorder = new()
		{
			StrokeThickness = 1,
			Content = this.innerStack
		};

		this.Content = this.innerBorder;

		// this.innerEntry.SetBinding(Entry.TextProperty, new Binding(EntryTextProperty.PropertyName, source: this, mode: BindingMode.TwoWay));
	}
}
