using NeuroAccessMaui.Services.Contacts;
using NeuroFeatures.Events;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenEvents.Events
{
	/// <summary>
	/// Represents a token ownership event.
	/// </summary>
	/// <param name="Event">Token event</param>
	public abstract class OwnershipEventItem(TokenOwnershipEvent Event) : ValueEventItem(Event)
	{
		private readonly TokenOwnershipEvent @event = Event;
		private string? ownerFriendlyName;

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
		public string OwnerFriendlyName => this.ownerFriendlyName ?? string.Empty;

		/// <summary>
		/// Binds properties
		/// </summary>
		public override async Task DoBind()
		{
			await base.DoBind();

			this.ownerFriendlyName = await ContactInfo.GetFriendlyName(this.Owner);
		}

	}
}
