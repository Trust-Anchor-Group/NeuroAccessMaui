using NeuroFeatures.Events;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenEvents.Events
{
	/// <summary>
	/// Represents a text note on a token.
	/// </summary>
	/// <param name="Event">Token event</param>
	public class NoteTextItem(NoteText Event) : NoteItem(Event)
	{
		/// <summary>
		/// Type of event.
		/// </summary>
		public override EventType Type => EventType.NoteText;
	}
}
