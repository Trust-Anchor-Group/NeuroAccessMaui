using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EDaler;
using EDaler.Uris;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.Contacts.VCard;
using NeuroAccessMaui.Services.Groups;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.UI.Toasts;
using NeuroAccessMaui.UI.Pages.Contacts.Chat;
using NeuroAccessMaui.UI.Pages.Contacts.EditContact;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts;
using NeuroAccessMaui.UI.Pages.Wallet;
using NeuroAccessMaui.UI.Pages.Wallet.Payment;
using NeuroAccessMaui.UI.Popups.Groups;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Events;
using Contact = NeuroAccessMaui.Services.Contacts.Contact;

namespace NeuroAccessMaui.UI.Pages.Contacts.ContactCard
{
	/// <summary>
	/// The view model of a contact's card: who they are, how to reach them, what they share with the user, and what can be
	/// done with them.
	/// </summary>
	public partial class ContactCardViewModel : BaseViewModel
	{
		private readonly string? chatAddress;
		private readonly string? legalId;
		private Contact? contact;
		private bool refreshing;

		/// <summary>
		/// Creates the view model of a contact's card.
		/// </summary>
		/// <param name="Args">Which contact to show.</param>
		public ContactCardViewModel(ContactCardNavigationArgs? Args)
		{
			this.contact = Args?.Contact;
			this.chatAddress = Args?.ChatAddress;
			this.legalId = Args?.LegalId;
		}

		#region Properties

		/// <summary>
		/// The contact's name.
		/// </summary>
		[ObservableProperty]
		private string name = string.Empty;

		/// <summary>
		/// The line under the name: role and organization.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasSubtitle))]
		private string subtitle = string.Empty;

		/// <summary>
		/// If there is a line under the name.
		/// </summary>
		public bool HasSubtitle => !string.IsNullOrEmpty(this.Subtitle);

		/// <summary>
		/// Initials on the avatar, when the contact has no photo.
		/// </summary>
		[ObservableProperty]
		private string initials = string.Empty;

		/// <summary>
		/// The contact's photo, if their card has one.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasPhoto))]
		private ImageSource? photo;

		/// <summary>
		/// If the contact has a photo.
		/// </summary>
		public bool HasPhoto => this.Photo is not null;

		/// <summary>
		/// If the app has checked the contact's ID.
		/// </summary>
		[ObservableProperty]
		private bool isVerified;

		/// <summary>
		/// If the user has blocked the contact.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(BlockText))]
		private bool isBlocked;

		/// <summary>
		/// Online, Away or Busy, when the user sees when the contact is online and it is known.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasPresence))]
		private string presenceText = string.Empty;

		/// <summary>
		/// If the contact's online status is shown.
		/// </summary>
		public bool HasPresence => !string.IsNullOrEmpty(this.PresenceText);

		/// <summary>
		/// The colour of the online status dot.
		/// </summary>
		[ObservableProperty]
		private Color presenceColor = Colors.Transparent;

		/// <summary>
		/// Labels the user has given the contact.
		/// </summary>
		public ObservableCollection<string> Labels { get; } = [];

		/// <summary>
		/// Phone numbers, email addresses, addresses, organization and birthday.
		/// </summary>
		public ObservableCollection<ContactDetailItem> Details { get; } = [];

		/// <summary>
		/// If the contact has any details.
		/// </summary>
		[ObservableProperty]
		private bool hasDetails;

		/// <summary>
		/// The user's notes about the contact.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasNotes))]
		private string notes = string.Empty;

		/// <summary>
		/// If the contact has notes.
		/// </summary>
		public bool HasNotes => !string.IsNullOrWhiteSpace(this.Notes);

		/// <summary>
		/// If the user can chat with the contact.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(MessageCommand))]
		private bool canChat;

		/// <summary>
		/// If the user can pay the contact.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(PayCommand))]
		private bool canPay;

		/// <summary>
		/// If the user sees when the contact is online, or has asked to.
		/// </summary>
		[ObservableProperty]
		private bool seesOnline;

		/// <summary>
		/// What the switch for seeing the contact online says.
		/// </summary>
		[ObservableProperty]
		private string seeOnlineText = string.Empty;

		/// <summary>
		/// A note under the switch for seeing the contact online, such as that the request waits for them.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasSeeOnlineHint))]
		private string seeOnlineHint = string.Empty;

