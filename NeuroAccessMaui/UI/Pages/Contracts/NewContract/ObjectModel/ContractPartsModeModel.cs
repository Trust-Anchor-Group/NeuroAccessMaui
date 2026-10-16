using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Contracts.NewContract.ObjectModel
{
	/// <summary>
	/// A choice of who can sign a new contract: only the people named in it, or anyone who can see it.
	/// </summary>
	public class ContractPartsModeModel
	{
		/// <summary>
		/// Creates an instance of the <see cref="ContractPartsModeModel"/> class.
		/// </summary>
		/// <param name="PartsMode">The parts mode.</param>
		/// <param name="Name">The localized name of the choice.</param>
		public ContractPartsModeModel(ContractParts PartsMode, string Name)
		{
			this.PartsMode = PartsMode;
			this.Name = Name;
		}

		/// <summary>
		/// The parts mode.
		/// </summary>
		public ContractParts PartsMode { get; }

		/// <summary>
		/// The localized name of the choice.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Returns the name of the choice.
		/// </summary>
		/// <returns>The name.</returns>
		public override string ToString()
		{
			return this.Name;
		}
	}
}
