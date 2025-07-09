using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.UI.Pages.Applications.ApplyId;

namespace NeuroAccessMaui.Services.Kyc.Models
{
	/// <summary>
	/// Represents an entire KYC process, consisting of multiple pages.
	/// </summary>
	public partial class KycProcess
	{
		/// <summary>
		/// Gets the pages of the KYC process.
		/// </summary>
		public List<KycPage> Pages { get; } = [];
	}

	/// <summary>
	/// Represents a page within the KYC process, containing fields and/or sections.
	/// </summary>
	public partial class KycPage
	{
		/// <summary>
		/// Gets or sets the unique page ID.
		/// </summary>
		public string Id { get; init; } = string.Empty;

		/// <summary>
		/// Gets or sets the localized title of the page.
		/// </summary>
		public KycLocalizedText? Title { get; set; }

		/// <summary>
		/// Gets or sets the localized description of the page.
		/// </summary>
		public KycLocalizedText? Description { get; set; }

		/// <summary>
		/// Gets the fields directly under this page (not in sections).
		/// </summary>
		public List<KycField> Fields { get; } = [];

		/// <summary>
		/// Gets the sections of this page.
		/// </summary>
		public List<KycSection> Sections { get; } = [];

		/// <summary>
		/// Gets or sets the optional condition controlling page visibility.
		/// </summary>
		public KycCondition? Condition { get; set; }

		public List<KycField> VisibleFields => Fields.Where(f => f.IsVisible).ToList();


		/// <summary>
		/// Determines if the page should be visible based on the current values.
		/// </summary>
		public bool IsVisible(IDictionary<string, string?> values) => Condition is null || Condition.Evaluate(values);

		/// <summary>
		/// Returns the visible fields on this page (direct fields + all visible fields in sections).
		/// </summary>
		public IEnumerable<KycField> GetVisibleFields(IDictionary<string, string?> values)
		{
			foreach (var field in Fields)
			{
				field.IsVisible = field.Condition is null || field.Condition.Evaluate(values);
				if (field.IsVisible)
					yield return field;
			}
			foreach (var section in Sections)
			{
				foreach (var field in section.GetVisibleFields(values))
					yield return field;
			}
		}
	}

	/// <summary>
	/// Represents a section grouping fields within a page.
	/// </summary>
	public partial class KycSection
	{
		/// <summary>
		/// Gets or sets the localized section label.
		/// </summary>
		public KycLocalizedText? Label { get; set; }

		/// <summary>
		/// Gets or sets the localized section description.
		/// </summary>
		public KycLocalizedText? Description { get; set; }

		/// <summary>
		/// Gets the fields within this section.
		/// </summary>
		public List<KycField> Fields { get; } = [];

		public List<KycField> VisibleFields => Fields.Where(f => f.IsVisible).ToList();


		/// <summary>
		/// Returns the visible fields in this section.
		/// </summary>
		public IEnumerable<KycField> GetVisibleFields(IDictionary<string, string?> values)
		{
			foreach (var field in Fields)
			{
				field.IsVisible = field.Condition is null || field.Condition.Evaluate(values);
				if (field.IsVisible)
					yield return field;
			}
		}
	}

	/// <summary>
	/// Represents a field in a KYC page or section.
	/// </summary>
	public partial class KycField : ObservableObject
	{
		/// <summary>
		/// Gets or sets the unique field ID.
		/// </summary>
		public string Id { get; init; } = string.Empty;

		/// <summary>
		/// Gets or sets the field type (e.g., text, date, file, picker, boolean).
		/// </summary>
		public string Type { get; init; } = string.Empty;

		/// <summary>
		/// Gets or sets the localized field label.
		/// </summary>
		public KycLocalizedText? Label { get; set; }

		/// <summary>
		/// Gets or sets the localized placeholder.
		/// </summary>
		public KycLocalizedText? Placeholder { get; set; }

