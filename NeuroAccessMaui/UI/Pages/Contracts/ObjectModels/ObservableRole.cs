using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Services;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Contracts.ObjectModel
{
    /// <summary>
    /// An observable object that wraps a <see cref="Waher.Networking.XMPP.Contracts.Role"/> object.
    /// This allows for easier binding in the UI.
    /// </summary>
    public class ObservableRole : ObservableObject
    {
        #region Constructor
        public ObservableRole(Role role)
        {
            this.Role = role;
            this.parts = new ObservableCollection<Part>();
        }
        #endregion

        #region Initialization
        /// <summary>
        /// Initializes the role in regards to a contract.
        /// E.g Sets the description of the role, with the contract language.
        /// </summary>
        /// <param name="contract"></param>
        public async Task InitializeAsync(Contract contract)
        {
            try
            {
                // Set the description based on the contract language
                this.Description = await contract.ToPlainText(this.Role.Descriptions, contract.DeviceLanguage());

                // Add the parts associated with the role
                foreach (Part part in contract.Parts)
                {
                    if (part.Role == this.Name)
                    {
                        this.AddPart(part);
                    }
                }
            }
            catch (Exception e)
            {
                ServiceRef.LogService.LogException(e);
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// The wrapped Role object
        /// </summary>
        public Role Role { get; }

        /// <summary>
        /// The parts associated with the role
        /// </summary>
        public ObservableCollection<Part> Parts
        {
            get => this.parts;
            private set => this.SetProperty(ref this.parts, value);
        }
        private ObservableCollection<Part> parts;

        /// <summary>
        /// The name of the role
        /// </summary>
        public string Name => this.Role.Name;

        /// <summary>
        /// The localized description of the role
        /// Has to be initialized with <see cref="InitializeAsync"/>
        /// </summary>
        public string Description
        {
            get => this.description;
            private set
            {
                if (this.SetProperty(ref this.description, value))
                {
                    this.OnPropertyChanged(nameof(this.Label));
                }
            }
        }
        private string description = string.Empty;

        /// <summary>
        /// Label to display in the UI, defaults to Name if Description is not set.
        /// </summary>
        public string Label => string.IsNullOrEmpty(this.Description) ? this.Name : this.Description;

        /// <summary>
        /// Largest amount of signatures of this role required for a legally binding contract.
        /// </summary>
        public int MaxCount => this.Role.MaxCount;

        /// <summary>
        /// Smallest amount of signatures of this role required for a legally binding contract.
        /// </summary>
        public int MinCount => this.Role.MinCount;
        #endregion

        #region Methods
        /// <summary>
        /// Adds a part with a given LegalId to the role.
        /// </summary>
        /// <param name="LegalId"></param>
        public void AddPart(string LegalId)
        {
            this.Parts.Add(new Part { LegalId = LegalId, Role = this.Name });
        }

        /// <summary>
        /// Adds a part object to the role.
        /// </summary>
        /// <param name="part"></param>
        public void AddPart(Part part)
        {
            this.Parts.Add(part);
        }
        #endregion
    }
}
