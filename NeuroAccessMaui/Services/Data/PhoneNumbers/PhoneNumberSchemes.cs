using System.Xml;
using Waher.Content.Xml;
using Waher.Events;
using Waher.Script;

namespace NeuroAccessMaui.Services.Data.PhoneNumbers
{
	/// <summary>
	/// Phone Number Schemes with country-specific pattern/check/normalize, E.164-first.
	/// </summary>
	public static class PhoneNumberSchemes
	{
		private static readonly Dictionary<string, LinkedList<PhoneNumberScheme>> schemesByCode = [];
		private static LinkedList<PhoneNumberScheme>? defaultSchemes; // country="*"

		private static void LazyLoad()
		{
			if (defaultSchemes is not null || schemesByCode.Count > 0)
				return;

			try
			{
				using MemoryStream Ms = new(Waher.Runtime.IO.Resources.LoadResource(
					typeof(PhoneNumberSchemes).Namespace + "." + typeof(PhoneNumberSchemes).Name + ".xml"));

				XmlDocument Doc = new();
				Doc.Load(Ms);

				XmlNodeList? ChildNodes = Doc.DocumentElement?.ChildNodes;

				if (ChildNodes is null)
					return;

				foreach (XmlNode N in ChildNodes)
				{
					if (N is XmlElement E && E.LocalName == "Entry")
					{
						string Country = XML.Attribute(E, "country");
						string DisplayString = XML.Attribute(E, "displayString");
						string? Variable = null;
						Expression? Pattern = null;
						Expression? Check = null;
						Expression? Normalize = null;

						try
						{
							foreach (XmlNode N2 in E.ChildNodes)
							{
								if (N2 is XmlElement E2)
								{
									switch (E2.LocalName)
									{
										case "Pattern":
											Pattern = new Expression(E2.InnerText);
											Variable = XML.Attribute(E2, "variable");
											break;

										case "Check":
											Check = new Expression(E2.InnerText);
											break;

										case "Normalize":
											Normalize = new Expression(E2.InnerText);
											break;
									}
								}
							}
						}
						catch
						{
							continue;
						}

						if (Pattern is null || string.IsNullOrWhiteSpace(Variable) || string.IsNullOrWhiteSpace(DisplayString))
							continue;

						PhoneNumberScheme Scheme = new(Variable, DisplayString, Pattern, Check, Normalize);

						if (Country == "*")
						{
							defaultSchemes ??= new LinkedList<PhoneNumberScheme>();
							defaultSchemes.AddLast(Scheme);
						}
						else
						{
							if (!schemesByCode.TryGetValue(Country, out LinkedList<PhoneNumberScheme>? List))
							{
								List = new LinkedList<PhoneNumberScheme>();
								schemesByCode[Country] = List;
							}
							List.AddLast(Scheme);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
			}
		}

		/// <summary>
		/// Validates and normalizes a phone number according to configured schemes.
		/// </summary>
		/// <param name="countryCode">ISO 3166-1 alpha-2 (e.g. "SE"). Use null/empty to try generic E.164 only.</param>
		/// <param name="rawNumber">User-entered phone number.</param>
		public static async Task<PhoneNumberInformation> ValidateAndNormalize(string? countryCode, string rawNumber)
		{
			LazyLoad();

			async Task<PhoneNumberInformation?> TrySchemes(LinkedList<PhoneNumberScheme>? schemes)
			{
				if (schemes is null)
					return null;

				foreach (var scheme in schemes)
				{
					var info = await scheme.Validate(rawNumber);
					if (info.IsValid.HasValue)
					{
						info.DisplayString = scheme.DisplayString;
						return info;
					}
				}
				return null;
			}

			// 1) Try country-specific
			if (!string.IsNullOrWhiteSpace(countryCode) &&
				schemesByCode.TryGetValue(countryCode!, out var byCountry))
			{
				var hit = await TrySchemes(byCountry);
				if (hit is not null)
					return hit;
			}

			// 2) Try generic fallback(s)
			{
				var hit = await TrySchemes(defaultSchemes);
				if (hit is not null)
					return hit;
			}

			// 3) If we had country-specific but it failed => invalid; otherwise unknown
			if (!string.IsNullOrWhiteSpace(countryCode) && schemesByCode.ContainsKey(countryCode!))
			{
				return new PhoneNumberInformation
				{
					Original = rawNumber,
					DisplayString = string.Empty,
					IsValid = false
				};
			}

			return new PhoneNumberInformation
			{
				Original = rawNumber,
				DisplayString = string.Empty,
				IsValid = null
			};
		}

		/// <summary>
		/// Gets a human-friendly example format for a country (first scheme display string).
		/// </summary>
		public static string? DisplayStringForCountry(string countryCode)
		{
			LazyLoad();
			if (!string.IsNullOrWhiteSpace(countryCode) &&
				schemesByCode.TryGetValue(countryCode, out var list))
			{
				return list?.First?.Value?.DisplayString;
			}
			return null;
		}
	}

	#region Support types (mirrors your PersonalNumberScheme/NumberInformation style)
	public sealed class PhoneNumberScheme
	{
		public string Variable { get; }
		public string DisplayString { get; }
		private readonly Expression pattern;
		private readonly Expression? check;
		private readonly Expression? normalize;

		public PhoneNumberScheme(string variable, string displayString, Expression pattern, Expression? check, Expression? normalize)
		{
			this.Variable = variable;
			this.DisplayString = displayString;
			this.pattern = pattern;
			this.check = check;
			this.normalize = normalize;
		}

		public async Task<PhoneNumberInformation> Validate(string input)
		{
			try
			{
				// Setup script variables
				Variables v = new();
				v[this.Variable] = input;

				// 1) Pattern must evaluate without exception; otherwise "not for me"
				var patternResult = await this.pattern.EvaluateAsync(v);
				// The Waher.Script regex "like" with named groups will populate *_STR vars into 'v'
				// If it doesn't match, no IsValid is set -> continue to next scheme
				if (patternResult is not bool matched || !matched)
					return new PhoneNumberInformation { Original = input, IsValid = null };

				// 2) If Check exists, it must be true
				if (this.check is not null)
				{
					var checkResult = await this.check.EvaluateAsync(v);
					if (checkResult is not bool ok || !ok)
						return new PhoneNumberInformation { Original = input, IsValid = false };
				}

				// 3) Normalize if available; else use original
				string normalized = input;
				if (this.normalize is not null)
				{
					var norm = await this.normalize.EvaluateAsync(v);
					if (norm is string s)
						normalized = s;
				}

				return new PhoneNumberInformation
				{
					Original = input,
					Normalized = normalized,
					IsValid = true
				};
			}
			catch (Exception)
			{
				// If the scheme throws, skip it silently (same pattern as your personal numbers)
				return new PhoneNumberInformation { Original = input, IsValid = null };
			}
		}
	}

	public sealed class PhoneNumberInformation
	{
		public string? Original { get; set; }
		public string? Normalized { get; set; }
		public string? DisplayString { get; set; }
		/// <summary>
		/// True = valid (and Normalized set), False = invalid, Null = unknown/not applicable for this scheme set
		/// </summary>
		public bool? IsValid { get; set; }
	}
	#endregion
}
