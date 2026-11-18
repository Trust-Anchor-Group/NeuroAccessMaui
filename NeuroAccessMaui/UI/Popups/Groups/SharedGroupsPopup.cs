using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Groups;
using NeuroAccessMaui.UI.Popups.Contacts;

namespace NeuroAccessMaui.UI.Popups.Groups
{
	/// <summary>
	/// The groups the user is in together with a contact, shown from the contact's card. Tapping one picks it.
	/// </summary>
	public sealed class SharedGroupsPopup : ContactSheetPopup
	{
		private const string groupIcon = "M9 4.5a3.5 3.5 0 1 1 0 7a3.5 3.5 0 1 1 0-7z M2.5 20a6.5 6.5 0 0 1 13 0 M17 6.5a2.5 2.5 0 1 1 0 5a2.5 2.5 0 1 1 0-5z M16 14.5a5 5 0 0 1 6 4.5";

		private readonly TaskCompletionSource<ChatGroup?> result = new();

		/// <summary>
		/// Creates the sheet.
		/// </summary>
		/// <param name="ContactName">The contact's name.</param>
		/// <param name="Groups">The groups.</param>
		public SharedGroupsPopup(string ContactName, IReadOnlyList<ChatGroup> Groups)
		{
			this.Add(this.CreateHeader(ServiceRef.Localizer[nameof(AppResources.GroupsSharedTitle), ContactName].Value, null));

			List<View> Rows = [];

			foreach (ChatGroup Group in Groups)
			{
				string Subtitle = Group.MemberCount == 1 ?
					ServiceRef.Localizer[nameof(AppResources.GroupsOneMember)].Value :
					ServiceRef.Localizer[nameof(AppResources.GroupsMemberCount), Group.MemberCount].Value;

				Rows.Add(this.CreateRow(groupIcon, Group.Name, Subtitle, false, null, () => this.result.TrySetResult(Group)));
			}

			if (Rows.Count > 0)
				this.Add(CreateCard([.. Rows]));
		}

		/// <summary>
		/// Shows the sheet, and waits for the user to pick a group.
		/// </summary>
		/// <param name="ContactName">The contact's name.</param>
		/// <param name="Groups">The groups.</param>
		/// <returns>The group picked, or null if the sheet was closed.</returns>
		public static async Task<ChatGroup?> ShowAsync(string ContactName, IReadOnlyList<ChatGroup> Groups)
		{
			SharedGroupsPopup Sheet = new(ContactName, Groups);
			await ShowSheetAsync(Sheet);
			return await Sheet.result.Task;
		}

		/// <inheritdoc/>
		public override Task OnDisappearingAsync()
		{
			this.result.TrySetResult(null);
			return base.OnDisappearingAsync();
		}
	}
}
