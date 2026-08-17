using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.Services.Xmpp
{
	/// <summary>
	/// Represents the bounded foreground result of signing a contract.
	/// </summary>
	public sealed class ContractSigningResult
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ContractSigningResult"/> class.
		/// </summary>
		/// <param name="Outcome">The confirmed foreground outcome.</param>
		/// <param name="Contract">The best available contract state.</param>
		/// <param name="FailureCategory">The privacy-safe failure category.</param>
		public ContractSigningResult(
			ContractSigningOutcome Outcome,
			Contract? Contract,
			ContractSigningFailureCategory FailureCategory)
		{
			this.Outcome = Outcome;
			this.Contract = Contract;
			this.FailureCategory = FailureCategory;
		}

		/// <summary>
		/// Gets the confirmed foreground outcome.
		/// </summary>
		public ContractSigningOutcome Outcome { get; }

		/// <summary>
		/// Gets the best available contract state.
		/// </summary>
		public Contract? Contract { get; }

		/// <summary>
		/// Gets the privacy-safe failure category.
		/// </summary>
		public ContractSigningFailureCategory FailureCategory { get; }
	}
}
