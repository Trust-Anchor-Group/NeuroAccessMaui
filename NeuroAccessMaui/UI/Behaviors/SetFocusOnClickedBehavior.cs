using System.ComponentModel;

namespace NeuroAccessMaui.UI.Behaviors
{
	/// <summary>
	/// Used for moving focus to the next UI component when a button has been clicked.
	/// </summary>
	public class SetFocusOnClickedBehavior : Behavior<View>
	{
		/// <summary>
		/// The view to move focus to.
		/// </summary>
		[TypeConverter(typeof(ReferenceTypeConverter))]
		public View? SetFocusTo { get; set; }

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
			FocusOn(this.SetFocusTo!);
		}

		/// <summary>
		/// Sets focus on an element.
		/// </summary>
		/// <param name="Element">Element to focus on.</param>
		public static void FocusOn(View Element)
		{
			if (Element is not null && Element.IsVisible)
			{
				Element.Focus();

				if (Element is Entry Entry && Entry.Text is not null)
					Entry.CursorPosition = Entry.Text.Length;
			}
		}
	}
}
