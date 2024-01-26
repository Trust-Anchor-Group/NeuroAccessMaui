using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Contracts.NewContract.ObjectModel
{
    /// <summary>
    /// The data model for contract visibility.
    /// </summary>
    public class ContractVisibilityModel
    {
        /// <summary>
        /// Create an instance of the <see cref="ContractVisibilityModel"/> class.
        /// </summary>
        /// <param name="Visibility">The contract visibility.</param>
        /// <param name="Name">The contract name.</param>
        public ContractVisibilityModel(ContractVisibility Visibility, string Name)
        {
            this.Visibility = Visibility;
            this.Name = Name;
        }

        /// <summary>
        /// The contract visibility.
        /// </summary>
        public ContractVisibility Visibility { get; }

        /// <summary>
        /// The contract name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Returns the string representation, i.e. name, of this <see cref="ContractVisibilityModel"/>.
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            return this.Name;
        }
    }
}
