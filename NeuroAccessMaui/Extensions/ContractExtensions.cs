using NeuroAccessMaui.UI.Rendering;
using Waher.Content.Markdown;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Contracts.HumanReadable;

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
		/// Creates a human-readable XAML document for the contract.
		/// </summary>
		/// <param name="Contract">Contract reference.</param>
		/// <param name="Language">Desired language</param>
		/// <returns>Markdown</returns>
		public static Task<string> ToXamarinForms(this Contract Contract, string Language)
		{
			return Contract.ToXamarinForms(Contract.ForHumans, Language);
		}

		/// <summary>
		/// Selects a human-readable text, and generates a XAML document from it.
		/// </summary>
		/// <param name="Contract">Contract reference.</param>
		/// <param name="Text">Collection of texts in different languages.</param>
		/// <param name="Language">Language</param>
		/// <returns>XAML document.</returns>
		public static Task<string> ToXamarinForms(this Contract Contract, HumanReadableText[] Text, string Language)
		{
			return Contract.Select(Text, Language)?.ToXamarinForms(Contract) ?? Task.FromResult<string>(string.Empty);
		}

		/// <summary>
		/// Generates XAML for the human-readable text.
		/// </summary>
		/// <param name="Text">Human-readable text being rendered.</param>
		/// <param name="Contract">Contract, of which the human-readable text is part.</param>
		/// <returns>XAML</returns>
		public static async Task<string> ToXamarinForms(this HumanReadableText Text, Contract Contract)
		{
			MarkdownDocument Doc = await Text.GenerateMarkdownDocument(Contract);
			return await Doc.GenerateXamarinForms();
		}

		/// <summary>
		/// Creates a human-readable XAML document for the contract.
		/// </summary>
		/// <param name="Description">Description being rendered.</param>
		/// <param name="Language">Desired language</param>
		/// <param name="Contract">Contract hosting the object.</param>
		/// <returns>Markdown</returns>
		public static Task<string> ToXamarinForms(this LocalizableDescription Description, string Language, Contract Contract)
		{
			return Contract.ToXamarinForms(Description.Descriptions, Language);
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
