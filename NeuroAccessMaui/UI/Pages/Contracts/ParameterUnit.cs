using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Contracts
{
	/// <summary>
	/// The unit or currency of a number in a contract, worked out from the number's name and the contract's other values.
	/// </summary>
	public sealed class ParameterUnit
	{
		/// <summary>
		/// Words in parameter names that mean an amount of money.
		/// </summary>
		private static readonly string[] amountWords =
			["Amount", "Price", "Cost", "Fee", "Total", "Sum", "Value", "Payment", "Deposit", "Commission", "Vat", "Tax", "Debt",
			"Balance", "Charge", "Shipping"];

		private readonly bool isPercent;
		private readonly Parameter? currencyParameter;

		/// <summary>
		/// Creates a unit that is either a percentage or the currency held by another parameter.
		/// </summary>
		/// <param name="IsPercent">If the number is a percentage.</param>
		/// <param name="CurrencyParameter">The parameter that holds the currency, if any.</param>
		private ParameterUnit(bool IsPercent, Parameter? CurrencyParameter)
		{
			this.isPercent = IsPercent;
			this.currencyParameter = CurrencyParameter;
		}

		/// <summary>
		/// The unit as it is shown after the number, such as "%" or "SEK", or an empty string when it isn't known yet.
		/// </summary>
		public string Text => this.isPercent ? "%" : this.currencyParameter?.StringValue ?? string.Empty;

		/// <summary>
		/// Works out the unit of a number. A number whose name or guide speaks of percent is a percentage. An amount, judged
		/// by its name, is in the currency of the contract's currency value: one named "Currency", or else one whose name ends
		/// with "Currency" or "Unit", such as "PriceUnit".
		/// </summary>
		/// <param name="Parameter">The number's parameter.</param>
		/// <param name="Contract">The contract the parameter belongs to.</param>
		/// <returns>The unit, or null when the number has none.</returns>
		public static ParameterUnit? Find(Parameter Parameter, Contract Contract)
		{
			string Name = Parameter.Name ?? string.Empty;
			string Guide = Parameter.Guide ?? string.Empty;

			if (Name.Contains("Percent", StringComparison.OrdinalIgnoreCase) || Guide.Contains('%'))
				return new ParameterUnit(true, null);

			if (!amountWords.Any(Word => Name.Contains(Word, StringComparison.OrdinalIgnoreCase)))
				return null;

			Parameter? Currency = FindCurrency(Contract);

			return Currency is null ? null : new ParameterUnit(false, Currency);
		}

		/// <summary>
		/// Finds the value that holds the contract's currency.
		/// </summary>
		/// <param name="Contract">The contract.</param>
		/// <returns>The parameter, or null when the contract has none.</returns>
		public static Parameter? FindCurrency(Contract Contract)
		{
			Parameter[] Texts = [.. (Contract.Parameters ?? []).Where(P => P is StringParameter or CalcParameter)];

			return Texts.FirstOrDefault(P => string.Equals(P.Name, "Currency", StringComparison.OrdinalIgnoreCase))
				?? Texts.FirstOrDefault(P => P.Name.EndsWith("Currency", StringComparison.OrdinalIgnoreCase))
				?? Texts.FirstOrDefault(P => P.Name.EndsWith("Unit", StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// If a parameter holds the contract's currency, so it adds nothing to show next to the amounts.
		/// </summary>
		/// <param name="Parameter">The parameter.</param>
		/// <param name="Contract">The contract.</param>
		/// <returns>If the parameter is the currency.</returns>
		public static bool IsCurrency(Parameter Parameter, Contract Contract)
		{
			return FindCurrency(Contract) == Parameter;
		}
	}
}
