using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contracts;
using NeuroAccessMaui.UI.Controls;

namespace NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels
{
	/// <summary>
	/// One group of the contracts inbox. Holds the loaded rows, contract requests first and then contracts sorted by last change.
	/// In the groups where nothing waits for the user, Active and Ended, contracts with the same name fold into one row.
	/// </summary>
	public class ContractGroupModel : ObservableCollection<object>
	{
		private readonly List<ContractPetitionModel> petitions = [];
		private readonly List<ContractModel> contracts = [];
		private readonly Dictionary<string, ContractFamilyModel> families = new(StringComparer.CurrentCultureIgnoreCase);
		private bool hasMore;

		/// <summary>
		/// One group of the contracts inbox.
		/// </summary>
		/// <param name="Group">The group.</param>
		/// <param name="Title">The localized title of the group.</param>
		public ContractGroupModel(ContractListGroup Group, string Title)
		{
			this.Group = Group;
			this.Title = Title;
		}

		/// <summary>
		/// The group.
		/// </summary>
		public ContractListGroup Group { get; }

		/// <summary>
		/// The localized title of the group.
		/// </summary>
		public string Title { get; }

		/// <summary>
		/// If contracts with the same name fold into one row: only where nothing waits for the user.
		/// </summary>
		public bool FoldsSimilar => this.Group is ContractListGroup.Signed or ContractListGroup.Ended;

		/// <summary>
		/// How many contracts and requests the group holds, folded or not.
		/// </summary>
		public int ItemCount => this.petitions.Count + this.contracts.Count;

		/// <summary>
		/// The heading of the group, with how many contracts it has, such as "Needs you (2)". A "+" shows that more can be
		/// loaded.
		/// </summary>
		public string HeaderText => this.ItemCount == 0
			? this.Title
			: ServiceRef.Localizer[nameof(AppResources.ContractGroupHeader), this.Title,
				this.HasMore ? this.ItemCount.ToString(CultureInfo.CurrentCulture) + "+" : this.ItemCount.ToString(CultureInfo.CurrentCulture)];

		/// <inheritdoc/>
		protected override void OnCollectionChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			base.OnCollectionChanged(e);

			// The rows of a group are drawn as one card.
			ListCard.UpdatePositions(this);
		}

