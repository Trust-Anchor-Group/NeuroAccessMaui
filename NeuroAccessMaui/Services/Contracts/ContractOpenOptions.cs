using Waher.Persistence;

namespace NeuroAccessMaui.Services.Contracts
{
	/// <summary>
	/// Options for <see cref="IContractOrchestratorService.OpenContractAsync(CaseInsensitiveString, ContractOpenOptions?)"/>.
	/// </summary>
	public class ContractOpenOptions
	{
		/// <summary>
		/// Parameter values to set when a template opens in the new-contract wizard.
		/// </summary>
		public Dictionary<CaseInsensitiveString, object>? ParameterValues { get; set; }

		/// <summary>
		/// If the contract page is shown without actions.
		/// </summary>
		public bool IsReadOnly { get; set; }

		/// <summary>
		/// The role proposed in an invitation, if the contract is opened from one.
		/// </summary>
		public string? InvitedRole { get; set; }

		/// <summary>
		/// Bare JID of the sender of an invitation, if the contract is opened from one.
		/// </summary>
		public string? InvitedBy { get; set; }

		/// <summary>
		/// The message sent with an invitation, if the contract is opened from one.
		/// </summary>
		public string? InvitationMessage { get; set; }
	}
}
