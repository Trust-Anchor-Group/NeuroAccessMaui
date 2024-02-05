using NeuroFeatures.Events;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenEvents.Events
{
	/// <summary>
	/// Represents a text note on a token from an external source.
	/// </summary>
	public class ExternalNoteTextItem : ExternalNoteItem
	{
		/// <summary>
		/// Represents a text note on a token from an external source.
		/// </summary>
		/// <param name="Event">Token event</param>
		public ExternalNoteTextItem(ExternalNoteText Event)
			: base(Event)
		{
		}

		/// <summary>
		/// Type of event.
		/// </summary>
		public override EventType Type => EventType.ExternalNoteText;
	}
}
