using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.UI.Core;
using PathShape = Microsoft.Maui.Controls.Shapes.Path;

namespace NeuroAccessMaui.UI.Controls;

class CompositeEntry : ContentView, IBorderDataElement, IStackElement, IPathDataElement, IEntryDataElement
{
	private readonly Border innerBorder;
	private readonly Grid innerGrid;
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
		this.innerGrid.ColumnSpacing = this.innerPath.IsVisible? NewValue : 0;
	}

	public void OnPathDataPropertyChanged(Geometry OldValue, Geometry NewValue)
	{
		this.innerPath.Data = NewValue;

		if (NewValue is null)
		{
			this.innerPath.IsVisible = false;
			this.innerGrid.ColumnSpacing = 0;
		}
		else
		{
			this.innerPath.IsVisible = true;
			this.innerGrid.ColumnSpacing = this.StackSpacing;
		}
	}

	public void OnPathStylePropertyChanged(Style OldValue, Style NewValue)
	{
		this.innerPath.Style = NewValue;
	}

	public void OnEntryDataPropertyChanged(string OldValue, string NewValue)
	{
		//!!! The Text is already the NewValue.
		//!!! this.innerEntry.Text = NewValue;
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

	public Entry Entry => this.innerEntry;

	public override string? ToString()
	{
		string? Result = this.innerEntry.Text;
		return Result;
	}

	public CompositeEntry() : base()
	{
		this.innerPath = new()
		{
			VerticalOptions = LayoutOptions.Center,
			IsVisible = this.PathData is not null,
			HeightRequest = 24,
			WidthRequest = 24,
			Aspect = Stretch.Uniform
		};

		this.innerEntry = new()
		{
			VerticalOptions = LayoutOptions.Center,
			HorizontalOptions = LayoutOptions.Fill,
		};

		this.innerGrid = new()
		{
			HorizontalOptions = LayoutOptions.Fill,
			ColumnDefinitions = new()
			{
				new() { Width = GridLength.Auto },
				new() { Width = GridLength.Star },
			},
			RowDefinitions = new()
			{
				new() { Height = GridLength.Auto },
			}
		};

		this.innerGrid.Add(this.innerPath, 0);
		this.innerGrid.Add(this.innerEntry, 1);

		this.innerBorder = new()
		{
			StrokeThickness = 2,
			Content = this.innerGrid
		};

		this.Content = this.innerBorder;

		this.innerEntry.SetBinding(Entry.TextProperty, new Binding(EntryDataProperty.PropertyName, source: this, mode: BindingMode.TwoWay));
	}
}
