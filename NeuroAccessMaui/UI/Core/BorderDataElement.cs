namespace NeuroAccessMaui.UI.Core;

internal static class BorderDataElement
{
	/// <summary>Bindable property for <see cref="IBorderDataElement.BorderStyle"/>.</summary>
	public static readonly BindableProperty BorderStyleProperty =
		BindableProperty.Create(nameof(IBorderDataElement.BorderStyle), typeof(Style), typeof(IBorderDataElement), default(Style),
								propertyChanged: OnBorderStylePropertyChanged);

	static void OnBorderStylePropertyChanged(BindableObject Bindable, object OldValue, object NewValue)
	{
		((IBorderDataElement)Bindable).OnBorderStylePropertyChanged((Style)OldValue, (Style)NewValue);
	}
}
