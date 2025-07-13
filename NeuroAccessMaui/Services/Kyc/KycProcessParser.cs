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
	/// <summary>
	/// Parses an XML-described KYC process into view-model objects.
	/// </summary>
	public static class KycProcessParser
	{
		/// <summary>
		/// Loads and parses the KYC pages from an embedded XML resource.
		/// </summary>
		public static async Task<KycProcess> LoadProcessAsync(string resource, string? lang = null)
		{
			var process = new KycProcess();
			var fileName = GetFileName(resource);
			using var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
			var doc = XDocument.Load(stream);

			foreach (var pageEl in doc.Root?.Elements("Page") ?? Enumerable.Empty<XElement>())
			{
				var page = new KycPage
				{
					Id = (string?)pageEl.Attribute("id") ?? string.Empty,
					Title = ParseLocalizedText(pageEl.Element("Title"))
							?? ParseLegacyText(pageEl.Attribute("title"), lang),
					Description = ParseLocalizedText(pageEl.Element("Description")),
					Condition = ParseCondition(pageEl.Element("Condition"))
				};

				// Fields
				foreach (var fieldEl in pageEl.Elements("Field"))
					page.AllFields.Add(ParseField(fieldEl, lang));

				// Sections
				foreach (var sectionEl in pageEl.Elements("Section"))
				{
					var section = new KycSection
					{
						Label = ParseLocalizedText(sectionEl.Element("Label"))
							?? ParseLegacyText(sectionEl.Attribute("label"), lang),
						Description = ParseLocalizedText(sectionEl.Element("Description"))
					};

					foreach (var fieldEl in sectionEl.Elements("Field"))
						section.AllFields.Add(ParseField(fieldEl, lang));

					page.AllSections.Add(section);
				}

				process.Pages.Add(page);
			}

			// (Optional) Initialize model listeners for field values
			process.Initialize();

			return process;
		}

		private static string GetFileName(string resource)
		{
			var idx = resource.LastIndexOf("Raw.", StringComparison.OrdinalIgnoreCase);
			return idx >= 0 ? resource[(idx + 4)..] : resource;
		}

		private static KycField ParseField(XElement el, string? lang)
		{
			// Basic attributes
			var typeAttr = (string?)el.Attribute("type") ?? "text";
			var field = new KycField
			{
				Id = (string?)el.Attribute("id") ?? string.Empty,
				FieldType = Enum.TryParse<FieldType>(typeAttr, true, out var ft) ? ft : FieldType.Text,
				Required = (bool?)el.Attribute("required") ?? false,
				Label = ParseLocalizedText(el.Element("Label"))
						?? ParseLegacyText(el.Attribute("label"), lang),
				Placeholder = ParseLocalizedText(el.Element("Placeholder")),
				Hint = ParseLocalizedText(el.Element("Hint")),
				Description = ParseLocalizedText(el.Element("Description")),
				SpecialType = (string?)el.Attribute("specialType")
			};
			field.Condition = ParseCondition(el.Element("Condition"));

			// Validation rules (<ValidationRule> or <Validation>) => IKycRule
			void TryAddLengthRules(XElement ruleEl)
			{
				int? min = null, max = null;
				if (int.TryParse((string?)ruleEl.Attribute("minLength"), out var minVal)) min = minVal;
				if (int.TryParse((string?)ruleEl.Attribute("maxLength"), out var maxVal)) max = maxVal;
				if (min.HasValue || max.HasValue)
				{
					var msg = ParseLocalizedText(ruleEl.Element("Message"))?.Get(lang);
					field.AddRule(new LengthRule(min, max, msg));
				}
			}

			void TryAddRegexRule(XElement ruleEl)
			{
				var pattern = ruleEl.Element("Regex")?.Value?.Trim();
				if (!string.IsNullOrEmpty(pattern))
				{
					var msg = ParseLocalizedText(ruleEl.Element("Message"))?.Get(lang);
					field.AddRule(new RegexRule(pattern, msg));
				}
			}

			void TryAddDateRangeRule(XElement ruleEl)
			{
				DateTime? dmin = null, dmax = null;
				if (DateTime.TryParse((string?)ruleEl.Attribute("min"), out var dminVal)) dmin = dminVal;
				if (DateTime.TryParse((string?)ruleEl.Attribute("max"), out var dmaxVal)) dmax = dmaxVal;
				if (dmin.HasValue || dmax.HasValue)
				{
					var msg = ParseLocalizedText(ruleEl.Element("Message"))?.Get(lang);
					field.AddRule(new DateRangeRule(dmin, dmax, msg));
				}
			}

			// <ValidationRule> elements
			foreach (var vr in el.Elements("ValidationRule"))
			{
				TryAddLengthRules(vr);
				TryAddRegexRule(vr);
				TryAddDateRangeRule(vr);
			}
			// legacy <Validation>
			if (el.Element("Validation") is XElement legacy)
			{
				TryAddLengthRules(legacy);
				TryAddRegexRule(legacy);
				TryAddDateRangeRule(legacy);
			}

			// Mappings
			foreach (var map in el.Elements("Mapping"))
			{
				field.Mappings.Add(new KycMapping
				{
					Key = (string?)map.Attribute("key") ?? string.Empty,
					Transform = map.Element("Transform")?.Value?.Trim()
				});
			}
			// legacy attribute mapping
			if (el.Attribute("mapping") is XAttribute mapAttr)
				field.Mappings.Add(new KycMapping { Key = mapAttr.Value });

			// Options for pickers
			if (el.Element("Options") is XElement opts)
			{
				foreach (var opt in opts.Elements("Option"))
				{
					var val = (string?)opt.Attribute("value") ?? string.Empty;
					var lbl = ParseLocalizedText(opt) ?? new KycLocalizedText();
					field.Options.Add(new KycOption(val, lbl));
				}
			}

			// Default value
			var def = el.Element("Default")?.Value?.Trim();
			if (!string.IsNullOrEmpty(def))
			{
				switch (field.FieldType)
				{
					case FieldType.Date:
						if (def.Equals("now()", StringComparison.OrdinalIgnoreCase))
							field.DateValue = DateTime.Today;
						else if (DateTime.TryParse(def, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
							field.DateValue = dt;
						break;
					case FieldType.Boolean:
						if (bool.TryParse(def, out var bv))
							field.BoolValue = bv;
						break;
					default:
						field.Value = def;
						break;
				}
			}

			return field;
		}

		private static KycCondition? ParseCondition(XElement? cond)
		{
			if (cond == null) return null;
			return new KycCondition
			{
				FieldRef = cond.Element("FieldRef")?.Value ?? string.Empty,
				Equals = cond.Element("Equals")?.Value
			};
		}

		private static KycLocalizedText? ParseLocalizedText(XElement? parent)
		{
			if (parent == null) return null;
			var loc = new KycLocalizedText();
			foreach (var txt in parent.Elements("Text"))
			{
				var l = (string?)txt.Attribute("lang") ?? "en";
				var v = txt.Value?.Trim();
				if (!string.IsNullOrEmpty(v)) loc.Add(l, v);
			}
			if (!loc.HasAny && !string.IsNullOrEmpty(parent.Value?.Trim()))
				loc.Add("en", parent.Value.Trim());
			return loc.HasAny ? loc : null;
		}

		private static KycLocalizedText? ParseLegacyText(XAttribute? attr, string? lang)
		{
			if (attr == null) return null;
			var loc = new KycLocalizedText();
			loc.Add(lang ?? "en", attr.Value);
			return loc;
		}
	}
}
