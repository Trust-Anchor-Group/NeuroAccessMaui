using System.Diagnostics.CodeAnalysis;
using Waher.Content.Emoji;

namespace NeuroAccessMaui.Services.Data
{
	/// <summary>
	/// Conversion between Country Names and ISO-3166-1 country codes.
	/// </summary>
	public static class ISO_3166_1
	{
		private static readonly ISO_3166_Country defaultCountry = new("United States of America", "US", "USA", 840, "1", EmojiUtilities.Emoji_flag_us);
		private static readonly SortedDictionary<string, ISO_3166_Country> code2ByCountry = new(StringComparer.InvariantCultureIgnoreCase);
		private static readonly SortedDictionary<string, ISO_3166_Country> countryByCode2 = new(StringComparer.InvariantCultureIgnoreCase);
		private static readonly SortedDictionary<string, ISO_3166_Country> countryByPhone = new(StringComparer.InvariantCultureIgnoreCase);

		static ISO_3166_1()
		{
			foreach (ISO_3166_Country Country in Countries)
			{
				if (EmojiUtilities.TryGetEmoji($"flag-{Country.Alpha2}".ToLowerInvariant(), out EmojiInfo EmojiInfo))
					Country.EmojiInfo = EmojiInfo;

				code2ByCountry[Country.Name] = Country;
				countryByCode2[Country.Alpha2] = Country;
				countryByPhone[Country.DialCode] = Country;
			}
		}

		/// <summary>
		/// This collection built from Wikipedia entry on ISO3166-1 on 9th Feb 2016
		/// </summary>
		public static ISO_3166_Country DefaultCountry => defaultCountry;

		/// <summary>
		/// This collection built from Wikipedia entry on ISO3166-1 on 9th Feb 2016
		/// </summary>
		public static ISO_3166_Country[] Countries => countries;

		/// <summary>
		/// Tries to get the country, given its country and phone codes.
		/// </summary>
		/// <param name="PhoneNumber">ISO-3166-1 phone number (case insensitive).</param>
		/// <param name="Country">ISO-3166-1 Country, if found.</param>
		/// <returns>If a country was found matching the country code.</returns>
		public static bool TryGetCountryByPhone(string PhoneNumber, [NotNullWhen(true)] out ISO_3166_Country? Country)
		{
			ISO_3166_Country? Result = null;

			foreach (ISO_3166_Country Item in Countries)
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
		public static bool TryGetCountryByCode(string? CountryCode, [NotNullWhen(true)] out ISO_3166_Country? Country)
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
		public static bool TryGetCountryByName(string CountryName, [NotNullWhen(true)] out ISO_3166_Country? Country)
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
			else if (TryGetCountryByCode(CountryCode, out ISO_3166_Country? Country))
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
			else if (TryGetCountryByName(CountryName, out ISO_3166_Country? Country))
				return Country.Alpha2;
			else
				return CountryName;
		}

