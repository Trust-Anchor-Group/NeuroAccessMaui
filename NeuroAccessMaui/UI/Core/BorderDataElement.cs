namespace NeuroAccessMaui.UI.Core;

static class BorderDataElement
{
	/// <summary>Bindable property for <see cref="IBorderDataElement.BorderStrokeShape"/>.</summary>
	public static readonly BindableProperty BorderStrokeShapeProperty =
		BindableProperty.Create(nameof(IBorderDataElement.BorderStrokeShape), typeof(IShape), typeof(IBorderDataElement), default(IShape),
								propertyChanged: OnBorderStrokeShapePropertyChanged);

	static void OnBorderStrokeShapePropertyChanged(BindableObject Bindable, object OldValue, object NewValue)
	{
		((IBorderDataElement)Bindable).OnBorderStrokeShapePropertyChanged((IShape)OldValue, (IShape)NewValue);
	}

	/// <summary>Bindable property for <see cref="IBorderDataElement.BorderBackgroundColor"/>.</summary>
	public static readonly BindableProperty BorderBackgroundColorProperty =
		BindableProperty.Create(nameof(IBorderDataElement.BorderBackgroundColor), typeof(Color), typeof(IBorderDataElement), default(Color),
								propertyChanged: OnBorderBackgroundColorPropertyChanged);

	static void OnBorderBackgroundColorPropertyChanged(BindableObject Bindable, object OldValue, object NewValue)
	{
		((IBorderDataElement)Bindable).OnBorderBackgroundColorPropertyChanged((Color)OldValue, (Color)NewValue);
	}

	/// <summary>Bindable property for <see cref="IBorderDataElement.BorderPadding"/>.</summary>
	public static readonly BindableProperty BorderPaddingProperty =
		BindableProperty.Create(nameof(IBorderDataElement.BorderPadding), typeof(Thickness), typeof(IBorderDataElement), default(Thickness),
								propertyChanged: OnBorderPaddingPropertyChanged,
								defaultValueCreator: BorderPaddingDefaultValueCreator);

	static void OnBorderPaddingPropertyChanged(BindableObject Bindable, object OldValue, object NewValue)
	{
		((IBorderDataElement)Bindable).OnBorderPaddingPropertyChanged((Thickness)OldValue, (Thickness)NewValue);
	}

	static object BorderPaddingDefaultValueCreator(BindableObject Bindable)
	{
		return ((IBorderDataElement)Bindable).BorderPaddingDefaultValueCreator();
	}

	/// <summary>Bindable property for attached property <c>BorderPaddingLeft</c>.</summary>
	public static readonly BindableProperty BorderPaddingLeftProperty =
		BindableProperty.Create("BorderPaddingLeft", typeof(double), typeof(IBorderDataElement), default(double),
								propertyChanged: OnBorderPaddingLeftChanged);

	static void OnBorderPaddingLeftChanged(BindableObject Bindable, object OldValue, object NewValue)
	{
		Thickness BorderPadding = (Thickness)Bindable.GetValue(BorderPaddingProperty);
		BorderPadding.Left = (double)NewValue;
		Bindable.SetValue(BorderPaddingProperty, BorderPadding);
	}

	/// <summary>Bindable property for attached property <c>BorderPaddingTop</c>.</summary>
	public static readonly BindableProperty BorderPaddingTopProperty =
		BindableProperty.Create("BorderPaddingTop", typeof(double), typeof(IBorderDataElement), default(double),
								propertyChanged: OnBorderPaddingTopChanged);

	static void OnBorderPaddingTopChanged(BindableObject Bindable, object OldValue, object NewValue)
	{
		Thickness BorderPadding = (Thickness)Bindable.GetValue(BorderPaddingProperty);
		BorderPadding.Top = (double)NewValue;
		Bindable.SetValue(BorderPaddingProperty, BorderPadding);
	}

	/// <summary>Bindable property for attached property <c>BorderPaddingRight</c>.</summary>
	public static readonly BindableProperty BorderPaddingRightProperty =
		BindableProperty.Create("BorderPaddingRight", typeof(double), typeof(IBorderDataElement), default(double),
								propertyChanged: OnBorderPaddingRightChanged);

	static void OnBorderPaddingRightChanged(BindableObject Bindable, object OldValue, object NewValue)
	{
		Thickness BorderPadding = (Thickness)Bindable.GetValue(BorderPaddingProperty);
		BorderPadding.Right = (double)NewValue;
		Bindable.SetValue(BorderPaddingProperty, BorderPadding);
	}

	/// <summary>Bindable property for attached property <c>BorderPaddingBottom</c>.</summary>
	public static readonly BindableProperty BorderPaddingBottomProperty =
		BindableProperty.Create("BorderPaddingBottom", typeof(double), typeof(IBorderDataElement), default(double),
								propertyChanged: OnBorderPaddingBottomChanged);

	static void OnBorderPaddingBottomChanged(BindableObject Bindable, object OldValue, object NewValue)
	{
		Thickness BorderPadding = (Thickness)Bindable.GetValue(BorderPaddingProperty);
		BorderPadding.Bottom = (double)NewValue;
		Bindable.SetValue(BorderPaddingProperty, BorderPadding);
	}
}
