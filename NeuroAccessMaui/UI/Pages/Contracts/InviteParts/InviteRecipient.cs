using CommunityToolkit.Mvvm.ComponentModel;

namespace NeuroAccessMaui.UI.Pages.Contracts.InviteParts
{
	/// <summary>
	/// A person who can be invited to sign a contract, and how their invitation went.
	/// </summary>
	public partial class InviteRecipient : ObservableObject
	{
		/// <summary>
		/// Creates a new instance of the <see cref="InviteRecipient"/> class.
		/// </summary>
		/// <param name="LegalId">The person's legal ID in the contract.</param>
		/// <param name="Role">The role the person is invited to sign as.</param>
		/// <param name="BareJid">Where the invitation is sent.</param>
		/// <param name="FriendlyName">The name the user knows the person by.</param>
		public InviteRecipient(string LegalId, string Role, string BareJid, string FriendlyName)
		{
			this.LegalId = LegalId;
			this.Role = Role;
			this.BareJid = BareJid;
			this.FriendlyName = FriendlyName;
		}

		/// <summary>
		/// The person's legal ID in the contract.
		/// </summary>
		public string LegalId { get; }

		/// <summary>
		/// The role the person is invited to sign as.
		/// </summary>
		public string Role { get; }

		/// <summary>
		/// Where the invitation is sent.
		/// </summary>
		public string BareJid { get; }

		/// <summary>
		/// The name the user knows the person by.
		/// </summary>
		public string FriendlyName { get; }

		/// <summary>
		/// If the person is to be invited.
		/// </summary>
		[ObservableProperty]
		private bool isSelected = true;

		/// <summary>
		/// If the invitation has been sent.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanChange))]
		private bool isSent;

		/// <summary>
		/// What happened to the invitation, or null before it is sent.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasStatus))]
		private string? statusText;

		/// <summary>
		/// If the invitation couldn't be sent.
		/// </summary>
		[ObservableProperty]
		private bool hasFailed;

		/// <summary>
		/// If there is a status to show.
		/// </summary>
		public bool HasStatus => !string.IsNullOrEmpty(this.StatusText);

		/// <summary>
		/// If the person can still be selected or deselected.
		/// </summary>
		public bool CanChange => !this.IsSent;
	}
}
