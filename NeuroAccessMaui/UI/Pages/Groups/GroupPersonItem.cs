using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Groups
{
	/// <summary>
	/// A person who can be added to a group: a row to tick in the list, and a chip once chosen.
	/// </summary>
	public partial class GroupPersonItem : ObservableObject
	{
		private static readonly (string Background, string Text)[] avatarTints =
		[
			("#D5E6E2", "#085E50"),
			("#DCE5F1", "#24519C"),
			("#FBE3D2", "#A84700"),
			("#ECDFF7", "#722FAD"),
			("#E6E8EA", "#444A4F")
		];

		private readonly Action<GroupPersonItem> toggle;

		/// <summary>
		/// Creates a person who can be added to a group.
		/// </summary>
		/// <param name="Address">The person's chat address.</param>
		/// <param name="Name">The person's name.</param>
		/// <param name="Toggle">Chooses the person, or stops choosing them.</param>
		public GroupPersonItem(string Address, string Name, Action<GroupPersonItem> Toggle)
		{
			this.Address = Address;
			this.Name = Name;
			this.toggle = Toggle;
			this.Initials = InitialsOf(Name);

			int Tint = (int)((uint)StringComparer.OrdinalIgnoreCase.GetHashCode(Address) % (uint)avatarTints.Length);
			this.AvatarBackground = Color.FromArgb(avatarTints[Tint].Background);
			this.AvatarText = Color.FromArgb(avatarTints[Tint].Text);
		}

		/// <summary>
		/// The person's chat address.
		/// </summary>
		public string Address { get; }

		/// <summary>
		/// The person's name.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// The first word of the person's name, as shown on the chip.
		/// </summary>
		public string FirstName => this.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? this.Name;

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
		/// If the person is chosen.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(StateText))]
		private bool isSelected;

		/// <summary>
		/// If the person is chosen, as a screen reader reads it.
		/// </summary>
		public string StateText => this.IsSelected ?
			ServiceRef.Localizer[nameof(AppResources.ContactImportSelected)] :
			ServiceRef.Localizer[nameof(AppResources.ContactImportNotSelected)];

		/// <summary>
		/// How the remove button on the chip is read by a screen reader.
		/// </summary>
		public string RemoveText => ServiceRef.Localizer[nameof(AppResources.GroupsRemovePerson), this.Name];

		/// <summary>
		/// Chooses the person, or stops choosing them.
		/// </summary>
		[RelayCommand]
		private void Toggle()
		{
			this.toggle(this);
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
