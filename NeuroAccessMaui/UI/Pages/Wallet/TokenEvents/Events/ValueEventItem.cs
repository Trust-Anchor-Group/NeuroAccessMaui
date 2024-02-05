using NeuroFeatures.Events;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenEvents.Events
{
	/// <summary>
	/// Represents a token value event.
	/// </summary>
	public abstract class ValueEventItem : EventItem
	{
		private readonly TokenValueEvent @event;

		/// <summary>
		/// Represents a token value event.
		/// </summary>
		/// <param name="Event">Token event</param>
		public ValueEventItem(TokenValueEvent Event)
			: base(Event)
		{
			this.@event = Event;
		}

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