		#region Build Collection
		private static readonly ISO_3166_Country[] countries = [
			new ISO_3166_Country("Afghanistan", "AF", "AFG", 4, "93"),
			new ISO_3166_Country("Åland Islands", "AX", "ALA", 248, "358"),
			new ISO_3166_Country("Albania", "AL", "ALB", 8, "355"),
			new ISO_3166_Country("Algeria", "DZ", "DZA", 12, "213"),
			new ISO_3166_Country("American Samoa", "AS", "ASM", 16, "1"),
			new ISO_3166_Country("Andorra", "AD", "AND", 20, "376"),
			new ISO_3166_Country("Angola", "AO", "AGO", 24, "244"),
			new ISO_3166_Country("Anguilla", "AI", "AIA", 660, "1"),
			new ISO_3166_Country("Antarctica", "AQ", "ATA", 10, "672"),
			new ISO_3166_Country("Antigua and Barbuda", "AG", "ATG", 28, "1"),
			new ISO_3166_Country("Argentina", "AR", "ARG", 32, "54"),
			new ISO_3166_Country("Armenia", "AM", "ARM", 51, "374"),
			new ISO_3166_Country("Aruba", "AW", "ABW", 533, "297"),
			new ISO_3166_Country("Australia", "AU", "AUS", 36, "61"),
			new ISO_3166_Country("Austria", "AT", "AUT", 40, "43"),
			new ISO_3166_Country("Azerbaijan", "AZ", "AZE", 31, "994"),
			new ISO_3166_Country("Bahamas", "BS", "BHS", 44, "1"),
			new ISO_3166_Country("Bahrain", "BH", "BHR", 48, "973"),
			new ISO_3166_Country("Bangladesh", "BD", "BGD", 50, "880"),
			new ISO_3166_Country("Barbados", "BB", "BRB", 52, "1"),
			new ISO_3166_Country("Belarus", "BY", "BLR", 112, "375"),
			new ISO_3166_Country("Belgium", "BE", "BEL", 56, "32"),
			new ISO_3166_Country("Belize", "BZ", "BLZ", 84, "501"),
			new ISO_3166_Country("Benin", "BJ", "BEN", 204, "229"),
			new ISO_3166_Country("Bermuda", "BM", "BMU", 60, "1"),
			new ISO_3166_Country("Bhutan", "BT", "BTN", 64, "975"),
			new ISO_3166_Country("Bolivia (Plurinational State of)", "BO", "BOL", 68, "591"),
			new ISO_3166_Country("Bonaire, Sint Eustatius and Saba", "BQ", "BES", 535, "599"),
			new ISO_3166_Country("Bosnia and Herzegovina", "BA", "BIH", 70, "387"),
			new ISO_3166_Country("Botswana", "BW", "BWA", 72, "267"),
			new ISO_3166_Country("Brazil", "BR", "BRA", 76, "55"),
			new ISO_3166_Country("British Indian Ocean Territory", "IO", "IOT", 86, "246"),
			new ISO_3166_Country("Brunei Darussalam", "BN", "BRN", 96, "673"),
			new ISO_3166_Country("Bulgaria", "BG", "BGR", 100, "359"),
			new ISO_3166_Country("Burkina Faso", "BF", "BFA", 854, "226"),
			new ISO_3166_Country("Burundi", "BI", "BDI", 108, "257"),
			new ISO_3166_Country("Cabo Verde", "CV", "CPV", 132, "238"),
			new ISO_3166_Country("Cambodia", "KH", "KHM", 116, "855"),
			new ISO_3166_Country("Cameroon", "CM", "CMR", 120, "237"),
			new ISO_3166_Country("Canada", "CA", "CAN", 124, "1"),
			new ISO_3166_Country("Cayman Islands", "KY", "CYM", 136, "1"),
			new ISO_3166_Country("Central African Republic", "CF", "CAF", 140, "236"),
			new ISO_3166_Country("Chad", "TD", "TCD", 148, "235"),
			new ISO_3166_Country("Chile", "CL", "CHL", 152, "56"),
			new ISO_3166_Country("China", "CN", "CHN", 156, "86"),
			new ISO_3166_Country("Christmas Island", "CX", "CXR", 162, "61"),
			new ISO_3166_Country("Cocos (Keeling) Islands", "CC", "CCK", 166, "61"),
			new ISO_3166_Country("Colombia", "CO", "COL", 170, "57"),
			new ISO_3166_Country("Comoros", "KM", "COM", 174, "269"),
			new ISO_3166_Country("Congo", "CG", "COG", 178, "242"),
			new ISO_3166_Country("Congo (Democratic Republic of the)", "CD", "COD", 180, "243"),
			new ISO_3166_Country("Cook Islands", "CK", "COK", 184, "682"),
			new ISO_3166_Country("Costa Rica", "CR", "CRI", 188, "506"),
			new ISO_3166_Country("Côte d'Ivoire", "CI", "CIV", 384, "225"),
			new ISO_3166_Country("Croatia", "HR", "HRV", 191, "385"),
			new ISO_3166_Country("Cuba", "CU", "CUB", 192, "53"),
			new ISO_3166_Country("Curaçao", "CW", "CUW", 531, "599"),
			new ISO_3166_Country("Cyprus", "CY", "CYP", 196, "357"),
			new ISO_3166_Country("Czech Republic", "CZ", "CZE", 203, "420"),
			new ISO_3166_Country("Denmark", "DK", "DNK", 208, "45"),
			new ISO_3166_Country("Djibouti", "DJ", "DJI", 262, "253"),
			new ISO_3166_Country("Dominica", "DM", "DMA", 212, "1"),
			new ISO_3166_Country("Dominican Republic", "DO", "DOM", 214, "1"),
			new ISO_3166_Country("Ecuador", "EC", "ECU", 218, "593"),
			new ISO_3166_Country("Egypt", "EG", "EGY", 818, "20"),
			new ISO_3166_Country("El Salvador", "SV", "SLV", 222, "503"),
			new ISO_3166_Country("Equatorial Guinea", "GQ", "GNQ", 226, "240"),
			new ISO_3166_Country("Eritrea", "ER", "ERI", 232, "291"),
			new ISO_3166_Country("Estonia", "EE", "EST", 233, "372"),
			new ISO_3166_Country("Ethiopia", "ET", "ETH", 231, "251"),
			new ISO_3166_Country("Falkland Islands (Malvinas)", "FK", "FLK", 238, "500"),
			new ISO_3166_Country("Faroe Islands", "FO", "FRO", 234, "298"),
			new ISO_3166_Country("Fiji", "FJ", "FJI", 242, "679"),
			new ISO_3166_Country("Finland", "FI", "FIN", 246, "358"),
			new ISO_3166_Country("France", "FR", "FRA", 250, "33"),
			new ISO_3166_Country("French Guiana", "GF", "GUF", 254, "594"),
			new ISO_3166_Country("French Polynesia", "PF", "PYF", 258, "689"),
			new ISO_3166_Country("French Southern Territories", "TF", "ATF", 260, "262"),
			new ISO_3166_Country("Gabon", "GA", "GAB", 266, "241"),
			new ISO_3166_Country("Gambia", "GM", "GMB", 270, "220"),
			new ISO_3166_Country("Georgia", "GE", "GEO", 268, "995"),
			new ISO_3166_Country("Germany", "DE", "DEU", 276, "49"),
			new ISO_3166_Country("Ghana", "GH", "GHA", 288, "233"),
			new ISO_3166_Country("Gibraltar", "GI", "GIB", 292, "350"),
			new ISO_3166_Country("Greece", "GR", "GRC", 300, "30"),
			new ISO_3166_Country("Greenland", "GL", "GRL", 304, "299"),
			new ISO_3166_Country("Grenada", "GD", "GRD", 308, "1"),
			new ISO_3166_Country("Guadeloupe", "GP", "GLP", 312, "590"),
			new ISO_3166_Country("Guam", "GU", "GUM", 316, "1"),
			new ISO_3166_Country("Guatemala", "GT", "GTM", 320, "502"),
			new ISO_3166_Country("Guernsey", "GG", "GGY", 831, "44"),
			new ISO_3166_Country("Guinea", "GN", "GIN", 324, "224"),
			new ISO_3166_Country("Guinea-Bissau", "GW", "GNB", 624, "245"),
			new ISO_3166_Country("Guyana", "GY", "GUY", 328, "592"),
			new ISO_3166_Country("Haiti", "HT", "HTI", 332, "509"),
			new ISO_3166_Country("Holy See", "VA", "VAT", 336, "379"),
			new ISO_3166_Country("Honduras", "HN", "HND", 340, "504"),
			new ISO_3166_Country("Hong Kong", "HK", "HKG", 344, "852"),
			new ISO_3166_Country("Hungary", "HU", "HUN", 348, "36"),
			new ISO_3166_Country("Iceland", "IS", "ISL", 352, "354"),
			new ISO_3166_Country("India", "IN", "IND", 356, "91"),
			new ISO_3166_Country("Indonesia", "ID", "IDN", 360, "62"),
			new ISO_3166_Country("Iran (Islamic Republic of)", "IR", "IRN", 364, "98"),
			new ISO_3166_Country("Iraq", "IQ", "IRQ", 368, "964"),
			new ISO_3166_Country("Ireland", "IE", "IRL", 372, "353"),
			new ISO_3166_Country("Isle of Man", "IM", "IMN", 833, "44"),
			new ISO_3166_Country("Israel", "IL", "ISR", 376, "972"),
			new ISO_3166_Country("Italy", "IT", "ITA", 380, "39"),
			new ISO_3166_Country("Jamaica", "JM", "JAM", 388, "1"),
			new ISO_3166_Country("Japan", "JP", "JPN", 392, "81"),
			new ISO_3166_Country("Jersey", "JE", "JEY", 832, "44"),
			new ISO_3166_Country("Jordan", "JO", "JOR", 400, "962"),
			new ISO_3166_Country("Kazakhstan", "KZ", "KAZ", 398, "7"),
			new ISO_3166_Country("Kenya", "KE", "KEN", 404, "254"),
			new ISO_3166_Country("Kiribati", "KI", "KIR", 296, "686"),
			new ISO_3166_Country("Korea (Democratic People's Republic of)", "KP", "PRK", 408, "850"),
			new ISO_3166_Country("Korea (Republic of)", "KR", "KOR", 410, "82"),
			new ISO_3166_Country("Kuwait", "KW", "KWT", 414, "965"),
			new ISO_3166_Country("Kyrgyzstan", "KG", "KGZ", 417, "996"),
			new ISO_3166_Country("Lao People's Democratic Republic", "LA", "LAO", 418, "856"),
			new ISO_3166_Country("Latvia", "LV", "LVA", 428, "371"),
			new ISO_3166_Country("Lebanon", "LB", "LBN", 422, "961"),
			new ISO_3166_Country("Lesotho", "LS", "LSO", 426, "266"),
			new ISO_3166_Country("Liberia", "LR", "LBR", 430, "231"),
			new ISO_3166_Country("Libya", "LY", "LBY", 434, "218"),
			new ISO_3166_Country("Liechtenstein", "LI", "LIE", 438, "423"),
			new ISO_3166_Country("Lithuania", "LT", "LTU", 440, "370"),
			new ISO_3166_Country("Luxembourg", "LU", "LUX", 442, "352"),
			new ISO_3166_Country("Macao", "MO", "MAC", 446, "853"),
			new ISO_3166_Country("Macedonia (the former Yugoslav Republic of)", "MK", "MKD", 807, "389"),
			new ISO_3166_Country("Madagascar", "MG", "MDG", 450, "261"),
			new ISO_3166_Country("Malawi", "MW", "MWI", 454, "265"),
			new ISO_3166_Country("Malaysia", "MY", "MYS", 458, "60"),
			new ISO_3166_Country("Maldives", "MV", "MDV", 462, "960"),
			new ISO_3166_Country("Mali", "ML", "MLI", 466, "223"),
			new ISO_3166_Country("Malta", "MT", "MLT", 470, "356"),
			new ISO_3166_Country("Marshall Islands", "MH", "MHL", 584, "692"),
			new ISO_3166_Country("Martinique", "MQ", "MTQ", 474, "596"),
			new ISO_3166_Country("Mauritania", "MR", "MRT", 478, "222"),
			new ISO_3166_Country("Mauritius", "MU", "MUS", 480, "230"),
			new ISO_3166_Country("Mayotte", "YT", "MYT", 175, "262"),
			new ISO_3166_Country("Mexico", "MX", "MEX", 484, "52"),
			new ISO_3166_Country("Micronesia (Federated States of)", "FM", "FSM", 583, "691"),
			new ISO_3166_Country("Moldova (Republic of)", "MD", "MDA", 498, "373"),
			new ISO_3166_Country("Monaco", "MC", "MCO", 492, "377"),
			new ISO_3166_Country("Mongolia", "MN", "MNG", 496, "976"),
			new ISO_3166_Country("Montenegro", "ME", "MNE", 499, "382"),
			new ISO_3166_Country("Montserrat", "MS", "MSR", 500, "1"),
			new ISO_3166_Country("Morocco", "MA", "MAR", 504, "212"),
			new ISO_3166_Country("Mozambique", "MZ", "MOZ", 508, "258"),
			new ISO_3166_Country("Myanmar", "MM", "MMR", 104, "95"),
			new ISO_3166_Country("Namibia", "NA", "NAM", 516, "264"),
			new ISO_3166_Country("Nauru", "NR", "NRU", 520, "674"),
			new ISO_3166_Country("Nepal", "NP", "NPL", 524, "977"),
			new ISO_3166_Country("Netherlands", "NL", "NLD", 528, "31"),
			new ISO_3166_Country("New Caledonia", "NC", "NCL", 540, "687"),
			new ISO_3166_Country("New Zealand", "NZ", "NZL", 554, "64"),
			new ISO_3166_Country("Nicaragua", "NI", "NIC", 558, "505"),
			new ISO_3166_Country("Niger", "NE", "NER", 562, "227"),
			new ISO_3166_Country("Nigeria", "NG", "NGA", 566, "234"),
			new ISO_3166_Country("Niue", "NU", "NIU", 570, "683"),
			new ISO_3166_Country("Norfolk Island", "NF", "NFK", 574, "672"),
			new ISO_3166_Country("Northern Mariana Islands", "MP", "MNP", 580, "1"),
			new ISO_3166_Country("Norway", "NO", "NOR", 578, "47"),
			new ISO_3166_Country("Oman", "OM", "OMN", 512, "968"),
			new ISO_3166_Country("Pakistan", "PK", "PAK", 586, "92"),
			new ISO_3166_Country("Palau", "PW", "PLW", 585, "680"),
			new ISO_3166_Country("Palestine, State of", "PS", "PSE", 275, "970"),
			new ISO_3166_Country("Panama", "PA", "PAN", 591, "507"),
			new ISO_3166_Country("Papua New Guinea", "PG", "PNG", 598, "675"),
			new ISO_3166_Country("Paraguay", "PY", "PRY", 600, "595"),
			new ISO_3166_Country("Peru", "PE", "PER", 604, "51"),
			new ISO_3166_Country("Philippines", "PH", "PHL", 608, "63"),
			new ISO_3166_Country("Pitcairn", "PN", "PCN", 612, "64"),
			new ISO_3166_Country("Poland", "PL", "POL", 616, "48"),
			new ISO_3166_Country("Portugal", "PT", "PRT", 620, "351"),
			new ISO_3166_Country("Puerto Rico", "PR", "PRI", 630, "1"),
			new ISO_3166_Country("Qatar", "QA", "QAT", 634, "974"),
			new ISO_3166_Country("Réunion", "RE", "REU", 638, "262"),
			new ISO_3166_Country("Romania", "RO", "ROU", 642, "40"),
			new ISO_3166_Country("Russian Federation", "RU", "RUS", 643, "7"),
			new ISO_3166_Country("Rwanda", "RW", "RWA", 646, "250"),
			new ISO_3166_Country("Saint Barthélemy", "BL", "BLM", 652, "590"),
			new ISO_3166_Country("Saint Helena, Ascension and Tristan da Cunha", "SH", "SHN", 654, "290"),
			new ISO_3166_Country("Saint Kitts and Nevis", "KN", "KNA", 659, "1"),
			new ISO_3166_Country("Saint Lucia", "LC", "LCA", 662, "1"),
			new ISO_3166_Country("Saint Martin (French part)", "MF", "MAF", 663, "590"),
			new ISO_3166_Country("Saint Pierre and Miquelon", "PM", "SPM", 666, "508"),
			new ISO_3166_Country("Saint Vincent and the Grenadines", "VC", "VCT", 670, "1"),
			new ISO_3166_Country("Samoa", "WS", "WSM", 882, "685"),
			new ISO_3166_Country("San Marino", "SP", "SMR", 674, "378"),
			new ISO_3166_Country("Sao Tome and Principe", "ST", "STP", 678, "239"),
			new ISO_3166_Country("Saudi Arabia", "SA", "SAU", 682, "966"),
			new ISO_3166_Country("Senegal", "SN", "SEN", 686, "221"),
			new ISO_3166_Country("Serbia", "RS", "SRB", 688, "381"),
			new ISO_3166_Country("Seychelles", "SC", "SYC", 690, "248"),
			new ISO_3166_Country("Sierra Leone", "SL", "SLE", 694, "232"),
			new ISO_3166_Country("Singapore", "SG", "SGP", 702, "65"),
			new ISO_3166_Country("Sint Maarten (Dutch part)", "SX", "SXM", 534, "1"),
			new ISO_3166_Country("Slovakia", "SK", "SVK", 703, "421"),
			new ISO_3166_Country("Slovenia", "SI", "SVN", 705, "386"),
			new ISO_3166_Country("Solomon Islands", "SB", "SLB", 90, "677"),
			new ISO_3166_Country("Somalia", "SO", "SOM", 706, "252"),
			new ISO_3166_Country("South Africa", "ZA", "ZAF", 710, "27"),
			new ISO_3166_Country("South Georgia and the South Sandwich Islands", "GS", "SGS", 239, "500"),
			new ISO_3166_Country("South Sudan", "SS", "SSD", 728, "211"),
			new ISO_3166_Country("Spain", "ES", "ESP", 724, "34"),
			new ISO_3166_Country("Sri Lanka", "LK", "LKA", 144, "94"),
			new ISO_3166_Country("Sudan", "SD", "SDN", 729, "249"),
			new ISO_3166_Country("Suriname", "SR", "SUR", 740, "597"),
			new ISO_3166_Country("Svalbard and Jan Mayen", "SJ", "SJM", 744, "47"),
			new ISO_3166_Country("Swaziland", "SZ", "SWZ", 748, "268"),
			new ISO_3166_Country("Sweden", "SE", "SWE", 752, "46"),
			new ISO_3166_Country("Switzerland", "CH", "CHE", 756, "41"),
			new ISO_3166_Country("Syrian Arab Republic", "SY", "SYR", 760, "963"),
			new ISO_3166_Country("Taiwan, Province of China[a]", "TW", "TWN", 158, "886"),
			new ISO_3166_Country("Tajikistan", "TJ", "TJK", 762, "992"),
			new ISO_3166_Country("Tanzania, United Republic of", "TZ", "TZA", 834, "255"),
			new ISO_3166_Country("Thailand", "TH", "THA", 764, "66"),
			new ISO_3166_Country("Timor-Leste", "TL", "TLS", 626, "670"),
			new ISO_3166_Country("Togo", "TG", "TGO", 768, "228"),
			new ISO_3166_Country("Tokelau", "TK", "TKL", 772, "690"),
			new ISO_3166_Country("Tonga", "TO", "TON", 776, "676"),
			new ISO_3166_Country("Trinidad and Tobago", "TT", "TTO", 780, "1"),
			new ISO_3166_Country("Tunisia", "TN", "TUN", 788, "216"),
			new ISO_3166_Country("Turkey", "TR", "TUR", 792, "90"),
			new ISO_3166_Country("Turkmenistan", "TM", "TKM", 795, "993"),
			new ISO_3166_Country("Turks and Caicos Islands", "TC", "TCA", 796, "1"),
			new ISO_3166_Country("Tuvalu", "TV", "TUV", 798, "688"),
			new ISO_3166_Country("Uganda", "UG", "UGA", 800, "256"),
			new ISO_3166_Country("Ukraine", "UA", "UKR", 804, "380"),
			new ISO_3166_Country("United Arab Emirates", "AE", "ARE", 784, "971"),
			new ISO_3166_Country("United Kingdom of Great Britain and Northern Ireland", "GB", "GBR", 826, "44"),
			defaultCountry,
			new ISO_3166_Country("Uruguay", "UY", "URY", 858, "598"),
			new ISO_3166_Country("Uzbekistan", "UZ", "UZB", 860, "998"),
			new ISO_3166_Country("Vanuatu", "VU", "VUT", 548, "678"),
			new ISO_3166_Country("Venezuela (Bolivarian Republic of)", "VE", "VEN", 862, "58"),
			new ISO_3166_Country("Viet Nam", "VN", "VNM", 704, "84"),
			new ISO_3166_Country("Virgin Islands (British)", "VG", "VGB", 92, "1"),
			new ISO_3166_Country("Virgin Islands (U.S.)", "VI", "VIR", 850, "1"),
			new ISO_3166_Country("Wallis and Futuna", "WF", "WLF", 876, "681"),
			new ISO_3166_Country("Western Sahara", "EH", "ESH", 732, "212"),
			new ISO_3166_Country("Yemen", "YE", "YEM", 887, "967"),
			new ISO_3166_Country("Zambia", "ZM", "ZMB", 894, "260"),
			new ISO_3166_Country("Zimbabwe", "ZW", "ZWE", 716, "263")];
		#endregion
	}
}
