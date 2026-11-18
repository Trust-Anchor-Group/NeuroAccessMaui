using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Groups;
using NeuroAccessMaui.UI.Controls;

namespace NeuroAccessMaui.UI.Pages.Groups.GroupInfo
{
	/// <summary>
	/// A member in the list on the group page: their name, their role, and if they are in the user's contacts.
	/// </summary>
	public partial class GroupMemberItem : ObservableObject
	{
		private static readonly (string Background, string Text)[] avatarTints =
		[
			("#D5E6E2", "#085E50"),
			("#DCE5F1", "#24519C"),
			("#FBE3D2", "#A84700"),
			("#ECDFF7", "#722FAD"),
			("#E6E8EA", "#444A4F")
		];

		private readonly Func<GroupMemberItem, Task> open;
		private readonly Func<GroupMemberItem, Task> addContact;

		/// <summary>
		/// Creates a member row.
		/// </summary>
		/// <param name="Member">The member.</param>
		/// <param name="ContactName">The member's name in the user's contacts, or null if they are not a contact.</param>
		/// <param name="IsMe">If the member is the user.</param>
		/// <param name="Open">Shows what can be done with the member.</param>
		/// <param name="AddContact">Adds the member to the user's contacts.</param>
		public GroupMemberItem(GroupMember Member, string? ContactName, bool IsMe, Func<GroupMemberItem, Task> Open,
			Func<GroupMemberItem, Task> AddContact)
		{
			this.Member = Member;
			this.IsMe = IsMe;
			this.IsContact = ContactName is not null;
			this.open = Open;
			this.addContact = AddContact;

			if (IsMe)
				this.Name = ServiceRef.Localizer[nameof(AppResources.GroupsYou)];
			else if (!string.IsNullOrWhiteSpace(ContactName))
				this.Name = ContactName;
			else if (!string.IsNullOrWhiteSpace(Member.Name))
				this.Name = Member.Name;
			else
				this.Name = Member.Address;

			if (IsMe)
			{
				this.Initials = ServiceRef.Localizer[nameof(AppResources.ChatMe)];
				this.AvatarBackground = Color.FromArgb("#0A715F");
				this.AvatarText = Color.FromArgb("#FCFCFC");
			}
			else
			{
				this.Initials = InitialsOf(this.Name);

				int Tint = (int)((uint)StringComparer.OrdinalIgnoreCase.GetHashCode(Member.Address.Length > 0 ? Member.Address : Member.Name) %
					(uint)avatarTints.Length);
				this.AvatarBackground = Color.FromArgb(avatarTints[Tint].Background);
				this.AvatarText = Color.FromArgb(avatarTints[Tint].Text);
			}
		}

		/// <summary>
		/// The member.
		/// </summary>
		public GroupMember Member { get; }

		/// <summary>
		/// The member's chat address, if known.
		/// </summary>
		public string Address => this.Member.Address;

		/// <summary>
		/// If the member is the user.
		/// </summary>
		public bool IsMe { get; }

		/// <summary>
		/// If the member is in the user's contacts.
		/// </summary>
		public bool IsContact { get; }

		/// <summary>
		/// The name shown: "You", the contact's name, or the member's name in the group.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// The letters in the avatar.
		/// </summary>
		public string Initials { get; }

		/// <summary>
		/// The avatar's background.
		/// </summary>
		public Color AvatarBackground { get; }

		/// <summary>
		/// The colour of the avatar's letters.
		/// </summary>
		public Color AvatarText { get; }

		/// <summary>
		/// The member's role, if they are an owner or an admin.
		/// </summary>
		public string RoleText => this.Member.Role switch
		{
			GroupRole.Owner => ServiceRef.Localizer[nameof(AppResources.GroupsOwner)],
			GroupRole.Admin => ServiceRef.Localizer[nameof(AppResources.GroupsAdmin)],
			_ => string.Empty
		};

		/// <summary>
		/// If the member is an owner or an admin.
		/// </summary>
		public bool HasRole => this.Member.Role != GroupRole.Member;

		/// <summary>
		/// If the member is an owner.
		/// </summary>
		public bool IsOwner => this.Member.Role == GroupRole.Owner;

		/// <summary>
		/// If the member can be added to the user's contacts: they aren't one, and their chat address is known.
		/// </summary>
		public bool CanAddContact => !this.IsMe && !this.IsContact && !string.IsNullOrEmpty(this.Address);

		/// <summary>
		/// The member as a screen reader reads them.
		/// </summary>
		public string AccessibleDescription
		{
			get
			{
				List<string> Parts = [this.Name];

				if (this.HasRole)
					Parts.Add(this.RoleText);

				if (this.CanAddContact)
					Parts.Add(ServiceRef.Localizer[nameof(AppResources.GroupsNotInContacts)]);

				return string.Join(", ", Parts);
			}
		}

		/// <summary>
		/// Where the row is in the card of members, for its corners and divider.
		/// </summary>
		[ObservableProperty]
		private ListCardPosition cardPosition = ListCardPosition.Only;

		/// <summary>
		/// Shows what can be done with the member.
		/// </summary>
		[RelayCommand]
		private Task OpenAsync()
		{
			return this.open(this);
		}

		/// <summary>
		/// Adds the member to the user's contacts.
		/// </summary>
		[RelayCommand]
		private Task AddContactAsync()
		{
			return this.addContact(this);
		}

		/// <summary>
		/// The first letters of the first two words of a name.
		/// </summary>
		private static string InitialsOf(string Name)
		{
			string[] Words = Name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

			return Words.Length switch
			{
				0 => "?",
				1 => Words[0][..1].ToUpper(CultureInfo.CurrentCulture),
				_ => (Words[0][..1] + Words[1][..1]).ToUpper(CultureInfo.CurrentCulture)
			};
		}
	}
}
