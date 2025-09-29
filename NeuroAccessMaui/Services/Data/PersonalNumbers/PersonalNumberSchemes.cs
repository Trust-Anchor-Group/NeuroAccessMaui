using System.Xml;
using Waher.Content.Xml;
using Waher.Events;
using Waher.Script;
using System.Collections.Generic;

namespace NeuroAccessMaui.Services.Data.PersonalNumbers
{
	/// <summary>
	/// Personal Number Schemes available in different countries.
	/// </summary>
	public static class PersonalNumberSchemes
	{
		// Immutable dictionary after first successful load. Values are read-only lists to avoid concurrent mutation.
		private static Dictionary<string, IReadOnlyList<PersonalNumberScheme>> schemesByCode = new(StringComparer.OrdinalIgnoreCase);
		private static volatile bool loaded;
		private static readonly object syncRoot = new();

		private static void LazyLoad()
		{
			if (loaded)
				return;

			lock (syncRoot)
			{
				if (loaded)
					return;

				try
				{
					using MemoryStream ms = new(Waher.Runtime.IO.Resources.LoadResource(
						typeof(PersonalNumberSchemes).Namespace + "." + typeof(PersonalNumberSchemes).Name + ".xml"));

					XmlDocument Doc = new();
					Doc.Load(ms);

					XmlNodeList? ChildNodes = Doc.DocumentElement?.ChildNodes;

					if (ChildNodes is null)
					{
						loaded = true; // mark as loaded even if empty to avoid repeated attempts
						return;
					}

					Dictionary<string, List<PersonalNumberScheme>> Temp = new(StringComparer.OrdinalIgnoreCase);

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
							catch (Exception)
							{
								continue;
							}

							if (Pattern is null || string.IsNullOrWhiteSpace(Variable) || string.IsNullOrWhiteSpace(DisplayString))
								continue;

							if (!Temp.TryGetValue(Country, out List<PersonalNumberScheme>? List))
							{
								List = [];
								Temp[Country] = List;
							}

							List.Add(new PersonalNumberScheme(Variable, DisplayString, Pattern, Check, Normalize));
						}
					}

					// Freeze collections
					Dictionary<string, IReadOnlyList<PersonalNumberScheme>> Final = new(StringComparer.OrdinalIgnoreCase);
					foreach (KeyValuePair<string, List<PersonalNumberScheme>> Kvp in Temp)
					{
						Final[Kvp.Key] = Kvp.Value.AsReadOnly();
					}

					schemesByCode = Final;
				}
				catch (Exception ex)
				{
					Log.Exception(ex);
				}
				finally
				{
					loaded = true;
				}
			}
		}

		/// <summary>
		/// Checks if a personal number is valid, in accordance with registered personal number schemes.
		/// </summary>
		/// <param name="CountryCode">ISO 3166-1 Country Codes.</param>
		/// <param name="PersonalNumber">Personal Number</param>
		/// <returns>Validation information about the number.</returns>
		public static async Task<NumberInformation> Validate(string CountryCode, string PersonalNumber)
		{
			LazyLoad();

			if (schemesByCode.TryGetValue(CountryCode, out IReadOnlyList<PersonalNumberScheme>? Schemes))
			{
				foreach (PersonalNumberScheme Scheme in Schemes)
				{
					NumberInformation Info = await Scheme.Validate(PersonalNumber);
					if (Info.IsValid.HasValue)
					{
						Info.DisplayString = Scheme.DisplayString;
						return Info;
					}
				}

				return new NumberInformation()
				{
					PersonalNumber = PersonalNumber,
					DisplayString = string.Empty,
					IsValid = false
				};
			}
			else
			{
				return new NumberInformation()
				{
					PersonalNumber = PersonalNumber,
					DisplayString = string.Empty,
					IsValid = null
				};
			}
		}

		/// <summary>
		/// Gets the expected personal number format for the given country.
		/// </summary>
		/// <param name="CountryCode">ISO 3166-1 Country Codes.</param>
		/// <returns>A string that can be displayed to a user, informing the user about the approximate format expected.</returns>
		public static string? DisplayStringForCountry(string CountryCode)
		{
			LazyLoad();

			if (!string.IsNullOrWhiteSpace(CountryCode))
			{
				if (schemesByCode.TryGetValue(CountryCode, out IReadOnlyList<PersonalNumberScheme>? Schemes))
					return Schemes.Count > 0 ? Schemes[0].DisplayString : null;
			}

			return null;
		}
	}
}
