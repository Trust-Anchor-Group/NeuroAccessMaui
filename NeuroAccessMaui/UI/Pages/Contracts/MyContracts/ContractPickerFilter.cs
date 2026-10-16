using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Contracts.MyContracts
{
	/// <summary>
	/// The rules a picked contract must meet, taken from a contract-reference parameter. Only contracts that would pass the
	/// parameter's validation are offered: the same machine-readable name and namespace, template and provider, signed and
	/// within their validity dates.
	/// </summary>
	public class ContractPickerFilter
	{
		/// <summary>
		/// The rules a picked contract must meet, taken from a contract-reference parameter.
		/// </summary>
		public ContractPickerFilter()
		{
		}

		/// <summary>
		/// The rules a picked contract must meet, taken from a contract-reference parameter.
		/// </summary>
		/// <param name="Parameter">The contract-reference parameter.</param>
		public ContractPickerFilter(ContractReferenceParameter Parameter)
		{
			this.LocalName = Parameter.LocalName;
			this.Namespace = Parameter.Namespace;
			this.TemplateId = Parameter.TemplateId;
			this.Provider = Parameter.Provider;
		}

		/// <summary>
		/// The required local name of the machine-readable part, if any.
		/// </summary>
		public string? LocalName { get; set; }

		/// <summary>
		/// The required namespace of the machine-readable part, if any.
		/// </summary>
		public string? Namespace { get; set; }

		/// <summary>
		/// The required template ID, if any.
		/// </summary>
		public string? TemplateId { get; set; }

		/// <summary>
		/// The required provider, if any.
		/// </summary>
		public string? Provider { get; set; }

		/// <summary>
		/// Checks if a contract meets the rules.
		/// </summary>
		/// <param name="Contract">The contract to check.</param>
		/// <param name="UtcNow">The current time, in UTC.</param>
		/// <returns>If the contract can be picked.</returns>
		public bool Matches(Contract Contract, DateTime UtcNow)
		{
			if (Contract.State != ContractState.Signed)
				return false;

			if (!IsRuleMet(this.LocalName, Contract.ForMachinesLocalName) ||
				!IsRuleMet(this.Namespace, Contract.ForMachinesNamespace) ||
				!IsRuleMet(this.TemplateId, Contract.TemplateId) ||
				!IsRuleMet(this.Provider, Contract.Provider))
			{
				return false;
			}

			if (Contract.From != DateTime.MinValue && ToUtc(Contract.From) > UtcNow)
				return false;

			if (Contract.To != DateTime.MaxValue && ToUtc(Contract.To) < UtcNow)
				return false;

			return true;
		}

		/// <summary>
		/// A rule is met when it is empty, or when the value is the same, as the server compares them.
		/// </summary>
		private static bool IsRuleMet(string? Rule, string? Value)
		{
			return string.IsNullOrEmpty(Rule) || string.Equals(Rule, Value, StringComparison.Ordinal);
		}

		private static DateTime ToUtc(DateTime Value)
		{
			return Value.Kind switch
			{
				DateTimeKind.Utc => Value,
				DateTimeKind.Local => Value.ToUniversalTime(),
				_ => DateTime.SpecifyKind(Value, DateTimeKind.Utc)
			};
		}
	}
}
