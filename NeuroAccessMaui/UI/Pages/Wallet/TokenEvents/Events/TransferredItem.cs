using NeuroAccessMaui.Services.Contacts;
using NeuroFeatures.Events;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenEvents.Events
{
	/// <summary>
	/// Represents a token transfer event.
	/// </summary>
	/// <param name="Event">Token event</param>
	public class TransferredItem(Transferred Event) : OwnershipEventItem(Event)
	{
		private readonly Transferred @event = Event;
		private string? sellerFriendlyName;

		/// <summary>
		/// Type of event.
		/// </summary>
		public override EventType Type => EventType.Transferred;

		/// <summary>
		/// Seller
		/// </summary>
		public string Seller => this.@event.Seller;

		/// <summary>
		/// Seller (Friendly Name)
		/// </summary>
		public string SellerFriendlyName => this.sellerFriendlyName ?? string.Empty;

		/// <summary>
		/// Binds properties
		/// </summary>
		public override async Task DoBind()
		{
			await base.DoBind();

			this.sellerFriendlyName = await ContactInfo.GetFriendlyName(this.Seller);
		}
	}
}
