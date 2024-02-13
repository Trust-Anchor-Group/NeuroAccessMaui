using NeuroFeatures.Events;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenEvents.Events
{
	/// <summary>
	/// Represents a text note on a token from an external source.
	/// </summary>
	/// <param name="Event">Token event</param>
	public class ExternalNoteTextItem(ExternalNoteText Event) : ExternalNoteItem(Event)
	{
		/// <summary>
		/// Type of event.
		/// </summary>
		public override EventType Type => EventType.ExternalNoteText;
	}
}
