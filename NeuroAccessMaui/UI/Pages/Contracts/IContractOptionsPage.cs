using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Persistence;

namespace NeuroAccessMaui.UI.Pages.Contracts
{
	/// <summary>
	/// Interface for pages that can receive contract options from an asynchronous process.
	/// </summary>
	public interface IContractOptionsPage
	{
		/// <summary>
		/// Method called (from main thread) when contract options are made available.
		/// </summary>
		/// <param name="Options">Available options, as dictionaries with contract parameters.</param>
		Task ShowContractOptions(IDictionary<CaseInsensitiveString, object>[] Options);
	}
}
