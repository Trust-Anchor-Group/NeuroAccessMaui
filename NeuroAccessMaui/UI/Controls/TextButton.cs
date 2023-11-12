using NeuroAccessMaui.UI.Core;

namespace NeuroAccessMaui.UI.Controls;

public class TextButton : TemplatedButton, IBorderDataElement, ILabelDataElement
{
	private readonly Border innerBorder;
	private readonly Label innerLabel;

	/// <summary>Bindable property for <see cref="BorderStyle"/>.</summary>
	public static readonly BindableProperty BorderStyleProperty = BorderDataElement.BorderStyleProperty;

	/// <summary>Bindable property for <see cref="BorderStrokeShape"/>.</summary>
	public static readonly BindableProperty BorderStrokeShapeProperty = BorderDataElement.BorderStrokeShapeProperty;

	/// <summary>Bindable property for <see cref="BorderBackgroundColor"/>.</summary>
	public static readonly BindableProperty BorderBackgroundColorProperty = BorderDataElement.BorderBackgroundColorProperty;

	/// <summary>Bindable property for <see cref="BorderPadding"/>.</summary>
	public static readonly BindableProperty BorderPaddingProperty = BorderDataElement.BorderPaddingProperty;

	/// <summary>Bindable property for <see cref="LabelData"/>.</summary>
	public static readonly BindableProperty LabelDataProperty = LabelDataElement.LabelDataProperty;

	/// <summary>Bindable property for <see cref="LabelStyle"/>.</summary>
	public static readonly BindableProperty LabelStyleProperty = LabelDataElement.LabelStyleProperty;

	/// <summary>Bindable property for <see cref="LabelTextColor"/>.</summary>
	public static readonly BindableProperty LabelTextColorProperty = LabelDataElement.LabelTextColorProperty;

	/// <summary>Bindable property for <see cref="LabelHorizontalOptions"/>.</summary>
	public static readonly BindableProperty LabelHorizontalOptionsProperty = LabelDataElement.LabelHorizontalOptionsProperty;

	/// <summary>Bindable property for <see cref="LabelVerticalOptions"/>.</summary>
	public static readonly BindableProperty LabelVerticalOptionsProperty = LabelDataElement.LabelVerticalOptionsProperty;

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

	public void OnLabelDataPropertyChanged(string OldValue, string NewValue)
	{
		this.innerLabel.Text = NewValue;
	}

	public void OnLabelStylePropertyChanged(Style OldValue, Style NewValue)
	{
		this.innerLabel.Style = NewValue;
	}

	public void OnLabelTextColorPropertyChanged(Color OldValue, Color NewValue)
	{
		this.innerLabel.TextColor = NewValue;
	}

	public void OnLabelHorizontalOptionsPropertyChanged(LayoutOptions OldValue, LayoutOptions NewValue)
	{
		this.innerLabel.HorizontalOptions = NewValue;
	}

	public void OnLabelVerticalOptionsPropertyChanged(LayoutOptions OldValue, LayoutOptions NewValue)
	{
		this.innerLabel.VerticalOptions = NewValue;
	}

	public Thickness BorderPaddingDefaultValueCreator() => Thickness.Zero;

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

	public string LabelData
	{
		get => (string)this.GetValue(LabelDataElement.LabelDataProperty);
		set => this.SetValue(LabelDataElement.LabelDataProperty, value);
	}

	public Style LabelStyle
	{
		get => (Style)this.GetValue(LabelDataElement.LabelStyleProperty);
		set => this.SetValue(LabelDataElement.LabelStyleProperty, value);
	}

	public Color LabelTextColor
	{
		get => (Color)this.GetValue(LabelDataElement.LabelTextColorProperty);
		set => this.SetValue(LabelDataElement.LabelTextColorProperty, value);
	}

	public LayoutOptions LabelHorizontalOptions
	{
		get => (LayoutOptions)this.GetValue(LabelDataElement.LabelHorizontalOptionsProperty);
		set => this.SetValue(LabelDataElement.LabelHorizontalOptionsProperty, value);
	}

	public LayoutOptions LabelVerticalOptions
	{
		get => (LayoutOptions)this.GetValue(LabelDataElement.LabelVerticalOptionsProperty);
		set => this.SetValue(LabelDataElement.LabelVerticalOptionsProperty, value);
	}

	public TextButton() : base()
	{
		this.innerLabel = new()
		{
		};

		this.innerBorder = new()
		{
			StrokeThickness = 0,
			Content = this.innerLabel
		};

		this.Content = this.innerBorder;
	}
}
