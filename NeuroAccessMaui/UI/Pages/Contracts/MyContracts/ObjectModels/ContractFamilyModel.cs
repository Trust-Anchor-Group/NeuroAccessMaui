using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contracts;
using NeuroAccessMaui.UI.Controls;

namespace NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels
{
	/// <summary>
	/// Contracts with the same name, folded into one row of the contracts inbox. A tap unfolds them, and each one opens
	/// on its own. Only groups where nothing waits for the user fold, so no contract that needs the user is hidden.
	/// </summary>
	/// <param name="Key">What the folded contracts have in common: their name.</param>
	public partial class ContractFamilyModel(string Key) : ObservableObject, IListCardItem
	{
		/// <summary>
		/// The contracts, newest change first.
		/// </summary>
		private IReadOnlyList<ContractModel> members = [];

		/// <summary>
		/// What the folded contracts have in common: their name.
		/// </summary>
		public string Key { get; } = Key;

		/// <summary>
		/// If the contracts are shown under the row.
		/// </summary>
		[ObservableProperty]
		private bool isExpanded;

		/// <summary>
		/// Where the row sits in its card.
		/// </summary>
		[ObservableProperty]
		private ListCardPosition cardPosition;

		/// <summary>
		/// The contracts, newest change first.
		/// </summary>
		public IReadOnlyList<ContractModel> Members
		{
			get => this.members;
			set
			{
				foreach (ContractModel Member in this.members)
					Member.PropertyChanged -= this.Member_PropertyChanged;

				this.members = value;

				foreach (ContractModel Member in this.members)
					Member.PropertyChanged += this.Member_PropertyChanged;

				this.OnPropertyChanged(string.Empty);
			}
		}

		/// <summary>
		/// The name the contracts share.
		/// </summary>
		public string Title => this.members.Count > 0 ? this.members[0].DisplayName : this.Key;

		/// <summary>
		/// How many contracts are folded, such as "3 contracts".
		/// </summary>
		public string CountText => ServiceRef.Localizer[nameof(AppResources.ContractFamilyCount), this.members.Count];

		/// <summary>
		/// When the newest of them changed.
		/// </summary>
		public string LastChangedText => this.members.Count > 0 ? this.members[0].LastChangedText : string.Empty;

		/// <summary>
		/// The group the contracts are listed under.
		/// </summary>
		public ContractListGroup Group => this.members.Count > 0 ? this.members[0].Group : ContractListGroup.Signed;

		/// <summary>
		/// If any of them changed since the user last opened it.
		/// </summary>
		public bool IsUnread => this.members.Any(Member => Member.IsUnread);

		/// <summary>
		/// What a screen reader says for the row.
		/// </summary>
		public string AccessibleDescription => this.Title + ", " + this.CountText;

		/// <summary>
		/// Shows a change to one of the contracts, such as being opened, on the row.
		/// </summary>
		private void Member_PropertyChanged(object? Sender, PropertyChangedEventArgs e)
		{
			this.OnPropertyChanged(nameof(this.IsUnread));
			this.OnPropertyChanged(nameof(this.LastChangedText));
		}
	}
}
