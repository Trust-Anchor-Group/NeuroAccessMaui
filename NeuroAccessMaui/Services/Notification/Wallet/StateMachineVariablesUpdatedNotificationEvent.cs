using NeuroAccessMaui.Resources.Languages;
using NeuroFeatures;
using NeuroFeatures.EventArguments;
using System.Text;

namespace NeuroAccessMaui.Services.Notification.Wallet
{
	/// <summary>
	/// Contains information about a change in internal variables of a state-machine associated with a token.
	/// </summary>
	[Obsolete("Token notifications are stored as notification records. Kept so that events stored by earlier versions can be loaded and deleted. Remove in a later release.")]
	public class StateMachineVariablesUpdatedNotificationEvent : TokenNotificationEvent
	{
		/// <summary>
		/// Contains information about a change in internal variables of a state-machine associated with a token.
		/// </summary>
		public StateMachineVariablesUpdatedNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Contains information about a change in internal variables of a state-machine associated with a token.
		/// </summary>
		/// <param name="e">Event arguments.</param>
		public StateMachineVariablesUpdatedNotificationEvent(VariablesUpdatedEventArgs e)
			: base(e)
		{
		}

		/// <summary>
		/// Gets a descriptive text for the event.
		/// </summary>
		public override async Task<string> GetDescription()
		{
			StringBuilder sb = new();

			sb.Append(ServiceRef.Localizer[nameof(AppResources.VariablesUpdated)]);
			sb.Append(": ");
			sb.Append(await base.GetDescription());

			return sb.ToString();
		}
	}
}
