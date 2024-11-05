using System.ComponentModel;
using System.Text;
using System.Windows.Input;
using Mopups.Services;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.UI.Popups.Xmpp.RemoveSubscription;
using NeuroAccessMaui.UI.Popups.Xmpp.SubscribeTo;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;

namespace NeuroAccessMaui.UI.Pages.Contacts.MyContacts
{
	/// <summary>
	/// Contact Information model, including related notification information.
	/// </summary>
	public class ContactInfoModel : INotifyPropertyChanged, IUniqueItem
	{
		private readonly ContactInfo? contact;
		private NotificationEvent[] events;

		private readonly ICommand toggleSubscriptionCommand;

		/// <summary>
		/// Contact Information model, including related notification information.
		/// </summary>
		/// <param name="Contact">Contact information.</param>
		/// <param name="Events">Notification events</param>
		public ContactInfoModel(ContactInfo? Contact, params NotificationEvent[] Events)
		{
			this.contact = Contact;
			this.events = Events;

			this.toggleSubscriptionCommand = new Command(async () => await this.ToggleSubscription(), () => this.CanToggleSubscription());
		}

		/// <inheritdoc/>
		public string UniqueName => this.contact?.ThingNotificationCategoryKey ?? string.Empty;

		/// <summary>
		/// Contact Information object in database.
		/// </summary>
		public ContactInfo? Contact => this.contact;

		/// <summary>
		/// Bare JID of contact.
		/// </summary>
		public CaseInsensitiveString? BareJid => this.contact?.BareJid;

		/// <summary>
		/// Legal ID of contact.
		/// </summary>
		public CaseInsensitiveString? LegalId => this.contact?.LegalId;

		/// <summary>
		/// Legal Identity object.
		/// </summary>
		public LegalIdentity? LegalIdentity => this.contact?.LegalIdentity;

		/// <summary>
		/// Friendly name.
		/// </summary>
		public string? FriendlyName => this.contact?.FriendlyName;

		/// <summary>
		/// Source ID
		/// </summary>
		public string? SourceId => this.contact?.SourceId;

		/// <summary>
		/// Partition
		/// </summary>
		public string? Partition => this.contact?.Partition;

		/// <summary>
		/// Node ID
		/// </summary>
		public string? NodeId => this.contact?.NodeId;

		/// <summary>
		/// Registry JID
		/// </summary>
		public CaseInsensitiveString? RegistryJid => this.contact?.RegistryJid;

		/// <summary>
		/// Subscribe to this contact
		/// </summary>
		public bool? SubcribeTo => this.contact?.SubscribeTo;

		/// <summary>
		/// Allow subscriptions from this contact
		/// </summary>
		public bool? AllowSubscriptionFrom => this.contact?.AllowSubscriptionFrom;

		/// <summary>
		/// The contact is a thing
		/// </summary>
		public bool? IsThing => this.contact?.IsThing;

		/// <summary>
		/// If the account is registered as the owner of the thing.
		/// </summary>
		public bool? Owner => this.contact?.Owner;

		/// <summary>
		/// Meta-data related to a thing.
		/// </summary>
		public Property[]? MetaData => this.contact?.MetaData;

		/// <summary>
		/// Notification events.
		/// </summary>
		public NotificationEvent[] Events => this.events;

		/// <summary>
		/// If the contact has associated events.
		/// </summary>
		public bool HasEvents => this.events is not null && this.events.Length > 0;

		/// <summary>
		/// Number of events associated with contact.
		/// </summary>
		public int NrEvents => this.events?.Length ?? 0;

		/// <summary>
		/// A color representing the current connection state of the contact.
		/// </summary>
		public Color ConnectionColor
		{
			get
			{
				if (string.IsNullOrEmpty(this.contact?.BareJid))
					return Colors.Transparent;

				RosterItem? Item = null;
				try
				{
					Item = ServiceRef.XmppService.GetRosterItem(this.contact?.BareJid);
				}
				catch (Exception)
				{
					return Colors.Transparent;
				}
				if (Item is null)
					return Colors.Transparent;

				if (Item.State != SubscriptionState.To && Item.State != SubscriptionState.Both)
					return Colors.Transparent;

				if (!Item.HasLastPresence)
					return Colors.LightSalmon;

				return Item.LastPresence.Availability switch
				{
					Availability.Online or Availability.Chat => Colors.LightGreen,
					Availability.Away or Availability.ExtendedAway => Colors.LightYellow,
					_ => Colors.LightSalmon,
				};
			}
		}

		/// <summary>
		/// Command to execute when user wants to toggle XMPP subcription.
		/// </summary>
		public ICommand ToggleSubscriptionCommand => this.toggleSubscriptionCommand;

		/// <summary>
		/// If toggle subscription can be performed on the contact.
		/// </summary>
		/// <returns>If command is enabled.</returns>
		public bool CanToggleSubscription()
		{
			return !string.IsNullOrEmpty(this.contact?.BareJid);
		}

