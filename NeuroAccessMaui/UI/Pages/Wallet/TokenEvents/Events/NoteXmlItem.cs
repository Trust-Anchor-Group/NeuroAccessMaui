using NeuroFeatures.Events;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenEvents.Events
{
	/// <summary>
	/// Represents an XML note on a token.
	/// </summary>
	public class NoteXmlItem : NoteItem
	{
		/// <summary>
		/// Represents an XML note on a token.
		/// </summary>
		/// <param name="Event">Token event</param>
		public NoteXmlItem(NoteXml Event)
			: base(Event)
		{
		}

		/// <summary>
		/// Type of event.
		/// </summary>
		public override EventType Type => EventType.NoteXml;
	}
}
