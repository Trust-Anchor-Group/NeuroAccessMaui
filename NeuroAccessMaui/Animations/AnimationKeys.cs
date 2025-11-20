namespace NeuroAccessMaui.Animations
{
	/// <summary>
	/// Provides strongly typed animation key definitions.
	/// </summary>
	public static class AnimationKeys
	{
		/// <summary>
		/// Keys for shell-level animations.
		/// </summary>
		public static class Shell
		{
		/// <summary>
		/// Page cross-fade transition key.
		/// </summary>
		public static AnimationKey PageCrossFade { get; } = new AnimationKey("Shell.PageCrossFade");

		/// <summary>
		/// Horizontal page slide transition key.
		/// </summary>
		public static AnimationKey PageSlideHorizontal { get; } = new AnimationKey("Shell.PageSlideHorizontal");

		/// <summary>
		/// Page slide from right to left transition key.
		/// </summary>
		public static AnimationKey PageSlideLeft { get; } = new AnimationKey("Shell.PageSlideLeft");

		/// <summary>
		/// Page slide from left to right transition key.
		/// </summary>
		public static AnimationKey PageSlideRight { get; } = new AnimationKey("Shell.PageSlideRight");

		/// <summary>
		/// Popup fade and scale transition key.
		/// </summary>
		public static AnimationKey PopupFadeScale { get; } = new AnimationKey("Shell.PopupFadeScale");

		/// <summary>
		/// Popup fade show animation key.
		/// </summary>
		public static AnimationKey PopupShowFade { get; } = new AnimationKey("Shell.PopupShowFade");

		/// <summary>
		/// Popup fade hide animation key.
		/// </summary>
		public static AnimationKey PopupHideFade { get; } = new AnimationKey("Shell.PopupHideFade");

		/// <summary>
		/// Popup scale show animation key.
		/// </summary>
		public static AnimationKey PopupShowScale { get; } = new AnimationKey("Shell.PopupShowScale");

		/// <summary>
		/// Popup scale hide animation key.
		/// </summary>
		public static AnimationKey PopupHideScale { get; } = new AnimationKey("Shell.PopupHideScale");

		/// <summary>
		/// Popup slide-up show animation key.
		/// </summary>
		public static AnimationKey PopupShowSlideUp { get; } = new AnimationKey("Shell.PopupShowSlideUp");

		/// <summary>
		/// Popup slide-up hide animation key.
		/// </summary>
		public static AnimationKey PopupHideSlideUp { get; } = new AnimationKey("Shell.PopupHideSlideUp");

		/// <summary>
		/// Toast slide-in transition key.
		/// </summary>
		public static AnimationKey ToastSlideTop { get; } = new AnimationKey("Shell.ToastSlideTop");

		/// <summary>
		/// Toast fade-in animation key.
		/// </summary>
		public static AnimationKey ToastShowFade { get; } = new AnimationKey("Shell.ToastShowFade");

		/// <summary>
		/// Toast fade-out animation key.
		/// </summary>
		public static AnimationKey ToastHideFade { get; } = new AnimationKey("Shell.ToastHideFade");

		/// <summary>
		/// Toast slide-in from top animation key.
		/// </summary>
		public static AnimationKey ToastShowSlideTop { get; } = new AnimationKey("Shell.ToastShowSlideTop");

		/// <summary>
		/// Toast slide-out to top animation key.
		/// </summary>
		public static AnimationKey ToastHideSlideTop { get; } = new AnimationKey("Shell.ToastHideSlideTop");

		/// <summary>
		/// Toast slide-in from bottom animation key.
		/// </summary>
		public static AnimationKey ToastShowSlideBottom { get; } = new AnimationKey("Shell.ToastShowSlideBottom");

		/// <summary>
		/// Toast slide-out to bottom animation key.
		/// </summary>
		public static AnimationKey ToastHideSlideBottom { get; } = new AnimationKey("Shell.ToastHideSlideBottom");
	}

		/// <summary>
		/// Keys for navigation bar animations.
		/// </summary>
		public static class NavigationBars
		{
			/// <summary>
			/// Default navigation bar attach/detach animation key.
			/// </summary>
			public static AnimationKey SwitchCrossFade { get; } = new AnimationKey("NavigationBars.SwitchCrossFade");
		}

		/// <summary>
		/// Keys for view switcher animations.
		/// </summary>
		public static class ViewSwitcher
		{
			/// <summary>
			/// Default view switcher cross-fade animation key.
			/// </summary>
			public static AnimationKey CrossFade { get; } = new AnimationKey("ViewSwitcher.CrossFade");
		}
	}
}