		/// <summary>
		/// Subscribes to an unsubscribed contact; unsubscribes from a subscribed one, with user permission.
		/// </summary>
		public async Task ToggleSubscription()
		{
			if (string.IsNullOrEmpty(this.contact?.BareJid))
				return;

			RosterItem? Item = null;
			try
			{
				Item = ServiceRef.XmppService.GetRosterItem(this.contact?.BareJid);
			}
			catch
			{
				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.Error)], ServiceRef.Localizer[nameof(AppResources.NetworkSeemsToBeMissing)], ServiceRef.Localizer[nameof(AppResources.Ok)]);
				return;
			}

			bool Subscribed;

			if (Item is null)
				Subscribed = false;
			else
				Subscribed = Item.State == SubscriptionState.To || Item.State == SubscriptionState.Both;

			if (Subscribed)
			{
				if (!await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.Question)],
					ServiceRef.Localizer[nameof(AppResources.RemoveSubscriptionFrom), this.FriendlyName ?? string.Empty],
					ServiceRef.Localizer[nameof(AppResources.Yes)], ServiceRef.Localizer[nameof(AppResources.Cancel)]))
				{
					return;
				}

				ServiceRef.XmppService.RequestPresenceUnsubscription(this.BareJid);

				if (Item is not null && (Item.State == SubscriptionState.From || Item.State == SubscriptionState.Both))
				{
					RemoveSubscriptionViewModel ViewModel = new(this.BareJid);
					RemoveSubscriptionPopup Page = new(ViewModel);

					await MopupService.Instance.PushAsync(Page);
					bool? Remove = await ViewModel.Result;

					if (Remove.HasValue && Remove.Value)
					{
						ServiceRef.XmppService.RequestRevokePresenceSubscription(this.BareJid);

						if ((this.contact?.AllowSubscriptionFrom.HasValue ?? false) && this.contact.AllowSubscriptionFrom.Value)
						{
							this.contact.AllowSubscriptionFrom = null;
							await Database.Update(this.contact);
						}
					}
				}
			}
			else
			{
				SubscribeToViewModel ViewModel = new(this.BareJid);
				SubscribeToPopup Page = new(ViewModel);

				await MopupService.Instance.PushAsync(Page);
				bool? SubscribeTo = await ViewModel.Result;

				if (SubscribeTo.HasValue && SubscribeTo.Value)
				{
					string IdXml;

					if (ServiceRef.TagProfile.LegalIdentity is null)
						IdXml = string.Empty;
					else
					{
						StringBuilder Xml = new();
						ServiceRef.TagProfile.LegalIdentity.Serialize(Xml, true, true, true, true, true, true, true);
						IdXml = Xml.ToString();
					}

					ServiceRef.XmppService.RequestPresenceSubscription(this.BareJid, IdXml);
				}
			}
		}

		/// <summary>
		/// Method called when presence for contact has been updated.
		/// </summary>
		public void PresenceUpdated()
		{
			this.OnPropertyChanged(nameof(this.ConnectionColor));
		}

		/// <summary>
		/// Called when a property has changed.
		/// </summary>
		/// <param name="PropertyName">Name of property</param>
		public void OnPropertyChanged(string PropertyName)
		{
			try
			{
				this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Occurs when a property value changes.
		/// </summary>
		public event PropertyChangedEventHandler? PropertyChanged;

		/// <summary>
		/// Method called when notifications for the item have been updated.
		/// </summary>
		/// <param name="Events">Updated set of events.</param>
		public void NotificationsUpdated(NotificationEvent[] Events)
		{
			this.events = Events;

			this.OnPropertyChanged(nameof(this.Events));
			this.OnPropertyChanged(nameof(this.HasEvents));
			this.OnPropertyChanged(nameof(this.NrEvents));
		}

		/// <summary>
		/// Adds a notification event.
		/// </summary>
		/// <param name="Event">Notification event.</param>
		public void AddEvent(NotificationEvent Event)
		{
			if (this.events is null)
				this.NotificationsUpdated([Event]);
			else
			{
				foreach (NotificationEvent Event2 in this.events)
				{
					if (Event2.ObjectId == Event.ObjectId)
						return;
				}

				int c = this.events.Length;
				NotificationEvent[] NewArray = new NotificationEvent[c + 1];
				Array.Copy(this.events, 0, NewArray, 0, c);
				NewArray[c] = Event;

				this.NotificationsUpdated(NewArray);
			}
		}

		/// <summary>
		/// Removes a notification event.
		/// </summary>
		/// <param name="Event">Notification event.</param>
		public void RemoveEvent(NotificationEvent Event)
		{
			if (this.events is not null)
			{
				int i, c = this.events.Length;

				for (i = 0; i < c; i++)
				{
					NotificationEvent Event2 = this.events[i];

					if (Event2.ObjectId == Event.ObjectId)
					{
						NotificationEvent[] NewArray = new NotificationEvent[c - 1];

						if (i > 0)
							Array.Copy(this.events, 0, NewArray, 0, i);

						if (i < c - 1)
							Array.Copy(this.events, i + 1, NewArray, i, c - i - 1);

						this.NotificationsUpdated(NewArray);

						return;
					}
				}

			}
		}
	}
}
