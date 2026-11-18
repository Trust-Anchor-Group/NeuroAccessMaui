using System.Globalization;
using NeuroAccessMaui.Services.Contacts.VCard;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Persistence.Filters;
using Waher.Runtime.Settings;
using Waher.Security;

namespace NeuroAccessMaui.Services.Contacts
{
	/// <summary>
	/// The user's contacts, each stored as a contact card. Finds, saves, removes, imports and exports contacts, and keeps
	/// them in step with the server's contact list.
	/// </summary>
	public static class ContactStore
	{
		/// <summary>
		/// The extension property that holds a contact's legal ID in a card.
		/// </summary>
		public const string LegalIdProperty = "X-NEURO-LEGAL-ID";

		/// <summary>
		/// The largest contact file that is read, in characters.
		/// </summary>
		public const int MaxImportLength = 8 * 1024 * 1024;

		private const string chatScheme = "xmpp:";

		private static readonly string[] olderChatAddressProperties = ["X-JABBER", "X-XMPP"];

		private static readonly SemaphoreSlim syncLock = new(1, 1);

		/// <summary>
		/// Raised when a contact has been added, changed or removed.
		/// </summary>
		public static event EventHandler? Changed;

		#region Finding

		/// <summary>
		/// Gets all contacts, sorted by name.
		/// </summary>
		/// <returns>The contacts.</returns>
		public static async Task<IReadOnlyList<Contact>> GetAllAsync()
		{
			IEnumerable<Contact> Found = await Database.Find<Contact>("SortKey");
			return [.. Found];
		}

		/// <summary>
		/// Finds the contact that chats at an address.
		/// </summary>
		/// <param name="ChatAddress">The chat address.</param>
		/// <returns>The contact, if found.</returns>
		public static async Task<Contact?> FindByChatAddressAsync(string? ChatAddress)
		{
			if (string.IsNullOrWhiteSpace(ChatAddress))
				return null;

			return await Database.FindFirstIgnoreRest<Contact>(new FilterFieldEqualTo("ChatAddress", new CaseInsensitiveString(ChatAddress)));
		}

		/// <summary>
		/// Finds the contact with a legal ID.
		/// </summary>
		/// <param name="LegalId">The legal ID.</param>
		/// <returns>The contact, if found.</returns>
		public static async Task<Contact?> FindByLegalIdAsync(string? LegalId)
		{
			if (string.IsNullOrWhiteSpace(LegalId))
				return null;

			return await Database.FindFirstIgnoreRest<Contact>(new FilterFieldEqualTo("LegalId", new CaseInsensitiveString(LegalId)));
		}

		/// <summary>
		/// Finds the contact with a card's unique ID.
		/// </summary>
		/// <param name="Uid">The unique ID.</param>
		/// <returns>The contact, if found.</returns>
		public static async Task<Contact?> FindByUidAsync(string? Uid)
		{
			if (string.IsNullOrWhiteSpace(Uid))
				return null;

			return await Database.FindFirstIgnoreRest<Contact>(new FilterFieldEqualTo("Uid", Uid));
		}

		/// <summary>
		/// Finds a contact by chat address, or else by legal ID.
		/// </summary>
		/// <param name="ChatAddress">The chat address, if known.</param>
		/// <param name="LegalId">The legal ID, if known.</param>
		/// <returns>The contact, if found.</returns>
		public static async Task<Contact?> FindAsync(string? ChatAddress, string? LegalId)
		{
			return await FindByChatAddressAsync(ChatAddress) ?? await FindByLegalIdAsync(LegalId);
		}

		/// <summary>
		/// The name to show for a person, from the contacts: by chat address, or else by legal ID.
		/// </summary>
		/// <param name="Address">A chat address or a legal ID.</param>
		/// <returns>The contact's name, or null if the person is not a contact.</returns>
		public static async Task<string?> TryGetNameAsync(string? Address)
		{
			if (string.IsNullOrWhiteSpace(Address))
				return null;

			Contact? Found = await FindAsync(Address, Address);
			return string.IsNullOrWhiteSpace(Found?.DisplayName) ? null : Found.DisplayName;
		}

		#endregion

		#region Saving

