using NeuroFeatures.Events;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenEvents.Events
{
	/// <summary>
	/// Represents a token note from an external source.
	/// </summary>
	public abstract class ExternalNoteItem : NoteItem
	{
		private readonly TokenExternalNoteEvent @event;

		/// <summary>
		/// Represents a token note from an external source.
		/// </summary>
		/// <param name="Event">Token event</param>
		public ExternalNoteItem(TokenExternalNoteEvent Event)
			: base(Event)
		{
			this.@event = Event;
		}

		/// <summary>
		/// Source of note.
		/// </summary>
		public string Source => this.@event.Source;
	}
}
