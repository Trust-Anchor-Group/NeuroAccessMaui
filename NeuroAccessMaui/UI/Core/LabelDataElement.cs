namespace NeuroAccessMaui.UI.Core;

static class LabelDataElement
{
	/// <summary>Bindable property for <see cref="ILabelDataElement.LabelData"/>.</summary>
	public static readonly BindableProperty LabelDataProperty =
		BindableProperty.Create(nameof(ILabelDataElement.LabelData), typeof(string), typeof(ILabelDataElement), default(string),
								propertyChanged: OnLabelDataPropertyChanged);

	static void OnLabelDataPropertyChanged(BindableObject Bindable, object OldValue, object NewValue)
	{
		((ILabelDataElement)Bindable).OnLabelDataPropertyChanged((string)OldValue, (string)NewValue);
	}

	/// <summary>Bindable property for <see cref="ILabelDataElement.LabelStyle"/>.</summary>
	public static readonly BindableProperty LabelStyleProperty =
		BindableProperty.Create(nameof(ILabelDataElement.LabelStyle), typeof(Style), typeof(ILabelDataElement), default(Style),
								propertyChanged: OnLabelStylePropertyChanged);

	static void OnLabelStylePropertyChanged(BindableObject Bindable, object OldValue, object NewValue)
	{
		((ILabelDataElement)Bindable).OnLabelStylePropertyChanged((Style)OldValue, (Style)NewValue);
	}

	/// <summary>Bindable property for <see cref="ILabelDataElement.LabelTextColor"/>.</summary>
	public static readonly BindableProperty LabelTextColorProperty =
		BindableProperty.Create(nameof(ILabelDataElement.LabelTextColor), typeof(Color), typeof(ILabelDataElement), default(Color),
								propertyChanged: OnLabelTextColorPropertyChanged);

	static void OnLabelTextColorPropertyChanged(BindableObject Bindable, object OldValue, object NewValue)
	{
		((ILabelDataElement)Bindable).OnLabelTextColorPropertyChanged((Color)OldValue, (Color)NewValue);
	}

	/// <summary>Bindable property for <see cref="ILabelDataElement.LabelHorizontalOptions"/>.</summary>
	public static readonly BindableProperty LabelHorizontalOptionsProperty =
		BindableProperty.Create(nameof(ILabelDataElement.LabelHorizontalOptions), typeof(LayoutOptions), typeof(ILabelDataElement), default(LayoutOptions),
								propertyChanged: OnLabelHorizontalOptionsPropertyChanged);

	static void OnLabelHorizontalOptionsPropertyChanged(BindableObject Bindable, object OldValue, object NewValue)
	{
		((ILabelDataElement)Bindable).OnLabelHorizontalOptionsPropertyChanged((LayoutOptions)OldValue, (LayoutOptions)NewValue);
	}

	/// <summary>Bindable property for <see cref="ILabelDataElement.LabelVerticalOptions"/>.</summary>
	public static readonly BindableProperty LabelVerticalOptionsProperty =
		BindableProperty.Create(nameof(ILabelDataElement.LabelVerticalOptions), typeof(LayoutOptions), typeof(ILabelDataElement), default(LayoutOptions),
								propertyChanged: OnLabelVerticalOptionsPropertyChanged);

	static void OnLabelVerticalOptionsPropertyChanged(BindableObject Bindable, object OldValue, object NewValue)
	{
		((ILabelDataElement)Bindable).OnLabelVerticalOptionsPropertyChanged((LayoutOptions)OldValue, (LayoutOptions)NewValue);
	}
}