		/// <summary>
		/// Saves a contact with its card. The card's unique ID and change time are set, and the contact's derived fields are
		/// updated from it. Every contact has a chat address, so a card without one is not saved.
		/// </summary>
		/// <param name="Contact">The contact, new or stored.</param>
		/// <param name="Card">The contact's card.</param>
		/// <exception cref="ArgumentException">If the card has no chat address.</exception>
		public static async Task SaveAsync(Contact Contact, VCard.VCard Card)
		{
			string ChatAddress = EnsureChatLink(Card) ?? throw new ArgumentException("A contact needs a chat address.", nameof(Card));

			if (string.IsNullOrWhiteSpace(Card.Uid))
				Card.Uid = "urn:uuid:" + Guid.NewGuid().ToString();

			if (string.IsNullOrWhiteSpace(Card.FormattedName))
				Card.FormattedName = NameOf(Card);

			DateTime Now = DateTime.UtcNow;
			Card.Rev = Now.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

			Contact.Uid = Card.Uid ?? string.Empty;
			Contact.VCardText = VCardWriter.Write(Card);
			Contact.DisplayName = NameOf(Card);
			Contact.SortKey = SortKeyOf(Contact.DisplayName);
			Contact.ChatAddress = ChatAddress;
			Contact.LegalId = Card.GetValue(LegalIdProperty)?.Trim() ?? string.Empty;
			Contact.Labels = Card.Categories.Count > 0 ? [.. Card.Categories] : null;
			Contact.Updated = Now;

			if (string.IsNullOrEmpty(Contact.ObjectId))
			{
				Contact.Created = Now;
				await Database.Insert(Contact);
			}
			else
				await Database.Update(Contact);

			RaiseChanged();
		}

		/// <summary>
		/// Removes a contact from the phone.
		/// </summary>
		/// <param name="Contact">The contact.</param>
		public static async Task DeleteAsync(Contact Contact)
		{
			if (!string.IsNullOrEmpty(Contact.ObjectId))
				await Database.Delete(Contact);

			RaiseChanged();
		}

		/// <summary>
		/// Adds a person as a contact, or updates their contact, from what is known about them: their ID, their chat address
		/// and the name they gave. Fields the user has typed are kept.
		/// </summary>
		/// <param name="ChatAddress">Their chat address. Every contact has one.</param>
		/// <param name="Identity">Their ID, if known. It should have been checked by the app.</param>
		/// <param name="Name">The name they gave, if any, such as in a request.</param>
		/// <returns>The contact.</returns>
		/// <exception cref="ArgumentException">If the chat address is empty.</exception>
		public static async Task<Contact> AddOrUpdateAsync(string ChatAddress, LegalIdentity? Identity, string? Name)
		{
			if (string.IsNullOrWhiteSpace(ChatAddress))
				throw new ArgumentException("A contact needs a chat address.", nameof(ChatAddress));

			Contact Contact = await FindAsync(ChatAddress, Identity?.Id) ?? new Contact();
			VCard.VCard Card = string.IsNullOrEmpty(Contact.ObjectId) ? new VCard.VCard() : Contact.GetCard();

			if (Identity is not null)
			{
				FillFromIdentity(Card, Identity);
				Contact.LegalIdentity = Identity;
				Contact.IsVerified = Identity.State == IdentityState.Approved;
			}

			if (ChatAddressOf(Card) is null)
				Card.AddImpp(chatScheme + XmppClient.GetBareJID(ChatAddress.Trim()));

			if (string.IsNullOrWhiteSpace(Card.DisplayName) && !string.IsNullOrWhiteSpace(Name))
				Card.FormattedName = Name.Trim();

			await SaveAsync(Contact, Card);
			return Contact;
		}

