using UIKit;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// iOS part of <see cref="ButtonRole"/>: VoiceOver says "button" for an element with the button trait.
	/// </summary>
	public static partial class ButtonRole
	{
		/// <summary>
		/// Adds the button trait to the view. The trait only counts on a view VoiceOver reads as one element, which a view
		/// with a description is.
		/// </summary>
		/// <param name="PlatformView">The platform view.</param>
		static partial void ApplyPlatform(object PlatformView)
		{
			if (PlatformView is UIView Native)
				Native.AccessibilityTraits |= UIAccessibilityTrait.Button;
		}
	}
}
