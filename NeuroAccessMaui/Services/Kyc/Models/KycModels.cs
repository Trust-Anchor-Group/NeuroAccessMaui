using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
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
		public ObservableCollection<KycPage> Pages { get; } = new();
	}

	/// <summary>
	/// Represents a page within the KYC process, containing fields and/or sections.
	/// </summary>
	public partial class KycPage : ObservableObject
	{
		public KycPage()
		{
			this.AllFields.CollectionChanged += (_, __) => RefreshVisibleFields();
			this.PropertyChanged += (_, e) =>
			{
				if (e.PropertyName == nameof(AllFields))
					RefreshVisibleFields();
			};
		}

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
		/// All fields directly under this page (not in sections).
		/// </summary>
		public ObservableCollection<KycField> AllFields { get; set; } = new();

		/// <summary>
		/// Fields visible on this page (for binding).
		/// </summary>
		public ObservableCollection<KycField> VisibleFields { get; set; } = new();

		/// <summary>
		/// All sections of this page.
		/// </summary>
		public ObservableCollection<KycSection> AllSections { get; set; } = new();

		/// <summary>
		/// Sections visible on this page (for binding).
		/// </summary>
		public ObservableCollection<KycSection> VisibleSections { get; set; } = new();

		/// <summary>
		/// Gets or sets the optional condition controlling page visibility.
		/// </summary>
		public KycCondition? Condition { get; set; }

		public void InitVisibilityHandlers()
		{
			// Attach for all fields in this page
			foreach (var field in AllFields)
			{
				field.PropertyChanged += Field_PropertyChanged;
			}
			AllFields.CollectionChanged += (s, e) =>
			{
				if (e.NewItems != null)
					foreach (KycField field in e.NewItems)
						field.PropertyChanged += Field_PropertyChanged;
				if (e.OldItems != null)
					foreach (KycField field in e.OldItems)
						field.PropertyChanged -= Field_PropertyChanged;
				RefreshVisibleFields();
			};
			RefreshVisibleFields();

			// Attach for all sections
			foreach (var section in AllSections)
			{
				section.InitVisibilityHandlers();
				section.PropertyChanged += Section_PropertyChanged;
			}
			AllSections.CollectionChanged += (s, e) =>
			{
				if (e.NewItems != null)
					foreach (KycSection section in e.NewItems)
					{
						section.InitVisibilityHandlers();
						section.PropertyChanged += Section_PropertyChanged;
					}
				if (e.OldItems != null)
					foreach (KycSection section in e.OldItems)
						section.PropertyChanged -= Section_PropertyChanged;
				RefreshVisibleSections();
			};
			RefreshVisibleSections();
		}

		private void Field_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(KycField.IsVisible))
				RefreshVisibleFields();
		}
		private void Section_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(KycSection.IsVisible))
				RefreshVisibleSections();
		}

		public void RefreshVisibleFields()
		{
			VisibleFields.Clear();
			foreach (var field in AllFields)
				if (field.IsVisible)
					VisibleFields.Add(field);
		}

		public void RefreshVisibleSections()
		{
			VisibleSections.Clear();
			foreach (var section in AllSections)
				if (section.IsVisible)
					VisibleSections.Add(section);
		}

		/// <summary>
		/// Determines if the page should be visible based on the current values.
		/// </summary>
		public bool IsVisible(IDictionary<string, string?> values) => this.Condition is null || this.Condition.Evaluate(values);

		/// <summary>
		/// Call this after updating visibility conditions to update the model/UI.
		/// </summary>
		public void UpdateAllVisibilities(IDictionary<string, string?> values)
		{
			foreach (var field in AllFields)
				field.IsVisible = field.Condition is null || field.Condition.Evaluate(values);

			foreach (var section in AllSections)
				section.UpdateAllVisibilities(values);
		}
	}

	/// <summary>
	/// Represents a section grouping fields within a page.
	/// </summary>
	public partial class KycSection : ObservableObject
	{
		public KycSection()
		{
			this.AllFields.CollectionChanged += (_, __) => RefreshVisibleFields();
		}

		/// <summary>
		/// Gets or sets the localized section label.
		/// </summary>
		public KycLocalizedText? Label { get; set; }

		/// <summary>
		/// Gets or sets the localized section description.
		/// </summary>
		public KycLocalizedText? Description { get; set; }

		/// <summary>
		/// All fields within this section.
		/// </summary>
		public ObservableCollection<KycField> AllFields { get; set; } = new();

		/// <summary>
		/// Fields visible in this section (for binding).
		/// </summary>
		public ObservableCollection<KycField> VisibleFields { get; set; } = new();

		[ObservableProperty]
		private bool isVisible = true;

		public void InitVisibilityHandlers()
		{
			foreach (var field in AllFields)
			{
				field.PropertyChanged += Field_PropertyChanged;
			}
			AllFields.CollectionChanged += (s, e) =>
			{
				if (e.NewItems != null)
					foreach (KycField field in e.NewItems)
						field.PropertyChanged += Field_PropertyChanged;
				if (e.OldItems != null)
					foreach (KycField field in e.OldItems)
						field.PropertyChanged -= Field_PropertyChanged;
				RefreshVisibleFields();
			};
			RefreshVisibleFields();
		}

		private void Field_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(KycField.IsVisible))
				RefreshVisibleFields();
		}

		public void RefreshVisibleFields()
		{
			VisibleFields.Clear();
			foreach (var field in AllFields)
				if (field.IsVisible)
					VisibleFields.Add(field);
		}

		/// <summary>
		/// Call this after updating visibility conditions to update the model/UI.
		/// </summary>
		public void UpdateAllVisibilities(IDictionary<string, string?> values)
		{
			foreach (var field in AllFields)
				field.IsVisible = field.Condition is null || field.Condition.Evaluate(values);
		}
	}

	/// <summary>
	/// Represents a field in a KYC page or section.
	/// </summary>
	public partial class KycField : ObservableObject
	{
		public string Id { get; init; } = string.Empty;
		public string Type { get; init; } = string.Empty;
		public KycLocalizedText? Label { get; set; }
		public KycLocalizedText? Placeholder { get; set; }
		public KycLocalizedText? Hint { get; set; }
		public KycLocalizedText? Description { get; set; }
		public string? SpecialType { get; set; }
		public ObservableCollection<KycMapping> Mappings { get; } = new();
		public KycValidation? Validation { get; set; }
		public KycCondition? Condition { get; set; }
		public ObservableCollection<KycOption> Options { get; } = new();

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
		[ObservableProperty]
		private bool isValid = true;
		[ObservableProperty]
		private string? validationText;

		partial void OnValueChanged(string? value) => Validate();
		partial void OnDateValueChanged(DateTime? value)
		{
			Value = value?.ToString("yyyy-MM-dd");
			Validate();
		}
		partial void OnSelectedOptionChanged(KycOption? value)
		{
			Value = value?.Value;
			Validate();
		}
		partial void OnBoolValueChanged(bool value)
		{
			Value = value ? "true" : "false";
			Validate();
		}

		public bool Validate(string? lang = null)
		{
			bool valid = this.Validate(out string error, lang);
			this.IsValid = valid;
			this.ValidationText = valid ? null : error;
			return valid;
		}

		public bool Validate(out string error, string? lang = null)
		{
			error = string.Empty;
			KycValidation? v = this.Validation;
			if (v is null)
				return true;

			string fieldLabel = this.Label?.Get(lang) ?? this.Id;

			switch (this.Type)
			{
				case "date":
					if (v.Required && this.DateValue is null)
					{
						error = v.GetMessage(lang) ?? $"{fieldLabel} is required";
						return false;
					}
					if (this.DateValue is not null)
					{
						if (v.MinDate.HasValue && this.DateValue.Value < v.MinDate.Value)
						{
							error = v.GetMessage(lang) ?? $"{fieldLabel} is too early";
							return false;
						}
						if (v.MaxDate.HasValue && this.DateValue.Value > v.MaxDate.Value)
						{
							error = v.GetMessage(lang) ?? $"{fieldLabel} is too late";
							return false;
						}
					}
					break;
				case "picker":
					if (v.Required && this.SelectedOption is null)
					{
						error = v.GetMessage(lang) ?? $"{fieldLabel} is required";
						return false;
					}
					break;
				case "boolean":
					if (v.Required && !this.BoolValue)
					{
						error = v.GetMessage(lang) ?? $"{fieldLabel} is required";
						return false;
					}
					break;
				default:
					string text = this.Value ?? string.Empty;
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
			this.Value = value;
			this.Label = label;
		}
		public string Value { get; }
		public KycLocalizedText Label { get; }
		public string GetLabel(string? lang = null) => this.Label.Get(lang) ?? this.Value;
	}

	public class KycMapping
	{
		public string Key { get; set; } = string.Empty;
		public string? Transform { get; set; }
	}

	public class KycValidation
	{
		public bool Required { get; set; }
		public int? MinLength { get; set; }
		public int? MaxLength { get; set; }
		public string? RegexPattern { get; set; }
		public DateTime? MinDate { get; set; }
		public DateTime? MaxDate { get; set; }
		public KycLocalizedText? Message { get; set; }
		public string? GetMessage(string? lang = null) => this.Message?.Get(lang);
	}

	public class KycCondition
	{
		public string FieldRef { get; set; } = string.Empty;
		public string? Equals { get; set; }

		public bool Evaluate(IDictionary<string, string?> values)
		{
			if (!values.TryGetValue(this.FieldRef, out string? value))
				return false;
			if (this.Equals is null)
				return !string.IsNullOrEmpty(value);
			return string.Equals(value, this.Equals, StringComparison.OrdinalIgnoreCase);
		}
	}

	public class KycLocalizedText : INotifyPropertyChanged
	{
		private readonly Dictionary<string, string> _byLang = new(StringComparer.OrdinalIgnoreCase);

		public KycLocalizedText() { }

		public KycLocalizedText(string value, string lang = "en")
		{
			this._byLang[lang] = value;
		}

		public bool HasAny => this._byLang.Count > 0;

		public void Add(string lang, string value) => this._byLang[lang] = value;

		public string? Get(string? lang)
		{
			if (lang != null && this._byLang.TryGetValue(lang, out string? value))
				return value;
			if (this._byLang.TryGetValue("en", out string? enValue))
				return enValue;
			if (this._byLang.Count > 0)
				return this._byLang.Values.First();
			return null;
		}

		public string Text
		{
			get
			{
				string lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
				if (this._byLang.TryGetValue(lang, out string? value))
					return value;
				if (this._byLang.TryGetValue("en", out string? enValue))
					return enValue;
				if (this._byLang.Count > 0)
					return this._byLang.Values.First();
				return string.Empty;
			}
		}

		public void Merge(KycLocalizedText other)
		{
			foreach (KeyValuePair<string, string> pair in other._byLang)
				this._byLang[pair.Key] = pair.Value;
			this.OnPropertyChanged(nameof(this.Text));
		}

		public event PropertyChangedEventHandler? PropertyChanged;
		protected void OnPropertyChanged(string propertyName)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