		/// <summary>
		/// Fills a card with what a person's ID says: name, organization, phone, email, address and birthday, and the legal ID.
		/// Values the card has already are kept, and values the ID adds are added.
		/// </summary>
		/// <param name="Card">The card.</param>
		/// <param name="Identity">The ID.</param>
		public static void FillFromIdentity(VCard.VCard Card, LegalIdentity Identity)
		{
			string First = Identity[Constants.XmppProperties.FirstName] ?? string.Empty;
			string Middle = Identity[Constants.XmppProperties.MiddleNames] ?? string.Empty;
			string Last = Identity[Constants.XmppProperties.LastNames] ?? string.Empty;
			string OrgName = Identity[Constants.XmppProperties.OrgName] ?? string.Empty;

			if (!string.IsNullOrWhiteSpace(First) || !string.IsNullOrWhiteSpace(Last))
			{
				Card.Name = new VCardName(Last.Trim(), First.Trim(), Middle.Trim(), string.Empty, string.Empty);
				Card.FormattedName = ContactInfo.GetFullName(First, Middle, Last);
			}
			else if (string.IsNullOrWhiteSpace(Card.FormattedName))
				Card.FormattedName = ContactInfo.GetFriendlyName(Identity);

			if (!string.IsNullOrWhiteSpace(OrgName) && Card.Organization.Count == 0)
			{
				string Department = Identity[Constants.XmppProperties.OrgDepartment] ?? string.Empty;
				Card.SetOrganization(string.IsNullOrWhiteSpace(Department) ? [OrgName.Trim()] : [OrgName.Trim(), Department.Trim()]);
			}

			string Role = Identity[Constants.XmppProperties.OrgRole] ?? string.Empty;
			if (!string.IsNullOrWhiteSpace(Role) && string.IsNullOrWhiteSpace(Card.Title))
				Card.Title = Role.Trim();

			string Phone = Identity[Constants.XmppProperties.Phone] ?? string.Empty;
			if (!string.IsNullOrWhiteSpace(Phone) && !Card.Telephones.Any(T => SamePhone(T.Value, Phone)))
				Card.AddTelephone(Phone, "cell");

			string Email = Identity[Constants.XmppProperties.EMail] ?? string.Empty;
			if (!string.IsNullOrWhiteSpace(Email) && !Card.Emails.Any(E => string.Equals(E.Value, Email.Trim(), StringComparison.OrdinalIgnoreCase)))
				Card.AddEmail(Email);

			string Street = Identity[Constants.XmppProperties.Address] ?? string.Empty;
			string City = Identity[Constants.XmppProperties.City] ?? string.Empty;
			if ((!string.IsNullOrWhiteSpace(Street) || !string.IsNullOrWhiteSpace(City)) && Card.Addresses.Count == 0)
			{
				string Street2 = Identity[Constants.XmppProperties.Address2] ?? string.Empty;
				Card.AddAddress(new VCardAddress(string.Empty, Street2.Trim(), Street.Trim(), City.Trim(),
					(Identity[Constants.XmppProperties.Region] ?? string.Empty).Trim(),
					(Identity[Constants.XmppProperties.ZipCode] ?? string.Empty).Trim(),
					(Identity[Constants.XmppProperties.Country] ?? string.Empty).Trim(), ["home"], null));
			}

			string Year = Identity[Constants.XmppProperties.BirthYear] ?? string.Empty;
			string Month = Identity[Constants.XmppProperties.BirthMonth] ?? string.Empty;
			string Day = Identity[Constants.XmppProperties.BirthDay] ?? string.Empty;
			if (string.IsNullOrWhiteSpace(Card.Birthday) &&
				int.TryParse(Year, NumberStyles.None, CultureInfo.InvariantCulture, out int Y) &&
				int.TryParse(Month, NumberStyles.None, CultureInfo.InvariantCulture, out int M) &&
				int.TryParse(Day, NumberStyles.None, CultureInfo.InvariantCulture, out int D) &&
				Y > 0 && M is >= 1 and <= 12 && D is >= 1 and <= 31)
			{
				Card.Birthday = Y.ToString("D4", CultureInfo.InvariantCulture) + "-" + M.ToString("D2", CultureInfo.InvariantCulture) + "-" +
					D.ToString("D2", CultureInfo.InvariantCulture);
			}

			Card.SetSingle(LegalIdProperty, Identity.Id);
		}

		/// <summary>
		/// The name to show for a card: its name, or else its first email address or phone number, or else its chat address.
		/// </summary>
		/// <param name="Card">The card.</param>
		/// <returns>The name. Never empty.</returns>
		public static string NameOf(VCard.VCard Card)
		{
			string? Name = Card.DisplayName;
			if (!string.IsNullOrWhiteSpace(Name))
				return Name.Trim();

			if (Card.Emails.Count > 0)
				return Card.Emails[0].Value;

			if (Card.Telephones.Count > 0)
				return Card.Telephones[0].Value;

			string? ChatAddress = ChatAddressOf(Card);
			if (!string.IsNullOrEmpty(ChatAddress))
			{
				int i = ChatAddress.IndexOf('@');
				return i > 0 ? ChatAddress[..i] : ChatAddress;
			}

			return "?";
		}

