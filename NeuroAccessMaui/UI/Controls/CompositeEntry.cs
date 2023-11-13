using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.UI.Core;
using PathShape = Microsoft.Maui.Controls.Shapes.Path;

namespace NeuroAccessMaui.UI.Controls;

class CompositeEntry : ContentView, IBorderDataElement, IPathDataElement
{
	private readonly Border innerBorder;
	private readonly HorizontalStackLayout innerStack;
	private readonly PathShape innerPath;
	private readonly Entry innerEntry;

	/// <summary>Bindable property for <see cref="BorderStyle"/>.</summary>
	public static readonly BindableProperty BorderStyleProperty = BorderDataElement.BorderStyleProperty;

	/// <summary>Bindable property for <see cref="PathData"/>.</summary>
	public static readonly BindableProperty PathDataProperty = PathDataElement.PathDataProperty;

	/// <summary>Bindable property for <see cref="PathStyle"/>.</summary>
	public static readonly BindableProperty PathStyleProperty = PathDataElement.PathStyleProperty;

	public void OnBorderStylePropertyChanged(Style OldValue, Style NewValue)
	{
		this.innerBorder.Style = NewValue;
	}

	public void OnPathDataPropertyChanged(Geometry OldValue, Geometry NewValue)
	{
		this.innerPath.Data = NewValue;
	}

	public void OnPathStylePropertyChanged(Style OldValue, Style NewValue)
	{
		this.innerPath.Style = NewValue;
	}

	public Geometry PathData
	{
		get => (Geometry)this.GetValue(PathDataElement.PathDataProperty);
		set => this.SetValue(PathDataElement.PathDataProperty, value);
	}

	public Style BorderStyle
	{
		get => (Style)this.GetValue(BorderDataElement.BorderStyleProperty);
		set => this.SetValue(BorderDataElement.BorderStyleProperty, value);
	}

	public Style PathStyle
	{
		get => (Style)this.GetValue(PathDataElement.PathStyleProperty);
		set => this.SetValue(PathDataElement.PathStyleProperty, value);
	}

	public CompositeEntry() : base()
	{
		this.innerPath = new()
		{
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
	}
}
