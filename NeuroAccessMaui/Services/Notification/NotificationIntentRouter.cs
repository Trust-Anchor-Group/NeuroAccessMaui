using System.Threading;
using System.Threading.Tasks;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.UI;
using NeuroAccessMaui.UI.Pages.Contacts.Chat;
using NeuroAccessMaui.UI.Pages.Contacts.MyContacts;
using NeuroAccessMaui.UI.Pages.Main.Settings;
using NeuroAccessMaui.Resources.Languages;

namespace NeuroAccessMaui.Services.Notification
{
	/// <summary>
	/// Default router for notification intents. Wiring to navigation can be extended here.
	/// </summary>
	public sealed class NotificationIntentRouter : INotificationIntentRouter
	{
		private readonly INotificationFilterRegistry filterRegistry;

		/// <summary>
		/// Initializes a new instance of the <see cref="NotificationIntentRouter"/> class.
		/// </summary>
		/// <param name="FilterRegistry">Filter registry deciding when to ignore a notification.</param>
		public NotificationIntentRouter(INotificationFilterRegistry FilterRegistry)
		{
			this.filterRegistry = FilterRegistry;
		}

		/// <inheritdoc/>
		public Task<NotificationRouteResult> RouteAsync(NotificationIntent Intent, bool FromUserInteraction, CancellationToken CancellationToken)
		{
			try
			{
				if (ServiceRef.NavigationService.CurrentPage is null)
					return Task.FromResult(NotificationRouteResult.Deferred);

				if (this.filterRegistry.ShouldIgnore(Intent, FromUserInteraction, CancellationToken))
					return Task.FromResult(NotificationRouteResult.Ignored);

				switch (Intent.Action)
				{
					case NotificationAction.OpenChat:
						return this.RouteChatAsync(Intent, CancellationToken);
					case NotificationAction.OpenProfile:
						return this.RouteProfileAsync(Intent, CancellationToken);
					case NotificationAction.OpenPresenceRequest:
						return this.RoutePresenceAsync(Intent, CancellationToken);
					case NotificationAction.OpenSettings:
						return this.RouteSettingsAsync(CancellationToken);
					default:
						return Task.FromResult(NotificationRouteResult.NoHandler);
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				return Task.FromResult(NotificationRouteResult.Failed);
			}
		}

		private async Task<NotificationRouteResult> RouteChatAsync(NotificationIntent Intent, CancellationToken CancellationToken)
		{
			if (string.IsNullOrEmpty(Intent.EntityId))
				return NotificationRouteResult.NoHandler;

			ContactInfo? Info = await ContactInfo.FindByBareJid(Intent.EntityId);
			string? FriendlyName = Info?.FriendlyName ?? Intent.EntityId;
			string? LegalId = Info?.LegalId;

			ChatNavigationArgs Args = new(LegalId, Intent.EntityId, FriendlyName);
			await ServiceRef.NavigationService.GoToAsync(nameof(ChatPage), Args);
			return NotificationRouteResult.Success;
		}

		private Task<NotificationRouteResult> RouteProfileAsync(NotificationIntent Intent, CancellationToken CancellationToken)
		{
			// No specific profile route mapped yet; fall back to contacts.
			return this.RoutePresenceAsync(Intent, CancellationToken);
		}

		private async Task<NotificationRouteResult> RoutePresenceAsync(NotificationIntent Intent, CancellationToken CancellationToken)
		{
			await ServiceRef.NavigationService.GoToAsync(nameof(MyContactsPage));
			return NotificationRouteResult.Deferred;
		}

		private async Task<NotificationRouteResult> RouteSettingsAsync(CancellationToken CancellationToken)
		{
			await ServiceRef.NavigationService.GoToAsync(nameof(SettingsPage));
			return NotificationRouteResult.Success;
		}
	}
}
