using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.UI;

namespace NeuroAccessMaui.UI.Pages.Contacts.MyContacts
{
	/// <summary>
	/// Actions to take when a contact has been selected.
	/// </summary>
	public enum SelectContactAction
	{
		/// <summary>
		/// Make a payment to contact.
		/// </summary>
		MakePayment,

		/// <summary>
		/// View the contacts list.
		/// </summary>
		View,

		/// <summary>
		/// Embed link to ID in chat
		/// </summary>
		Select
	}

	/// <summary>
	/// Holds navigation parameters specific to views displaying a list of contacts.
	/// </summary>
	public class ContactListNavigationArgs : NavigationArgs
	{
		private readonly string? description;
		private readonly SelectContactAction? action;
		private readonly TaskCompletionSource<ContactInfoModel?>? selection;

		/// <summary>
		/// Creates an instance of the <see cref="ContactListNavigationArgs"/> class.
		/// </summary>
		public ContactListNavigationArgs()
		{
		}

		/// <summary>
		/// Creates an instance of the <see cref="ContactListNavigationArgs"/> class.
		/// </summary>
		/// <param name="Description">Description presented to user.</param>
		/// <param name="Action">Action to take when a contact has been selected.</param>
		public ContactListNavigationArgs(string Description, SelectContactAction Action)
		{
			this.description = Description;
			this.action = Action;
		}

		/// <summary>
		/// Creates an instance of the <see cref="ContactListNavigationArgs"/> class.
		/// </summary>
		/// <param name="Description">Description presented to user.</param>
		/// <param name="Selection">Selection source, where selected item will be stored, or null if cancelled.</param>
		public ContactListNavigationArgs(string Description, TaskCompletionSource<ContactInfoModel?> Selection)
			: this(Description, SelectContactAction.Select)
		{
			this.selection = Selection;
		}

		/// <summary>
		/// Description presented to user.
		/// </summary>
		public string? Description => this.description;

		/// <summary>
		/// Action to take when a contact has been selected.
		/// </summary>
		public SelectContactAction? Action => this.action;

		/// <summary>
		/// Selection source, if selecting identity.
		/// </summary>
		public TaskCompletionSource<ContactInfoModel?>? Selection => this.selection;

		/// <summary>
		/// If the user should be able to scane QR Codes.
		/// </summary>
		public bool CanScanQrCode { get; set; } = false;

		/// <summary>
		/// If user is allowed to select an Anonymous option.
		/// </summary>
		public bool AllowAnonymous { get; set; } = false;

		/// <summary>
		/// String to display on the anonymous button
		/// </summary>
		public string? AnonymousText { get; set; }

		/// <summary>
		/// Any preloaded contacts
		/// </summary>
		public IEnumerable<ContactInfo>? Contacts { get; set; }

	}
}
