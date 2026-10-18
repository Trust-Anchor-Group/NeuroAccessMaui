using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace NeuroAccessMaui.UnitTests.Localization;

/// <summary>Validates production resource structure, formatting and the English fallback policy.</summary>
[TestClass, TestCategory("Unit")]
public sealed class LocalizationResourceTests
{
	/// <summary>Gets or sets the context used to report untranslated resource counts.</summary>
	public TestContext TestContext { get; set; } = null!;

	/// <summary>Gets every culture present in the linked production resource files.</summary>
	public static IEnumerable<object[]> ResourceCultures => Directory.GetFiles(
		Path.Combine(AppContext.BaseDirectory, "LocalizationSources"), "AppResources*.resx")
		.OrderBy(PathName => PathName, StringComparer.Ordinal)
		.Select(PathName => new object[] { Path.GetFileNameWithoutExtension(PathName) is "AppResources"
			? "en" : Path.GetFileNameWithoutExtension(PathName)["AppResources.".Length..] });

	/// <summary>Checks actual keys, values, format arguments and resource-manager fallback for each culture.</summary>
	/// <param name="Code">The production resource culture to validate.</param>
	[TestMethod, DynamicData(nameof(ResourceCultures))]
	public void Test_01_ValidateResourcesAndEnglishFallback(string Code)
	{
		Dictionary<string, string> Neutral = ReadResources("en");
		Dictionary<string, string> Localized = ReadResources(Code);
		AppTranslations Translations = new AppTranslations(Code);
		foreach (KeyValuePair<string, string> Entry in Localized)
		{
			Assert.IsTrue(Neutral.ContainsKey(Entry.Key), $"{Code}: unknown resource key {Entry.Key}.");
			CollectionAssert.AreEqual(PlaceholderIndices(Neutral[Entry.Key], "en", Entry.Key),
				PlaceholderIndices(Entry.Value, Code, Entry.Key), $"{Code}: incompatible placeholders in {Entry.Key}.");
		}
		foreach (KeyValuePair<string, string> Entry in Neutral)
		{
			string Expected = Localized.GetValueOrDefault(Entry.Key, Entry.Value);
			// XML normalizes newlines, while MSBuild's resource reader preserves the checkout's line endings.
			Assert.AreEqual(Expected.ReplaceLineEndings("\n"), Translations[Entry.Key].ReplaceLineEndings("\n"),
				$"{Code}: incorrect resource resolution for {Entry.Key}.");
		}
		foreach (string Key in new[] { "ActivateYourDigitalIdentity", "SelectForMe" })
			Assert.IsTrue(Localized.ContainsKey(Key), $"{Code}: language-switch probe {Key} must be translated locally.");
		foreach (string Key in new[] { "ResendCodeSeconds", "PasswordIsInvalid" })
		{
			string Message = string.Format(CultureInfo.InvariantCulture, Translations[Key], 12);
			Assert.IsTrue(Translations.MatchesNumber(Key, Message), $"{Code}: {Key} numeric state cannot be matched.");
			Assert.IsFalse(Translations.MatchesNumber(Key, "prefix" + Message));
			Assert.IsFalse(Translations.MatchesNumber(Key, Message + "\n"));
			Assert.IsFalse(Translations.MatchesNumber(Key, string.Format(CultureInfo.InvariantCulture, Translations[Key], "unexpected")));
		}
		this.TestContext.WriteLine($"{Code}: {Localized.Count} local keys; {Neutral.Count - Localized.Count} keys use neutral English fallback.");
	}

	/// <summary>Requires coverage for every resource culture and distinguishable language-switch probes.</summary>
	[TestMethod]
	public void Test_02_CoverAllResourceCulturesWithDistinctLanguageProbes()
	{
		CollectionAssert.AreEquivalent(AppTranslations.SupportedCodes.ToArray(), ResourceCultures.Select(Row => (string)Row[0]).ToArray());
		HashSet<(string Title, string Button)> Probes = new HashSet<(string Title, string Button)>();
		foreach (string Code in AppTranslations.SupportedCodes)
		{
			AppTranslations Translations = new AppTranslations(Code);
			Assert.IsTrue(Probes.Add((Translations["ActivateYourDigitalIdentity"], Translations["SelectForMe"])),
				$"{Code}: the language-switch probes cannot distinguish this culture from another.");
		}
	}

	/// <summary>Keeps UI expectations independent of the test process's current culture.</summary>
	[TestMethod]
	public void Test_03_ResolveExplicitCultureRegardlessOfAmbientCulture()
	{
		CultureInfo Original = CultureInfo.CurrentUICulture;
		try
		{
			CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr");
			Assert.AreEqual(ReadResources("sv")["SelectForMe"], new AppTranslations("sv")["SelectForMe"]);
			Assert.AreEqual(ReadResources("en")["SelectForMe"], new AppTranslations("en")["SelectForMe"]);
		}
		finally
		{
			CultureInfo.CurrentUICulture = Original;
		}
	}

	/// <summary>Fails unsupported cultures and unknown resource keys instead of silently accepting them.</summary>
	[TestMethod]
	public void Test_04_RejectUnsupportedCultureAndMissingKey()
	{
		Assert.ThrowsExactly<AssertFailedException>(() => new AppTranslations("unsupported"));
		Assert.ThrowsExactly<InvalidDataException>(() => new AppTranslations("en")["__missing_test_key__"]);
	}

	private static Dictionary<string, string> ReadResources(string Code)
	{
		string Name = Code == "en" ? "AppResources.resx" : $"AppResources.{Code}.resx";
		XDocument Document = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "LocalizationSources", Name));
		Dictionary<string, string> Values = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (XElement Entry in Document.Root!.Elements("data"))
		{
			string? Key = (string?)Entry.Attribute("name");
			string? Value = Entry.Element("value")?.Value;
			Assert.IsFalse(string.IsNullOrWhiteSpace(Key), $"{Name}: blank resource key.");
			Assert.IsFalse(string.IsNullOrWhiteSpace(Value), $"{Name}: empty value for {Key}.");
			Assert.IsTrue(Values.TryAdd(Key, Value), $"{Name}: duplicate resource key {Key}.");
		}
		Assert.IsNotEmpty(Values, $"{Name}: resource file is empty.");
		return Values;
	}

	private static int[] PlaceholderIndices(string Value, string Code, string Key)
	{
		try
		{
			CompositeFormat.Parse(Value);
		}
		catch (FormatException)
		{
			Assert.Fail($"{Code}: invalid composite format in {Key}.");
		}
		// Consume escaped braces before looking for argument indices; reordering arguments is allowed.
		return Regex.Matches(Value, @"\{\{|\}\}|\{(?<index>[0-9]+)")
			.Where(Match => Match.Groups["index"].Success)
			.Select(Match => int.Parse(Match.Groups["index"].Value, CultureInfo.InvariantCulture))
			.Distinct().Order().ToArray();
	}
}
