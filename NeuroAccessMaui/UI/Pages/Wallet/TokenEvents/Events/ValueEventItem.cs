using NeuroFeatures.Events;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenEvents.Events
{
	/// <summary>
	/// Represents a token value event.
	/// </summary>
	/// <param name="Event">Token event</param>
	public abstract class ValueEventItem(TokenValueEvent Event) : EventItem(Event)
	{
		private readonly TokenValueEvent @event = Event;

		/// <summary>
		/// Currency
		/// </summary>
		public string Currency => this.@event.Currency;

		/// <summary>
		/// Value
		/// </summary>
		public decimal Value => this.@event.Value;
	}
}