		/// <summary>
		/// Gets or sets the localized hint.
		/// </summary>
		public KycLocalizedText? Hint { get; set; }

		/// <summary>
		/// Gets or sets the localized description.
		/// </summary>
		public KycLocalizedText? Description { get; set; }

		/// <summary>
		/// Gets or sets the special type for custom UI handling.
		/// </summary>
		public string? SpecialType { get; set; }

		/// <summary>
		/// Gets the list of mappings for this field (for key-value and transforms).
		/// </summary>
		public List<KycMapping> Mappings { get; } = [];

		/// <summary>
		/// Gets or sets the field validation info.
		/// </summary>
		public KycValidation? Validation { get; set; }

		/// <summary>
		/// Gets or sets the field condition for visibility.
		/// </summary>
		public KycCondition? Condition { get; set; }

		/// <summary>
		/// Gets the available options (for picker fields).
		/// </summary>
		public List<KycOption> Options { get; } = [];

		[ObservableProperty]
		private string? value;

		[ObservableProperty]
		private DateTime? dateValue;

		[ObservableProperty]
		private KycOption? selectedOption;

		[ObservableProperty]
		private bool boolValue;

		[ObservableProperty]
		private bool isVisible = true;

		partial void OnDateValueChanged(DateTime? value) => Value = value?.ToString("yyyy-MM-dd");
		partial void OnSelectedOptionChanged(KycOption? value) => Value = value?.Value;
		partial void OnBoolValueChanged(bool value) => Value = value ? "true" : "false";

		/// <summary>
		/// Validates this field against its validation rules and returns a localized error message.
		/// </summary>
		public bool Validate(out string error, string? lang = null)
		{
			error = string.Empty;
			var v = Validation;
			if (v is null)
				return true;

			string fieldLabel = Label?.Get(lang) ?? Id;

			switch (Type)
			{
				case "date":
					if (v.Required && DateValue is null)
					{
						error = v.GetMessage(lang) ?? $"{fieldLabel} is required";
						return false;
					}
					if (DateValue is not null)
					{
						if (v.MinDate.HasValue && DateValue.Value < v.MinDate.Value)
						{
							error = v.GetMessage(lang) ?? $"{fieldLabel} is too early";
							return false;
						}
						if (v.MaxDate.HasValue && DateValue.Value > v.MaxDate.Value)
						{
							error = v.GetMessage(lang) ?? $"{fieldLabel} is too late";
							return false;
						}
					}
					break;
				case "picker":
					if (v.Required && SelectedOption is null)
					{
						error = v.GetMessage(lang) ?? $"{fieldLabel} is required";
						return false;
					}
					break;
				case "boolean":
					if (v.Required && !BoolValue)
					{
						error = v.GetMessage(lang) ?? $"{fieldLabel} is required";
						return false;
					}
					break;
				default:
					string text = Value ?? string.Empty;
					if (v.Required && string.IsNullOrEmpty(text))
					{
						error = v.GetMessage(lang) ?? $"{fieldLabel} is required";
						return false;
					}
					if (v.MinLength.HasValue && text.Length < v.MinLength.Value)
					{
						error = v.GetMessage(lang) ?? $"{fieldLabel} is too short";
						return false;
					}
					if (v.MaxLength.HasValue && text.Length > v.MaxLength.Value)
					{
						error = v.GetMessage(lang) ?? $"{fieldLabel} is too long";
						return false;
					}
					if (!string.IsNullOrEmpty(v.RegexPattern) && !Regex.IsMatch(text, v.RegexPattern))
					{
						error = v.GetMessage(lang) ?? $"{fieldLabel} is invalid";
						return false;
					}
					break;
			}
			return true;
		}

		/// <summary>
		/// Placeholder for mapping field value(s) to your domain/application model.
		/// </summary>
		public void MapToApplyModel(ApplyIdViewModel vm)
		{
			// Implement mapping logic based on Mappings & Transforms
		}
	}

