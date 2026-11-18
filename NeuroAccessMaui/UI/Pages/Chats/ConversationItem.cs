using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Chat;
using NeuroAccessMaui.UI.Controls;
using Waher.Networking.XMPP;
using Contact = NeuroAccessMaui.Services.Contacts.Contact;

namespace NeuroAccessMaui.UI.Pages.Chats
{
	/// <summary>
	/// A row in the list of chats: who the chat is with, its last message and when it came, and if it has unread messages, is
	/// muted or is pinned.
	/// </summary>
	public partial class ConversationItem : ObservableObject
	{
		private const int maxUnreadShown = 99;

		private static readonly (string Background, string Text)[] avatarTints =
		[
			("#D5E6E2", "#085E50"),
			("#DCE5F1", "#24519C"),
			("#FBE3D2", "#A84700"),
			("#ECDFF7", "#722FAD"),
			("#E6E8EA", "#444A4F")
		];

		private readonly Func<ConversationItem, Task> open;
		private readonly Func<ConversationItem, Task> showActions;

		/// <summary>
		/// Creates a row for a conversation.
		/// </summary>
		/// <param name="Conversation">The conversation.</param>
		/// <param name="Contact">The contact the chat is with, if any.</param>
		/// <param name="Open">Opens the chat.</param>
		/// <param name="ShowActions">Shows what can be done with the chat.</param>
		public ConversationItem(Conversation Conversation, Contact? Contact, Func<ConversationItem, Task> Open,
			Func<ConversationItem, Task> ShowActions)
		{
			this.Conversation = Conversation;
			this.Contact = Contact;
			this.open = Open;
			this.showActions = ShowActions;
			this.IsNotes = ConversationStore.IsOwnAddress(Conversation.Address.Value);

			if (this.IsNotes)
				this.Title = ServiceRef.Localizer[nameof(AppResources.ChatMe)];
			else if (Contact is not null && !string.IsNullOrWhiteSpace(Contact.DisplayName))
				this.Title = Contact.DisplayName;
			else if (!string.IsNullOrWhiteSpace(Conversation.Title))
				this.Title = Conversation.Title;
			else
				this.Title = Conversation.Address.Value;

			this.Initials = this.IsNotes ? ServiceRef.Localizer[nameof(AppResources.ChatMe)] : InitialsOf(this.Title);

			if (this.IsNotes)
			{
				this.AvatarBackground = Color.FromArgb("#0A715F");
				this.AvatarText = Color.FromArgb("#FCFCFC");
			}
			else
			{
				int Tint = (int)((uint)StringComparer.OrdinalIgnoreCase.GetHashCode(this.Address) % (uint)avatarTints.Length);
				this.AvatarBackground = Color.FromArgb(avatarTints[Tint].Background);
				this.AvatarText = Color.FromArgb(avatarTints[Tint].Text);
			}

			if (string.IsNullOrEmpty(Conversation.LastPreview))
				this.Preview = this.IsNotes ? ServiceRef.Localizer[nameof(AppResources.ContactsMeDescription)] : string.Empty;
			else if (Conversation.LastFromMe && !this.IsNotes)
				this.Preview = ServiceRef.Localizer[nameof(AppResources.ChatsYouPrefix), Conversation.LastPreview];
			else
				this.Preview = Conversation.LastPreview;

			this.TimeText = TimeTextOf(Conversation.LastMessageAt);
			this.UpdatePresence();
		}

		/// <summary>
		/// The conversation.
		/// </summary>
		public Conversation Conversation { get; }

		/// <summary>
		/// The contact the chat is with, if any.
		/// </summary>
		public Contact? Contact { get; }

		/// <summary>
		/// The chat address of the other person, or the user's own for their notes.
		/// </summary>
		public string Address => this.Conversation.Address.Value;

		/// <summary>
		/// If the chat holds the user's own notes.
		/// </summary>
		public bool IsNotes { get; }

		/// <summary>
		/// If the chat is a group's.
		/// </summary>
		public bool IsGroup => this.Conversation.Kind == ConversationKind.Group;

		/// <summary>
		/// If the avatar shows letters. A group's shows a group icon.
		/// </summary>
		public bool ShowsInitials => !this.IsGroup;

		/// <summary>
		/// The person's photo, or the user's own on their notes, if there is one. The avatar shows initials otherwise.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasPhoto))]
		private ImageSource? photo;

		/// <summary>
		/// If there is a photo to show.
		/// </summary>
		public bool HasPhoto => this.Photo is not null;

		/// <summary>
		/// The name shown for the chat.
		/// </summary>
		public string Title { get; }

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
		/// If the app has checked the person's ID.
		/// </summary>
		public bool IsVerified => this.Contact is not null && this.Contact.IsVerified;

		/// <summary>
		/// The last message, short, on one line.
		/// </summary>
		public string Preview { get; }

