using NeuroAccessMaui.Services;
using NeuroFeatures.Events;
using System.Threading.Tasks;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenEvents.Events
{
	/// <summary>
	/// Represents a token creation event.
	/// </summary>
	public class CreatedItem : OwnershipEventItem
	{
		private readonly Created @event;
		private string creatorFriendlyName;

		/// <summary>
		/// Represents a token creation event.
		/// </summary>
		/// <param name="Event">Token event</param>
		public CreatedItem(Created Event)
			: base(Event)
		{
			this.@event = Event;
		}

		/// <summary>
		/// Type of event.
		/// </summary>
		public override EventType Type => EventType.Created;

		/// <summary>
		/// Creator
		/// </summary>
		public string Creator => this.@event.Creator;

		/// <summary>
		/// Creator (Friendly Name)
		/// </summary>
		public string CreatorFriendlyName => this.creatorFriendlyName;

		/// <summary>
		/// Binds properties
		/// </summary>
		/// <param name="Ref">Service references.</param>
		public override async Task DoBind(IServiceReferences Ref)
		{
			await base.DoBind(Ref);

			this.creatorFriendlyName = await ContactInfo.GetFriendlyName(this.Creator, Ref);
		}
	}
}
