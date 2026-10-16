using NeuroAccessMaui.Resources.Languages;
using NeuroFeatures;
using NeuroFeatures.EventArguments;
using System.Text;

namespace NeuroAccessMaui.Services.Notification.Wallet
{
	/// <summary>
	/// Contains information about a token that has been added.
	/// </summary>
	[Obsolete("Token notifications are stored as notification records. Kept so that events stored by earlier versions can be loaded and deleted. Remove in a later release.")]
	public class TokenAddedNotificationEvent : TokenNotificationEvent
	{
		/// <summary>
		/// Contains information about a token that has been added.
		/// </summary>
		public TokenAddedNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Contains information about a token that has been added.
		/// </summary>
		/// <param name="e">Event arguments.</param>
		public TokenAddedNotificationEvent(TokenEventArgs e)
			: base(e)
		{
		}

		/// <summary>
		/// Gets a descriptive text for the event.
		/// </summary>
		public override async Task<string> GetDescription()
		{
			StringBuilder sb = new();

			sb.Append(ServiceRef.Localizer[nameof(AppResources.TokenAdded2)]);
			sb.Append(": ");
			sb.Append(await base.GetDescription());

			return sb.ToString();
		}
	}
}
