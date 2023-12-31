﻿using System.Diagnostics.CodeAnalysis;
using Waher.Content.Emoji;

namespace NeuroAccessMaui.Services.Data
{
	/// <summary>
	/// Representation of an ISO3166-1 Country
	/// </summary>
	public class ISO3166Country(string Name, string Alpha2, string Alpha3,
		int NumericCode, string DialCode, EmojiInfo? EmojiInfo = null)
	{
		public string Name { get; private set; } = Name;
		public string Alpha2 { get; private set; } = Alpha2;
		public string Alpha3 { get; private set; } = Alpha3;
		public int NumericCode { get; private set; } = NumericCode;
		public string DialCode { get; private set; } = DialCode;
		public EmojiInfo EmojiInfo { get; set; } = EmojiInfo ?? EmojiUtilities.Emoji_waving_white_flag;
	}

	/// <summary>
	/// Conversion between Country Names and ISO-3166-1 country codes.
	/// </summary>
	public static class ISO_3166_1
	{
		private static readonly ISO3166Country defaultCountry = new("United States of America", "US", "USA", 840, "1", EmojiUtilities.Emoji_flag_us);
		private static readonly SortedDictionary<string, ISO3166Country> code2ByCountry = new(StringComparer.InvariantCultureIgnoreCase);
		private static readonly SortedDictionary<string, ISO3166Country> countryByCode2 = new(StringComparer.InvariantCultureIgnoreCase);
		private static readonly SortedDictionary<string, ISO3166Country> countryByPhone = new(StringComparer.InvariantCultureIgnoreCase);

		static ISO_3166_1()
		{
			foreach (ISO3166Country Country in Countries)
			{
				if (EmojiUtilities.TryGetEmoji($"flag-{Country.Alpha2}".ToLowerInvariant(), out EmojiInfo EmojiInfo))
				{
					Country.EmojiInfo = EmojiInfo;
				}

				code2ByCountry[Country.Name] = Country;
				countryByCode2[Country.Alpha2] = Country;
				countryByPhone[Country.DialCode] = Country;
			}
		}

		/// <summary>
		/// This collection built from Wikipedia entry on ISO3166-1 on 9th Feb 2016
		/// </summary>
		public static ISO3166Country DefaultCountry => defaultCountry;

		/// <summary>
		/// This collection built from Wikipedia entry on ISO3166-1 on 9th Feb 2016
		/// </summary>
		public static List<ISO3166Country> Countries => countries;

		/// <summary>
		/// Tries to get the country, given its country and phone codes.
		/// </summary>
		/// <param name="PhoneNumber">ISO-3166-1 phone number (case insensitive).</param>
		/// <param name="Country">ISO-3166-1 Country, if found.</param>
		/// <returns>If a country was found matching the country code.</returns>
		public static bool TryGetCountryByPhone(string PhoneNumber, [NotNullWhen(true)] out ISO3166Country? Country)
		{
			ISO3166Country? Result = null;

			foreach (ISO3166Country Item in Countries)
			{
				if (PhoneNumber.StartsWith($"+{Item.DialCode}", StringComparison.OrdinalIgnoreCase))
				{
					if ((Result is null) ||
						(Result.DialCode.Length < Item.DialCode.Length))
					{
						Result = Item;
					}
				}
			}

			Country = Result;
			return Country is not null;
		}

		/// <summary>
		/// Tries to get the country, given its country code.
		/// </summary>
		/// <param name="CountryCode">ISO-3166-1 Country code (case insensitive).</param>
		/// <param name="Country">ISO-3166-1 Country, if found.</param>
		/// <returns>If a country was found matching the country code.</returns>
		public static bool TryGetCountryByCode(string? CountryCode, [NotNullWhen(true)] out ISO3166Country? Country)
		{
			if (string.IsNullOrEmpty(CountryCode))
			{
				Country = null;
				return false;
			}
			else
				return countryByCode2.TryGetValue(CountryCode, out Country);
		}

		/// <summary>
		/// Tries to get the ISO-3166-1 country code, given its country name.
		/// </summary>
		/// <param name="CountryName">Country name (case insensitive).</param>
		/// <param name="Country">ISO-3166-1 Country, if found.</param>
		/// <returns>If a country code was found matching the country name.</returns>
		public static bool TryGetCountryByName(string CountryName, [NotNullWhen(true)] out ISO3166Country? Country)
		{
			return code2ByCountry.TryGetValue(CountryName, out Country);
		}

