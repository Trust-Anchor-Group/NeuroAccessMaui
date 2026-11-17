namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Tells screen readers that a view is a button, so TalkBack and VoiceOver say "button" for it. Set
	/// <see cref="IsButtonProperty"/> on any view that does something when tapped. <see cref="TemplatedButton"/> sets it on
	/// itself.
	/// </summary>
	public static partial class ButtonRole
	{
		/// <summary>
		/// Identifies the attached property that marks a view as a button for screen readers.
		/// </summary>
		public static readonly BindableProperty IsButtonProperty = BindableProperty.CreateAttached(
			"IsButton",
			typeof(bool),
			typeof(ButtonRole),
			false,
			propertyChanged: OnIsButtonChanged);

		/// <summary>
		/// Gets if a view is a button for screen readers.
		/// </summary>
		/// <param name="View">The view.</param>
		/// <returns>If the view is a button.</returns>
		public static bool GetIsButton(BindableObject View)
		{
			return (bool)View.GetValue(IsButtonProperty);
		}

		/// <summary>
		/// Sets if a view is a button for screen readers.
		/// </summary>
		/// <param name="View">The view.</param>
		/// <param name="Value">If the view is a button.</param>
		public static void SetIsButton(BindableObject View, bool Value)
		{
			View.SetValue(IsButtonProperty, Value);
		}

		/// <summary>
		/// Gives a view's platform view the button role, if the view is marked as a button. Called when the view's
		/// semantics are set, so the role is kept when the platform sets them again.
		/// </summary>
		/// <param name="Handler">The view's handler.</param>
		/// <param name="View">The view.</param>
		public static void Apply(IViewHandler Handler, IView View)
		{
			if (View is BindableObject Bindable && GetIsButton(Bindable) && Handler.PlatformView is not null)
				ApplyPlatform(Handler.PlatformView);
		}

		/// <summary>
		/// Gives a platform view the button role. Implemented for Android and iOS.
		/// </summary>
		/// <param name="PlatformView">The platform view.</param>
		static partial void ApplyPlatform(object PlatformView);

		private static void OnIsButtonChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			// A view that already has a platform view gets its semantics again, which applies the role.
			if (Bindable is VisualElement Element)
				Element.Handler?.UpdateValue(nameof(IView.Semantics));
		}
	}
}
