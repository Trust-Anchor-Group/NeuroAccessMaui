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
		/// Parses the KYC pages from an XML string.
		/// </summary>
		public static Task<KycProcess> LoadProcessAsync(string Xml, string? Lang = null)
		{
			KycProcess Process = new();
			XDocument Doc = XDocument.Parse(Xml);

			foreach (XElement PageEl in Doc.Root?.Elements("Page") ?? Enumerable.Empty<XElement>())
			{
				KycPage Page = new KycPage
				{
					Id = (string?)PageEl.Attribute("id") ?? string.Empty,
					Title = ParseLocalizedText(PageEl.Element("Title"))
							?? ParseLegacyText(PageEl.Attribute("title"), Lang),
					Description = ParseLocalizedText(PageEl.Element("Description")),
					Condition = ParseCondition(PageEl.Element("Condition"))
				};

				// Fields (direct under Page)
				foreach (XElement FieldEl in PageEl.Elements("Field"))
					Page.AllFields.Add(ParseField(FieldEl, Lang));

				// Sections
				foreach (XElement SectionEl in PageEl.Elements("Section"))
				{
					KycSection Section = new KycSection
					{
						Label = ParseLocalizedText(SectionEl.Element("Label"))
							?? ParseLegacyText(SectionEl.Attribute("label"), Lang),
						Description = ParseLocalizedText(SectionEl.Element("Description"))
					};

					foreach (XElement FieldEl in SectionEl.Elements("Field"))
						Section.AllFields.Add(ParseField(FieldEl, Lang));

					Page.AllSections.Add(Section);
				}

				Process.Pages.Add(Page);
			}

			Process.Initialize();

			return Task.FromResult(Process);
		}

		private static KycField ParseField(XElement El, string? Lang)
		{
			// Determine field type
			string TypeAttr = (string?)El.Attribute("type") ?? "text";
			FieldType FieldType = Enum.TryParse<FieldType>(TypeAttr, true, out FieldType Ft) ? Ft : FieldType.Text;

			KycField Field;

			if (FieldType == FieldType.File)
			{
				FileField FileField = new FileField();
				Field = FileField;
			}
			else if (FieldType == FieldType.Image)
			{
				ImageField ImageField = new ImageField();
				Field = ImageField;
			}
			else
			{
				Field = new KycField();
			}

			// Basic attributes
			Field.Id = (string?)El.Attribute("id") ?? string.Empty;
			Field.FieldType = FieldType;
			Field.Required = (bool?)El.Attribute("required") ?? false;
			Field.Label = ParseLocalizedText(El.Element("Label"))
				?? ParseLegacyText(El.Attribute("label"), Lang);
			Field.Placeholder = ParseLocalizedText(El.Element("Placeholder"));
			Field.Hint = ParseLocalizedText(El.Element("Hint"));
			Field.Description = ParseLocalizedText(El.Element("Description"));
			Field.SpecialType = (string?)El.Attribute("specialType");
			Field.Condition = ParseCondition(El.Element("Condition"));

			// Metadata
			if (El.Element("Metadata") is XElement MetadataEl)
			{
				foreach (XElement MetaChild in MetadataEl.Elements())
				{
					string Key = MetaChild.Name.LocalName;
					string Value = MetaChild.Value?.Trim() ?? string.Empty;

					// Attempt to parse to int, double, bool; otherwise keep string
					if (int.TryParse(Value, out int IntVal))
						Field.Metadata[Key] = IntVal;
					else if (double.TryParse(Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double DoubleVal))
						Field.Metadata[Key] = DoubleVal;
					else if (bool.TryParse(Value, out bool BoolVal))
						Field.Metadata[Key] = BoolVal;
					else if (Key.Equals("AllowedFileTypes", StringComparison.OrdinalIgnoreCase))
						Field.Metadata[Key] = Value.Split(',', ';', ' ').Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
					else
						Field.Metadata[Key] = Value;
				}
			}

			// Validation rules (<ValidationRule> or <Validation>)
			void TryAddLengthRules(XElement RuleEl)
			{
				int? Min = null, Max = null;
				if (int.TryParse((string?)RuleEl.Attribute("minLength"), out int MinVal)) Min = MinVal;
				if (int.TryParse((string?)RuleEl.Attribute("maxLength"), out int MaxVal)) Max = MaxVal;
				if (Min.HasValue || Max.HasValue)
				{
					string? Msg = ParseLocalizedText(RuleEl.Element("Message"))?.Get(Lang);
					Field.AddRule(new LengthRule(Min, Max, Msg));
				}
			}

			void TryAddRegexRule(XElement RuleEl)
			{
				string? Pattern = RuleEl.Element("Regex")?.Value?.Trim();
				if (!string.IsNullOrEmpty(Pattern))
				{
					string? Msg = ParseLocalizedText(RuleEl.Element("Message"))?.Get(Lang);
					Field.AddRule(new RegexRule(Pattern, Msg));
				}
			}

			void TryAddDateRangeRule(XElement RuleEl)
			{
				DateTime? DMin = null, DMax = null;
				if (DateTime.TryParse((string?)RuleEl.Attribute("min"), out DateTime DMinVal)) DMin = DMinVal;
				if (DateTime.TryParse((string?)RuleEl.Attribute("max"), out DateTime DMaxVal)) DMax = DMaxVal;
				if (DMin.HasValue || DMax.HasValue)
				{
					string? Msg = ParseLocalizedText(RuleEl.Element("Message"))?.Get(Lang);
					Field.AddRule(new DateRangeRule(DMin, DMax, Msg));
				}
			}

			foreach (XElement Vr in El.Elements("ValidationRule"))
			{
				TryAddLengthRules(Vr);
				TryAddRegexRule(Vr);
				TryAddDateRangeRule(Vr);
			}
			if (El.Element("Validation") is XElement Legacy)
			{
				TryAddLengthRules(Legacy);
				TryAddRegexRule(Legacy);
				TryAddDateRangeRule(Legacy);
			}

			// Mappings
			foreach (XElement Map in El.Elements("Mapping"))
			{
				Field.Mappings.Add(new KycMapping
				{
					Key = (string?)Map.Attribute("key") ?? string.Empty,
					Transform = Map.Element("Transform")?.Value?.Trim()
				});
			}
			if (El.Attribute("mapping") is XAttribute MapAttr)
				Field.Mappings.Add(new KycMapping { Key = MapAttr.Value });

			// Options for pickers/checkboxes/radio/country
			if (El.Element("Options") is XElement Opts)
			{
				foreach (XElement Opt in Opts.Elements("Option"))
				{
					string Val = (string?)Opt.Attribute("value") ?? string.Empty;
					KycLocalizedText Lbl = ParseLocalizedText(Opt) ?? new KycLocalizedText();
					Field.Options.Add(new KycOption(Val, Lbl));
				}
			}

			// Default value
			string? Def = El.Element("Default")?.Value?.Trim();
			if (!string.IsNullOrEmpty(Def))
			{
				switch (Field.FieldType)
				{
					case FieldType.Date:
						if (Def.Equals("now()", StringComparison.OrdinalIgnoreCase))
							Field.DateValue = DateTime.Today;
						else if (DateTime.TryParse(Def, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime Dt))
							Field.DateValue = Dt;
						break;
					case FieldType.Boolean:
						if (bool.TryParse(Def, out bool Bv))
							Field.BoolValue = Bv;
						break;
					case FieldType.Integer:
						if (int.TryParse(Def, NumberStyles.Integer, CultureInfo.InvariantCulture, out int Iv))
							Field.IntValue = Iv;
						break;
					case FieldType.Decimal:
						if (decimal.TryParse(Def, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal Dv))
							Field.DecimalValue = Dv;
						break;
					default:
						Field.Value = Def;
						break;
				}
			}

			return Field;
		}

		private static KycCondition? ParseCondition(XElement? Cond)
		{
			if (Cond is null) return null;
			return new KycCondition
			{
				FieldRef = Cond.Element("FieldRef")?.Value ?? string.Empty,
				Equals = Cond.Element("Equals")?.Value
			};
		}

		private static KycLocalizedText? ParseLocalizedText(XElement? Parent)
		{
			if (Parent is null) return null;
			KycLocalizedText Loc = new KycLocalizedText();
			foreach (XElement Txt in Parent.Elements("Text"))
			{
				string Lang = (string?)Txt.Attribute("lang") ?? "en";
				string? Val = Txt.Value?.Trim();
				if (!string.IsNullOrEmpty(Val)) Loc.Add(Lang, Val);
			}
			if (!Loc.HasAny && !string.IsNullOrEmpty(Parent.Value?.Trim()))
				Loc.Add("en", Parent.Value.Trim());
			return Loc.HasAny ? Loc : null;
		}

		private static KycLocalizedText? ParseLegacyText(XAttribute? Attr, string? Lang)
		{
			if (Attr is null) return null;
			KycLocalizedText Loc = new KycLocalizedText();
			Loc.Add(Lang ?? "en", Attr.Value);
			return Loc;
		}
	}
}