		/// <summary>
		/// Converts the code to a country name (if found). If not found, returns the original code.
		/// </summary>
		/// <param name="CountryCode">Country code.</param>
		/// <returns>Country name, or if not found, the original code.</returns>
		public static string? ToName(string? CountryCode)
		{
			if (CountryCode is null)
				return null;
			else if (TryGetCountryByCode(CountryCode, out ISO3166Country? Country))
				return Country.Name;
			else
				return CountryCode;
		}

		/// <summary>
		/// Converts the name to a country code (if found). If not found, returns the original name.
		/// </summary>
		/// <param name="CountryName">Country name.</param>
		/// <returns>Country code, or if not found, the original name.</returns>
		public static string? ToCode(string? CountryName)
		{
			if (CountryName is null)
				return null;
			else if (TryGetCountryByName(CountryName, out ISO3166Country? Country))
				return Country.Alpha2;
			else
				return CountryName;
		}

		#region Build Collection
		private static readonly List<ISO3166Country> countries = [
			new ISO3166Country("Afghanistan", "AF", "AFG", 4, "93"),
			new ISO3166Country("Åland Islands", "AX", "ALA", 248, "358"),
			new ISO3166Country("Albania", "AL", "ALB", 8, "355"),
			new ISO3166Country("Algeria", "DZ", "DZA", 12, "213"),
			new ISO3166Country("American Samoa", "AS", "ASM", 16, "1"),
			new ISO3166Country("Andorra", "AD", "AND", 20, "376"),
			new ISO3166Country("Angola", "AO", "AGO", 24, "244"),
			new ISO3166Country("Anguilla", "AI", "AIA", 660, "1"),
			new ISO3166Country("Antarctica", "AQ", "ATA", 10, "672"),
			new ISO3166Country("Antigua and Barbuda", "AG", "ATG", 28, "1"),
			new ISO3166Country("Argentina", "AR", "ARG", 32, "54"),
			new ISO3166Country("Armenia", "AM", "ARM", 51, "374"),
			new ISO3166Country("Aruba", "AW", "ABW", 533, "297"),
			new ISO3166Country("Australia", "AU", "AUS", 36, "61"),
			new ISO3166Country("Austria", "AT", "AUT", 40, "43"),
			new ISO3166Country("Azerbaijan", "AZ", "AZE", 31, "994"),
			new ISO3166Country("Bahamas", "BS", "BHS", 44, "1"),
			new ISO3166Country("Bahrain", "BH", "BHR", 48, "973"),
			new ISO3166Country("Bangladesh", "BD", "BGD", 50, "880"),
			new ISO3166Country("Barbados", "BB", "BRB", 52, "1"),
			new ISO3166Country("Belarus", "BY", "BLR", 112, "375"),
			new ISO3166Country("Belgium", "BE", "BEL", 56, "32"),
			new ISO3166Country("Belize", "BZ", "BLZ", 84, "501"),
			new ISO3166Country("Benin", "BJ", "BEN", 204, "229"),
			new ISO3166Country("Bermuda", "BM", "BMU", 60, "1"),
			new ISO3166Country("Bhutan", "BT", "BTN", 64, "975"),
			new ISO3166Country("Bolivia (Plurinational State of)", "BO", "BOL", 68, "591"),
			new ISO3166Country("Bonaire, Sint Eustatius and Saba", "BQ", "BES", 535, "599"),
			new ISO3166Country("Bosnia and Herzegovina", "BA", "BIH", 70, "387"),
			new ISO3166Country("Botswana", "BW", "BWA", 72, "267"),
			new ISO3166Country("Brazil", "BR", "BRA", 76, "55"),
			new ISO3166Country("British Indian Ocean Territory", "IO", "IOT", 86, "246"),
			new ISO3166Country("Brunei Darussalam", "BN", "BRN", 96, "673"),
			new ISO3166Country("Bulgaria", "BG", "BGR", 100, "359"),
			new ISO3166Country("Burkina Faso", "BF", "BFA", 854, "226"),
			new ISO3166Country("Burundi", "BI", "BDI", 108, "257"),
			new ISO3166Country("Cabo Verde", "CV", "CPV", 132, "238"),
			new ISO3166Country("Cambodia", "KH", "KHM", 116, "855"),
			new ISO3166Country("Cameroon", "CM", "CMR", 120, "237"),
			new ISO3166Country("Canada", "CA", "CAN", 124, "1"),
			new ISO3166Country("Cayman Islands", "KY", "CYM", 136, "1"),
			new ISO3166Country("Central African Republic", "CF", "CAF", 140, "236"),
			new ISO3166Country("Chad", "TD", "TCD", 148, "235"),
			new ISO3166Country("Chile", "CL", "CHL", 152, "56"),
			new ISO3166Country("China", "CN", "CHN", 156, "86"),
			new ISO3166Country("Christmas Island", "CX", "CXR", 162, "61"),
			new ISO3166Country("Cocos (Keeling) Islands", "CC", "CCK", 166, "61"),
			new ISO3166Country("Colombia", "CO", "COL", 170, "57"),
			new ISO3166Country("Comoros", "KM", "COM", 174, "269"),
			new ISO3166Country("Congo", "CG", "COG", 178, "242"),
			new ISO3166Country("Congo (Democratic Republic of the)", "CD", "COD", 180, "243"),
			new ISO3166Country("Cook Islands", "CK", "COK", 184, "682"),
			new ISO3166Country("Costa Rica", "CR", "CRI", 188, "506"),
			new ISO3166Country("Côte d'Ivoire", "CI", "CIV", 384, "225"),
			new ISO3166Country("Croatia", "HR", "HRV", 191, "385"),
			new ISO3166Country("Cuba", "CU", "CUB", 192, "53"),
			new ISO3166Country("Curaçao", "CW", "CUW", 531, "599"),
			new ISO3166Country("Cyprus", "CY", "CYP", 196, "357"),
			new ISO3166Country("Czech Republic", "CZ", "CZE", 203, "420"),
			new ISO3166Country("Denmark", "DK", "DNK", 208, "45"),
			new ISO3166Country("Djibouti", "DJ", "DJI", 262, "253"),
			new ISO3166Country("Dominica", "DM", "DMA", 212, "1"),
			new ISO3166Country("Dominican Republic", "DO", "DOM", 214, "1"),
			new ISO3166Country("Ecuador", "EC", "ECU", 218, "593"),
			new ISO3166Country("Egypt", "EG", "EGY", 818, "20"),
			new ISO3166Country("El Salvador", "SV", "SLV", 222, "503"),
			new ISO3166Country("Equatorial Guinea", "GQ", "GNQ", 226, "240"),
			new ISO3166Country("Eritrea", "ER", "ERI", 232, "291"),
			new ISO3166Country("Estonia", "EE", "EST", 233, "372"),
			new ISO3166Country("Ethiopia", "ET", "ETH", 231, "251"),
			new ISO3166Country("Falkland Islands (Malvinas)", "FK", "FLK", 238, "500"),
			new ISO3166Country("Faroe Islands", "FO", "FRO", 234, "298"),
			new ISO3166Country("Fiji", "FJ", "FJI", 242, "679"),
			new ISO3166Country("Finland", "FI", "FIN", 246, "358"),
			new ISO3166Country("France", "FR", "FRA", 250, "33"),
			new ISO3166Country("French Guiana", "GF", "GUF", 254, "594"),
			new ISO3166Country("French Polynesia", "PF", "PYF", 258, "689"),
			new ISO3166Country("French Southern Territories", "TF", "ATF", 260, "262"),
			new ISO3166Country("Gabon", "GA", "GAB", 266, "241"),
			new ISO3166Country("Gambia", "GM", "GMB", 270, "220"),
			new ISO3166Country("Georgia", "GE", "GEO", 268, "995"),
			new ISO3166Country("Germany", "DE", "DEU", 276, "49"),
			new ISO3166Country("Ghana", "GH", "GHA", 288, "233"),
			new ISO3166Country("Gibraltar", "GI", "GIB", 292, "350"),
			new ISO3166Country("Greece", "GR", "GRC", 300, "30"),
			new ISO3166Country("Greenland", "GL", "GRL", 304, "299"),
			new ISO3166Country("Grenada", "GD", "GRD", 308, "1"),
			new ISO3166Country("Guadeloupe", "GP", "GLP", 312, "590"),
			new ISO3166Country("Guam", "GU", "GUM", 316, "1"),
			new ISO3166Country("Guatemala", "GT", "GTM", 320, "502"),
			new ISO3166Country("Guernsey", "GG", "GGY", 831, "44"),
			new ISO3166Country("Guinea", "GN", "GIN", 324, "224"),
			new ISO3166Country("Guinea-Bissau", "GW", "GNB", 624, "245"),
			new ISO3166Country("Guyana", "GY", "GUY", 328, "592"),
			new ISO3166Country("Haiti", "HT", "HTI", 332, "509"),
			new ISO3166Country("Holy See", "VA", "VAT", 336, "379"),
			new ISO3166Country("Honduras", "HN", "HND", 340, "504"),
			new ISO3166Country("Hong Kong", "HK", "HKG", 344, "852"),
			new ISO3166Country("Hungary", "HU", "HUN", 348, "36"),
			new ISO3166Country("Iceland", "IS", "ISL", 352, "354"),
			new ISO3166Country("India", "IN", "IND", 356, "91"),
			new ISO3166Country("Indonesia", "ID", "IDN", 360, "62"),
			new ISO3166Country("Iran (Islamic Republic of)", "IR", "IRN", 364, "98"),
			new ISO3166Country("Iraq", "IQ", "IRQ", 368, "964"),
			new ISO3166Country("Ireland", "IE", "IRL", 372, "353"),
			new ISO3166Country("Isle of Man", "IM", "IMN", 833, "44"),
			new ISO3166Country("Israel", "IL", "ISR", 376, "972"),
			new ISO3166Country("Italy", "IT", "ITA", 380, "39"),
			new ISO3166Country("Jamaica", "JM", "JAM", 388, "1"),
			new ISO3166Country("Japan", "JP", "JPN", 392, "81"),
			new ISO3166Country("Jersey", "JE", "JEY", 832, "44"),
			new ISO3166Country("Jordan", "JO", "JOR", 400, "962"),
			new ISO3166Country("Kazakhstan", "KZ", "KAZ", 398, "7"),
			new ISO3166Country("Kenya", "KE", "KEN", 404, "254"),
			new ISO3166Country("Kiribati", "KI", "KIR", 296, "686"),
			new ISO3166Country("Korea (Democratic People's Republic of)", "KP", "PRK", 408, "850"),
			new ISO3166Country("Korea (Republic of)", "KR", "KOR", 410, "82"),
			new ISO3166Country("Kuwait", "KW", "KWT", 414, "965"),
			new ISO3166Country("Kyrgyzstan", "KG", "KGZ", 417, "996"),
			new ISO3166Country("Lao People's Democratic Republic", "LA", "LAO", 418, "856"),
			new ISO3166Country("Latvia", "LV", "LVA", 428, "371"),
			new ISO3166Country("Lebanon", "LB", "LBN", 422, "961"),
			new ISO3166Country("Lesotho", "LS", "LSO", 426, "266"),
			new ISO3166Country("Liberia", "LR", "LBR", 430, "231"),
			new ISO3166Country("Libya", "LY", "LBY", 434, "218"),
			new ISO3166Country("Liechtenstein", "LI", "LIE", 438, "423"),
			new ISO3166Country("Lithuania", "LT", "LTU", 440, "370"),
			new ISO3166Country("Luxembourg", "LU", "LUX", 442, "352"),
			new ISO3166Country("Macao", "MO", "MAC", 446, "853"),
			new ISO3166Country("Macedonia (the former Yugoslav Republic of)", "MK", "MKD", 807, "389"),
			new ISO3166Country("Madagascar", "MG", "MDG", 450, "261"),
			new ISO3166Country("Malawi", "MW", "MWI", 454, "265"),
			new ISO3166Country("Malaysia", "MY", "MYS", 458, "60"),
			new ISO3166Country("Maldives", "MV", "MDV", 462, "960"),
			new ISO3166Country("Mali", "ML", "MLI", 466, "223"),
			new ISO3166Country("Malta", "MT", "MLT", 470, "356"),
			new ISO3166Country("Marshall Islands", "MH", "MHL", 584, "692"),
			new ISO3166Country("Martinique", "MQ", "MTQ", 474, "596"),
			new ISO3166Country("Mauritania", "MR", "MRT", 478, "222"),
			new ISO3166Country("Mauritius", "MU", "MUS", 480, "230"),
			new ISO3166Country("Mayotte", "YT", "MYT", 175, "262"),
			new ISO3166Country("Mexico", "MX", "MEX", 484, "52"),
			new ISO3166Country("Micronesia (Federated States of)", "FM", "FSM", 583, "691"),
			new ISO3166Country("Moldova (Republic of)", "MD", "MDA", 498, "373"),
			new ISO3166Country("Monaco", "MC", "MCO", 492, "377"),
			new ISO3166Country("Mongolia", "MN", "MNG", 496, "976"),
			new ISO3166Country("Montenegro", "ME", "MNE", 499, "382"),
			new ISO3166Country("Montserrat", "MS", "MSR", 500, "1"),
			new ISO3166Country("Morocco", "MA", "MAR", 504, "212"),
			new ISO3166Country("Mozambique", "MZ", "MOZ", 508, "258"),
			new ISO3166Country("Myanmar", "MM", "MMR", 104, "95"),
			new ISO3166Country("Namibia", "NA", "NAM", 516, "264"),
			new ISO3166Country("Nauru", "NR", "NRU", 520, "674"),
			new ISO3166Country("Nepal", "NP", "NPL", 524, "977"),
			new ISO3166Country("Netherlands", "NL", "NLD", 528, "31"),
			new ISO3166Country("New Caledonia", "NC", "NCL", 540, "687"),
			new ISO3166Country("New Zealand", "NZ", "NZL", 554, "64"),
			new ISO3166Country("Nicaragua", "NI", "NIC", 558, "505"),
			new ISO3166Country("Niger", "NE", "NER", 562, "227"),
			new ISO3166Country("Nigeria", "NG", "NGA", 566, "234"),
			new ISO3166Country("Niue", "NU", "NIU", 570, "683"),
			new ISO3166Country("Norfolk Island", "NF", "NFK", 574, "672"),
			new ISO3166Country("Northern Mariana Islands", "MP", "MNP", 580, "1"),
			new ISO3166Country("Norway", "NO", "NOR", 578, "47"),
			new ISO3166Country("Oman", "OM", "OMN", 512, "968"),
			new ISO3166Country("Pakistan", "PK", "PAK", 586, "92"),
			new ISO3166Country("Palau", "PW", "PLW", 585, "680"),
			new ISO3166Country("Palestine, State of", "PS", "PSE", 275, "970"),
			new ISO3166Country("Panama", "PA", "PAN", 591, "507"),
			new ISO3166Country("Papua New Guinea", "PG", "PNG", 598, "675"),
			new ISO3166Country("Paraguay", "PY", "PRY", 600, "595"),
			new ISO3166Country("Peru", "PE", "PER", 604, "51"),
			new ISO3166Country("Philippines", "PH", "PHL", 608, "63"),
			new ISO3166Country("Pitcairn", "PN", "PCN", 612, "64"),
			new ISO3166Country("Poland", "PL", "POL", 616, "48"),
			new ISO3166Country("Portugal", "PT", "PRT", 620, "351"),
			new ISO3166Country("Puerto Rico", "PR", "PRI", 630, "1"),
			new ISO3166Country("Qatar", "QA", "QAT", 634, "974"),
			new ISO3166Country("Réunion", "RE", "REU", 638, "262"),
			new ISO3166Country("Romania", "RO", "ROU", 642, "40"),
			new ISO3166Country("Russian Federation", "RU", "RUS", 643, "7"),
			new ISO3166Country("Rwanda", "RW", "RWA", 646, "250"),
			new ISO3166Country("Saint Barthélemy", "BL", "BLM", 652, "590"),
			new ISO3166Country("Saint Helena, Ascension and Tristan da Cunha", "SH", "SHN", 654, "290"),
			new ISO3166Country("Saint Kitts and Nevis", "KN", "KNA", 659, "1"),
			new ISO3166Country("Saint Lucia", "LC", "LCA", 662, "1"),
			new ISO3166Country("Saint Martin (French part)", "MF", "MAF", 663, "590"),
			new ISO3166Country("Saint Pierre and Miquelon", "PM", "SPM", 666, "508"),
			new ISO3166Country("Saint Vincent and the Grenadines", "VC", "VCT", 670, "1"),
			new ISO3166Country("Samoa", "WS", "WSM", 882, "685"),
			new ISO3166Country("San Marino", "SP", "SMR", 674, "378"),
			new ISO3166Country("Sao Tome and Principe", "ST", "STP", 678, "239"),
			new ISO3166Country("Saudi Arabia", "SA", "SAU", 682, "966"),
			new ISO3166Country("Senegal", "SN", "SEN", 686, "221"),
			new ISO3166Country("Serbia", "RS", "SRB", 688, "381"),
			new ISO3166Country("Seychelles", "SC", "SYC", 690, "248"),
			new ISO3166Country("Sierra Leone", "SL", "SLE", 694, "232"),
			new ISO3166Country("Singapore", "SG", "SGP", 702, "65"),
			new ISO3166Country("Sint Maarten (Dutch part)", "SX", "SXM", 534, "1"),
			new ISO3166Country("Slovakia", "SK", "SVK", 703, "421"),
			new ISO3166Country("Slovenia", "SI", "SVN", 705, "386"),
			new ISO3166Country("Solomon Islands", "SB", "SLB", 90, "677"),
			new ISO3166Country("Somalia", "SO", "SOM", 706, "252"),
			new ISO3166Country("South Africa", "ZA", "ZAF", 710, "27"),
			new ISO3166Country("South Georgia and the South Sandwich Islands", "GS", "SGS", 239, "500"),
			new ISO3166Country("South Sudan", "SS", "SSD", 728, "211"),
			new ISO3166Country("Spain", "ES", "ESP", 724, "34"),
			new ISO3166Country("Sri Lanka", "LK", "LKA", 144, "94"),
			new ISO3166Country("Sudan", "SD", "SDN", 729, "249"),
			new ISO3166Country("Suriname", "SR", "SUR", 740, "597"),
			new ISO3166Country("Svalbard and Jan Mayen", "SJ", "SJM", 744, "47"),
			new ISO3166Country("Swaziland", "SZ", "SWZ", 748, "268"),
			new ISO3166Country("Sweden", "SE", "SWE", 752, "46"),
			new ISO3166Country("Switzerland", "CH", "CHE", 756, "41"),
			new ISO3166Country("Syrian Arab Republic", "SY", "SYR", 760, "963"),
			new ISO3166Country("Taiwan, Province of China[a]", "TW", "TWN", 158, "886"),
			new ISO3166Country("Tajikistan", "TJ", "TJK", 762, "992"),
			new ISO3166Country("Tanzania, United Republic of", "TZ", "TZA", 834, "255"),
			new ISO3166Country("Thailand", "TH", "THA", 764, "66"),
			new ISO3166Country("Timor-Leste", "TL", "TLS", 626, "670"),
			new ISO3166Country("Togo", "TG", "TGO", 768, "228"),
			new ISO3166Country("Tokelau", "TK", "TKL", 772, "690"),
			new ISO3166Country("Tonga", "TO", "TON", 776, "676"),
			new ISO3166Country("Trinidad and Tobago", "TT", "TTO", 780, "1"),
			new ISO3166Country("Tunisia", "TN", "TUN", 788, "216"),
			new ISO3166Country("Turkey", "TR", "TUR", 792, "90"),
			new ISO3166Country("Turkmenistan", "TM", "TKM", 795, "993"),
			new ISO3166Country("Turks and Caicos Islands", "TC", "TCA", 796, "1"),
			new ISO3166Country("Tuvalu", "TV", "TUV", 798, "688"),
			new ISO3166Country("Uganda", "UG", "UGA", 800, "256"),
			new ISO3166Country("Ukraine", "UA", "UKR", 804, "380"),
			new ISO3166Country("United Arab Emirates", "AE", "ARE", 784, "971"),
			new ISO3166Country("United Kingdom of Great Britain and Northern Ireland", "GB", "GBR", 826, "44"),
			defaultCountry,
			new ISO3166Country("Uruguay", "UY", "URY", 858, "598"),
			new ISO3166Country("Uzbekistan", "UZ", "UZB", 860, "998"),
			new ISO3166Country("Vanuatu", "VU", "VUT", 548, "678"),
			new ISO3166Country("Venezuela (Bolivarian Republic of)", "VE", "VEN", 862, "58"),
			new ISO3166Country("Viet Nam", "VN", "VNM", 704, "84"),
			new ISO3166Country("Virgin Islands (British)", "VG", "VGB", 92, "1"),
			new ISO3166Country("Virgin Islands (U.S.)", "VI", "VIR", 850, "1"),
			new ISO3166Country("Wallis and Futuna", "WF", "WLF", 876, "681"),
			new ISO3166Country("Western Sahara", "EH", "ESH", 732, "212"),
			new ISO3166Country("Yemen", "YE", "YEM", 887, "967"),
			new ISO3166Country("Zambia", "ZM", "ZMB", 894, "260"),
			new ISO3166Country("Zimbabwe", "ZW", "ZWE", 716, "263")];
		#endregion
	}
}
