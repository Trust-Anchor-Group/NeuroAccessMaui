using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.UI.Core;
using PathShape = Microsoft.Maui.Controls.Shapes.Path;

namespace NeuroAccessMaui.UI.Controls;

public class ImageButton : TemplatedButton, IBorderDataElement, IPathDataElement
{
	private readonly Border innerBorder;
	private readonly PathShape innerPath;

	/// <summary>Bindable property for <see cref="BorderStyle"/>.</summary>
	public static readonly BindableProperty BorderStyleProperty = BorderDataElement.BorderStyleProperty;

	/// <summary>Bindable property for <see cref="BorderStrokeShape"/>.</summary>
	public static readonly BindableProperty BorderStrokeShapeProperty = BorderDataElement.BorderStrokeShapeProperty;

	/// <summary>Bindable property for <see cref="BorderBackgroundColor"/>.</summary>
	public static readonly BindableProperty BorderBackgroundColorProperty = BorderDataElement.BorderBackgroundColorProperty;

	/// <summary>Bindable property for <see cref="BorderPadding"/>.</summary>
	public static readonly BindableProperty BorderPaddingProperty = BorderDataElement.BorderPaddingProperty;

	/// <summary>Bindable property for <see cref="GeometryData"/>.</summary>
	public static readonly BindableProperty GeometryDataProperty = PathDataElement.GeometryDataProperty;

	/// <summary>Bindable property for <see cref="PathStyle"/>.</summary>
	public static readonly BindableProperty PathStyleProperty = PathDataElement.PathStyleProperty;

	/// <summary>Bindable property for <see cref="PathFill"/>.</summary>
	public static readonly BindableProperty PathFillProperty = PathDataElement.PathFillProperty;

	/// <summary>Bindable property for <see cref="PathStroke"/>.</summary>
	public static readonly BindableProperty PathStrokeProperty = PathDataElement.PathStrokeProperty;

	public void OnBorderStylePropertyChanged(Style OldValue, Style NewValue)
	{
		this.innerBorder.Style = NewValue;
	}

	public void OnBorderStrokeShapePropertyChanged(IShape OldValue, IShape NewValue)
	{
		this.innerBorder.StrokeShape = NewValue;
	}

	public void OnBorderBackgroundColorPropertyChanged(Color OldValue, Color NewValue)
	{
		this.innerBorder.BackgroundColor = NewValue;
	}

	public void OnBorderPaddingPropertyChanged(Thickness OldValue, Thickness NewValue)
	{
		this.innerBorder.Padding = NewValue;
	}

	public void OnGeometryDataPropertyChanged(Geometry OldValue, Geometry NewValue)
	{
		this.innerPath.Data = NewValue;
	}

	public void OnPathStylePropertyChanged(Style OldValue, Style NewValue)
	{
		this.innerPath.Style = NewValue;
	}

	public void OnPathFillPropertyChanged(Brush OldValue, Brush NewValue)
	{
		this.innerPath.Fill = NewValue;
	}

	public void OnPathStrokePropertyChanged(Brush OldValue, Brush NewValue)
	{
		this.innerPath.Stroke = NewValue;
	}

	public Thickness BorderPaddingDefaultValueCreator() => Thickness.Zero;

	public Geometry GeometryData
	{
		get => (Geometry)this.GetValue(PathDataElement.GeometryDataProperty);
		set => this.SetValue(PathDataElement.GeometryDataProperty, value);
	}

	public Style BorderStyle
	{
		get => (Style)this.GetValue(BorderDataElement.BorderStyleProperty);
		set => this.SetValue(BorderDataElement.BorderStyleProperty, value);
	}

	public IShape BorderStrokeShape
	{
		get => (IShape)this.GetValue(BorderDataElement.BorderStrokeShapeProperty);
		set => this.SetValue(BorderDataElement.BorderStrokeShapeProperty, value);
	}

	public Color BorderBackgroundColor
	{
		get => (Color)this.GetValue(BorderDataElement.BorderBackgroundColorProperty);
		set => this.SetValue(BorderDataElement.BorderBackgroundColorProperty, value);
	}

	public Thickness BorderPadding
	{
		get => (Thickness)this.GetValue(BorderDataElement.BorderPaddingProperty);
		set => this.SetValue(BorderDataElement.BorderPaddingProperty, value);
	}

	public Style PathStyle
	{
		get => (Style)this.GetValue(PathDataElement.PathStyleProperty);
		set => this.SetValue(PathDataElement.PathStyleProperty, value);
	}

	public Brush PathFill
	{
		get => (Brush)this.GetValue(PathDataElement.PathFillProperty);
		set => this.SetValue(PathDataElement.PathFillProperty, value);
	}

	public Brush PathStroke
	{
		get => (Brush)this.GetValue(PathDataElement.PathStrokeProperty);
		set => this.SetValue(PathDataElement.PathStrokeProperty, value);
	}

	public ImageButton() : base()
	{
		this.innerPath = new()
		{
			HeightRequest = 24,
			WidthRequest = 24,
			Aspect = Stretch.Uniform
		};

		this.innerBorder = new()
		{
			StrokeThickness = 0,
			Content = this.innerPath
		};

		this.Content = this.innerBorder;
	}
}
