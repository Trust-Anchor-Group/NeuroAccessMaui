using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
		public static async Task<ObservableCollection<KycPage>> LoadProcessAsync(string resource, string? lang = null)
		{
			string fileName = GetFileName(resource);
			using Stream stream = await FileSystem.OpenAppPackageFileAsync(fileName);
			XDocument doc = XDocument.Load(stream);
			var pages = new ObservableCollection<KycPage>();

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
				var allFields = new ObservableCollection<KycField>();
				foreach (XElement fieldEl in pageEl.Elements("Field"))
				{
					allFields.Add(ParseField(fieldEl));
				}
				page.AllFields = allFields;
				page.VisibleFields = new ObservableCollection<KycField>(allFields.Where(f => f.IsVisible));

				// Sections
				var allSections = new ObservableCollection<KycSection>();
				foreach (XElement sectionEl in pageEl.Elements("Section"))
				{
					var section = new KycSection
					{
						Label = ParseLocalizedText(sectionEl.Element("Label")) ?? ParseLegacyString(sectionEl.Attribute("label")),
						Description = ParseLocalizedText(sectionEl.Element("Description"))
					};

					var sectionFields = new ObservableCollection<KycField>();
					foreach (XElement fieldEl in sectionEl.Elements("Field"))
					{
						sectionFields.Add(ParseField(fieldEl));
					}
					section.AllFields = sectionFields;
					section.VisibleFields = new ObservableCollection<KycField>(sectionFields.Where(f => f.IsVisible));
					allSections.Add(section);
				}
				page.AllSections = allSections;
				page.VisibleSections = new ObservableCollection<KycSection>(allSections.Where(s => s.VisibleFields.Count > 0));

				pages.Add(page);
			}

			// Set up IsVisible and handlers for all fields/sections
			foreach (var page in pages)
				page.InitVisibilityHandlers();

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
				Required = (bool?)fieldEl.Attribute("required") ?? false,
				Label = ParseLocalizedText(fieldEl.Element("Label")) ?? ParseLegacyString(fieldEl.Attribute("label")),
				Placeholder = ParseLocalizedText(fieldEl.Element("Placeholder")),
				Hint = ParseLocalizedText(fieldEl.Element("Hint")),
				Description = ParseLocalizedText(fieldEl.Element("Description")),
				SpecialType = (string?)fieldEl.Attribute("specialType"),
				Condition = ParseCondition(fieldEl.Element("Condition"))

			};
			foreach (var ruleEl in fieldEl.Elements("ValidationRule"))
			{
				var rule = new KycValidationRule
				{
					MinLength = (int?)ruleEl.Attribute("minLength"),
					MaxLength = (int?)ruleEl.Attribute("maxLength"),
					RegexPattern = ruleEl.Element("Regex")?.Value?.Trim(),
					Message = ParseLocalizedText(ruleEl.Element("Message")),
				};

				string? min = (string?)ruleEl.Attribute("min");
				string? max = (string?)ruleEl.Attribute("max");
				if (DateTime.TryParse(min, out DateTime minDate))
					rule.MinDate = minDate;
				if (DateTime.TryParse(max, out DateTime maxDate))
					rule.MaxDate = maxDate;

				field.ValidationRules.Add(rule);
			}

			// LEGACY: Convert single <Validation> node to ValidationRule if present
			var legacyValidation = fieldEl.Element("Validation");
			if (legacyValidation != null)
			{
				var legacyRule = new KycValidationRule
				{
					MinLength = (int?)legacyValidation.Attribute("minLength"),
					MaxLength = (int?)legacyValidation.Attribute("maxLength"),
					RegexPattern = legacyValidation.Element("Regex")?.Value?.Trim(),
					Message = ParseLocalizedText(legacyValidation.Element("Message")),
				};
				string? min = (string?)legacyValidation.Attribute("min");
				string? max = (string?)legacyValidation.Attribute("max");
				if (DateTime.TryParse(min, out DateTime minDate))
					legacyRule.MinDate = minDate;
				if (DateTime.TryParse(max, out DateTime maxDate))
					legacyRule.MaxDate = maxDate;
				field.ValidationRules.Add(legacyRule);
			}
			// Parse mappings (multiple supported)
			foreach (XElement mappingEl in fieldEl.Elements("Mapping"))
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

			KycLocalizedText localized = new KycLocalizedText();
			foreach (XElement textEl in parent.Elements("Text"))
			{
				string lang = (string?)textEl.Attribute("lang") ?? "en";
				string? value = textEl.Value?.Trim();
				if (!string.IsNullOrEmpty(value))
					localized.Add(lang, value);
			}
			if (!localized.HasAny)
			{
				string? val = parent.Value?.Trim();
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
