using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Extensions;
using Waher.Content;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Contracts.NewContract.ObjectModel
{
	/// <summary>
	/// An observable object that wraps a <see cref="Waher.Networking.XMPP.Contracts.Role"/> object.
	/// This allows for easier binding in the UI.
	/// </summary>
	public class RoleInfo : ObservableObject
    {
        public RoleInfo(Role role)
        {
            this.Role = role;
        }

		/// <summary>
		/// Initializes the role in regards to a contract.
		/// E.g Sets the description of the role, with the contract language.
		/// </summary>
		/// <param name="contract"></param>
		/// <returns></returns>
		public async Task InitalizeWithContractAsync(Contract contract)
		{
			this.Description = await contract.ToPlainText(this.Role.Descriptions, contract.DeviceLanguage());
		}

		/// <summary>
		/// The wrapped Role object
		/// </summary>
		public Role Role { get; }

        /// <summary>
        /// The name of the role
        /// </summary>
        public string Name => this.Role.Name;

		/// <summary>
		/// The localized description of the role
		/// Has to be initialized with <see cref="InitalizeWithContractAsync(Contract)"/>
		/// </summary>
		public string Description
		{
			get => this.description;
			private set => this.SetProperty(ref this.description, value);
		}
		private string description = string.Empty;

        /// <summary>
        /// Largest amount of signatures of this role required for a legally binding contract.    
        /// /// </summary>
        public int MaxCount => this.Role.MaxCount;

        /// <summary>
        /// Smallest amount of signatures of this role required for a legally binding contract.    
        /// /// </summary>
        public int MinCount => this.Role.MinCount;
	}
}
