using NeuroFeatures.Events;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenEvents.Events
{
	/// <summary>
	/// Represents an XML note on a token from an external source.
	/// </summary>
	public class ExternalNoteXmlItem : ExternalNoteItem
	{
		/// <summary>
		/// Represents an XML note on a token from an external source.
		/// </summary>
		/// <param name="Event">Token event</param>
		public ExternalNoteXmlItem(ExternalNoteXml Event)
			: base(Event)
		{
		}

		/// <summary>
		/// Type of event.
		/// </summary>
		public override EventType Type => EventType.ExternalNoteXml;
	}
}