		/// <summary>
		/// If there is a note under the switch for seeing the contact online.
		/// </summary>
		public bool HasSeeOnlineHint => !string.IsNullOrEmpty(this.SeeOnlineHint);

		/// <summary>
		/// If the contact sees when the user is online.
		/// </summary>
		[ObservableProperty]
		private bool letsSeeOnline;

		/// <summary>
		/// If the switch for letting the contact see the user online can be changed.
		/// </summary>
		[ObservableProperty]
		private bool canChangeLetsSeeOnline;

		/// <summary>
		/// What the switch for letting the contact see the user online says.
		/// </summary>
		[ObservableProperty]
		private string letSeeOnlineText = string.Empty;

		/// <summary>
		/// A note under the switch for letting the contact see the user online, when it can't be turned on.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasLetSeeOnlineHint))]
		private string letSeeOnlineHint = string.Empty;

		/// <summary>
		/// If there is a note under the switch for letting the contact see the user online.
		/// </summary>
		public bool HasLetSeeOnlineHint => !string.IsNullOrEmpty(this.LetSeeOnlineHint);

		/// <summary>
		/// If the technical details show.
		/// </summary>
		[ObservableProperty]
		private bool isTechnicalExpanded;

		/// <summary>
		/// The contact's chat address, shown only in the technical details.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasChatAddress))]
		private string chatAddressText = string.Empty;

		/// <summary>
		/// If the contact has a chat address.
		/// </summary>
		public bool HasChatAddress => !string.IsNullOrEmpty(this.ChatAddressText);

		/// <summary>
		/// The contact's ID number, shown only in the technical details.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasLegalId))]
		private string legalIdText = string.Empty;

		/// <summary>
		/// If the contact has an ID number.
		/// </summary>
		public bool HasLegalId => !string.IsNullOrEmpty(this.LegalIdText);

		/// <summary>
		/// What the block button says.
		/// </summary>
		public string BlockText => this.IsBlocked ?
			ServiceRef.Localizer[nameof(AppResources.ContactUnblock), this.Name] :
			ServiceRef.Localizer[nameof(AppResources.ContactBlock), this.Name];

		/// <summary>
		/// If the contact was found.
		/// </summary>
		[ObservableProperty]
		private bool isFound;

		#endregion

		#region Life cycle

		/// <inheritdoc/>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			ServiceRef.XmppService.OnPresence += this.XmppService_OnPresence;
			ServiceRef.XmppService.OnRosterItemAdded += this.XmppService_OnRosterItem;
			ServiceRef.XmppService.OnRosterItemUpdated += this.XmppService_OnRosterItem;
			ServiceRef.XmppService.OnRosterItemRemoved += this.XmppService_OnRosterItem;
			ContactRequests.Changed += this.ContactRequests_Changed;
		}

		/// <inheritdoc/>
		public override async Task OnDisposeAsync()
		{
			ServiceRef.XmppService.OnPresence -= this.XmppService_OnPresence;
			ServiceRef.XmppService.OnRosterItemAdded -= this.XmppService_OnRosterItem;
			ServiceRef.XmppService.OnRosterItemUpdated -= this.XmppService_OnRosterItem;
			ServiceRef.XmppService.OnRosterItemRemoved -= this.XmppService_OnRosterItem;
			ContactRequests.Changed -= this.ContactRequests_Changed;

			await base.OnDisposeAsync();
		}

		/// <inheritdoc/>
		public override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();

