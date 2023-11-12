using NeuroAccessMaui.UI.Core;

namespace NeuroAccessMaui.UI.Controls;

public class TextButton : TemplatedButton, IBorderDataElement, ILabelDataElement
{
	private readonly Border innerBorder;
	private readonly Label innerLabel;

	/// <summary>Bindable property for <see cref="BorderStyle"/>.</summary>
	public static readonly BindableProperty BorderStyleProperty = BorderDataElement.BorderStyleProperty;

	/// <summary>Bindable property for <see cref="LabelData"/>.</summary>
	public static readonly BindableProperty LabelDataProperty = LabelDataElement.LabelDataProperty;

	/// <summary>Bindable property for <see cref="LabelStyle"/>.</summary>
	public static readonly BindableProperty LabelStyleProperty = LabelDataElement.LabelStyleProperty;

	public void OnBorderStylePropertyChanged(Style OldValue, Style NewValue)
	{
		this.innerBorder.Style = NewValue;
	}

	public void OnLabelDataPropertyChanged(string OldValue, string NewValue)
	{
		this.innerLabel.Text = NewValue;
	}

	public void OnLabelStylePropertyChanged(Style OldValue, Style NewValue)
	{
		this.innerLabel.Style = NewValue;
	}

	public Thickness BorderPaddingDefaultValueCreator() => Thickness.Zero;

	public Style BorderStyle
	{
		get => (Style)this.GetValue(BorderDataElement.BorderStyleProperty);
		set => this.SetValue(BorderDataElement.BorderStyleProperty, value);
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