		/// <summary>
		/// The chat address in a card: its xmpp: link, or else the X-JABBER or X-XMPP value older contact files use.
		/// </summary>
		/// <param name="Card">The card.</param>
		/// <returns>The chat address, or null.</returns>
		public static string? ChatAddressOf(VCard.VCard Card)
		{
			foreach (string Link in Card.Impp)
			{
				if (!Link.StartsWith(chatScheme, StringComparison.OrdinalIgnoreCase))
					continue;

				string? Address = ParseChatAddress(Link[chatScheme.Length..]);
				if (Address is not null)
					return Address;
			}

			foreach (string PropertyName in olderChatAddressProperties)
			{
				string? Address = ParseChatAddress(Card.GetValue(PropertyName));
				if (Address is not null)
					return Address;
			}

			return null;
		}

		/// <summary>
		/// Makes sure a card has its chat address as an xmpp: link, which is where the app keeps it. Cards from older contact
		/// files may only have it in an X-JABBER or X-XMPP value.
		/// </summary>
		/// <param name="Card">The card.</param>
		/// <returns>The chat address, or null if the card has none.</returns>
		public static string? EnsureChatLink(VCard.VCard Card)
		{
			string? Address = ChatAddressOf(Card);
			if (Address is null)
				return null;

			bool HasLink = Card.Impp.Any(Link => Link.StartsWith(chatScheme, StringComparison.OrdinalIgnoreCase) &&
				string.Equals(ParseChatAddress(Link[chatScheme.Length..]), Address, StringComparison.OrdinalIgnoreCase));

			if (!HasLink)
				Card.AddImpp(chatScheme + Address);

			return Address;
		}

		/// <summary>
		/// The xmpp: link for a chat address, as a card holds it.
		/// </summary>
		/// <param name="ChatAddress">The chat address.</param>
		/// <returns>The link.</returns>
		public static string ChatLinkOf(string ChatAddress)
		{
			return chatScheme + ChatAddress;
		}

		/// <summary>
		/// Reads a chat address, as typed or as the part of an xmpp: link after the scheme. A query after it is left out.
		/// </summary>
		/// <param name="Value">The text.</param>
		/// <returns>The chat address, or null if the text isn't one.</returns>
		public static string? ParseChatAddress(string? Value)
		{
			if (string.IsNullOrWhiteSpace(Value))
				return null;

			Value = Value.Trim();
			if (Value.StartsWith(chatScheme, StringComparison.OrdinalIgnoreCase))
				Value = Value[chatScheme.Length..];

			int i = Value.IndexOf('?');
			if (i >= 0)
				Value = Value[..i];

			Value = Uri.UnescapeDataString(Value.Trim());
			int At = Value.IndexOf('@');
			if (At <= 0 || At == Value.Length - 1 || Value.Any(char.IsWhiteSpace))
				return null;

			return XmppClient.GetBareJID(Value);
		}

		private static string SortKeyOf(string Name)
		{
			string Key = Name.Trim();
			return Key.Length == 0 ? "~" : Key.ToUpper(CultureInfo.InvariantCulture);
		}

		private static bool SamePhone(string A, string B)
		{
			return string.Equals(DigitsOf(A), DigitsOf(B), StringComparison.Ordinal);
		}

