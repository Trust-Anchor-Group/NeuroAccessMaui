using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Controls;

namespace NeuroAccessMaui.UI.Behaviors
{
	/// <summary>
	/// Used for hiding keyboard once the user hits the Enter or Return key.
	/// </summary>
	public class HideKeyboardOnCompletedBehavior : Behavior<CompositeEntry>
	{
		/// <inheritdoc/>
		protected override void OnAttachedTo(CompositeEntry entry)
		{
			entry.Completed += this.Entry_Completed;
			base.OnAttachedTo(entry);
		}

		/// <inheritdoc/>
		protected override void OnDetachingFrom(CompositeEntry entry)
		{
			entry.Completed -= this.Entry_Completed;
			base.OnDetachingFrom(entry);
		}

		private void Entry_Completed(object? Sender, EventArgs e)
		{
			ServiceRef.PlatformSpecific.HideKeyboard();
		}
	}
}