			// The contact is read again, as it may have been changed on the edit page.
			try
			{
				Contact? Found;

				if (!string.IsNullOrEmpty(this.contact?.ObjectId))
					Found = await ContactStore.FindByUidAsync(this.contact.Uid) ?? this.contact;
				else
					Found = this.contact ?? await ContactStore.FindAsync(this.chatAddress, this.legalId);

				this.contact = Found;
				this.Show(Found);
				await this.ShowTogetherAsync();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		#endregion

		#region Together

		private IReadOnlyList<ChatGroup> sharedGroups = [];

		/// <summary>
		/// If the user is in any groups together with the contact.
		/// </summary>
		[ObservableProperty]
		private bool hasSharedGroups;

		/// <summary>
		/// How many groups the user is in together with the contact.
		/// </summary>
		[ObservableProperty]
		private string sharedGroupsText = string.Empty;

		/// <summary>
		/// Shows what the user and the contact do together: the groups they are both in.
		/// </summary>
		private async Task ShowTogetherAsync()
		{
			string Address = this.contact?.ChatAddress?.Value ?? string.Empty;
			IReadOnlyList<ChatGroup> Groups = Address.Length == 0 ? [] : await GroupStore.GetSharedWithAsync(Address);

			this.sharedGroups = Groups;
			this.HasSharedGroups = Groups.Count > 0;
			this.SharedGroupsText = Groups.Count == 1 ?
				ServiceRef.Localizer[nameof(AppResources.GroupsSharedOne)] :
				ServiceRef.Localizer[nameof(AppResources.GroupsSharedMany), Groups.Count];
		}

		/// <summary>
		/// Opens the group the user and the contact are both in, or lets the user pick one if there are several.
		/// </summary>
		[RelayCommand]
		private async Task OpenSharedGroupsAsync()
		{
			try
			{
				ChatGroup? Group = this.sharedGroups.Count == 1 ? this.sharedGroups[0] :
					this.sharedGroups.Count > 1 ? await SharedGroupsPopup.ShowAsync(this.Name, this.sharedGroups) : null;

				if (Group is null)
					return;

				ChatNavigationArgs Args = new(null, Group.Address.Value, Group.Name);
				await ServiceRef.NavigationService.GoToAsync(nameof(ChatPage), Args, BackMethod.Pop, Group.Address.Value);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		#endregion

		#region Showing the contact

		private void Show(Contact? Contact)
		{
			this.refreshing = true;

			try
			{
				this.IsFound = Contact is not null;
				if (Contact is null)
					return;

				VCard Card = Contact.GetCard();
				LegalIdentity? Identity = Contact.LegalIdentity;

				this.Name = Contact.DisplayName;
				this.Initials = InitialsOf(Contact.DisplayName);
				this.IsVerified = Contact.IsVerified;
				this.IsBlocked = Contact.IsBlocked;
				this.CanChat = Contact.CanChat;
				this.CanPay = (Contact.CanChat || !string.IsNullOrEmpty(Contact.LegalId?.Value)) &&
					!string.IsNullOrEmpty(ServiceRef.TagProfile.EDalerJid);
				this.ChatAddressText = Contact.ChatAddress?.Value ?? string.Empty;
				this.LegalIdText = Contact.LegalId?.Value ?? string.Empty;
				this.Notes = Card.Note ?? string.Empty;
				this.Photo = PhotoOf(Card);

				List<string> Subtitle = [];
				if (!string.IsNullOrWhiteSpace(Card.Title))
					Subtitle.Add(Card.Title.Trim());

				if (Card.Organization.Count > 0 && !string.IsNullOrWhiteSpace(Card.Organization[0]))
					Subtitle.Add(Card.Organization[0]);

				this.Subtitle = string.Join(", ", Subtitle);

				this.Labels.Clear();
				foreach (string Label in Card.Categories)
					this.Labels.Add(Label);

				this.Details.Clear();
				foreach (ContactDetailItem Item in DetailsOf(Card, Identity))
					this.Details.Add(Item);

				this.HasDetails = this.Details.Count > 0;
				this.OnPropertyChanged(nameof(this.BlockText));

				this.ShowSharing();
			}
			finally
			{
				this.refreshing = false;
			}
		}

		/// <summary>
		/// Shows what the user and the contact share with each other: if each sees when the other is online, and the
		/// contact's online status.
		/// </summary>
		private void ShowSharing()
		{
			bool Bak = this.refreshing;
			this.refreshing = true;

			try
			{
				string Address = this.ChatAddressText;
				RosterItem? Item = string.IsNullOrEmpty(Address) ? null : ServiceRef.XmppService.GetRosterItem(Address);
				bool Sees = Item is not null && (Item.State == SubscriptionState.To || Item.State == SubscriptionState.Both);
				bool Asked = Item is not null && !Sees && Item.PendingSubscription == PendingSubscription.Subscribe;
				bool Lets = Item is not null && (Item.State == SubscriptionState.From || Item.State == SubscriptionState.Both);
				bool TheyAsk = ContactRequests.Find(Address) is not null;

				this.SeeOnlineText = ServiceRef.Localizer[nameof(AppResources.ContactSeeOnline), this.Name];
				this.LetSeeOnlineText = ServiceRef.Localizer[nameof(AppResources.ContactLetSeeOnline), this.Name];

				this.SeesOnline = Sees || Asked;
				this.SeeOnlineHint = Asked ? ServiceRef.Localizer[nameof(AppResources.ContactSeeOnlinePending), this.Name] : string.Empty;

				this.LetsSeeOnline = Lets;
				this.CanChangeLetsSeeOnline = Lets || TheyAsk;
				this.LetSeeOnlineHint = Lets || TheyAsk ? string.Empty : ServiceRef.Localizer[nameof(AppResources.ContactLetSeeOnlineHint), this.Name];

				if (Sees && Item is not null && Item.HasLastPresence)
				{
					switch (Item.LastPresence.Availability)
					{
						case Availability.Online:
						case Availability.Chat:
							this.PresenceText = ServiceRef.Localizer[nameof(AppResources.ChatOnline)];
							this.PresenceColor = AppColors.TnPSuccessFigure;
							break;

						case Availability.Away:
						case Availability.ExtendedAway:
							this.PresenceText = ServiceRef.Localizer[nameof(AppResources.ContactAway)];
							this.PresenceColor = AppColors.TnPWarningContent;
							break;

						case Availability.DoNotDisturb:
							this.PresenceText = ServiceRef.Localizer[nameof(AppResources.ContactBusy)];
							this.PresenceColor = AppColors.TnPDangerContent;
							break;

						default:
							this.PresenceText = string.Empty;
							break;
					}
				}
				else
					this.PresenceText = string.Empty;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
			finally
			{
				this.refreshing = Bak;
			}
		}

		/// <summary>
		/// The rows of contact details: phone numbers, email addresses, addresses, organization and birthday. A value the
		/// same as in the contact's checked ID is marked as coming from the ID.
		/// </summary>
		private static List<ContactDetailItem> DetailsOf(VCard Card, LegalIdentity? Identity)
		{
			List<ContactDetailItem> Result = [];
			string IdPhone = Identity?[Constants.XmppProperties.Phone] ?? string.Empty;
			string IdEmail = Identity?[Constants.XmppProperties.EMail] ?? string.Empty;
			string IdOrganization = Identity?[Constants.XmppProperties.OrgName] ?? string.Empty;

			foreach (VCardTypedValue Phone in Card.Telephones)
			{
				bool FromId = !string.IsNullOrEmpty(IdPhone) && DigitsOf(IdPhone) == DigitsOf(Phone.Value);
				Result.Add(new ContactDetailItem(ContactDetailKind.Phone, PhoneLabel(Phone.Types), Phone.Value, FromId));
			}

			foreach (VCardTypedValue Email in Card.Emails)
			{
				bool FromId = string.Equals(IdEmail.Trim(), Email.Value, StringComparison.OrdinalIgnoreCase);
				Result.Add(new ContactDetailItem(ContactDetailKind.Email, ServiceRef.Localizer[nameof(AppResources.ContactEmail)], Email.Value, FromId));
			}

			foreach (VCardAddress Address in Card.Addresses)
			{
				string Line = string.IsNullOrWhiteSpace(Address.Label) ? AddressLine(Address) : Address.Label.Replace('\n', ' ').Trim();
				if (Line.Length > 0)
					Result.Add(new ContactDetailItem(ContactDetailKind.Address, ServiceRef.Localizer[nameof(AppResources.Address)], Line, false));
			}

			if (Card.Organization.Count > 0)
			{
				string Organization = string.Join(", ", Card.Organization.Where(Part => !string.IsNullOrWhiteSpace(Part)));
				bool FromId = !string.IsNullOrEmpty(IdOrganization) && string.Equals(IdOrganization.Trim(), Card.Organization[0], StringComparison.OrdinalIgnoreCase);

				if (Organization.Length > 0)
					Result.Add(new ContactDetailItem(ContactDetailKind.Organization, ServiceRef.Localizer[nameof(AppResources.ContactOrganization)], Organization, FromId));
			}

			string? Birthday = BirthdayText(Card.Birthday);
			if (!string.IsNullOrEmpty(Birthday))
				Result.Add(new ContactDetailItem(ContactDetailKind.Birthday, ServiceRef.Localizer[nameof(AppResources.ContactBirthday)], Birthday, false));

			return Result;
		}

		private static string PhoneLabel(IReadOnlyList<string> Types)
		{
			if (Types.Any(Type => Type.Equals("cell", StringComparison.OrdinalIgnoreCase)))
				return ServiceRef.Localizer[nameof(AppResources.ContactMobile)];

			if (Types.Any(Type => Type.Equals("work", StringComparison.OrdinalIgnoreCase)))
				return ServiceRef.Localizer[nameof(AppResources.ContactWork)];

			if (Types.Any(Type => Type.Equals("home", StringComparison.OrdinalIgnoreCase)))
				return ServiceRef.Localizer[nameof(AppResources.ContactHome)];

			return ServiceRef.Localizer[nameof(AppResources.Phone)];
		}

		/// <summary>
		/// An address on one line: street, then postal code and city, then region and country.
		/// </summary>
		private static string AddressLine(VCardAddress Address)
		{
			List<string> Parts = [];

			void Add(string? Part)
			{
				if (!string.IsNullOrWhiteSpace(Part))
					Parts.Add(Part.Trim());
			}

			Add(Address.Extended);
			Add(Address.Street);
			Add((Address.PostalCode + " " + Address.Locality).Trim());
			Add(Address.Region);
			Add(Address.Country);

			return string.Join(", ", Parts);
		}

		/// <summary>
		/// A birthday as written in a card, such as 1985-04-12 or --04-12, shown as a date.
		/// </summary>
		private static string? BirthdayText(string? Birthday)
		{
			if (string.IsNullOrWhiteSpace(Birthday))
				return null;

			string Text = Birthday.Trim();
			CultureInfo Culture = CultureInfo.CurrentCulture;

			if (DateTime.TryParseExact(Text, ["yyyy-MM-dd", "yyyyMMdd"], CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime Date))
				return Date.ToString("D", Culture);

			if (Text.StartsWith("--", StringComparison.Ordinal) &&
				DateTime.TryParseExact("2000" + Text[2..].Replace("-", string.Empty), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out Date))
			{
				return Date.ToString("M", Culture);
			}

			return Text;
		}

		private static string DigitsOf(string Phone)
		{
			return new string([.. Phone.Where(char.IsDigit)]);
		}

		/// <summary>
		/// The initials of a name: the first letters of its first two words.
		/// </summary>
		private static string InitialsOf(string Name)
		{
			StringBuilder Result = new();

			foreach (string Word in Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2))
			{
				if (char.IsLetterOrDigit(Word[0]))
					Result.Append(char.ToUpper(Word[0], CultureInfo.CurrentCulture));
			}

			return Result.Length > 0 ? Result.ToString() : "?";
		}

		private static ImageSource? PhotoOf(VCard Card)
		{
			try
			{
				VCardPhoto? Photo = Card.Photo;
				if (Photo?.Data is byte[] Data && Data.Length > 0)
					return ImageSource.FromStream(() => new MemoryStream(Data));

				if (!string.IsNullOrEmpty(Photo?.Uri) && Uri.TryCreate(Photo.Uri, UriKind.Absolute, out Uri? Link) && Link.Scheme == Uri.UriSchemeHttps)
					return ImageSource.FromUri(Link);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}

			return null;
		}

		#endregion

		#region Events

		private Task XmppService_OnPresence(object? Sender, PresenceEventArgs e)
		{
			if (string.Equals(e.FromBareJID, this.ChatAddressText, StringComparison.OrdinalIgnoreCase))
				MainThread.BeginInvokeOnMainThread(this.ShowSharing);

			return Task.CompletedTask;
		}

		private Task XmppService_OnRosterItem(object? Sender, RosterItem Item)
		{
			if (string.Equals(Item.BareJid, this.ChatAddressText, StringComparison.OrdinalIgnoreCase))
				MainThread.BeginInvokeOnMainThread(this.ShowSharing);

			return Task.CompletedTask;
		}

		private void ContactRequests_Changed(object? Sender, EventArgs e)
		{
			MainThread.BeginInvokeOnMainThread(this.ShowSharing);
		}

		#endregion

		#region Sharing online status

		partial void OnSeesOnlineChanged(bool value)
		{
			if (this.refreshing || string.IsNullOrEmpty(this.ChatAddressText))
				return;

			try
			{
				if (value)
					ServiceRef.XmppService.RequestPresenceSubscription(this.ChatAddressText, ContactRequests.OwnIdentityXml());
				else
					ServiceRef.XmppService.RequestPresenceUnsubscription(this.ChatAddressText);

				this.SeeOnlineHint = value ? ServiceRef.Localizer[nameof(AppResources.ContactSeeOnlinePending), this.Name] : string.Empty;
				this.PresenceText = value ? this.PresenceText : string.Empty;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				_ = ServiceRef.UiService.DisplayException(Ex);
			}
		}

		partial void OnLetsSeeOnlineChanged(bool value)
		{
			if (this.refreshing || string.IsNullOrEmpty(this.ChatAddressText))
				return;

			_ = this.ChangeLetsSeeOnlineAsync(value);
		}

		/// <summary>
		/// Lets the contact see when the user is online, by accepting their waiting request, or stops letting them.
		/// </summary>
		private async Task ChangeLetsSeeOnlineAsync(bool Lets)
		{
			try
			{
				if (Lets)
				{
					ContactRequest? Request = ContactRequests.Find(this.ChatAddressText);
					if (Request is not null)
					{
						await Request.Request.Accept();
						await ContactRequests.RemoveAsync(this.ChatAddressText);
					}
				}
				else
					ServiceRef.XmppService.RequestRevokePresenceSubscription(this.ChatAddressText);

				// Remembered, so that a later request from the contact asks the user again.
				if (this.contact is not null && this.contact.StoppedSharing == Lets)
				{
					this.contact.StoppedSharing = !Lets;
					await ContactStore.SaveAsync(this.contact, this.contact.GetCard());
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		#endregion

		#region Actions

		/// <summary>
		/// Opens the page where the contact is changed.
		/// </summary>
		[RelayCommand]
		private async Task EditAsync()
		{
			if (this.contact is null)
				return;

			await ServiceRef.NavigationService.GoToAsync(nameof(EditContactPage), new EditContactNavigationArgs(this.contact), BackMethod.Pop);
		}

		/// <summary>
		/// Opens the chat with the contact.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanChat))]
		private async Task MessageAsync()
		{
			if (this.contact is null || !this.contact.CanChat)
				return;

			string Address = this.contact.ChatAddress.Value;
			ChatNavigationArgs Args = new(this.contact.LegalId?.Value, Address, this.contact.DisplayName);

			await ServiceRef.NavigationService.GoToAsync(nameof(ChatPage), Args, BackMethod.Inherited, Address);
		}

		/// <summary>
		/// Starts a payment to the contact.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanPay))]
		private async Task PayAsync()
		{
			if (this.contact is null)
				return;

			try
			{
				StringBuilder Link = new();

				Link.Append("edaler:");

				if (ServiceRef.TagProfile.LegalIdentity is null)
				{
					Link.Append("f=");
					Link.Append(ServiceRef.XmppService.BareJid);
				}
				else
				{
					Link.Append("fi=");
					Link.Append(ServiceRef.TagProfile.LegalIdentity.Id);
				}

				if (!string.IsNullOrEmpty(this.contact.LegalId?.Value))
				{
					Link.Append(";ti=");
					Link.Append(this.contact.LegalId.Value);
				}
				else
				{
					Link.Append(";t=");
					Link.Append(this.contact.ChatAddress.Value);
				}

				Balance CurrentBalance = await ServiceRef.XmppService.GetEDalerBalance();

				Link.Append(";cu=");
				Link.Append(CurrentBalance.Currency);

				if (!EDalerUri.TryParse(Link.ToString(), out EDalerUri Parsed))
					return;

				await ServiceRef.NavigationService.GoToAsync(nameof(PaymentPage), new EDalerUriNavigationArgs(Parsed), BackMethod.Pop);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// Starts a new contract, from the list of contract templates.
		/// </summary>
		[RelayCommand]
		private async Task ContractAsync()
		{
			try
			{
				MyContractsNavigationArgs Args = new(ContractsListMode.ContractTemplates);
				await ServiceRef.NavigationService.GoToAsync(nameof(MyContractsPage), Args, BackMethod.Pop);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// Shares the contact as a contact file.
		/// </summary>
		[RelayCommand]
		private async Task ShareAsync()
		{
			if (this.contact is null)
				return;

			try
			{
				string Text = ContactStore.Export([this.contact], true, true);
				string FileName = FileNameOf(this.contact.DisplayName) + ".vcf";
				string FilePath = Path.Combine(FileSystem.CacheDirectory, FileName);

				await File.WriteAllTextAsync(FilePath, Text, new UTF8Encoding(false));

				await Share.Default.RequestAsync(new ShareFileRequest()
				{
					Title = this.contact.DisplayName,
					File = new ShareFile(FilePath, "text/vcard")
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// Calls a phone number, writes to an email address, or shows an address on a map.
		/// </summary>
		/// <param name="Item">The row tapped.</param>
		[RelayCommand]
		private async Task OpenDetailAsync(ContactDetailItem? Item)
		{
			if (Item is null)
				return;

			try
			{
				switch (Item.Kind)
				{
					case ContactDetailKind.Phone:
						string Number = new([.. Item.Value.Where(Character => char.IsDigit(Character) || Character == '+')]);
						if (Number.Length > 0)
							await Launcher.Default.OpenAsync("tel:" + Number);
						break;

					case ContactDetailKind.Email:
						await Launcher.Default.OpenAsync("mailto:" + Item.Value);
						break;

					case ContactDetailKind.Address:
						await Browser.Default.OpenAsync("https://www.openstreetmap.org/search?query=" + Uri.EscapeDataString(Item.Value),
							BrowserLaunchMode.External);
						break;
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// Shows or hides the technical details.
		/// </summary>
		[RelayCommand]
		private void ToggleTechnical()
		{
			this.IsTechnicalExpanded = !this.IsTechnicalExpanded;
		}

		/// <summary>
		/// Copies the contact's chat address.
		/// </summary>
		[RelayCommand]
		private Task CopyChatAddressAsync()
		{
			return CopyAsync(this.ChatAddressText);
		}

		/// <summary>
		/// Copies the contact's ID number.
		/// </summary>
		[RelayCommand]
		private Task CopyLegalIdAsync()
		{
			return CopyAsync(this.LegalIdText);
		}

		private static async Task CopyAsync(string Text)
		{
			if (string.IsNullOrEmpty(Text))
				return;

			try
			{
				await Clipboard.Default.SetTextAsync(Text);
				await ServiceRef.ToastService.ShowTextAsync(ServiceRef.Localizer[nameof(AppResources.ContactCopied)]);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Blocks the contact, after asking, or unblocks them. A blocked contact can't send requests or see when the user is
		/// online, and the user stops seeing when they are online.
		/// </summary>
		[RelayCommand]
		private async Task ToggleBlockAsync()
		{
			if (this.contact is null)
				return;

			try
			{
				bool Block = !this.contact.IsBlocked;

				if (Block && !await ServiceRef.UiService.DisplayAlert(this.BlockText,
					ServiceRef.Localizer[nameof(AppResources.ContactBlockQuestion), this.Name],
					ServiceRef.Localizer[nameof(AppResources.ContactBlockButton)],
					ServiceRef.Localizer[nameof(AppResources.Cancel)]))
				{
					return;
				}

				this.contact.IsBlocked = Block;
				await ContactStore.SaveAsync(this.contact, this.contact.GetCard());

				if (Block && this.contact.CanChat)
				{
					string Address = this.contact.ChatAddress.Value;
					ServiceRef.XmppService.RequestRevokePresenceSubscription(Address);
					ServiceRef.XmppService.RequestPresenceUnsubscription(Address);
				}

				this.Show(this.contact);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// Removes the contact, after asking. They stop seeing when the user is online, and the other way round. The chat with
		/// them stays.
		/// </summary>
		[RelayCommand]
		private async Task RemoveAsync()
		{
			if (this.contact is null)
				return;

			try
			{
				if (!await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ContactRemove)],
					ServiceRef.Localizer[nameof(AppResources.ContactRemoveQuestion), this.Name],
					ServiceRef.Localizer[nameof(AppResources.ContactRemoveButton)],
					ServiceRef.Localizer[nameof(AppResources.Cancel)]))
				{
					return;
				}

				await ContactStore.DeleteAsync(this.contact);

				if (this.contact.CanChat && ServiceRef.XmppService.GetRosterItem(this.contact.ChatAddress.Value) is not null)
					ServiceRef.XmppService.RemoveRosterItem(this.contact.ChatAddress.Value);

				await this.GoBack();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// A file name made from a name, without characters that files can't have.
		/// </summary>
		private static string FileNameOf(string Name)
		{
			StringBuilder Result = new();
			char[] Invalid = Path.GetInvalidFileNameChars();

			foreach (char ch in Name.Trim())
				Result.Append(Array.IndexOf(Invalid, ch) >= 0 ? '_' : ch);

			return Result.Length > 0 ? Result.ToString() : "contact";
		}

		#endregion
	}
}
