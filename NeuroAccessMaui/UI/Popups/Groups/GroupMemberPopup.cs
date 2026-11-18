using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Popups.Contacts;

namespace NeuroAccessMaui.UI.Popups.Groups
{
	/// <summary>
	/// What can be done with a member of a group, shown when the member is tapped on the group page: message them, view
	/// their contact, make them an admin, or remove them from the group.
	/// </summary>
	public sealed class GroupMemberPopup : ContactSheetPopup
	{
		private const string messageIcon = "M21 12a8 8 0 0 1-11.6 7.1L4 20l1-4.6A8 8 0 1 1 21 12z";
		private const string contactIcon = "M12 4a4 4 0 1 1 0 8a4 4 0 1 1 0-8z M4 21a8 8 0 0 1 16 0";
		private const string adminIcon = "M12 2l8 3v6.5c0 5-3.4 8.7-8 10.5-4.6-1.8-8-5.5-8-10.5V5l8-3z";
		private const string removeIcon = "M10 4a4 4 0 1 1 0 8a4 4 0 1 1 0-8z M3 21a7 7 0 0 1 14 0 M16 11h6";

		private readonly TaskCompletionSource<GroupMemberAction?> result = new();

		/// <summary>
		/// Creates the sheet.
		/// </summary>
		/// <param name="Name">The member's name.</param>
		/// <param name="CanMessage">If a chat with the member can be opened.</param>
		/// <param name="CanViewContact">If the member is in the user's contacts.</param>
		/// <param name="CanMakeAdmin">If the user can make the member an admin.</param>
		/// <param name="CanRemove">If the user can remove the member from the group.</param>
		public GroupMemberPopup(string Name, bool CanMessage, bool CanViewContact, bool CanMakeAdmin, bool CanRemove)
		{
			this.Add(this.CreateHeader(Name, null));

			List<View> Rows = [];

			if (CanMessage)
				Rows.Add(this.CreateChoice(messageIcon, ServiceRef.Localizer[nameof(AppResources.Message)].Value, null, false, GroupMemberAction.Message));

			if (CanViewContact)
			{
				Rows.Add(this.CreateChoice(contactIcon, ServiceRef.Localizer[nameof(AppResources.GroupsViewContact)].Value, null, false,
					GroupMemberAction.ViewContact));
			}

			if (CanMakeAdmin)
			{
				Rows.Add(this.CreateChoice(adminIcon, ServiceRef.Localizer[nameof(AppResources.GroupsMakeAdmin)].Value,
					ServiceRef.Localizer[nameof(AppResources.GroupsMakeAdminHint)].Value, false, GroupMemberAction.MakeAdmin));
			}

			if (Rows.Count > 0)
				this.Add(CreateCard([.. Rows]));

			if (CanRemove)
			{
				this.Add(CreateCard(this.CreateChoice(removeIcon, ServiceRef.Localizer[nameof(AppResources.GroupsRemoveFromGroup)].Value, null, true,
					GroupMemberAction.Remove)));
			}
		}

		/// <summary>
		/// Shows the sheet, and waits for the user's choice.
		/// </summary>
		/// <param name="Name">The member's name.</param>
		/// <param name="CanMessage">If a chat with the member can be opened.</param>
		/// <param name="CanViewContact">If the member is in the user's contacts.</param>
		/// <param name="CanMakeAdmin">If the user can make the member an admin.</param>
		/// <param name="CanRemove">If the user can remove the member from the group.</param>
		/// <returns>What the user chose, or null if the sheet was closed.</returns>
		public static async Task<GroupMemberAction?> ShowAsync(string Name, bool CanMessage, bool CanViewContact, bool CanMakeAdmin, bool CanRemove)
		{
			GroupMemberPopup Sheet = new(Name, CanMessage, CanViewContact, CanMakeAdmin, CanRemove);
			await ShowSheetAsync(Sheet);
			return await Sheet.result.Task;
		}

		/// <inheritdoc/>
		public override Task OnDisappearingAsync()
		{
			this.result.TrySetResult(null);
			return base.OnDisappearingAsync();
		}

		private View CreateChoice(string IconData, string Title, string? Subtitle, bool Danger, GroupMemberAction Action)
		{
			return this.CreateRow(IconData, Title, Subtitle, Danger, null, () => this.result.TrySetResult(Action));
		}
	}
}
