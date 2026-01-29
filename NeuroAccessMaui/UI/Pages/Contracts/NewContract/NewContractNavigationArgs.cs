using NeuroAccessMaui.Services.UI;
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
	/// <param name="TemplateContractId">The contract identifier to load as template if the template instance is not provided.</param>
	/// <param name="Purpose">The purpose to state if access to the template requires a petition.</param>
	public class NewContractNavigationArgs(Contract? Template, bool SetVisibility, Dictionary<CaseInsensitiveString, object>? ParameterValues, string? TemplateContractId = null, string? Purpose = null) : NavigationArgs
	{
		private List<CaseInsensitiveString>? suppressProposals = null;

		/// <summary>
		/// Creates an instance of the <see cref="NewContractNavigationArgs"/> class.
		/// </summary>
		public NewContractNavigationArgs()
			: this(null, false, null, null, null)
		{
		}

		/// <summary>
		/// Creates an instance of the <see cref="NewContractNavigationArgs"/> class.
		/// </summary>
		/// <param name="ParameterValues">Parameter values to set in new contract.</param>
		public NewContractNavigationArgs(Dictionary<CaseInsensitiveString, object>? ParameterValues)
			: this(null, false, ParameterValues, null, null)
		{
		}

		/// <summary>
		/// Creates an instance of the <see cref="NewContractNavigationArgs"/> class.
		/// </summary>
		/// <param name="Template">The contract to use as template.</param>
		/// <param name="ParameterValues">Parameter values to set in new contract.</param>
		public NewContractNavigationArgs(Contract? Template, Dictionary<CaseInsensitiveString, object>? ParameterValues)
			: this(Template, false, ParameterValues, Template?.ContractId, null)
		{
		}

		/// <summary>
		/// Creates an instance of the <see cref="NewContractNavigationArgs"/> class.
		/// </summary>
		/// <param name="TemplateContractId">The contract identifier to load as template.</param>
		/// <param name="ParameterValues">Parameter values to set in new contract.</param>
		/// <param name="Purpose">The purpose to state if access to the template requires a petition.</param>
		public NewContractNavigationArgs(string TemplateContractId, Dictionary<CaseInsensitiveString, object>? ParameterValues, string? Purpose = null)
			: this(null, false, ParameterValues, TemplateContractId, Purpose)
		{
		}

		/// <summary>
		/// The contract to use as template.
		/// </summary>
		public Contract? Template { get; } = Template;

		/// <summary>
		/// The contract identifier to load as template if the template instance is not provided.
		/// </summary>
		public string? TemplateContractId { get; } = TemplateContractId ?? Template?.ContractId;

		/// <summary>
		/// The purpose to state if access to the template requires a petition.
		/// </summary>
		public string? Purpose { get; } = Purpose;

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
