using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.Extensions
{
	/// <summary>
	/// Extensions for the <see cref="Contract"/> class.
	/// </summary>
	public static class ContractExtensions
	{
		/// <summary>
		/// Returns the language to use when displaying a contract on the device
		/// </summary>
		/// <param name="Contract">Contract</param>
		/// <returns>Language to use when displaying contract.</returns>
		public static string DeviceLanguage(this Contract Contract)
		{
			string[] Languages = Contract.GetLanguages();
			string Language = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

			foreach (string Option in Languages)
			{
				if (string.Compare(Option.Before("-"), Language, StringComparison.OrdinalIgnoreCase) == 0)
					return Option;
			}

			return Contract.DefaultLanguage;
		}
	}
}
