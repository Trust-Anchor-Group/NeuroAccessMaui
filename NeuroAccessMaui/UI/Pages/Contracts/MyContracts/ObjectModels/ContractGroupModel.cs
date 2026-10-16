using System.Collections.ObjectModel;
using System.ComponentModel;
using NeuroAccessMaui.Services.Contracts;

namespace NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels
{
	/// <summary>
	/// One group of the contracts inbox. Holds the loaded rows, contract requests first and then contracts sorted by last change.
	/// </summary>
	public class ContractGroupModel : ObservableCollection<object>
	{
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
		/// Finds the row of a contract.
		/// </summary>
		/// <param name="ContractId">The contract ID.</param>
		/// <returns>The row, or null if the contract isn't in this group.</returns>
		public ContractModel? FindContract(string? ContractId)
		{
			if (string.IsNullOrEmpty(ContractId))
				return null;

			foreach (object Item in this)
			{
				if (Item is ContractModel Model && string.Equals(Model.ContractId, ContractId, StringComparison.OrdinalIgnoreCase))
					return Model;
			}

			return null;
		}

		/// <summary>
		/// Replaces the contract rows. When the same contracts are listed in the same order, the rows are updated in place,
		/// so the list doesn't flash.
		/// </summary>
		/// <param name="References">The stored contracts, sorted by last change.</param>
		public void SetContracts(IReadOnlyList<ContractReference> References)
		{
			List<ContractModel> Current = [.. this.OfType<ContractModel>()];

			bool Same = Current.Count == References.Count;
			for (int i = 0; Same && i < Current.Count; i++)
			{
				if (!string.Equals(Current[i].ContractId, References[i].ContractId?.Value, StringComparison.OrdinalIgnoreCase))
					Same = false;
			}

			if (Same)
			{
				for (int i = 0; i < Current.Count; i++)
					Current[i].Update(References[i]);

				return;
			}

			foreach (ContractModel Model in Current)
				this.Remove(Model);

			foreach (ContractReference Ref in References)
				this.Add(new ContractModel(Ref));
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
					this.Add(new ContractModel(Ref));
			}
		}

		/// <summary>
		/// Replaces the contract-request rows, which are listed first.
		/// </summary>
		/// <param name="Petitions">The pending requests, newest first.</param>
		public void SetPetitions(IReadOnlyList<ContractPetitionModel> Petitions)
		{
			List<ContractPetitionModel> Current = [.. this.OfType<ContractPetitionModel>()];

			bool Same = Current.Count == Petitions.Count;
			for (int i = 0; Same && i < Current.Count; i++)
			{
				if (!string.Equals(Current[i].RecordId, Petitions[i].RecordId, StringComparison.Ordinal))
					Same = false;
			}

			if (Same)
				return;

			foreach (ContractPetitionModel Model in Current)
				this.Remove(Model);

			for (int i = 0; i < Petitions.Count; i++)
				this.Insert(i, Petitions[i]);
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

			while (Index < this.Count)
			{
				object Item = this[Index];

				if (Item is ContractModel Other && Other.ContractRef.LastChanged <= Changed)
					break;

				Index++;
			}

			if (Index >= this.Count && this.HasMore)
				return false;

			this.Insert(Index, Model);
			return true;
		}
	}
}