		/// <inheritdoc/>
		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			if (e.PropertyName is nameof(this.Count) or nameof(this.HasMore))
				base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(this.HeaderText)));
		}

		/// <summary>
		/// Number of stored contracts read for this group. The next page starts here.
		/// </summary>
		public int LoadedCount { get; set; }

		/// <summary>
		/// If more stored contracts can be loaded for this group.
		/// </summary>
		public bool HasMore
		{
			get => this.hasMore;
			set
			{
				if (this.hasMore != value)
				{
					this.hasMore = value;
					this.OnPropertyChanged(new PropertyChangedEventArgs(nameof(this.HasMore)));
				}
			}
		}

		/// <summary>
		/// Finds the row of a contract, also when it is folded with others.
		/// </summary>
		/// <param name="ContractId">The contract ID.</param>
		/// <returns>The row, or null if the contract isn't in this group.</returns>
		public ContractModel? FindContract(string? ContractId)
		{
			if (string.IsNullOrEmpty(ContractId))
				return null;

			return this.contracts.FirstOrDefault(Model => string.Equals(Model.ContractId, ContractId, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Replaces the contract rows. When the same contracts are listed in the same order, the rows are updated in place,
		/// so the list doesn't flash.
		/// </summary>
		/// <param name="References">The stored contracts, sorted by last change.</param>
		public void SetContracts(IReadOnlyList<ContractReference> References)
		{
			bool Same = this.contracts.Count == References.Count;
			for (int i = 0; Same && i < this.contracts.Count; i++)
			{
				if (!string.Equals(this.contracts[i].ContractId, References[i].ContractId?.Value, StringComparison.OrdinalIgnoreCase))
					Same = false;
			}

			if (Same)
			{
				for (int i = 0; i < this.contracts.Count; i++)
					this.contracts[i].Update(References[i]);
			}
			else
			{
				this.contracts.Clear();

				foreach (ContractReference Ref in References)
					this.contracts.Add(new ContractModel(Ref));
			}

			this.Rebuild();
		}

		/// <summary>
		/// Adds contract rows after the loaded ones, skipping contracts that are already listed.
		/// </summary>
		/// <param name="References">The stored contracts, sorted by last change.</param>
		public void AddContracts(IEnumerable<ContractReference> References)
		{
			foreach (ContractReference Ref in References)
			{
				if (this.FindContract(Ref.ContractId?.Value) is null)
					this.contracts.Add(new ContractModel(Ref));
			}

			this.Rebuild();
		}

		/// <summary>
		/// Replaces the contract-request rows, which are listed first.
		/// </summary>
		/// <param name="Petitions">The pending requests, newest first.</param>
		public void SetPetitions(IReadOnlyList<ContractPetitionModel> Petitions)
		{
			bool Same = this.petitions.Count == Petitions.Count;
			for (int i = 0; Same && i < this.petitions.Count; i++)
			{
				if (!string.Equals(this.petitions[i].RecordId, Petitions[i].RecordId, StringComparison.Ordinal))
					Same = false;
			}

			if (Same)
				return;

			this.petitions.Clear();
			this.petitions.AddRange(Petitions);
			this.Rebuild();
		}

		/// <summary>
		/// Inserts a contract row where it belongs by last change.
		/// </summary>
		/// <param name="Model">The row.</param>
		/// <returns>If the row was inserted. A row older than the loaded ones isn't, when more rows can be loaded, since it
		/// belongs to a later page.</returns>
		public bool InsertContract(ContractModel Model)
		{
			DateTime Changed = Model.ContractRef.LastChanged;
			int Index = 0;

			while (Index < this.contracts.Count && this.contracts[Index].ContractRef.LastChanged > Changed)
				Index++;

			if (Index >= this.contracts.Count && this.HasMore)
				return false;

			this.contracts.Insert(Index, Model);
			this.Rebuild();
			return true;
		}

		/// <summary>
		/// Removes a contract row, also when it is folded with others.
		/// </summary>
		/// <param name="Model">The row.</param>
		public void RemoveContract(ContractModel Model)
		{
			if (this.contracts.Remove(Model))
				this.Rebuild();
		}

		/// <summary>
		/// Shows the requests and contracts, folding contracts with the same name where the group allows it. Rows that stay
		/// are kept and moved, so the list doesn't flash, and a folded row stays open or closed.
		/// </summary>
		private void Rebuild()
		{
			List<object> Rows = [.. this.petitions];

			if (this.FoldsSimilar)
			{
				Dictionary<string, List<ContractModel>> ByName = new(StringComparer.CurrentCultureIgnoreCase);
				List<string> Order = [];

				foreach (ContractModel Contract in this.contracts)
				{
					string Name = Contract.DisplayName;

					if (!ByName.TryGetValue(Name, out List<ContractModel>? Members))
					{
						Members = [];
						ByName[Name] = Members;
						Order.Add(Name);
					}

					Members.Add(Contract);
				}

				Dictionary<string, ContractFamilyModel> Kept = new(StringComparer.CurrentCultureIgnoreCase);

				foreach (string Name in Order)
				{
					List<ContractModel> Members = ByName[Name];

					if (Members.Count == 1)
					{
						Rows.Add(Members[0]);
						continue;
					}

					if (!this.families.TryGetValue(Name, out ContractFamilyModel? Family))
						Family = new ContractFamilyModel(Name);

					Family.Members = Members;
					Kept[Name] = Family;
					Rows.Add(Family);
				}

				this.families.Clear();
				foreach (KeyValuePair<string, ContractFamilyModel> Pair in Kept)
					this.families[Pair.Key] = Pair.Value;
			}
			else
				Rows.AddRange(this.contracts);

			for (int i = 0; i < Rows.Count; i++)
			{
				if (i < this.Count && ReferenceEquals(this[i], Rows[i]))
					continue;

				int Found = this.IndexOf(Rows[i]);

				if (Found > i)
					this.Move(Found, i);
				else
					this.Insert(i, Rows[i]);
			}

			while (this.Count > Rows.Count)
				this.RemoveAt(this.Count - 1);

			base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(this.HeaderText)));
		}
	}
}
