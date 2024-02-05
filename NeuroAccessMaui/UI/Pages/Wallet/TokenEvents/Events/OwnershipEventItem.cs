using NeuroAccessMaui.Services;
using NeuroFeatures.Events;
using System.Threading.Tasks;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenEvents.Events
{
	/// <summary>
	/// Represents a token ownership event.
	/// </summary>
	public abstract class OwnershipEventItem : ValueEventItem
	{
		private readonly TokenOwnershipEvent @event;
		private string ownerFriendlyName;

		/// <summary>
		/// Represents a token ownership event.
		/// </summary>
		/// <param name="Event">Token event</param>
		public OwnershipEventItem(TokenOwnershipEvent Event)
			: base(Event)
		{
			this.@event = Event;
		}

		/// <summary>
		/// Owner
		/// </summary>
		public string Owner => this.@event.Owner;

		/// <summary>
		/// Ownership contract
		/// </summary>
		public string OwnershipContract => this.@event.OwnershipContract;

		/// <summary>
		/// Owner (Friendly Name)
		/// </summary>
		public string OwnerFriendlyName => this.ownerFriendlyName;

		/// <summary>
		/// Binds properties
		/// </summary>
		/// <param name="Ref">Service references.</param>
		public override async Task DoBind(IServiceReferences Ref)
		{
			await base.DoBind(Ref);

			this.ownerFriendlyName = await ContactInfo.GetFriendlyName(this.Owner, Ref);
		}

	}
}
