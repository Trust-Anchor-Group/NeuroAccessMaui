using NeuroFeatures.Events;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenEvents.Events
{
	/// <summary>
	/// Represents a token note.
	/// </summary>
	public abstract class NoteItem : EventItem
	{
		private readonly TokenNoteEvent @event;

		/// <summary>
		/// Represents a token note.
		/// </summary>
		/// <param name="Event">Token event</param>
		public NoteItem(TokenNoteEvent Event)
			: base(Event)
		{
			this.@event = Event;
		}

		/// <summary>
		/// Note
		/// </summary>
		public string Note => this.@event.Note;
	}
}
