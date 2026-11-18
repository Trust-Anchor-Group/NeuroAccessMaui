using System.ComponentModel;
using System.Text;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.Contacts.VCard;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.UI.Pages.Contacts.Chat;
using NeuroAccessMaui.UI.Pages.Identity.ViewIdentity;
using NeuroAccessMaui.UI.Popups.Xmpp.RemoveSubscription;
using NeuroAccessMaui.UI.Popups.Xmpp.SubscribeTo;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Provisioning.Events;
using Waher.Persistence;
using Contact = NeuroAccessMaui.Services.Contacts.Contact;

namespace NeuroAccessMaui.UI.Pages.Contacts.MyContacts
{
	/// <summary>
	/// Contact Information model, including related notification information.
	/// </summary>
	public partial class ContactInfoModel : INotifyPropertyChanged, IUniqueItem
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

			this.OnPropertyChanged(nameof(this.IsFriend));
		}

		/// <summary>
		/// A person in the contacts, from their contact card. The row keeps the old contact information shape, so screens
		/// that pick a contact get what they always got.
		/// </summary>
		/// <param name="Card">The contact.</param>
		/// <param name="Events">Notification events.</param>
		public ContactInfoModel(Contact Card, params NotificationEvent[] Events)
			: this(new ContactInfo()
			{
				BareJid = Card.ChatAddress,
				LegalId = Card.LegalId,
				LegalIdentity = Card.LegalIdentity,
				FriendlyName = Card.DisplayName,
				IsThing = false
			}, Events)
		{
			this.Card = Card;

			VCard Parsed = Card.GetCard();
			IReadOnlyList<string> Organization = Parsed.Organization;
			this.Organization = Organization.Count > 0 ? Organization[0] : null;
			this.SearchText = string.Join(' ', new[] { Card.DisplayName, this.Organization ?? string.Empty }
				.Concat(Parsed.Emails.Select(Email => Email.Value))
				.Concat(Parsed.Telephones.Select(Phone => Phone.Value)));
			this.Initials = InitialsOf(Card.DisplayName);

			int Tint = (int)((uint)StringComparer.OrdinalIgnoreCase.GetHashCode(Card.Uid) % (uint)avatarTints.Length);
			this.AvatarBackground = Color.FromArgb(avatarTints[Tint].Background);
			this.AvatarText = Color.FromArgb(avatarTints[Tint].Text);
		}

		private static readonly (string Background, string Text)[] avatarTints =
		[
			("#D5E6E2", "#085E50"),
			("#DCE5F1", "#24519C"),
			("#FBE3D2", "#A84700"),
			("#ECDFF7", "#722FAD"),
			("#E6E8EA", "#444A4F")
		];

		/// <summary>
		/// The person's contact card, if the row is a person in the contacts.
		/// </summary>
		public Contact? Card { get; }

		/// <summary>
		/// The person's organization, if any.
		/// </summary>
		public string? Organization { get; }

		/// <summary>
		/// Text a search matches: name, organization, email addresses and phone numbers.
		/// </summary>
		public string SearchText { get; } = string.Empty;

		/// <summary>
		/// The initials on the avatar.
		/// </summary>
		public string Initials { get; } = string.Empty;

		/// <summary>
		/// The avatar's tint.
		/// </summary>
		public Color AvatarBackground { get; } = Colors.LightGray;

		/// <summary>
		/// The colour of the initials on the avatar.
		/// </summary>
		public Color AvatarText { get; } = Colors.Black;

		/// <summary>
		/// If the app has checked the person's ID.
		/// </summary>
		public bool IsVerified => this.Card?.IsVerified ?? false;

		/// <summary>
		/// If the person is blocked.
		/// </summary>
		public bool IsBlocked => this.Card?.IsBlocked ?? false;

		/// <summary>
		/// If the user sees when the person is online, and knows if they are.
		/// </summary>
		public bool ShowsOnlineStatus => this.OnlineAvailability is not null;

		/// <summary>
		/// The line under the name: if they are blocked or online, else their organization, else that they aren't verified.
		/// </summary>
		public string Subtitle
		{
			get
			{
				if (this.Card is null)
					return string.Empty;

				if (this.IsBlocked)
					return ServiceRef.Localizer[nameof(AppResources.ContactBlockedLabel)];

				switch (this.OnlineAvailability)
				{
					case Waher.Networking.XMPP.Availability.Online:
					case Waher.Networking.XMPP.Availability.Chat:
						return ServiceRef.Localizer[nameof(AppResources.ChatOnline)];

					case Waher.Networking.XMPP.Availability.Away:
					case Waher.Networking.XMPP.Availability.ExtendedAway:
						return ServiceRef.Localizer[nameof(AppResources.ContactAway)];

					case Waher.Networking.XMPP.Availability.DoNotDisturb:
						return ServiceRef.Localizer[nameof(AppResources.ContactBusy)];
				}

				if (!string.IsNullOrWhiteSpace(this.Organization))
					return this.Organization;

				return this.IsVerified ? string.Empty : ServiceRef.Localizer[nameof(AppResources.ContactNotVerified)];
			}
		}

		/// <summary>
		/// If there is a line under the name.
		/// </summary>
		public bool HasSubtitle => !string.IsNullOrEmpty(this.Subtitle);

		/// <summary>
		/// If the person is online, away or busy, when the user sees their status. Null otherwise.
		/// </summary>
		private Availability? OnlineAvailability
		{
			get
			{
				if (string.IsNullOrEmpty(this.contact?.BareJid))
					return null;

				try
				{
					RosterItem? Item = ServiceRef.XmppService.GetRosterItem(this.contact.BareJid);

					if (Item is null || (Item.State != SubscriptionState.To && Item.State != SubscriptionState.Both) || !Item.HasLastPresence)
						return null;

					return Item.LastPresence.Availability == Waher.Networking.XMPP.Availability.Offline ? null : Item.LastPresence.Availability;
				}
				catch (Exception)
				{
					return null;
				}
			}
		}

		/// <summary>
		/// The initials of a name: the first letters of its first two words.
		/// </summary>
		private static string InitialsOf(string Name)
		{
			string[] Words = Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			string Result = string.Concat(Words.Take(2).Select(Word => char.ToUpper(Word[0], System.Globalization.CultureInfo.CurrentCulture)));
			return Result.Length > 0 ? Result : "?";
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
					return AppColors.ContentSecondary;

				RosterItem? Item = null;
				try
				{
					Item = ServiceRef.XmppService.GetRosterItem(this.contact?.BareJid);
				}
				catch (Exception)
				{
					return AppColors.ContentSecondary;
				}
				if (Item is null)
					return AppColors.ContentSecondary;

				if (Item.State != SubscriptionState.To && Item.State != SubscriptionState.Both)
					return AppColors.ContentSecondary;

				if (!Item.HasLastPresence)
					return AppColors.ContentSecondary;

				return Item.LastPresence.Availability switch
				{
					Availability.Online or Availability.Chat => AppColors.TnPSuccessFigure,
					Availability.Away or Availability.ExtendedAway => AppColors.TnPWarningContent,
					Availability.DoNotDisturb => AppColors.TnPDangerContent,
					_ => AppColors.ContentSecondary,
				};
			}
		}

		/// <summary>
		/// Secondary Color representing the current connection state of the contact.
		/// </summary>
		public Color ConnectionSecondaryColor
		{
			get
			{
				if (string.IsNullOrEmpty(this.contact?.BareJid))
					return AppColors.ButtonNeutralNavButtonsOnContainerbgActive;

				RosterItem? Item = null;
				try
				{
					Item = ServiceRef.XmppService.GetRosterItem(this.contact?.BareJid);
				}
				catch (Exception)
				{
					return AppColors.ButtonNeutralNavButtonsOnContainerbgActive;
				}
				if (Item is null)
					return AppColors.ButtonNeutralNavButtonsOnContainerbgActive;
				if (Item.State != SubscriptionState.To && Item.State != SubscriptionState.Both)
					return AppColors.ButtonNeutralNavButtonsOnContainerbgActive;
				if (!Item.HasLastPresence)
					return AppColors.ButtonNeutralNavButtonsOnContainerbgActive;

				return Item.LastPresence.Availability switch
				{
					Availability.Online or Availability.Chat => AppColors.TnPSuccessBg,
					Availability.Away or Availability.ExtendedAway => AppColors.TnPWarningBg,
					Availability.DoNotDisturb => AppColors.TnPDangerContent,
					_ => AppColors.ButtonNeutralNavButtonsOnContainerbgActive,
				};
			}
		}

		public bool IsFriend
		{
			get
			{
				if (string.IsNullOrEmpty(this.contact?.BareJid))
					return false;
				RosterItem? Item;
				try
				{
					Item = ServiceRef.XmppService.GetRosterItem(this.contact?.BareJid);
				}
				catch (Exception)
				{
					return false;
				}
				if (Item is null)
					return false;
				return Item.State == SubscriptionState.To || Item.State == SubscriptionState.Both;
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

					await ServiceRef.PopupService.PushAsync(Page);
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

				await ServiceRef.PopupService.PushAsync(Page);
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

			this.OnPropertyChanged(nameof(this.IsFriend));
		}

		/// <summary>
		/// Method called when presence for contact has been updated.
		/// </summary>
		public void PresenceUpdated()
		{
			this.OnPropertyChanged(nameof(this.ConnectionColor));
			this.OnPropertyChanged(nameof(this.ConnectionSecondaryColor));
			this.OnPropertyChanged(nameof(this.IsFriend));
			this.OnPropertyChanged(nameof(this.ShowsOnlineStatus));
			this.OnPropertyChanged(nameof(this.Subtitle));
			this.OnPropertyChanged(nameof(this.HasSubtitle));
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

		[RelayCommand]
		public async Task ViewContact()
		{
			if (this.LegalIdentity is not null)
			{
				ViewIdentityNavigationArgs ViewIdentityArgs = new(this.LegalIdentity);

				await ServiceRef.NavigationService.GoToAsync(nameof(ViewIdentityPage), ViewIdentityArgs);
			}
			else if (!string.IsNullOrEmpty(this.LegalId))
			{
				await ServiceRef.ContractOrchestratorService.OpenLegalIdentity(this.LegalId,
					ServiceRef.Localizer[nameof(AppResources.ScannedQrCode)]);
			}
		}

		[RelayCommand]
		public async Task ViewChat()
		{
			if (!string.IsNullOrEmpty(this.BareJid) && this.Contact is not null)
			{
				ChatNavigationArgs ChatArgs = new(this.Contact);
				await ServiceRef.NavigationService.GoToAsync(nameof(ChatPage), ChatArgs, BackMethod.Inherited, this.BareJid);
			}
		}
	}
}
