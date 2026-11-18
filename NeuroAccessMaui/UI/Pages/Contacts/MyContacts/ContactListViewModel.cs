using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EDaler;
using EDaler.Uris;
using NeuroAccessMaui.Models;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.Services.Profiles;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.UI.QR;
using NeuroAccessMaui.Services.UI.Toasts;
using NeuroAccessMaui.UI.Pages.Contacts.Chat;
using NeuroAccessMaui.UI.Pages.Contacts.ContactCard;
using NeuroAccessMaui.UI.Pages.Contacts.ContactsBackup;
using NeuroAccessMaui.UI.Pages.Contacts.EditContact;
using NeuroAccessMaui.UI.Pages.Contacts.Export;
using NeuroAccessMaui.UI.Pages.Contacts.Import;
using NeuroAccessMaui.UI.Pages.Contacts.MyCard;
using NeuroAccessMaui.UI.Pages.Identity.ViewIdentity;
using NeuroAccessMaui.UI.Pages.Wallet;
using NeuroAccessMaui.UI.Pages.Wallet.Payment;
using NeuroAccessMaui.UI.Popups.Contacts;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Events;
using Waher.Persistence;
using Contact = NeuroAccessMaui.Services.Contacts.Contact;

namespace NeuroAccessMaui.UI.Pages.Contacts.MyContacts
{
	/// <summary>
	/// The contacts with names starting with the same letter.
	/// </summary>
	public class GroupedContacts : ObservableCollection<ContactInfoModel>
	{
		/// <summary>
		/// The contacts with names starting with the same letter.
		/// </summary>
		/// <param name="Group">The letter.</param>
		/// <param name="Contacts">The contacts.</param>
		public GroupedContacts(char Group, IEnumerable<ContactInfoModel> Contacts)
			: base(Contacts)
		{
			this.Group = Group;
		}

		/// <summary>
		/// The letter.
		/// </summary>
		public char Group { get; }
	}

	/// <summary>
	/// The view model of the contacts list: the user's own row, requests waiting for an answer, and the contacts from A to
	/// Z. The same list is used to pick a contact, for example to pay or share with.
	/// </summary>
	public partial class ContactListViewModel : BaseViewModel
	{
		private readonly Dictionary<CaseInsensitiveString, List<ContactInfoModel>> byBareJid = [];
		private readonly TaskCompletionSource<ContactInfoModel?>? selection;
		private readonly ContactListNavigationArgs? navigationArguments;
		private readonly List<ContactInfoModel> all = [];
		private CancellationTokenSource? searchDelay;
		private bool reloadPending;

		/// <summary>
		/// Creates the view model of the contacts list.
		/// </summary>
		/// <param name="Args">Navigation arguments.</param>
		public ContactListViewModel(ContactListNavigationArgs? Args)
		{
			this.navigationArguments = Args;

			if (Args is not null && Args.Action.HasValue && Args.Action.Value != SelectContactAction.View)
			{
				this.Description = Args.Description;
				this.Action = Args.Action;
				this.selection = Args.Selection;
				this.CanScanQrCode = Args.CanScanQrCode;
				this.AllowAnonymous = Args.AllowAnonymous;
				this.AnonymousText = string.IsNullOrEmpty(Args.AnonymousText) ?
					ServiceRef.Localizer[nameof(AppResources.Anonymous)] : Args.AnonymousText;
			}
			else
			{
				this.Description = ServiceRef.Localizer[nameof(AppResources.Contacts)];
				this.Action = SelectContactAction.View;
				this.selection = Args?.Selection;
			}

			this.IsViewMode = this.Action == SelectContactAction.View;
		}

		#region Properties

		/// <summary>
		/// If the list is shown as the user's contacts, rather than to pick a contact.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(ShowsMe))]
		[NotifyPropertyChangedFor(nameof(ShowsRequests))]
		private bool isViewMode;

		/// <summary>
		/// If the contacts are being loaded.
		/// </summary>
		[ObservableProperty]
		private bool isLoading = true;

		/// <summary>
		/// The title: Contacts, or what the user picks a contact for.
		/// </summary>
		[ObservableProperty]
		private string? description;

		/// <summary>
		/// If a contact can be picked by scanning their code.
		/// </summary>
		[ObservableProperty]
		private bool canScanQrCode;

