using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.UI.Popups
{
	/// <summary>
	/// Compatibility wrapper that mirrors the previous Mopups-based BasePopup API while using the new popup infrastructure.
	/// </summary>
	[ContentProperty(nameof(CustomContentProperty))]
	public class BasePopup : BasePopupView
	{
		public static readonly BindableProperty CustomContentProperty = BindableProperty.Create(
			nameof(CustomContent),
			typeof(View),
			typeof(BasePopup),
			null,
			propertyChanged: OnCustomContentChanged);

		public View? CustomContent
		{
			get => (View?)this.GetValue(CustomContentProperty);
			set => this.SetValue(CustomContentProperty, value);
		}

		private static void OnCustomContentChanged(BindableObject bindable, object? oldValue, object? newValue)
		{
			if (bindable is BasePopup popup)
			{
				popup.PopupContent = newValue as View;
			}
		}
	}
}
