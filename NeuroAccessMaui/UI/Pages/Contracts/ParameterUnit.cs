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
		private static readonly string[] amountWords = ["Amount", "Price", "Cost", "Fee", "Total", "Sum", "Value", "Payment", "Deposit", "Commission"];

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
		/// by its name, is in the currency of a parameter named "Currency", when the contract has one.
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

			Parameter? Currency = Contract.Parameters?.FirstOrDefault(P =>
				P is StringParameter && string.Equals(P.Name, "Currency", StringComparison.OrdinalIgnoreCase));

			return Currency is null ? null : new ParameterUnit(false, Currency);
		}
	}
}
