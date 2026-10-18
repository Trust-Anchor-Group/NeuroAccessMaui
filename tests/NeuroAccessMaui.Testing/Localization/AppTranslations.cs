using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;

namespace NeuroAccessMaui.Testing.Localization;

/// <summary>Resolves the application's real resources for an explicitly requested test culture.</summary>
internal sealed class AppTranslations
{
	internal static readonly IReadOnlyList<string> SupportedCodes = Array.AsReadOnly(new[]
	{
		"en", "sv", "es", "fr", "de", "da", "no", "fi", "sr", "pt", "ro", "ru"
	});
	private static readonly ResourceManager resources = new ResourceManager(
		"NeuroAccessMaui.Testing.Localization.AppResources", typeof(AppTranslations).Assembly);
	private readonly CultureInfo culture;

	internal AppTranslations(string Code)
	{
		Assert.IsTrue(SupportedCodes.Contains(Code), $"Unsupported test culture {Code}.");
		this.Code = Code;
		this.culture = CultureInfo.GetCultureInfo(Code);
	}

	internal string Code { get; }
	internal string this[string Key] => resources.GetString(Key, this.culture)
		?? throw new InvalidDataException($"Missing application resource key {Key}.");

	internal bool MatchesNumber(string Key, string Value)
	{
		string[] Parts = this[Key].Split("{0}");
		Assert.IsTrue(Parts.Length == 2, "Numeric message must have exactly one placeholder.");
		return Regex.IsMatch(Value, "\\A" + Regex.Escape(Parts[0]) + "[0-9]+" + Regex.Escape(Parts[1]) + "\\z");
	}
}
