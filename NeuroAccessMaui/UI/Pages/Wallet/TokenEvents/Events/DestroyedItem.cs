using NeuroFeatures.Events;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenEvents.Events
{
	/// <summary>
	/// Represents a token destruction event.
	/// </summary>
	public class DestroyedItem : OwnershipEventItem
	{
		/// <summary>
		/// Represents a token destruction event.
		/// </summary>
		/// <param name="Event">Token event</param>
		public DestroyedItem(Destroyed Event)
			: base(Event)
		{
		}

		/// <summary>
		/// Type of event.
		/// </summary>
		public override EventType Type => EventType.Destroyed;
	}
}