		/// <summary>
		/// If the user can choose no one in particular.
		/// </summary>
		[ObservableProperty]
		private bool allowAnonymous;

		/// <summary>
		/// What choosing no one in particular says.
		/// </summary>
		[ObservableProperty]
		private string? anonymousText;

		/// <summary>
		/// What happens when a contact is picked.
		/// </summary>
		[ObservableProperty]
		private SelectContactAction? action;

		/// <summary>
		/// The contact that was picked, if any.
		/// </summary>
		[ObservableProperty]
		private ContactInfoModel? selectedContact;

		/// <summary>
		/// If the list has no contacts to show.
		/// </summary>
		[ObservableProperty]
		private bool showContactsMissing;

		/// <summary>
		/// The text searched for.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(ShowsMe))]
		[NotifyPropertyChangedFor(nameof(ShowsRequests))]
		private string? searchText;

		/// <summary>
		/// The selected filter chip's key.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(ShowsMe))]
		[NotifyPropertyChangedFor(nameof(ShowsRequests))]
		[NotifyPropertyChangedFor(nameof(ShowsContacts))]
		private string filter = ContactFilterChip.AllKey;

		/// <summary>
		/// The user's own row shows: in the contacts, unfiltered, and not searching.
		/// </summary>
		public bool ShowsMe => this.IsViewMode && string.IsNullOrWhiteSpace(this.SearchText) && this.Filter == ContactFilterChip.AllKey &&
			!string.IsNullOrEmpty(ServiceRef.XmppService.BareJid);

		/// <summary>
		/// If the user has an ID to show from their row.
		/// </summary>
		public bool HasMyId => ServiceRef.TagProfile.LegalIdentity is not null;

		/// <summary>
		/// The user's own photo, on the Me row, if they have chosen one.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasMyPhoto))]
		private ImageSource? myPhoto;

		/// <summary>
		/// If the user has a photo of their own.
		/// </summary>
		public bool HasMyPhoto => this.MyPhoto is not null;

		/// <summary>
		/// If the requests show: in the contacts, when there are any, and the filter shows them.
		/// </summary>
		public bool ShowsRequests => this.IsViewMode && this.Requests.Count > 0 && string.IsNullOrWhiteSpace(this.SearchText) &&
			(this.Filter == ContactFilterChip.AllKey || this.Filter == ContactFilterChip.RequestsKey);

		/// <summary>
		/// If the contacts from A to Z show. Not when only requests are shown.
		/// </summary>
		public bool ShowsContacts => this.Filter != ContactFilterChip.RequestsKey;

		/// <summary>
		/// The number of requests waiting, as shown on their heading.
		/// </summary>
		public string RequestCount => this.Requests.Count.ToString(CultureInfo.CurrentCulture);

		/// <summary>
		/// Requests waiting for an answer.
		/// </summary>
		public ObservableCollection<ContactRequestItem> Requests { get; } = [];

		/// <summary>
		/// The chips that filter the list.
		/// </summary>
		public ObservableCollection<ContactFilterChip> Chips { get; } = [];

		/// <summary>
		/// The contacts shown, grouped by first letter.
		/// </summary>
		public ObservableCollection<GroupedContacts> FilteredContacts { get; } = [];

		#endregion

		#region Life cycle

		/// <inheritdoc/>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			ServiceRef.XmppService.OnPresence += this.Xmpp_OnPresence;
			ServiceRef.NotificationService.OnNewNotification += this.NotificationService_OnNewNotification;
			ServiceRef.NotificationService.OnNotificationsDeleted += this.NotificationService_OnNotificationsDeleted;
			ContactStore.Changed += this.ContactStore_Changed;
			ContactRequests.Changed += this.ContactRequests_Changed;
			PhotoCache.Changed += this.PhotoCache_Changed;

			await this.LoadAsync();
			await this.OfferRestoreAsync();
		}

		/// <inheritdoc/>
		public override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();

			if (this.selection is not null && this.selection.Task.IsCompleted)
			{
				await this.GoBack();
				return;
			}

			this.SelectedContact = null;

