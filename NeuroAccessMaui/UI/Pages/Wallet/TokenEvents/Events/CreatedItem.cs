using NeuroAccessMaui.Services.Contacts;
using NeuroFeatures.Events;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenEvents.Events
{
	/// <summary>
	/// Represents a token creation event.
	/// </summary>
	/// <param name="Event">Token event</param>
	public class CreatedItem(Created Event) : OwnershipEventItem(Event)
	{
		private readonly Created @event = Event;
		private string? creatorFriendlyName;

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
		public string CreatorFriendlyName => this.creatorFriendlyName ?? string.Empty;

		/// <summary>
		/// Binds properties
		/// </summary>
		public override async Task DoBind()
		{
			await base.DoBind();

			this.creatorFriendlyName = await ContactInfo.GetFriendlyName(this.Creator);
		}
	}
}
