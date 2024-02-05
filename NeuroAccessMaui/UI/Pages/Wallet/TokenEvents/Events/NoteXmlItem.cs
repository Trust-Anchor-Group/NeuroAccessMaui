using NeuroFeatures.Events;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenEvents.Events
{
	/// <summary>
	/// Represents an XML note on a token.
	/// </summary>
	/// <param name="Event">Token event</param>
	public class NoteXmlItem(NoteXml Event) : NoteItem(Event)
	{
		/// <summary>
		/// Type of event.
		/// </summary>
		public override EventType Type => EventType.NoteXml;
	}
}
