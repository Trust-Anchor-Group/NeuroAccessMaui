using NeuroAccessMaui.Animations;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Shows and hides a view the way the identity flow reveals its content. When <see cref="IsShownProperty"/> turns true,
	/// the view fades in while it rises 10 units into place over 260 ms. When it turns false, the view is hidden at once.
	/// With reduced motion, the view appears in place.
	/// </summary>
	/// <remarks>
	/// The view should start hidden, with <c>IsVisible="False"</c> set before the binding, since the property only acts when
	/// its value changes.
	/// </remarks>
	public static class Reveal
	{
		private const uint durationMs = 260;
		private const double liftDistance = 10;

		/// <summary>
		/// Identifies the attached property that shows the view, with the reveal, when it turns true.
		/// </summary>
		public static readonly BindableProperty IsShownProperty = BindableProperty.CreateAttached(
			"IsShown",
			typeof(bool),
			typeof(Reveal),
			false,
			propertyChanged: OnIsShownChanged);

		/// <summary>
		/// Gets if a view is shown.
		/// </summary>
		/// <param name="View">The view.</param>
		/// <returns>If the view is shown.</returns>
		public static bool GetIsShown(BindableObject View)
		{
			return (bool)View.GetValue(IsShownProperty);
		}

		/// <summary>
		/// Sets if a view is shown. Showing it plays the reveal.
		/// </summary>
		/// <param name="View">The view.</param>
		/// <param name="Value">If the view is shown.</param>
		public static void SetIsShown(BindableObject View, bool Value)
		{
			View.SetValue(IsShownProperty, Value);
		}

		private static void OnIsShownChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			if (Bindable is not VisualElement Element)
				return;

			if ((bool)NewValue)
				_ = ShowAsync(Element);
			else
			{
				Element.CancelAnimations();
				Element.IsVisible = false;
				Element.Opacity = 1;
				Element.TranslationY = 0;
			}
		}

		private static async Task ShowAsync(VisualElement Element)
		{
			Element.CancelAnimations();

			if (MotionPreferences.ReduceMotion || Element.Handler is null)
			{
				Element.Opacity = 1;
				Element.TranslationY = 0;
				Element.IsVisible = true;
				return;
			}

			try
			{
				Element.Opacity = 0;
				Element.TranslationY = liftDistance;
				Element.IsVisible = true;

				await Task.WhenAll(
					Element.FadeToAsync(1, durationMs, Easing.CubicOut),
					Element.TranslateToAsync(0, 0, durationMs, Easing.CubicOut));
			}
			catch (Exception Ex)
			{
				Element.Opacity = 1;
				Element.TranslationY = 0;
				ServiceRef.LogService.LogException(Ex);
			}
		}
	}
}
