using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels;
using Waher.Content;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Contracts.NewContract.ObjectModel
{
	/// <summary>
	/// An observable object that wraps a <see cref="Waher.Networking.XMPP.Contracts.Contract"/> object.
	/// This allows for easier binding in the UI.
    /// Either create instances with <see cref="CreateAsync"/> or initiailize with <see cref="InitializeAsync"/>
	/// </summary>
	public class ContractInfo : ObservableObject
	{
		private ContractInfo(Contract contract)
		{
            this.Contract = contract;
		}

        public static async Task<ContractInfo> CreateAsync(Contract contract)
        {
            ContractInfo contractInfo = new(contract);
            await contractInfo.InitializeAsync();
            return contractInfo;
        }

        public async Task InitializeAsync()
        {
            this.Category =  await ContractModel.GetCategory(this.Contract) ?? string.Empty;

            foreach (Parameter P in this.Contract.Parameters)
            {
                this.Parameters.Add(await ParameterInfo2.CreateAsync(P, this.Contract));
            }

        }

        /// <summary>
        /// The wrapped contract object
        /// </summary>
        public Contract Contract { get; private set; }

        /// <summary>
        /// The Roles of the contract
        /// </summary>
        private ObservableCollection<ParameterInfo2> parameters = [];
        public ObservableCollection<ParameterInfo2> Parameters
        {
            get => this.parameters;
            private set => this.SetProperty(ref this.parameters, value);
        }

        public bool IsTemplate => this.Contract.CanActAsTemplate;

        /// <summary>
        /// The Category of the contract
        /// </summary>
        public string Category 
        {
            get => this.category;
            private set => this.SetProperty(ref this.category, value);
        }
        private string category = "";



        /// <summary>
        /// The Parameters of the contract
        /// </summary>
        
	}
}
