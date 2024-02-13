using NeuroFeatures.Events;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenEvents.Events
{
	/// <summary>
	/// Represents a token note from an external source.
	/// </summary>
	/// <param name="Event">Token event</param>
	public abstract class ExternalNoteItem(TokenExternalNoteEvent Event) : NoteItem(Event)
	{
		private readonly TokenExternalNoteEvent @event = Event;

		/// <summary>
		/// Source of note.
		/// </summary>
		public string Source => this.@event.Source;
	}
}
