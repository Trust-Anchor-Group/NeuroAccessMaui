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
				if (string.Equals(Option.Before("-"), Language, StringComparison.OrdinalIgnoreCase))
					return Option;
			}

			return Contract.DefaultLanguage;
		}

		/// <summary>
		/// Generates Maui XAML for a contract.
		/// </summary>
		/// <param name="Contract">Contract</param>
		/// <param name="Language">Language</param>
		/// <returns>Maui XAML</returns>
		public static async Task<string> ToMaui(this Contract Contract, string Language)
		{
			string Xaml = await Contract.ToXamarinForms(Language);

			return XamarinToMaui(Xaml);
		}

		/// <summary>
		/// Generates Maui XAML for a contract.
		/// </summary>
		/// <param name="Paramteter">Contract parameter.</param>
		/// <param name="Language">Language</param>
		/// <param name="Contract">Contract</param>
		/// <returns>Maui XAML</returns>
		public static async Task<string> ToMaui(this Parameter Paramteter, string Language, Contract Contract)
		{
			string Xaml = await Paramteter.ToXamarinForms(Language, Contract);

			return XamarinToMaui(Xaml);
		}

		/// <summary>
		/// Generates Maui XAML for a contract.
		/// </summary>
		/// <param name="Role">Contract role.</param>
		/// <param name="Language">Language</param>
		/// <param name="Contract">Contract</param>
		/// <returns>Maui XAML</returns>
		public static async Task<string> ToMaui(this Role Role, string Language, Contract Contract)
		{
			string Xaml = await Role.ToXamarinForms(Language, Contract);

			return XamarinToMaui(Xaml);
		}

		private static string XamarinToMaui(string Xaml)
		{
			int i, j, k;

			Xaml = Xaml.Replace("http://xamarin.com/schemas/2014/forms", "http://schemas.microsoft.com/dotnet/2021/maui");
			Xaml = Xaml.Replace("<StackLayout", "<VerticalStackLayout");
			Xaml = Xaml.Replace("</StackLayout>", "</VerticalStackLayout>");

			i = Xaml.IndexOf("<VerticalStackLayout", StringComparison.Ordinal);
			while (i >= 0)
			{
				j = Xaml.IndexOf('>', i);
				if (j < i)
					break;

				k = Xaml.IndexOf(" Orientation=\"Vertical\"", i, StringComparison.Ordinal);

				if (k > i && k < j)
				{
					Xaml = Xaml.Remove(k, 23);
					j -= 23;
				}

				i = Xaml.IndexOf("<VerticalStackLayout", j, StringComparison.Ordinal);
			}

			return Xaml;
		}
	}
}
