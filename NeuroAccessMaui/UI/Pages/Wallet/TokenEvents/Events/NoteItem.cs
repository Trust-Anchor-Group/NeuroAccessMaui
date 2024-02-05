using NeuroFeatures.Events;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenEvents.Events
{
	/// <summary>
	/// Represents a token note.
	/// </summary>
	/// <param name="Event">Token event</param>
	public abstract class NoteItem(TokenNoteEvent Event) : EventItem(Event)
	{
		private readonly TokenNoteEvent @event = Event;

		/// <summary>
		/// Note
		/// </summary>
		public string Note => this.@event.Note;
	}
}
