using System.ComponentModel;

namespace NeuroAccessMaui.UI.Behaviors
{
	/// <summary>
	/// Used for moving focus to the next UI component when a view has been tapped.
	/// </summary>
	public class SetFocusOnTappedBehavior : Behavior<View>
	{
		/// <summary>
		/// The view to move focus to.
		/// </summary>
		[TypeConverter(typeof(ReferenceTypeConverter))]
		public View? SetFocusTo { get; set; }

		/// <inheritdoc/>
		protected override void OnAttachedTo(View View)
		{
			TapGestureRecognizer Tap = new();
			View.GestureRecognizers.Add(Tap);
			Tap.Tapped += this.Tap_Tapped;

			base.OnAttachedTo(View);
		}

		/// <inheritdoc/>
		protected override void OnDetachingFrom(View View)
		{
			foreach (IGestureRecognizer Gesture in View.GestureRecognizers)
			{
				if (Gesture is TapGestureRecognizer Tap)
				{
					Tap.Tapped -= this.Tap_Tapped;
					View.GestureRecognizers.Remove(Tap);
					break;
				}
			}

			base.OnDetachingFrom(View);
		}

		private void Tap_Tapped(object? Sender, EventArgs e)
		{
			FocusOn(this.SetFocusTo);
		}

		/// <summary>
		/// Sets focus on an element.
		/// </summary>
		/// <param name="Element">Element to focus on.</param>
		public static void FocusOn(View? Element)
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
