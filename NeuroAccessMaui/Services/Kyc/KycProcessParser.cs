using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Maui.Storage;
using NeuroAccessMaui.Services.Kyc.Models;

namespace NeuroAccessMaui.Services.Kyc
{
	public static class KycProcessParser
	{
		/// <summary>
		/// Loads KYC process pages from an XML resource.
		/// </summary>
		public static async Task<List<KycPage>> LoadProcessAsync(string resource, string? lang = null)
		{
			string fileName = GetFileName(resource);
			using Stream stream = await FileSystem.OpenAppPackageFileAsync(fileName);
			XDocument doc = XDocument.Load(stream);
			List<KycPage> pages = [];

			foreach (XElement pageEl in doc.Root?.Elements("Page") ?? Enumerable.Empty<XElement>())
			{
				var page = new KycPage
				{
					Id = (string?)pageEl.Attribute("id") ?? string.Empty,
					Title = ParseLocalizedText(pageEl.Element("Title")) ?? ParseLegacyString(pageEl.Attribute("title")),
					Description = ParseLocalizedText(pageEl.Element("Description")),
					Condition = ParseCondition(pageEl.Element("Condition"))
				};

				// Fields directly under <Page>
				foreach (XElement fieldEl in pageEl.Elements("Field"))
				{
					page.Fields.Add(ParseField(fieldEl));
				}

				// Sections
				foreach (XElement sectionEl in pageEl.Elements("Section"))
				{
					var section = new KycSection
					{
						Label = ParseLocalizedText(sectionEl.Element("Label")) ?? ParseLegacyString(sectionEl.Attribute("label")),
						Description = ParseLocalizedText(sectionEl.Element("Description"))
					};
					foreach (XElement fieldEl in sectionEl.Elements("Field"))
					{
						section.Fields.Add(ParseField(fieldEl));
					}
					page.Sections.Add(section);
				}

				pages.Add(page);
			}
			return pages;
		}

		private static string GetFileName(string resource)
		{
			int index = resource.LastIndexOf("Raw.", StringComparison.OrdinalIgnoreCase);
			return index >= 0 ? resource[(index + 4)..] : resource;
		}

		private static KycField ParseField(XElement fieldEl)
		{
			var field = new KycField
			{
				Id = (string?)fieldEl.Attribute("id") ?? string.Empty,
				Type = (string?)fieldEl.Attribute("type") ?? "text",
				Label = ParseLocalizedText(fieldEl.Element("Label")) ?? ParseLegacyString(fieldEl.Attribute("label")),
				Placeholder = ParseLocalizedText(fieldEl.Element("Placeholder")),
				Hint = ParseLocalizedText(fieldEl.Element("Hint")),
				Description = ParseLocalizedText(fieldEl.Element("Description")),
				SpecialType = (string?)fieldEl.Attribute("specialType"),
				Validation = ParseValidation(fieldEl.Element("Validation")),
				Condition = ParseCondition(fieldEl.Element("Condition"))
			};

			// Parse mappings (multiple supported)
			foreach (var mappingEl in fieldEl.Elements("Mapping"))
			{
				field.Mappings.Add(new KycMapping
				{
					Key = (string?)mappingEl.Attribute("key") ?? string.Empty,
					Transform = mappingEl.Element("Transform")?.Value?.Trim()
				});
			}
			// Legacy attribute for mapping
			if (fieldEl.Attribute("mapping") is XAttribute mappingAttr)
			{
				field.Mappings.Add(new KycMapping { Key = mappingAttr.Value });
			}

			// Parse options for pickers
			XElement optionsEl = fieldEl.Element("Options");
			if (optionsEl is not null)
			{
				foreach (XElement optionEl in optionsEl.Elements("Option"))
				{
					string value = (string?)optionEl.Attribute("value") ?? string.Empty;
					KycLocalizedText label = ParseLocalizedText(optionEl) ?? new KycLocalizedText(value, "en");
					field.Options.Add(new KycOption(value, label));
				}
			}

			// Default value handling
			string? @default = fieldEl.Element("Default")?.Value?.Trim();
			if (@default is not null)
			{
				switch (field.Type)
				{
					case "date":
						if (@default == "now()")
							field.DateValue = DateTime.Today;
						else if (DateTime.TryParse(@default, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d))
							field.DateValue = d;
						break;
					case "boolean":
						if (bool.TryParse(@default, out bool b))
							field.BoolValue = b;
						break;
					default:
						field.Value = @default;
						break;
				}
			}
			return field;
		}

		private static KycValidation? ParseValidation(XElement? validationEl)
		{
			if (validationEl is null)
				return null;

			var validation = new KycValidation
			{
				Required = (bool?)validationEl.Attribute("required") ?? false,
				MinLength = (int?)validationEl.Attribute("minLength"),
				MaxLength = (int?)validationEl.Attribute("maxLength"),
				RegexPattern = validationEl.Element("Regex")?.Value?.Trim(),
				Message = ParseLocalizedText(validationEl.Element("Message"))
			};

			string? min = (string?)validationEl.Attribute("min");
			string? max = (string?)validationEl.Attribute("max");
			if (DateTime.TryParse(min, out DateTime minDate))
				validation.MinDate = minDate;
			if (DateTime.TryParse(max, out DateTime maxDate))
				validation.MaxDate = maxDate;

			return validation;
		}

		private static KycCondition? ParseCondition(XElement? conditionEl)
		{
			if (conditionEl is null)
				return null;
			return new KycCondition
			{
				FieldRef = conditionEl.Element("FieldRef")?.Value ?? string.Empty,
				Equals = conditionEl.Element("Equals")?.Value
			};
		}

		private static KycLocalizedText? ParseLocalizedText(XElement? parent)
		{
			if (parent is null)
				return null;

			var localized = new KycLocalizedText();
			foreach (var textEl in parent.Elements("Text"))
			{
				var lang = (string?)textEl.Attribute("lang") ?? "en";
				var value = textEl.Value?.Trim();
				if (!string.IsNullOrEmpty(value))
					localized.Add(lang, value);
			}
			if (!localized.HasAny)
			{
				var val = parent.Value?.Trim();
				if (!string.IsNullOrEmpty(val))
					localized.Add("en", val);
			}
			return localized.HasAny ? localized : null;
		}

		private static KycLocalizedText? ParseLegacyString(XAttribute? attr)
		{
			if (attr is null)
				return null;
			return new KycLocalizedText(attr.Value, "en");
		}
	}
}