		private static string DigitsOf(string Phone)
		{
			return new string([.. Phone.Where(char.IsDigit)]);
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

		#endregion

		#region Keeping in step

		/// <summary>
		/// Moves the people in the old contact list to contact cards the first time it runs, and makes a card for each person
		/// on the server's contact list who has none. Called when the app has connected.
		/// </summary>
		public static async Task SyncAsync()
		{
			await syncLock.WaitAsync();

			try
			{
				if (!await RuntimeSettings.GetAsync(Constants.Settings.ContactsMovedToCards, false))
				{
					await MoveOldContactsAsync();
					await RuntimeSettings.SetAsync(Constants.Settings.ContactsMovedToCards, true);
				}

				await AddServerListCardsAsync();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
			finally
			{
				syncLock.Release();
			}
		}

		/// <summary>
		/// Makes a card for each person in the old contact list. Smart things stay where they are, and the old rows are left
		/// in place. Blocked people stay blocked.
		/// </summary>
		private static async Task MoveOldContactsAsync()
		{
			IEnumerable<ContactInfo> Old = await Database.Find<ContactInfo>();
			List<ContactInfo> Rows = [.. Old];
			HashSet<string> Things = new(Rows.Where(IsThing).Select(Info => Info.BareJid.Value), StringComparer.OrdinalIgnoreCase);
			int Moved = 0;

			foreach (ContactInfo Info in Rows)
			{
				string BareJid = Info.BareJid?.Value ?? string.Empty;
				string LegalId = Info.LegalId?.Value ?? string.Empty;

				// Every contact has a chat address, so old rows without one are not moved.
				if (IsThing(Info) || Things.Contains(BareJid) || string.IsNullOrEmpty(BareJid))
					continue;

				if (await FindAsync(BareJid, LegalId) is not null)
					continue;

				VCard.VCard Card = new();
				Contact Contact = new()
				{
					IsBlocked = Info.AllowSubscriptionFrom.HasValue && !Info.AllowSubscriptionFrom.Value
				};

				if (Info.LegalIdentity is not null)
				{
					FillFromIdentity(Card, Info.LegalIdentity);
					Contact.LegalIdentity = Info.LegalIdentity;
					Contact.IsVerified = Info.LegalIdentity.State == IdentityState.Approved;
				}

				if (!string.IsNullOrWhiteSpace(Info.FriendlyName) && !string.Equals(Info.FriendlyName, BareJid, StringComparison.OrdinalIgnoreCase))
					Card.FormattedName ??= Info.FriendlyName;

				Card.AddImpp(chatScheme + BareJid);

				if (!string.IsNullOrEmpty(LegalId))
					Card.SetSingle(LegalIdProperty, LegalId);

				await SaveAsync(Contact, Card);
				Moved++;
			}

			ServiceRef.LogService.LogInformational("ContactsMovedToCards", new KeyValuePair<string, object?>("Count", Moved));
		}

		/// <summary>
		/// Makes a card, with just a name, for each person on the server's contact list who has none. Smart things and the
		/// user's own address are left out.
		/// </summary>
		private static async Task AddServerListCardsAsync()
		{
			RosterItem[] Items;

			try
			{
				Items = ServiceRef.XmppService.Roster;
			}
			catch (Exception)
			{
				return;
			}

			if (Items.Length == 0)
				return;

			IReadOnlyList<Contact> Contacts = await GetAllAsync();
			HashSet<string> Known = new(Contacts.Select(C => C.ChatAddress.Value), StringComparer.OrdinalIgnoreCase);
			string Own = ServiceRef.XmppService.BareJid ?? string.Empty;

			foreach (RosterItem Item in Items)
			{
				if (Known.Contains(Item.BareJid) || string.Equals(Item.BareJid, Own, StringComparison.OrdinalIgnoreCase))
					continue;

				ContactInfo? Thing = await Database.FindFirstIgnoreRest<ContactInfo>(new FilterAnd(
					new FilterFieldEqualTo("BareJid", Item.BareJid),
					new FilterFieldEqualTo("IsThing", true)));

				if (Thing is not null)
					continue;

				VCard.VCard Card = new();
				if (!string.IsNullOrWhiteSpace(Item.Name))
					Card.FormattedName = Item.Name.Trim();

				Card.AddImpp(chatScheme + Item.BareJid);

				await SaveAsync(new Contact(), Card);
				Known.Add(Item.BareJid);
			}
		}

		private static bool IsThing(ContactInfo Info)
		{
			return (Info.IsThing.HasValue && Info.IsThing.Value) || !string.IsNullOrEmpty(Info.NodeId) || !string.IsNullOrEmpty(Info.SourceId);
		}

		#endregion

		#region Import and export

		/// <summary>
		/// Reads a contact file and works out what importing each card in it would do. Nothing in the file is trusted:
		/// imported contacts are never marked as verified. Every contact has a chat address, so cards without one are skipped
		/// and only counted.
		/// </summary>
		/// <remarks>
		/// A card matches a contact by its unique ID, then by chat address, then by legal ID, then by the same name and a phone
		/// number or email address in common.
		/// </remarks>
		/// <param name="Text">The contact file's text.</param>
		/// <returns>The cards that can be imported, with what importing each one does, and how many were skipped.</returns>
		public static async Task<ContactImportPreview> PreviewImportAsync(string Text)
		{
			if (Text.Length > MaxImportLength)
				Text = Text[..MaxImportLength];

			IReadOnlyList<VCard.VCard> Cards = VCardReader.Parse(Text);
			IReadOnlyList<Contact> Contacts = await GetAllAsync();
			List<ContactImportItem> Result = [];
			Dictionary<Contact, VCard.VCard> Parsed = [];
			int Skipped = 0;

			foreach (VCard.VCard Card in Cards)
			{
				if (EnsureChatLink(Card) is null)
				{
					Skipped++;
					continue;
				}

				Contact? Existing = Match(Card, Contacts, Parsed);
				if (Existing is null)
				{
					Result.Add(new ContactImportItem(Card, null, ContactImportKind.New, ContactFields.None));
					continue;
				}

				VCard.VCard Merged = Existing.GetCard();
				ContactFields Changes = Merge(Merged, Card);
				Result.Add(new ContactImportItem(Card, Existing, Changes == ContactFields.None ? ContactImportKind.Unchanged : ContactImportKind.Update, Changes));
			}

			return new ContactImportPreview(Result, Skipped);
		}

		/// <summary>
		/// Imports cards read with <see cref="PreviewImportAsync"/>: new cards become contacts, and matching contacts get
		/// what the card adds. The legal ID in a card is kept, but the contact is not marked as verified.
		/// </summary>
		/// <param name="Items">The cards to import.</param>
		/// <returns>The number of contacts added or changed.</returns>
		public static async Task<int> ImportAsync(IEnumerable<ContactImportItem> Items)
		{
			int Count = 0;

			foreach (ContactImportItem Item in Items)
			{
				if (Item.Kind == ContactImportKind.Unchanged)
					continue;

				if (Item.Existing is null)
				{
					VCard.VCard Card = Item.Card;
					Card.Version = "4.0";
					await SaveAsync(new Contact(), Card);
				}
				else
				{
					VCard.VCard Card = Item.Existing.GetCard();
					Merge(Card, Item.Card);
					await SaveAsync(Item.Existing, Card);
				}

				Count++;
			}

			return Count;
		}

		/// <summary>
		/// Writes contacts to a contact file, as vCard 4.0 cards one after another.
		/// </summary>
		/// <param name="Contacts">The contacts.</param>
		/// <param name="IncludePhotos">If photos are included.</param>
		/// <param name="IncludeNotes">If the user's notes are included.</param>
		/// <returns>The file's text.</returns>
		public static string Export(IEnumerable<Contact> Contacts, bool IncludePhotos, bool IncludeNotes)
		{
			List<VCard.VCard> Cards = [];

			foreach (Contact Contact in Contacts)
			{
				VCard.VCard Card = Contact.GetCard();

				if (!IncludePhotos)
					Card.RemovePhoto();

				if (!IncludeNotes)
					Card.Note = null;

				Cards.Add(Card);
			}

			return VCardWriter.Write(Cards);
		}

		private static Contact? Match(VCard.VCard Card, IReadOnlyList<Contact> Contacts, Dictionary<Contact, VCard.VCard> Parsed)
		{
			string? Uid = Card.Uid;
			if (!string.IsNullOrWhiteSpace(Uid))
			{
				Contact? ByUid = Contacts.FirstOrDefault(C => string.Equals(C.Uid, Uid, StringComparison.Ordinal));
				if (ByUid is not null)
					return ByUid;
			}

			string? ChatAddress = ChatAddressOf(Card);
			if (!string.IsNullOrEmpty(ChatAddress))
			{
				Contact? ByChat = Contacts.FirstOrDefault(C => string.Equals(C.ChatAddress.Value, ChatAddress, StringComparison.OrdinalIgnoreCase));
				if (ByChat is not null)
					return ByChat;
			}

			string? LegalId = Card.GetValue(LegalIdProperty)?.Trim();
			if (!string.IsNullOrEmpty(LegalId))
			{
				Contact? ById = Contacts.FirstOrDefault(C => string.Equals(C.LegalId.Value, LegalId, StringComparison.OrdinalIgnoreCase));
				if (ById is not null)
					return ById;
			}

			string Name = NameOf(Card);
			foreach (Contact Contact in Contacts)
			{
				if (!string.Equals(Contact.DisplayName, Name, StringComparison.OrdinalIgnoreCase))
					continue;

				if (!Parsed.TryGetValue(Contact, out VCard.VCard? Other))
				{
					Other = Contact.GetCard();
					Parsed[Contact] = Other;
				}

				if (Card.Telephones.Any(T => Other.Telephones.Any(O => SamePhone(T.Value, O.Value))) ||
					Card.Emails.Any(E => Other.Emails.Any(O => string.Equals(E.Value, O.Value, StringComparison.OrdinalIgnoreCase))))
				{
					return Contact;
				}
			}

			return null;
		}

		/// <summary>
		/// Adds to a card what another card has and it does not: phone numbers, email addresses, postal addresses and labels
		/// are added, and a name, organization, birthday, photo, notes and chat address are filled in where missing.
		/// </summary>
		/// <returns>What was added or changed.</returns>
		private static ContactFields Merge(VCard.VCard Target, VCard.VCard Source)
		{
			ContactFields Changes = ContactFields.None;

			if (string.IsNullOrWhiteSpace(Target.DisplayName) && !string.IsNullOrWhiteSpace(Source.DisplayName))
			{
				Target.FormattedName = Source.DisplayName;
				if (Source.Name is not null)
					Target.Name = Source.Name;

				Changes |= ContactFields.Name;
			}

			foreach (VCardTypedValue Phone in Source.Telephones)
			{
				if (!Target.Telephones.Any(T => SamePhone(T.Value, Phone.Value)))
				{
					Target.AddTelephone(Phone.Value, [.. Phone.Types]);
					Changes |= ContactFields.Phone;
				}
			}

			foreach (VCardTypedValue Email in Source.Emails)
			{
				if (!Target.Emails.Any(T => string.Equals(T.Value, Email.Value, StringComparison.OrdinalIgnoreCase)))
				{
					Target.AddEmail(Email.Value, [.. Email.Types]);
					Changes |= ContactFields.Email;
				}
			}

			foreach (VCardAddress Address in Source.Addresses)
			{
				string Line = Address.ToSingleLine();
				if (!Target.Addresses.Any(T => string.Equals(T.ToSingleLine(), Line, StringComparison.OrdinalIgnoreCase)))
				{
					Target.AddAddress(Address);
					Changes |= ContactFields.Address;
				}
			}

			if (Target.Organization.Count == 0 && Source.Organization.Count > 0)
			{
				Target.SetOrganization([.. Source.Organization]);
				Changes |= ContactFields.Organization;
			}

			if (string.IsNullOrWhiteSpace(Target.Title) && !string.IsNullOrWhiteSpace(Source.Title))
			{
				Target.Title = Source.Title;
				Changes |= ContactFields.Organization;
			}

			if (string.IsNullOrWhiteSpace(Target.Birthday) && !string.IsNullOrWhiteSpace(Source.Birthday))
			{
				Target.Birthday = Source.Birthday;
				Changes |= ContactFields.Birthday;
			}

			if (Target.Photo is null && Source.Photo is VCardPhoto Photo && Photo.Data is not null && !string.IsNullOrEmpty(Photo.MediaType))
			{
				Target.SetPhoto(Photo.Data, Photo.MediaType);
				Changes |= ContactFields.Photo;
			}

			if (string.IsNullOrWhiteSpace(Target.Note) && !string.IsNullOrWhiteSpace(Source.Note))
			{
				Target.Note = Source.Note;
				Changes |= ContactFields.Notes;
			}

			List<string> Labels = [.. Target.Categories];
			foreach (string Label in Source.Categories)
			{
				if (!Labels.Contains(Label, StringComparer.CurrentCultureIgnoreCase))
				{
					Labels.Add(Label);
					Changes |= ContactFields.Labels;
				}
			}

			if ((Changes & ContactFields.Labels) != 0)
				Target.Categories = Labels;

			string? ChatAddress = ChatAddressOf(Source);
			if (ChatAddressOf(Target) is null && ChatAddress is not null)
			{
				Target.AddImpp(chatScheme + ChatAddress);
				Changes |= ContactFields.ChatAddress;
			}

			if (string.IsNullOrWhiteSpace(Target.GetValue(LegalIdProperty)) && !string.IsNullOrWhiteSpace(Source.GetValue(LegalIdProperty)))
				Target.SetSingle(LegalIdProperty, Source.GetValue(LegalIdProperty));

			return Changes;
		}

		#endregion
	}
}