			if (this.reloadPending)
			{
				this.reloadPending = false;
				await this.LoadAsync();
			}
		}

		/// <inheritdoc/>
		public override Task OnDisposeAsync()
		{
			ServiceRef.XmppService.OnPresence -= this.Xmpp_OnPresence;
			ServiceRef.NotificationService.OnNewNotification -= this.NotificationService_OnNewNotification;
			ServiceRef.NotificationService.OnNotificationsDeleted -= this.NotificationService_OnNotificationsDeleted;
			ContactStore.Changed -= this.ContactStore_Changed;
			ContactRequests.Changed -= this.ContactRequests_Changed;
			PhotoCache.Changed -= this.PhotoCache_Changed;

			this.searchDelay?.Cancel();
			this.selection?.TrySetResult(this.SelectedContact);

			return base.OnDisposeAsync();
		}

		#endregion

		#region Loading and filtering

		private async Task LoadAsync()
		{
			try
			{
				List<ContactInfoModel> Rows = [];

				if (this.navigationArguments?.Contacts is IEnumerable<ContactInfo> Given)
				{
					foreach (ContactInfo Info in Given)
					{
						if (Info.IsThing.HasValue && Info.IsThing.Value)
							continue;

						Rows.Add(new ContactInfoModel(Info, EventsOf(Info.BareJid)));
					}
				}
				else
				{
					foreach (Contact Card in await ContactStore.GetAllAsync())
					{
						if (Card.IsBlocked && !this.IsViewMode)
							continue;

						Rows.Add(new ContactInfoModel(Card, EventsOf(Card.ChatAddress)));
					}
				}

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.all.Clear();
					this.all.AddRange(Rows);

					this.byBareJid.Clear();
					foreach (ContactInfoModel Row in Rows)
					{
						if (string.IsNullOrEmpty(Row.BareJid))
							continue;

						if (!this.byBareJid.TryGetValue(Row.BareJid, out List<ContactInfoModel>? List))
						{
							List = [];
							this.byBareJid[Row.BareJid] = List;
						}

						List.Add(Row);
					}

					this.LoadRequests();
					this.BuildChips();
					this.ApplyFilter();
					this.IsLoading = false;
				});

				_ = this.LoadPhotosAsync(Rows);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await MainThread.InvokeOnMainThreadAsync(() => this.IsLoading = false);
			}
		}

		private static NotificationEvent[] EventsOf(CaseInsensitiveString? ChatAddress)
		{
			if (string.IsNullOrEmpty(ChatAddress) ||
				!ServiceRef.NotificationService.TryGetNotificationEvents(NotificationEventType.Contacts, ChatAddress, out NotificationEvent[]? Events))
			{
				return [];
			}

			return Events;
		}

		private void LoadRequests()
		{
			this.Requests.Clear();

			if (this.IsViewMode)
			{
				foreach (ContactRequest Request in ContactRequests.Pending)
					this.Requests.Add(new ContactRequestItem(Request));
			}

			this.OnPropertyChanged(nameof(this.ShowsRequests));
			this.OnPropertyChanged(nameof(this.RequestCount));
		}

		private void BuildChips()
		{
			this.Chips.Clear();

			if (!this.IsViewMode)
				return;

			int Count = this.all.Count(Row => !Row.IsBlocked);
			this.Chips.Add(new ContactFilterChip(ContactFilterChip.AllKey,
				ServiceRef.Localizer[nameof(AppResources.ContactFilterAll), Count]));

			if (this.Requests.Count > 0)
			{
				this.Chips.Add(new ContactFilterChip(ContactFilterChip.RequestsKey,
					ServiceRef.Localizer[nameof(AppResources.ContactFilterRequests), this.Requests.Count]));
			}

			if (this.all.Any(Row => Row.IsVerified))
				this.Chips.Add(new ContactFilterChip(ContactFilterChip.VerifiedKey, ServiceRef.Localizer[nameof(AppResources.ContactFilterVerified)]));

			SortedSet<string> Labels = new(StringComparer.CurrentCultureIgnoreCase);
			foreach (ContactInfoModel Row in this.all)
			{
				foreach (string Label in Row.Card?.Labels ?? [])
				{
					if (!string.IsNullOrWhiteSpace(Label))
						Labels.Add(Label.Trim());
				}
			}

			foreach (string Label in Labels)
				this.Chips.Add(new ContactFilterChip(ContactFilterChip.LabelPrefix + Label, Label));

			if (this.all.Any(Row => Row.IsBlocked))
				this.Chips.Add(new ContactFilterChip(ContactFilterChip.BlockedKey, ServiceRef.Localizer[nameof(AppResources.ContactFilterBlocked)]));

			if (!this.Chips.Any(Chip => Chip.Key == this.Filter))
				this.Filter = ContactFilterChip.AllKey;

			foreach (ContactFilterChip Chip in this.Chips)
				Chip.IsSelected = Chip.Key == this.Filter;
		}

		private void ApplyFilter()
		{
			string Search = (this.SearchText ?? string.Empty).Trim();
			string Key = this.Filter;
			Dictionary<char, List<ContactInfoModel>> Groups = [];

			foreach (ContactInfoModel Row in this.all)
			{
				bool Include = Key switch
				{
					ContactFilterChip.BlockedKey => Row.IsBlocked,
					ContactFilterChip.VerifiedKey => !Row.IsBlocked && Row.IsVerified,
					ContactFilterChip.RequestsKey => false,
					_ when Key.StartsWith(ContactFilterChip.LabelPrefix, StringComparison.Ordinal) =>
						!Row.IsBlocked && (Row.Card?.Labels?.Contains(Key[ContactFilterChip.LabelPrefix.Length..], StringComparer.CurrentCultureIgnoreCase) ?? false),
					_ => !Row.IsBlocked || !this.IsViewMode
				};

				if (!Include)
					continue;

				if (Search.Length > 0)
				{
					string Haystack = string.IsNullOrEmpty(Row.SearchText) ? Row.FriendlyName ?? string.Empty : Row.SearchText;
					if (Haystack.IndexOf(Search, StringComparison.CurrentCultureIgnoreCase) < 0)
						continue;
				}

				string Name = Row.FriendlyName ?? string.Empty;
				char Group = Name.Length > 0 ? char.ToUpper(Name[0], CultureInfo.CurrentCulture) : '#';
				if (!char.IsLetter(Group))
					Group = '#';

				if (!Groups.TryGetValue(Group, out List<ContactInfoModel>? List))
				{
					List = [];
					Groups[Group] = List;
				}

				List.Add(Row);
			}

			this.FilteredContacts.Clear();

			foreach (char Group in Groups.Keys.OrderBy(Letter => Letter == '#' ? char.MaxValue : Letter))
			{
				IEnumerable<ContactInfoModel> Sorted = Groups[Group].OrderBy(Row => Row.FriendlyName, StringComparer.CurrentCultureIgnoreCase);
				this.FilteredContacts.Add(new GroupedContacts(Group, Sorted));
			}

			this.ShowContactsMissing = this.FilteredContacts.Count == 0 && Key != ContactFilterChip.RequestsKey;
		}

		partial void OnSearchTextChanged(string? value)
		{
			this.searchDelay?.Cancel();
			CancellationTokenSource Delay = new();
			this.searchDelay = Delay;

			_ = Task.Delay(250, Delay.Token).ContinueWith(T =>
			{
				if (!T.IsCanceled)
					MainThread.BeginInvokeOnMainThread(this.ApplyFilter);
			}, TaskScheduler.Default);
		}

		/// <summary>
		/// Filters the list by a chip.
		/// </summary>
		/// <param name="Chip">The chip.</param>
		[RelayCommand]
		private void SelectChip(ContactFilterChip? Chip)
		{
			if (Chip is null)
				return;

			this.Filter = Chip.Key;

			foreach (ContactFilterChip Other in this.Chips)
				Other.IsSelected = Other.Key == Chip.Key;

			this.ApplyFilter();
		}

		#endregion

		#region The user's own row

		/// <summary>
		/// Opens notes to yourself: a chat with your own address, kept on the phone.
		/// </summary>
		[RelayCommand]
		private async Task OpenMyChatAsync()
		{
			string BareJid = ServiceRef.XmppService.BareJid;
			if (string.IsNullOrEmpty(BareJid))
				return;

			ChatNavigationArgs Args = new(ServiceRef.TagProfile.LegalIdentity?.Id, BareJid, ServiceRef.Localizer[nameof(AppResources.ChatMe)]);
			await ServiceRef.NavigationService.GoToAsync(nameof(ChatPage), Args, BackMethod.Inherited, BareJid);
		}

		/// <summary>
		/// Shows the user's own ID, with the code others scan to add them.
		/// </summary>
		[RelayCommand]
		private async Task ViewMyIdAsync()
		{
			LegalIdentity? Identity = ServiceRef.TagProfile.LegalIdentity;
			if (Identity is not null)
				await ServiceRef.NavigationService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(Identity));
		}

		#endregion

		#region Adding and the menu

		/// <summary>
		/// Shows the ways to add a contact, and goes on with the one the user picks.
		/// </summary>
		[RelayCommand]
		private async Task AddContactAsync()
		{
			AddContactChoice? Choice = await AddContactPopup.ShowAsync();

			switch (Choice)
			{
				case AddContactChoice.ScanCode:
					await this.ScanToAddAsync();
					break;

				case AddContactChoice.ShowMyCode:
					await this.ViewMyIdAsync();
					break;

				case AddContactChoice.ImportFile:
					await ServiceRef.NavigationService.GoToAsync(nameof(ImportContactsPage), new ImportContactsNavigationArgs(), BackMethod.Pop);
					break;

				case AddContactChoice.TypeIn:
					await ServiceRef.NavigationService.GoToAsync(nameof(EditContactPage), new EditContactNavigationArgs(), BackMethod.Pop);
					break;
			}
		}

		/// <summary>
		/// Shows the list's menu: import, export and blocked people.
		/// </summary>
		[RelayCommand]
		private async Task ShowMenuAsync()
		{
			ContactsMenuChoice? Choice = await ContactsMenuPopup.ShowAsync();

			switch (Choice)
			{
				case ContactsMenuChoice.Import:
					await ServiceRef.NavigationService.GoToAsync(nameof(ImportContactsPage), new ImportContactsNavigationArgs(), BackMethod.Pop);
					break;

				case ContactsMenuChoice.Export:
					await ServiceRef.NavigationService.GoToAsync(nameof(ExportContactsPage), new ExportContactsNavigationArgs(), BackMethod.Pop);
					break;

				case ContactsMenuChoice.Blocked:
					ContactFilterChip? Blocked = this.Chips.FirstOrDefault(Chip => Chip.Key == ContactFilterChip.BlockedKey);
					if (Blocked is not null)
						this.SelectChip(Blocked);
					else
						await ServiceRef.ToastService.ShowTextAsync(ServiceRef.Localizer[nameof(AppResources.ContactNoneBlocked)]);
					break;

				case ContactsMenuChoice.Backup:
					await ServiceRef.NavigationService.GoToAsync(nameof(BackupPage), BackMethod.Pop);
					break;
			}
		}

		/// <summary>
		/// Opens the user's own card, where they choose the name and photo others see.
		/// </summary>
		[RelayCommand]
		private Task OpenMyCardAsync()
		{
			return ServiceRef.NavigationService.GoToAsync(nameof(MyCardPage), BackMethod.Pop);
		}

		/// <summary>
		/// On a phone with no contacts, offers once to restore the contacts backup, if there is one.
		/// </summary>
		private async Task OfferRestoreAsync()
		{
			try
			{
				if (!this.IsViewMode || !await ContactBackup.ShouldOfferRestoreAsync())
					return;

				await ContactBackup.MarkRestoreOfferedAsync();

				if (!await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ContactBackupRestoreTitle)],
					ServiceRef.Localizer[nameof(AppResources.ContactBackupOfferQuestion)],
					ServiceRef.Localizer[nameof(AppResources.ContactBackupRestore)],
					ServiceRef.Localizer[nameof(AppResources.ContactBackupLater)]))
				{
					return;
				}

				int Count = await ContactBackup.RestoreAsync();
				await ServiceRef.ToastService.ShowTextAsync(Count == 1 ?
					ServiceRef.Localizer[nameof(AppResources.ContactBackupRestoredOne)] :
					ServiceRef.Localizer[nameof(AppResources.ContactBackupRestoredMany), Count]);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// Shows the photos of the contacts in the list, and the user's own.
		/// </summary>
		private async Task LoadPhotosAsync(IReadOnlyList<ContactInfoModel> Rows)
		{
			try
			{
				ImageSource? Own = await PhotoCache.GetAsync(ServiceRef.XmppService.BareJid);
				await MainThread.InvokeOnMainThreadAsync(() => this.MyPhoto = Own);

				foreach (ContactInfoModel Row in Rows)
				{
					if (string.IsNullOrEmpty(Row.BareJid))
						continue;

					ImageSource? Photo = await PhotoCache.GetAsync(Row.BareJid);
					if (Photo is not null)
						MainThread.BeginInvokeOnMainThread(() => Row.SetPhoto(Photo));
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private void PhotoCache_Changed(object? Sender, string Address)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				List<ContactInfoModel> Rows = [.. this.all.Where(Row => string.Equals(Row.BareJid, Address, StringComparison.OrdinalIgnoreCase))];
				_ = this.LoadPhotosAsync(Rows);
			});
		}

		/// <summary>
		/// Scans someone's code and adds them: their card is saved from their ID, and they get a request.
		/// </summary>
		private async Task ScanToAddAsync()
		{
			IdentityReference? Scan = await QrCode.GetQrService().ScanIdentityAsync(nameof(AppResources.ScanQRCode));
			if (Scan is null)
				return;

			try
			{
				string? ChatAddress = Scan.Identity?[Constants.XmppProperties.Jid];

				if (Scan.Identity is null)
				{
					// Their ID is not open to everyone, so it is opened by asking them for it.
					await ServiceRef.ContractOrchestratorService.OpenLegalIdentity(Scan.Identifier,
						ServiceRef.Localizer[nameof(AppResources.ScannedQrCode)]);
					return;
				}

				if (string.IsNullOrEmpty(ChatAddress))
				{
					await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ContactAddTitle)],
						ServiceRef.Localizer[nameof(AppResources.ContactNoChatAddress)]);
					return;
				}

				Contact Card = await ContactStore.AddOrUpdateAsync(ChatAddress, Scan.Identity, null);

				ServiceRef.XmppService.RequestPresenceSubscription(Card.ChatAddress, ContactRequests.OwnIdentityXml());
				await ServiceRef.ToastService.ShowTextAsync(ServiceRef.Localizer[nameof(AppResources.ContactRequestSent), Card.DisplayName]);

				await this.OpenCardAsync(Card);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		#endregion

		#region Opening and picking

		/// <summary>
		/// Shows a request, where the user can accept it or not.
		/// </summary>
		/// <param name="Item">The request.</param>
		[RelayCommand]
		private Task ReviewRequestAsync(ContactRequestItem? Item)
		{
			return Item is null ? Task.CompletedTask : ContactRequestPopup.ShowAsync(Item.Request);
		}

		/// <summary>
		/// Opens a contact's card, or picks the contact.
		/// </summary>
		/// <param name="Contact">The contact.</param>
		[RelayCommand]
		private void SelectContact(ContactInfoModel? Contact)
		{
			if (Contact is not null)
				this.SelectedContact = Contact;
		}

		private Task OpenCardAsync(Contact Card)
		{
			return ServiceRef.NavigationService.GoToAsync(nameof(ContactCardPage), new ContactCardNavigationArgs(Card), BackMethod.Pop);
		}

		/// <inheritdoc/>
		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			if (e.PropertyName == nameof(this.SelectedContact) && this.SelectedContact is ContactInfoModel Contact)
				MainThread.BeginInvokeOnMainThread(async () => await this.ContactSelectedAsync(Contact));
		}

		private async Task ContactSelectedAsync(ContactInfoModel Contact)
		{
			this.IsOverlayVisible = true;

			try
			{
				switch (this.Action)
				{
					case SelectContactAction.MakePayment:
						await this.PayAsync(Contact);
						break;

					case SelectContactAction.Select:
						await this.GoBack();
						this.selection?.TrySetResult(Contact);
						break;

					case SelectContactAction.View:
					default:
						if (Contact.Card is not null)
							await this.OpenCardAsync(Contact.Card);
						else if (Contact.LegalIdentity is not null)
							await ServiceRef.NavigationService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(Contact.LegalIdentity));
						else if (!string.IsNullOrEmpty(Contact.LegalId))
							await ServiceRef.ContractOrchestratorService.OpenLegalIdentity(Contact.LegalId, ServiceRef.Localizer[nameof(AppResources.ScannedQrCode)]);
						break;
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
			finally
			{
				this.IsOverlayVisible = false;
			}
		}

		private async Task PayAsync(ContactInfoModel Contact)
		{
			StringBuilder Sb = new();

			Sb.Append("edaler:");

			if (ServiceRef.TagProfile.LegalIdentity is null)
			{
				Sb.Append("f=");
				Sb.Append(ServiceRef.XmppService.BareJid);
			}
			else
			{
				Sb.Append("fi=");
				Sb.Append(ServiceRef.TagProfile.LegalIdentity.Id);
			}

			if (!string.IsNullOrEmpty(Contact.LegalId))
			{
				Sb.Append(";ti=");
				Sb.Append(Contact.LegalId);
			}
			else if (!string.IsNullOrEmpty(Contact.BareJid))
			{
				Sb.Append(";t=");
				Sb.Append(Contact.BareJid);
			}

			Balance Balance = await ServiceRef.XmppService.GetEDalerBalance();

			Sb.Append(";cu=");
			Sb.Append(Balance.Currency);

			if (!EDalerUri.TryParse(Sb.ToString(), out EDalerUri Parsed))
				return;

			await ServiceRef.NavigationService.GoToAsync(nameof(PaymentPage), new EDalerUriNavigationArgs(Parsed), BackMethod.Pop2);
		}

		/// <summary>
		/// Picks a contact by scanning their code.
		/// </summary>
		[RelayCommand]
		private async Task ScanQrCodeAsync()
		{
			IdentityReference? Scan = await QrCode.GetQrService().ScanIdentityAsync(nameof(AppResources.ScanQRCode));
			if (Scan is null)
				return;

			// The scanner already resolved the identity when it is accessible, so viewing it does not fetch it again.
			this.SelectedContact = new ContactInfoModel(new ContactInfo()
			{
				LegalId = Scan.Identifier,
				LegalIdentity = Scan.Identity
			});
		}

		/// <summary>
		/// Picks no one in particular.
		/// </summary>
		[RelayCommand]
		private void Anonymous()
		{
			this.SelectedContact = new ContactInfoModel((ContactInfo?)null, []);
		}

		#endregion

		#region Events

		private Task Xmpp_OnPresence(object? Sender, PresenceEventArgs e)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				if (this.byBareJid.TryGetValue(e.FromBareJID, out List<ContactInfoModel>? Rows))
				{
					foreach (ContactInfoModel Row in Rows)
						Row.PresenceUpdated();
				}
			});

			return Task.CompletedTask;
		}

		private void ContactStore_Changed(object? Sender, EventArgs e)
		{
			// The list reloads when it shows again, so a page on top of it, such as a contact card, isn't slowed down.
			this.reloadPending = true;
		}

		private void ContactRequests_Changed(object? Sender, EventArgs e)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.LoadRequests();
				this.BuildChips();
				this.ApplyFilter();
			});
		}

		private Task NotificationService_OnNewNotification(object? Sender, NotificationEventArgs e)
		{
			if (e.Event.Type == NotificationEventType.Contacts)
				MainThread.BeginInvokeOnMainThread(() => this.UpdateNotifications(e.Event.Category ?? string.Empty));

			return Task.CompletedTask;
		}

		private Task NotificationService_OnNotificationsDeleted(object? Sender, NotificationEventsArgs e)
		{
			HashSet<CaseInsensitiveString> Categories = [.. e.Events.Select(Event => Event.Category ?? string.Empty)];

			MainThread.BeginInvokeOnMainThread(() =>
			{
				foreach (CaseInsensitiveString Category in Categories)
					this.UpdateNotifications(Category);
			});

			return Task.CompletedTask;
		}

		private void UpdateNotifications(CaseInsensitiveString Category)
		{
			if (this.byBareJid.TryGetValue(Category, out List<ContactInfoModel>? Rows))
			{
				NotificationEvent[] Events = EventsOf(Category);

				foreach (ContactInfoModel Row in Rows)
					Row.NotificationsUpdated(Events);
			}
		}

		#endregion
	}
}
