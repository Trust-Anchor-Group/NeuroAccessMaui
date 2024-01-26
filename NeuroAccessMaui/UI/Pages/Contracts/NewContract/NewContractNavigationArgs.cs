using NeuroAccessMaui.Services.Navigation;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;

namespace NeuroAccessMaui.UI.Pages.Contracts.NewContract
{
	/// <summary>
	/// Holds navigation parameters specific to views displaying a new contract.
	/// </summary>
	/// <param name="Template">The contract to use as template.</param>
	/// <param name="SetVisibility">If visibility should be set by default.</param>
	/// <param name="ParameterValues">Parameter values to set in new contract.</param>
	public class NewContractNavigationArgs(Contract? Template, bool SetVisibility, Dictionary<CaseInsensitiveString, object>? ParameterValues) : NavigationArgs
	{
		private List<CaseInsensitiveString>? suppressProposals = null;

		/// <summary>
		/// Creates an instance of the <see cref="NewContractNavigationArgs"/> class.
		/// </summary>
		public NewContractNavigationArgs()
			: this(null, false, null)
		{
		}

		/// <summary>
		/// Creates an instance of the <see cref="NewContractNavigationArgs"/> class.
		/// </summary>
		/// <param name="ParameterValues">Parameter values to set in new contract.</param>
		public NewContractNavigationArgs(Dictionary<CaseInsensitiveString, object> ParameterValues)
			: this(null, false, ParameterValues)
		{
		}

		/// <summary>
		/// Creates an instance of the <see cref="NewContractNavigationArgs"/> class.
		/// </summary>
		/// <param name="Template">The contract to use as template.</param>
		/// <param name="ParameterValues">Parameter values to set in new contract.</param>
		public NewContractNavigationArgs(Contract? Template, Dictionary<CaseInsensitiveString, object>? ParameterValues)
			: this(Template, false, ParameterValues)
		{
		}

		/// <summary>
		/// The contract to use as template.
		/// </summary>
		public Contract? Template { get; } = Template;

		/// <summary>
		/// If visibility should be set by default.
		/// </summary>
		public bool SetVisibility { get; } = SetVisibility;

		/// <summary>
		/// Parameter values to set in new contract.
		/// </summary>
		public Dictionary<CaseInsensitiveString, object>? ParameterValues { get; } = ParameterValues;

		/// <summary>
		/// Any legal IDs to whom proposals should not be sent. May be null if no proposals should be suppressed.
		/// </summary>
		public CaseInsensitiveString[]? SuppressedProposalLegalIds
		{
			get => this.suppressProposals?.ToArray();
		}

		/// <summary>
		/// Suppresses a proposal for a given Legal ID.
		/// </summary>
		/// <param name="LegalId">Legal ID to whom a proposal should not be sent.</param>
		public void SuppressProposal(CaseInsensitiveString LegalId)
		{
			this.suppressProposals ??= [];
			this.suppressProposals.Add(LegalId);
		}
	}
}