	/// <summary>
	/// Represents a picker/select option.
	/// </summary>
	public class KycOption
	{
		public KycOption(string value, KycLocalizedText label)
		{
			Value = value;
			Label = label;
		}
		/// <summary>
		/// Gets the value used for data mapping/storage.
		/// </summary>
		public string Value { get; }

		/// <summary>
		/// Gets the localized label.
		/// </summary>
		public KycLocalizedText Label { get; }

		/// <summary>
		/// Returns the display label for the given language.
		/// </summary>
		public string GetLabel(string? lang = null) => Label.Get(lang) ?? Value;
	}

	/// <summary>
	/// Represents a mapping (for key-value pairs), with optional transformation logic.
	/// </summary>
	public class KycMapping
	{
		public string Key { get; set; } = string.Empty;
		public string? Transform { get; set; }
	}

	/// <summary>
	/// Represents field validation information.
	/// </summary>
	public class KycValidation
	{
		public bool Required { get; set; }
		public int? MinLength { get; set; }
		public int? MaxLength { get; set; }
		public string? RegexPattern { get; set; }
		public DateTime? MinDate { get; set; }
		public DateTime? MaxDate { get; set; }

		/// <summary>
		/// Gets or sets the localized message(s) for this validation.
		/// </summary>
		public KycLocalizedText? Message { get; set; }

		/// <summary>
		/// Gets the error message in the given language, or null.
		/// </summary>
		public string? GetMessage(string? lang = null) => Message?.Get(lang);
	}

	/// <summary>
	/// Represents a field or page condition for visibility.
	/// </summary>
	public class KycCondition
	{
		public string FieldRef { get; set; } = string.Empty;
		public string? Equals { get; set; }

		public bool Evaluate(IDictionary<string, string?> values)
		{
			if (!values.TryGetValue(FieldRef, out string? value))
				return false;
			if (Equals is null)
				return !string.IsNullOrEmpty(value);
			return string.Equals(value, Equals, StringComparison.OrdinalIgnoreCase);
		}
	}

	/// <summary>
	/// Represents a set of localized text values (per language code).
	/// </summary>
	public class KycLocalizedText : INotifyPropertyChanged
	{
		private readonly Dictionary<string, string> _byLang = new(StringComparer.OrdinalIgnoreCase);

		public KycLocalizedText() { }

		public KycLocalizedText(string value, string lang = "en")
		{
			_byLang[lang] = value;
		}

		public bool HasAny => _byLang.Count > 0;

		/// <summary>
		/// Adds or updates a translation for a language.
		/// </summary>
		public void Add(string lang, string value) => _byLang[lang] = value;

		/// <summary>
		/// Gets the value for a given language code, or null if not available.
		/// </summary>
		public string? Get(string? lang)
		{
			if (lang != null && _byLang.TryGetValue(lang, out var value))
				return value;
			if (_byLang.TryGetValue("en", out var enValue))
				return enValue;
			if (_byLang.Count > 0)
				return _byLang.Values.First();
			return null;
		}

		/// <summary>
		/// Returns the text for the current UI culture (falls back to en or any available).
		/// </summary>
		public string Text
		{
			get
			{
				var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
				if (_byLang.TryGetValue(lang, out var value))
					return value;
				if (_byLang.TryGetValue("en", out var enValue))
					return enValue;
				if (_byLang.Count > 0)
					return _byLang.Values.First();
				return string.Empty;
			}
		}

		/// <summary>
		/// Merges another KycLocalizedText into this one (overriding existing languages).
		/// </summary>
		public void Merge(KycLocalizedText other)
		{
			foreach (var pair in other._byLang)
				_byLang[pair.Key] = pair.Value;
			OnPropertyChanged(nameof(Text));
		}

		public event PropertyChangedEventHandler? PropertyChanged;
		protected void OnPropertyChanged(string propertyName)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
