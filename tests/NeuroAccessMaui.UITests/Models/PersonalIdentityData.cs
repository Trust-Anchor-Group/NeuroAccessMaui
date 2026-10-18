using System.Globalization;
using System.Text.RegularExpressions;

namespace NeuroAccessMaui.UITests.Models;

/// <summary>Defines deterministic US identity fixtures without deriving expectations from the app.</summary>
internal sealed record PersonalIdentityData
{
	internal required string FirstName { get; init; }
	internal required string MiddleName { get; init; }
	internal required string LastName { get; init; }
	internal required string PersonalNumber { get; init; }
	internal required DateOnly DateOfBirth { get; init; }
	internal string Nationality => "United States of America";
	internal required string Gender { get; init; }
	internal string ZipCode => "12345";
	internal string Address => "Testgatan 12";
	internal string Apartment => "1201";
	internal string Area => "Centrum";
	internal string City => "Stockholm";
	internal string Region => "Stockholm";

	internal static PersonalIdentityData FromSettings(TestSettings Settings)
	{
		(string Number, int Year) Seed = Settings.Get("PERSONAL_NUMBER_AGE_GROUP", "adult-2000s").ToLowerInvariant() switch
		{
			"extreme-old" => ("900010001", 1890),
			"older-adult" => ("900250002", 1925),
			"adult-2000s" => ("900000003", 2000),
			"infant-2026" => ("900260004", 2026),
			_ => throw new AssertFailedException("PERSONAL_NUMBER_AGE_GROUP must be extreme-old, older-adult, adult-2000s or infant-2026.")
		};
		string Number = Settings.Get("SOCIAL_SECURITY_NUMBER", Seed.Number);
		Assert.IsTrue(Regex.IsMatch(Number, @"\A[0-9]{9}\z"), $"SOCIAL_SECURITY_NUMBER must contain exactly nine digits, not '{Number}'.");
		int Selector = int.Parse(Number[^4..], CultureInfo.InvariantCulture);
		string[] FirstNames = ["Alice", "Erik", "Linnea", "Oscar", "Saga", "Viktor"];
		string[] MiddleNames = ["Maria", "Karl", "Elin", "Nils", "Anna", "Lars"];
		string[] LastNames = ["Andersson", "Berg", "Lindgren", "Nilsson", "Svensson", "Wallin"];
		string[] Genders = ["Male", "Female", "Other"];
		return new PersonalIdentityData
		{
			FirstName = FirstNames[Selector % FirstNames.Length],
			MiddleName = MiddleNames[(Selector / 3) % MiddleNames.Length],
			LastName = LastNames[(Selector / 7) % LastNames.Length],
			PersonalNumber = Number,
			DateOfBirth = new DateOnly(Seed.Year, 1, 1),
			Gender = Genders[Selector % Genders.Length]
		};
	}

	internal Dictionary<string, HashSet<string>> SummaryValues(bool IncludesMiddleName)
	{
		Dictionary<string, HashSet<string>> Expected = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
		{
			["FIRST"] = [this.FirstName], ["LAST"] = [this.LastName], ["PNR"] = [this.PersonalNumber],
			["NATIONALITY"] = [this.Nationality], ["GENDER"] = [this.Gender], ["ADDR"] = [this.Address],
			["ADDR2"] = [this.Apartment], ["AREA"] = [this.Area], ["CITY"] = [this.City],
			["ZIP"] = [this.ZipCode], ["REGION"] = [this.Region]
		};
		if (IncludesMiddleName)
			Expected["MIDDLE"] = [this.MiddleName];
		string[] Formats = ["dddd, MMMM d, yyyy", "dddd, d MMMM yyyy", "dddd, dd MMMM yyyy", "dddd d MMMM yyyy"];
		Expected["BDATE"] = Formats.Select(Format => this.DateOfBirth.ToString(Format, CultureInfo.GetCultureInfo("en")))
			.ToHashSet(StringComparer.Ordinal);
		return Expected;
	}
}
