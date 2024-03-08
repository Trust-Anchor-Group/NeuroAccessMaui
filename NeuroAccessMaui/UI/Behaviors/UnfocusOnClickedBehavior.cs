using System.ComponentModel;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Behaviors
{
	/// <summary>
	/// Used for unfocusing an input control.
	/// </summary>
	public class UnfocusOnClickedBehavior : Behavior<View>
	{
		/// <summary>
		/// The view to unfocus.
		/// </summary>
		[TypeConverter(typeof(ReferenceTypeConverter))]
		public View? UnfocusControl { get; set; }

		/// <inheritdoc/>
		protected override void OnAttachedTo(View View)
		{
			if (View is Controls.ImageButton ImageButton)
				ImageButton.Clicked += this.Button_Clicked;
			else if (View is Button Button)
				Button.Clicked += this.Button_Clicked;

			base.OnAttachedTo(View);
		}

		/// <inheritdoc/>
		protected override void OnDetachingFrom(View View)
		{
			if (View is Controls.ImageButton ImageButton)
				ImageButton.Clicked -= this.Button_Clicked;
			else if (View is Button Button)
				Button.Clicked -= this.Button_Clicked;

			base.OnDetachingFrom(View);
		}

		private void Button_Clicked(object? Sender, EventArgs e)
		{
			Unfocus(this.UnfocusControl);
		}

		/// <summary>
		/// Sets focus on an element.
		/// </summary>
		/// <param name="Element">Element to focus on.</param>
		public static void Unfocus(View? Element)
		{
			ServiceRef.PlatformSpecific.HideKeyboard();

			if (Element is not null && Element.IsVisible)
				Element.Unfocus();
		}
	}
}