		/// <summary>
		/// If there is a last message to show.
		/// </summary>
		public bool HasPreview => !string.IsNullOrEmpty(this.Preview);

		/// <summary>
		/// When the last message came: the time today, else the day.
		/// </summary>
		public string TimeText { get; }

		/// <summary>
		/// If the chat has messages the user has not seen.
		/// </summary>
		public bool HasUnread => this.Conversation.UnreadCount > 0;

		/// <summary>
		/// The number of unread messages, as shown in the badge.
		/// </summary>
		public string UnreadText => this.Conversation.UnreadCount > maxUnreadShown ?
			maxUnreadShown.ToString(CultureInfo.CurrentCulture) + "+" :
			this.Conversation.UnreadCount.ToString(CultureInfo.CurrentCulture);

		/// <summary>
		/// If the chat gives no notifications.
		/// </summary>
		public bool IsMuted => this.Conversation.Muted;

		/// <summary>
		/// If the chat is kept at the top of the list.
		/// </summary>
		public bool IsPinned => this.Conversation.Pinned;

		/// <summary>
		/// The text a search matches: the name and the last message.
		/// </summary>
		public string SearchText => this.Title + " " + this.Conversation.LastPreview;

		/// <summary>
		/// The row as a screen reader reads it.
		/// </summary>
		public string AccessibleDescription
		{
			get
			{
				List<string> Parts = [this.Title];

				if (this.HasUnread)
					Parts.Add(ServiceRef.Localizer[nameof(AppResources.ChatsUnreadCount), this.Conversation.UnreadCount]);

				if (this.IsPinned)
					Parts.Add(ServiceRef.Localizer[nameof(AppResources.ChatsPinned)]);

				if (this.IsMuted)
					Parts.Add(ServiceRef.Localizer[nameof(AppResources.ChatsMuted)]);

				if (this.HasPreview)
					Parts.Add(this.Preview);

				if (!string.IsNullOrEmpty(this.TimeText))
					Parts.Add(this.TimeText);

				return string.Join(", ", Parts);
			}
		}

		/// <summary>
		/// Where the row is in the card of chats, for its corners and divider.
		/// </summary>
		[ObservableProperty]
		private ListCardPosition cardPosition = ListCardPosition.Only;

		/// <summary>
		/// Opens the chat.
		/// </summary>
		[RelayCommand]
		private Task OpenAsync()
		{
			return this.open(this);
		}

		/// <summary>
		/// Shows what can be done with the chat: pin, mute, mark as read or delete.
		/// </summary>
		[RelayCommand]
		private Task ShowActionsAsync()
		{
			return this.showActions(this);
		}

		/// <summary>
		/// If the user sees when the person is online, and they are.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(OnlineColor))]
		private bool showsOnlineStatus;

		/// <summary>
		/// The person's availability, if the user sees it.
		/// </summary>
		private Availability? onlineAvailability;

		/// <summary>
		/// The colour of the online dot: green when online, orange when away, red when busy.
		/// </summary>
		public Color OnlineColor => this.onlineAvailability switch
		{
			Availability.Away or Availability.ExtendedAway => AppColors.TnPWarningContent,
			Availability.DoNotDisturb => AppColors.TnPDangerContent,
			_ => AppColors.TnPSuccessFigure
		};

		/// <summary>
		/// Reads the person's online status again.
		/// </summary>
		public void UpdatePresence()
		{
			Availability? Current = null;

			if (!this.IsNotes && !this.IsGroup)
			{
				try
				{
					RosterItem? Item = ServiceRef.XmppService.GetRosterItem(this.Address);
					if (Item is not null && (Item.State == SubscriptionState.To || Item.State == SubscriptionState.Both) && Item.HasLastPresence &&
						Item.LastPresence.Availability != Availability.Offline)
					{
						Current = Item.LastPresence.Availability;
					}
				}
				catch (Exception)
				{
					// Not connected: no online status.
				}
			}

			this.onlineAvailability = Current;
			this.ShowsOnlineStatus = Current is not null;
			this.OnPropertyChanged(nameof(this.OnlineColor));
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

		/// <summary>
		/// When a message came, as shown in the list: the time today, "Yesterday", the weekday in the last week, else the date.
		/// </summary>
		private static string TimeTextOf(DateTime Utc)
		{
			if (Utc == DateTime.MinValue)
				return string.Empty;

			DateTime Local = Utc.ToLocalTime();
			DateTime Today = DateTime.Today;

			if (Local.Date == Today)
				return Local.ToString("t", CultureInfo.CurrentCulture);

			if (Local.Date == Today.AddDays(-1))
				return ServiceRef.Localizer[nameof(AppResources.Yesterday)];

			if (Local.Date > Today.AddDays(-7))
				return Local.ToString("dddd", CultureInfo.CurrentCulture);

			return Local.ToString("d", CultureInfo.CurrentCulture);
		}
	}
}
