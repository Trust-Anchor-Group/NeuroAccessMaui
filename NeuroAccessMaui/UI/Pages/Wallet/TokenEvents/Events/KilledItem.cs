using NeuroAccessMaui.Services.Contacts;
using NeuroFeatures.Events;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenEvents.Events
{
	/// <summary>
	/// Represents a statemachine killed event.
	/// </summary>
	/// <param name="Event">Token event</param>
	public class KilledItem(Killed Event) : OwnershipEventItem(Event)
	{
		private readonly Killed @event = Event;

		/// <summary>
		/// Type of event.
		/// </summary>
		public override EventType Type => EventType.Killed;

		/// <summary>
		/// Name of the user who killed the state machine.
		/// </summary>
		public string User => this.@event.User;

		/// <summary>
		/// Legal identity who approvied the killing.
		/// </summary>
		public string LegalId => this.@event.LegalId;

		/// <summary>
		/// Binds properties
		/// </summary>
		public override async Task DoBind()
		{
			await base.DoBind();
		}
	}
}
