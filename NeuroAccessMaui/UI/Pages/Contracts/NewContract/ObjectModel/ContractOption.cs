using System.Collections.Generic;
using Waher.Persistence;

namespace NeuroAccessMaui.UI.Pages.Contracts.NewContract.ObjectModel
{
    /// <summary>
    /// The data model for contract visibility.
    /// </summary>
    public class ContractOption
	{
        /// <summary>
        /// Create an instance of the <see cref="ContractVisibilityModel"/> class.
        /// </summary>
        /// <param name="Name">The contract name.</param>
		/// <param name="Option">Parameter dictionary of option.</param>
        public ContractOption(string Name, IDictionary<CaseInsensitiveString, object> Option)
        {
            this.Name = Name;
			this.Option = Option;
        }

        /// <summary>
        /// The contract visibility.
        /// </summary>
        public IDictionary<CaseInsensitiveString, object> Option { get; }

        /// <summary>
        /// The contract name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Returns the string representation, i.e. name, of this contract option.
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            return this.Name;
        }
    }
}
