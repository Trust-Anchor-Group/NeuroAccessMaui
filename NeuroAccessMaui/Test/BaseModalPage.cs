using Microsoft.Maui.Controls;
using NeuroAccessMaui.UI.Pages;

namespace NeuroAccessMaui.Test
{
    /// <summary>
    /// Base class for modal pages with overlay customization.
    /// </summary>
    public class BaseModalPage : BaseContentPage
    {
        /// <summary>
        /// Bindable property controlling the background overlay color.
        /// </summary>
        public static readonly BindableProperty OverlayColorProperty =
            BindableProperty.CreateAttached(
                "OverlayColor",
                typeof(Color),
                typeof(BaseModalPage),
                Color.FromArgb("#80000000"));

        /// <summary>
        /// Gets the background overlay color.
        /// </summary>
        /// <param name="View">Target page.</param>
        /// <returns>Overlay color.</returns>
        public static Color GetOverlayColor(BindableObject View) =>
            (Color)View.GetValue(OverlayColorProperty);

        /// <summary>
        /// Sets the background overlay color.
        /// </summary>
        /// <param name="View">Target page.</param>
        /// <param name="Color">Overlay color.</param>
        public static void SetOverlayColor(BindableObject View, Color Color) =>
            View.SetValue(OverlayColorProperty, Color);

        /// <summary>
        /// Bindable property controlling if tapping outside pops the modal.
        /// </summary>
        public static readonly BindableProperty PopOnBackgroundPressProperty =
            BindableProperty.CreateAttached(
                "PopOnBackgroundPress",
                typeof(bool),
                typeof(BaseModalPage),
                false);

        /// <summary>
        /// Gets if tapping the background should pop the modal.
        /// </summary>
        /// <param name="View">Target page.</param>
        /// <returns>If pop is triggered on background press.</returns>
        public static bool GetPopOnBackgroundPress(BindableObject View) =>
            (bool)View.GetValue(PopOnBackgroundPressProperty);

        /// <summary>
        /// Sets if tapping the background should pop the modal.
        /// </summary>
        /// <param name="View">Target page.</param>
        /// <param name="Value">Value to set.</param>
        public static void SetPopOnBackgroundPress(BindableObject View, bool Value) =>
            View.SetValue(PopOnBackgroundPressProperty, Value);

		public virtual Task OnInitializeAsync()
		{
			return Task.CompletedTask;
		}

		public virtual Task OnDisposeAsync()
		{
			return Task.CompletedTask;
		}

		public virtual Task OnAppearingAsync()
		{
			return Task.CompletedTask;
		}

		public virtual Task OnDisappearingAsync()
		{
			return Task.CompletedTask;
		}
	}
}
