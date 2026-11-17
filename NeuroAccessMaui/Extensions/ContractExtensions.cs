using NeuroAccessMaui.UI.Controls.Markdown;
using Waher.Content.Markdown;
using Waher.Content.Markdown.Functions;
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
		/// Builds the contract's human-readable text, in the language closest to <paramref name="Language"/>, as a text
		/// model for a <see cref="MarkdownView"/>. Parameters without a value show their guide text as missing values.
		/// </summary>
		/// <param name="Contract">Contract reference.</param>
		/// <param name="Language">Desired language</param>
		/// <returns>Text model, or null if the contract has no human-readable text.</returns>
		public static async Task<MarkdownModel?> ToMarkdownModel(this Contract Contract, string Language)
		{
			HumanReadableText? Text = Contract.Select(Contract.ForHumans, Language);
			if (Text is null)
				return null;

			MarkdownDocument Doc = await Text.GenerateMarkdownDocument(Contract);
			return await MarkdownModel.FromDocumentAsync(Doc, MarkdownBuildOptions.Contract);
		}
	}
}
