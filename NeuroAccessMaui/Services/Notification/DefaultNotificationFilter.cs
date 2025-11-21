using System.Threading;
using NeuroAccessMaui.UI.Pages.Contacts.Chat;

namespace NeuroAccessMaui.Services.Notification
{
	/// <summary>
	/// Default filter that ignores notifications already in view.
	/// </summary>
	public sealed class DefaultNotificationFilter : INotificationFilter
	{
		/// <summary>
		/// Returns true if the notification should be ignored.
		/// </summary>
		/// <param name="Intent">Notification intent.</param>
		/// <param name="FromUserInteraction">If the notification came from explicit user interaction.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		public bool ShouldIgnore(NotificationIntent Intent, bool FromUserInteraction, CancellationToken CancellationToken)
		{
			// Example: ignore chat notifications when already in the same chat.
			if (Intent.Action == NotificationAction.OpenChat && ServiceRef.NavigationService.CurrentPage is ChatPage ChatPage)
			{
				if (ChatPage.BindingContext is ChatViewModel Vm && string.Equals(Vm.BareJid, Intent.EntityId, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}
	}
}
