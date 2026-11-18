using System.Text;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.Notification;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Abuse;

namespace NeuroAccessMaui.Services.Contacts
{
	/// <summary>
	/// Requests from people who want to add the user as a contact. They wait here, and in a notification, until the user
	/// answers them. The server sends unanswered requests again each time the app connects, so they are kept in memory only.
	/// </summary>
	public static class ContactRequests
	{
		private static readonly Dictionary<string, ContactRequest> pending = new(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// Raised when a request has arrived or has been answered.
		/// </summary>
		public static event EventHandler? Changed;

		/// <summary>
		/// The requests that wait for an answer, oldest first.
		/// </summary>
		public static IReadOnlyList<ContactRequest> Pending
		{
			get
			{
				lock (pending)
				{
					return [.. pending.Values.OrderBy(Request => Request.Received)];
				}
			}
		}

		/// <summary>
		/// Finds the request from a chat address.
		/// </summary>
		/// <param name="ChatAddress">The chat address.</param>
		/// <returns>The request, if one waits.</returns>
		public static ContactRequest? Find(string? ChatAddress)
		{
			if (string.IsNullOrEmpty(ChatAddress))
				return null;

			lock (pending)
			{
				return pending.TryGetValue(ChatAddress, out ContactRequest? Request) ? Request : null;
			}
		}

		/// <summary>
		/// Adds a request that has arrived, and a notification about it.
		/// </summary>
		/// <param name="Request">The request.</param>
		public static async Task AddAsync(ContactRequest Request)
		{
			lock (pending)
			{
				pending[Request.ChatAddress] = Request;
			}

			RaiseChanged();

			try
			{
				NotificationIntent Intent = new()
				{
					Channel = Constants.PushChannels.Messages,
					Title = ServiceRef.Localizer[nameof(AppResources.ContactRequestWantsToAddYou), Request.Name],
					Body = ServiceRef.Localizer[nameof(AppResources.ContactRequestNotificationBody)],
					Action = NotificationAction.OpenPresenceRequest,
					EntityId = Request.ChatAddress,
					CorrelationId = CorrelationId(Request.ChatAddress),
					Presentation = NotificationPresentation.RenderAndStore
				};

				await Notifications.AddAsync(Intent, NotificationSource.Xmpp, null, CancellationToken.None);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Accepts a request. The person becomes a contact, and a request is sent back, so both can chat and see when the other
		/// is online.
		/// </summary>
		/// <param name="Request">The request.</param>
		/// <returns>The contact.</returns>
		public static async Task<Contact> AcceptAsync(ContactRequest Request)
		{
			await ConnectBothWaysAsync(Request);

			Contact Contact = await ContactStore.AddOrUpdateAsync(Request.ChatAddress, Request.Identity, Request.Name);
			if (Contact.IsBlocked || Contact.StoppedSharing)
			{
				Contact.IsBlocked = false;
				Contact.StoppedSharing = false;
				await ContactStore.SaveAsync(Contact, Contact.GetCard());
			}

			await RemoveAsync(Request.ChatAddress);
			return Contact;
		}

		/// <summary>
		/// What accepting a request does: the person may now see when the user is online, and a request is sent back, so that
		/// the user may also see when they are online. Their app accepts the request sent back without asking, so one tap
		/// connects both ways. Each side can later stop sharing with the switches on the contact card.
		/// </summary>
		/// <param name="Request">The request that is accepted.</param>
		/// <remarks>
		/// The other half is <see cref="IsAcceptedWithoutAsking"/>, which accepts the request sent back.
		/// </remarks>
		private static async Task ConnectBothWaysAsync(ContactRequest Request)
		{
			await Request.Request.Accept();

			RosterItem? Item = ServiceRef.XmppService.GetRosterItem(Request.ChatAddress);
			if (Item is null || (Item.State != SubscriptionState.To && Item.State != SubscriptionState.Both))
				ServiceRef.XmppService.RequestPresenceSubscription(Request.ChatAddress, OwnIdentityXml());
		}

		/// <summary>
		/// If a request is accepted without asking the user: it is the request sent back by someone who accepted the user's own
		/// request, or it comes from someone the user already shares with. This is the other half of
		/// <see cref="ConnectBothWaysAsync"/>. If the user has stopped letting the person see when they are online, the
		/// request waits for the user to answer it.
		/// </summary>
		/// <param name="Item">The person's entry in the user's list on the server, if any.</param>
		/// <param name="Contact">The person's contact, if any.</param>
		/// <returns>If the request is accepted without asking.</returns>
		public static bool IsAcceptedWithoutAsking(RosterItem? Item, Contact? Contact)
		{
			if (Contact is not null && Contact.StoppedSharing)
				return false;

			return Item is not null && (Item.State != SubscriptionState.None || Item.PendingSubscription == PendingSubscription.Subscribe);
		}

		/// <summary>
		/// Turns down a request and blocks the person, so later requests from them are turned down without asking. If a reason
		/// is given, the person is also reported to the server.
		/// </summary>
		/// <param name="Request">The request.</param>
		/// <param name="Reason">Why the person is reported, or null to only block them.</param>
		public static async Task BlockAsync(ContactRequest Request, ReportingReason? Reason)
		{
			await Request.Request.Decline();

			Contact Contact = await ContactStore.FindByChatAddressAsync(Request.ChatAddress) ??
				await ContactStore.AddOrUpdateAsync(Request.ChatAddress, null, Request.Name);

			if (!Contact.IsBlocked)
			{
				Contact.IsBlocked = true;
				await ContactStore.SaveAsync(Contact, Contact.GetCard());
			}

			if (Reason.HasValue)
				await ServiceRef.XmppService.ReportAsync(Request.ChatAddress, Reason.Value);

			await RemoveAsync(Request.ChatAddress);
		}

		/// <summary>
		/// Removes a request, and its notification, when it has been answered.
		/// </summary>
		/// <param name="ChatAddress">The chat address it came from.</param>
		public static async Task RemoveAsync(string ChatAddress)
		{
			bool Removed;

			lock (pending)
			{
				Removed = pending.Remove(ChatAddress);
			}

			if (Removed)
				RaiseChanged();

			try
			{
				string Correlation = CorrelationId(ChatAddress);
				IReadOnlyList<NotificationRecord> Records = await Notifications.GetAsync(new NotificationQuery()
				{
					Channels = [Constants.PushChannels.Messages]
				}, CancellationToken.None);

				string[] Ids = [.. Records.Where(Record => Record.CorrelationId == Correlation).Select(Record => Record.Id)];
				if (Ids.Length > 0)
					await Notifications.DeleteAsync(Ids, CancellationToken.None);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// The user's own ID, as XML to send with a request, so the other person sees who is asking. Empty if the user has no ID.
		/// </summary>
		/// <returns>The XML.</returns>
		public static string OwnIdentityXml()
		{
			if (ServiceRef.TagProfile.LegalIdentity is null)
				return string.Empty;

			StringBuilder Xml = new();
			ServiceRef.TagProfile.LegalIdentity.Serialize(Xml, true, true, true, true, true, true, true);
			return Xml.ToString();
		}

		private static INotificationServiceV2 Notifications => ServiceRef.Provider.GetRequiredService<INotificationServiceV2>();

		/// <summary>
		/// What ties the notification about a request to the request, so the notification goes when the request is answered.
		/// </summary>
		private static string CorrelationId(string ChatAddress)
		{
			return "ContactRequest:" + ChatAddress.ToLowerInvariant();
		}

		private static void RaiseChanged()
		{
			try
			{
				Changed?.Invoke(null, EventArgs.Empty);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}
	}
}
