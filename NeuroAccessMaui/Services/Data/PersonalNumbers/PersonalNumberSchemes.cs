using System.Xml;
using Waher.Content.Xml;
using Waher.Events;
using Waher.Script;

namespace NeuroAccessMaui.Services.Data.PersonalNumbers
{
	/// <summary>
	/// Personal Number Schemes available in different countries.
	/// </summary>
	public static class PersonalNumberSchemes
	{
		private static readonly Dictionary<string, LinkedList<PersonalNumberScheme>> schemesByCode = [];

		private static void LazyLoad()
		{
			try
			{
				using MemoryStream ms = new(Waher.Content.Resources.LoadResource(
					typeof(PersonalNumberSchemes).Namespace + "." + typeof(PersonalNumberSchemes).Name + ".xml"));

				XmlDocument Doc = new();
				Doc.Load(ms);

				XmlNodeList? childNodes = Doc.DocumentElement?.ChildNodes;

				if (childNodes is null)
					return;

				foreach (XmlNode N in childNodes)
				{
					if (N is XmlElement E && E.LocalName == "Entry")
					{
						string country = XML.Attribute(E, "country");
						string displayString = XML.Attribute(E, "displayString");
						string? variable = null;
						Expression? pattern = null;
						Expression? check = null;
						Expression? normalize = null;

						try
						{
							foreach (XmlNode N2 in E.ChildNodes)
							{
								if (N2 is XmlElement E2)
								{
									switch (E2.LocalName)
									{
										case "Pattern":
											pattern = new Expression(E2.InnerText);
											variable = XML.Attribute(E2, "variable");
											break;

										case "Check":
											check = new Expression(E2.InnerText);
											break;

										case "Normalize":
											normalize = new Expression(E2.InnerText);
											break;
									}
								}
							}
						}
						catch (Exception)
						{
							continue;
						}

						if (pattern is null || string.IsNullOrWhiteSpace(variable) || string.IsNullOrWhiteSpace(displayString))
							continue;

						if (!schemesByCode.TryGetValue(country, out LinkedList<PersonalNumberScheme>? Schemes))
						{
							Schemes = new LinkedList<PersonalNumberScheme>();
							schemesByCode[country] = Schemes;
						}

						Schemes.AddLast(new PersonalNumberScheme(variable, displayString, pattern, check, normalize));
					}
				}
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
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

			if (schemesByCode.TryGetValue(CountryCode, out LinkedList<PersonalNumberScheme>? Schemes))
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
		/// <param name="countryCode">ISO 3166-1 Country Codes.</param>
		/// <returns>A string that can be displayed to a user, informing the user about the approximate format expected.</returns>
		public static string? DisplayStringForCountry(string countryCode)
		{
			LazyLoad();

			if (!string.IsNullOrWhiteSpace(countryCode))
			{
				if (schemesByCode.TryGetValue(countryCode, out LinkedList<PersonalNumberScheme>? Schemes))
					return Schemes?.First?.Value?.DisplayString;
			}

			return null;
		}
	}
}
