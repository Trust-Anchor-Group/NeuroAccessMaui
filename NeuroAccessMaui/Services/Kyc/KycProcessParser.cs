using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Linq;
using CommunityToolkit.Common;
using Microsoft.Maui.Storage;
using NeuroAccessMaui.Services.Data;
using NeuroAccessMaui.Services.Data.PersonalNumbers;
using NeuroAccessMaui.Services.Kyc.Models;
using NeuroAccessMaui.Services.Kyc.ViewModels;
using Waher.Content.Html.Elements;

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

            // Optional process-level name (localized)
            if (Doc.Root is not null)
            {
                Process.Name = ParseLocalizedText(Doc.Root.Element("Name"))
                                ?? ParseLegacyText(Doc.Root.Attribute("name"), Lang);
            }

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
				{
					ObservableKycField Field = ParseField(FieldEl, Lang);
					Field.SetOwnerProcess(Process);
					Page.AllFields.Add(Field);

				}

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
					{
						ObservableKycField Field = ParseField(FieldEl, Lang);
						Field.SetOwnerProcess(Process);
						Section.AllFields.Add(Field);
					}

					Page.AllSections.Add(Section);
				}

				Process.Pages.Add(Page);
			}

			Process.Initialize();

			return Task.FromResult(Process);
		}

		private static ObservableKycField ParseField(XElement El, string? Lang)
		{
			// Determine field type
			string TypeAttr = (string?)El.Attribute("type") ?? "text";
			FieldType FieldType = Enum.TryParse<FieldType>(TypeAttr, true, out FieldType Ft) ? Ft : FieldType.Text;

			ObservableKycField Field = FieldType switch
			{
				FieldType.Boolean => new ObservableBooleanField(),
				FieldType.Date => new ObservableDateField(),
				FieldType.Integer => new ObservableIntegerField(),
				FieldType.Decimal => new ObservableDecimalField(),
				FieldType.Picker => new ObservablePickerField(),
				FieldType.Gender => new ObservablePickerField(),
				FieldType.Radio => new ObservableRadioField(),
				FieldType.Country => new ObservablePickerField(),
				FieldType.Checkbox => new ObservableCheckboxField(),
				FieldType.File => new ObservableFileField(),
				FieldType.Image => new ObservableImageField(),
				FieldType.Label => new ObservableLabelField(),
				FieldType.Info => new ObservableInfoField(),
				_ => new ObservableGenericField(),
			};

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

					// Handle special cases
					// For livenessCheck image fields, disable manual upload
					if (Field is ObservableImageField ImageField && Key.Equals("AllowUpload", StringComparison.OrdinalIgnoreCase) && bool.TryParse(Value, out bool ParsedBool))
						ImageField.AllowUpload = ParsedBool;

					// Special cases for PNR placeholders
					if (Key.Equals("Placeholder", StringComparison.OrdinalIgnoreCase) && Value.Equals("pnr", StringComparison.OrdinalIgnoreCase))
					{
						KycProcess? Process;
						Field.TryGetOwnerProcess(out Process);
						string? CountryCode = null;
						if (Process != null)
						{
							CountryCode = Process.Values.TryGetValue("country", out string? Cc) && !string.IsNullOrEmpty(Cc) ? Cc : string.Empty;
						}
						if (string.IsNullOrEmpty(CountryCode))
						{
							try
							{
								CountryCode = ServiceRef.TagProfile.LegalIdentity?.GetPersonalInformation().Country;
							}
							catch (Exception)
							{
								CountryCode = string.Empty;
							}
						}

						string? PnrExample = PersonalNumberSchemes.DisplayStringForCountry(CountryCode);
						Field.Placeholder = new KycLocalizedText();
						Field.Placeholder.Add(CountryCode, string.IsNullOrEmpty(PnrExample) ? "" : PnrExample);
					}
				}
			}

			// Validation rules (<ValidationRules>)
			void TryAddLengthRules(XElement RuleEl)
			{
				int? Min = null, Max = null;
				if (int.TryParse((string?)RuleEl.Attribute("min"), out int MinVal)) Min = MinVal;
				if (int.TryParse((string?)RuleEl.Attribute("max"), out int MaxVal)) Max = MaxVal;
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

			void TryAddPnrRule(XElement RuleEl)
			{
				string? FieldRef = (string?)RuleEl.Element("CountryFieldReference");
				string? Msg = ParseLocalizedText(RuleEl.Element("Message"))?.Get(Lang);
				Field.AddRule(new PersonalNumberRule(FieldRef, Msg));
			}

			XElement? ValidationEl = El.Element("ValidationRules");
			if (ValidationEl is not null)
			{
				foreach (XElement Vr in ValidationEl.Elements())
				{
					switch (Vr.Name.ToString())
					{
						case "DateRangeRule":
							TryAddDateRangeRule(Vr); break;
						case "RegexRule":
							TryAddRegexRule(Vr); break;
						case "LengthRule":
							TryAddLengthRules(Vr); break;
						case "PersonalNumberRule":
							TryAddPnrRule(Vr); break;
						default:
							break;
					}
				}
			}

			// Mappings
			foreach (XElement Map in El.Elements("Mapping"))
			{
				KycMapping Mapping = new KycMapping
				{
					Key = (string?)Map.Attribute("key") ?? string.Empty
				};

				// New multi-transform syntax only
				foreach (XElement Tx in Map.Elements("Transform"))
				{
					string? NameAttr = (string?)Tx.Attribute("name");
					if (!string.IsNullOrWhiteSpace(NameAttr))
					{
						Mapping.TransformNames.Add(NameAttr.Trim());
					}
				}

				Field.Mappings.Add(Mapping);
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

			// For country fields
			if (Field is ObservablePickerField CountryField && Field.FieldType == FieldType.Country && Field.Options.Count == 0)
			{
				string DefaultCountry;
				// Set default to current country
				try
				{
					DefaultCountry = ServiceRef.TagProfile.LegalIdentity?.GetPersonalInformation().Country;
				}
				catch (Exception)
				{
					DefaultCountry = string.Empty;
				}

				// if no options defined, load all countries
				if (CountryField.Options.Count == 0)
				{
					foreach (ISO_3166_Country Country in ISO_3166_1.Countries)
					{
						KycLocalizedText LocalizedText = new KycLocalizedText();
						LocalizedText.Add(CultureInfo.CurrentCulture.TwoLetterISOLanguageName, Country.FlagAndName);

						if (Country.Alpha2.Equals(DefaultCountry, StringComparison.OrdinalIgnoreCase))
						{
							Field.Options.Insert(0, new KycOption(Country.Alpha2, LocalizedText));
						}
						else
						{
							Field.Options.Add(new KycOption(Country.Alpha2, LocalizedText));
						}
					}
				}
			}

			// For gender fields, if no options defined, load default options
			if (Field is ObservablePickerField && Field.FieldType == FieldType.Gender && Field.Options.Count == 0)
			{
				foreach (ISO_5218_Gender Gender in ISO_5218.Genders)
				{
					KycLocalizedText LocalizedText = new KycLocalizedText();
					LocalizedText.Add(CultureInfo.CurrentCulture.TwoLetterISOLanguageName, new string(Gender.Unicode, 1) + "\t" + ServiceRef.Localizer[Gender.LocalizedNameId]);

					Field.Options.Add(new KycOption(Gender.Letter, LocalizedText));
				}
			}

			// Default value
			string ? Def = El.Element("Default")?.Value?.Trim();
			if (!string.IsNullOrEmpty(Def))
			{
				switch (Field)
				{
					case ObservableDateField DateField:
						if (Def.Equals("now()", StringComparison.OrdinalIgnoreCase))
							DateField.DateValue = DateTime.Today;
						else if (DateTime.TryParse(Def, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime Dt))
							DateField.DateValue = Dt;
						break;
					case ObservableBooleanField BoolField:
						if (bool.TryParse(Def, out bool Bv))
							BoolField.BoolValue = Bv;
						break;
					case ObservableIntegerField IntField:
						if (int.TryParse(Def, NumberStyles.Integer, CultureInfo.InvariantCulture, out int Iv))
							IntField.IntValue = Iv;
						break;
					case ObservableDecimalField DecField:
						if (decimal.TryParse(Def, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal Dv))
							DecField.DecimalValue = Dv;
						break;
					default:
						Field.StringValue = Def;
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
