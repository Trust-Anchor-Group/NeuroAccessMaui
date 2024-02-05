using NeuroAccessMaui.Services;
using NeuroFeatures.Events;
using System.Threading.Tasks;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenEvents.Events
{
	/// <summary>
	/// Represents a token transfer event.
	/// </summary>
	public class TransferredItem : OwnershipEventItem
	{
		private readonly Transferred @event;
		private string sellerFriendlyName;

		/// <summary>
		/// Represents a token transfer event.
		/// </summary>
		/// <param name="Event">Token event</param>
		public TransferredItem(Transferred Event)
			: base(Event)
		{
			this.@event = Event;
		}

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
		public string SellerFriendlyName => this.sellerFriendlyName;

		/// <summary>
		/// Binds properties
		/// </summary>
		/// <param name="Ref">Service references.</param>
		public override async Task DoBind(IServiceReferences Ref)
		{
			await base.DoBind(Ref);

			this.sellerFriendlyName = await ContactInfo.GetFriendlyName(this.Seller, Ref);
		}
	}
}
