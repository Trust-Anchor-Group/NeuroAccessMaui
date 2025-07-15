using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.UI.Pages.Applications.ApplyId;

namespace NeuroAccessMaui.Services.Kyc.Models
{
	/// <summary>
	/// Enumeration of all supported field types in the KYC process.
	/// </summary>
	public enum FieldType
	{
		Text,
		Date,
		Boolean,
		Picker,
		File,
		Image,
		Phone,
		Email,
		Integer,
		Decimal,
		Country,
		Checkbox,
		Radio,
		Label,   // non-input
		Info     // non-input
	}

	/// <summary>
	/// The base KYC field model. Use as-is for generic fields, or subclass for custom logic.
	/// </summary>
	public partial class KycField : ObservableObject
	{
		private readonly List<IKycRule> rules = new();
		public ReadOnlyCollection<IKycRule> ValidationRules => this.rules.AsReadOnly();

		public KycField()
		{
			this.Metadata = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
			this.rules.Add(new RequiredRule());
			if (this.FieldType == FieldType.Checkbox)
				this.SelectedOptions = new ObservableCollection<KycOption>();
		}

		/// <summary>
		/// The unique identifier for the field.
		/// </summary>
		public string Id { get; set; } = string.Empty;

		/// <summary>
		/// The field type (input, picker, info, etc).
		/// </summary>
		public FieldType FieldType { get; set; } = FieldType.Text;

		/// <summary>
		/// Whether the field is required.
		/// </summary>
		public bool Required { get; set; }

		/// <summary>
		/// Field label.
		/// </summary>
		public KycLocalizedText? Label { get; set; }

		/// <summary>
		/// Placeholder text for the field input.
		/// </summary>
		public KycLocalizedText? Placeholder { get; set; }

		/// <summary>
		/// Help/hint text for the field.
		/// </summary>
		public KycLocalizedText? Hint { get; set; }

		/// <summary>
		/// Field description.
		/// </summary>
		public KycLocalizedText? Description { get; set; }

		/// <summary>
		/// Optional special type for custom UI/logic.
		/// </summary>
		public string? SpecialType { get; set; }

		/// <summary>
		/// Conditional display of this field.
		/// </summary>
		public KycCondition? Condition { get; set; }

		/// <summary>
		/// All possible options (for picker, country, checkbox, radio).
		/// </summary>
		public ObservableCollection<KycOption> Options { get; } = new();

		/// <summary>
		/// Mappings to apply output.
		/// </summary>
		public ObservableCollection<KycMapping> Mappings { get; } = new();

		/// <summary>
		/// For Checkbox type: multi-selection of options.
		/// </summary>
		public ObservableCollection<KycOption> SelectedOptions { get; } = new();

		/// <summary>
		/// Arbitrary metadata for field-specific extensions.
		/// Use e.g. Metadata["TargetWidth"] or Metadata["MaxFileSizeMB"].
		/// </summary>
		public Dictionary<string, object?> Metadata { get; }

		[ObservableProperty]
		private object? rawValue;

		[ObservableProperty]
		private bool isVisible = true;

		[ObservableProperty]
		private bool isValid = true;

		[ObservableProperty]
		private string? validationText;

		partial void OnRawValueChanged(object? value)
		{
			this.Validate();
		}

		// Value helpers for different field types:
		public string? Value
		{
			get => this.RawValue as string;
			set => this.RawValue = value;
		}

		public DateTime? DateValue
		{
			get => this.RawValue as DateTime?;
			set => this.RawValue = value;
		}

		public bool? BoolValue
		{
			get => this.RawValue as bool?;
			set => this.RawValue = value;
		}

		public KycOption? SelectedOption
		{
			get => this.RawValue as KycOption;
			set => this.RawValue = value;
		}

		public int? IntValue
		{
			get => this.RawValue as int?;
			set => this.RawValue = value;
		}

		public decimal? DecimalValue
		{
			get => this.RawValue as decimal?;
			set => this.RawValue = value;
		}

		/// <summary>
		/// For "country" field, selected value is a country ISO code.
		/// </summary>
		public string? CountryCode
		{
			get => this.RawValue as string;
			set => this.RawValue = value;
		}

		/// <summary>
		/// Convenience for string-based output for mapping/submission.
		/// </summary>
		public virtual string? ValueString
		{
			get
			{
				return this.FieldType switch
				{
					FieldType.Picker or FieldType.Radio => this.SelectedOption?.Value,
					FieldType.Boolean => this.BoolValue?.ToString().ToLowerInvariant(),
					FieldType.Date => this.DateValue?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
					FieldType.Integer => this.IntValue?.ToString(CultureInfo.InvariantCulture),
					FieldType.Decimal => this.DecimalValue?.ToString(CultureInfo.InvariantCulture),
					FieldType.Country => this.CountryCode,
					FieldType.Phone or FieldType.Email => this.Value,
					FieldType.Checkbox => string.Join(",", this.SelectedOptions.Select(o => o.Value)),
					FieldType.Label or FieldType.Info => null,
					_ => this.Value
				};
			}
		}

		/// <summary>
		/// Add a validation rule to this field.
		/// </summary>
		public void AddRule(IKycRule Rule) => this.rules.Add(Rule);

		/// <summary>
		/// Validate this field using all attached rules.
		/// </summary>
		public virtual bool Validate(string? Lang = null)
		{
			foreach (IKycRule Rule in this.rules)
			{
				if (!Rule.Validate(this, out string Error, Lang))
				{
					this.IsValid = false;
					this.ValidationText = Error;
					return false;
				}
			}
			this.IsValid = true;
			this.ValidationText = null;
			return true;
		}

		/// <summary>
		/// Map the value of this field to your application viewmodel.
		/// </summary>
		public virtual void MapToApplyModel(ApplyIdViewModel Vm)
		{
			// TODO: Implement mapping logic based on Mappings/Transforms
		}
	}

	/// <summary>
	/// File field with file-specific metadata and actions.
	/// </summary>
	public class FileField : KycField
	{
		/// <summary>
		/// Target resolution or file metadata (e.g. max size, file types).
		/// </summary>
		public int? TargetWidth => this.Metadata.TryGetValue("TargetWidth", out var v) ? v as int? : null;
		public int? TargetHeight => this.Metadata.TryGetValue("TargetHeight", out var v) ? v as int? : null;
		public int? MaxFileSizeMB => this.Metadata.TryGetValue("MaxFileSizeMB", out var v) ? v as int? : null;
		public string[]? AllowedFileTypes => this.Metadata.TryGetValue("AllowedFileTypes", out var v) ? v as string[] : null;

		// Example commands for UI actions
		public ICommand? TakePhotoCommand { get; set; }
		public ICommand? PickPhotoCommand { get; set; }
	}

	/// <summary>
	/// Image field with image-specific metadata and actions.
	/// </summary>
	public class ImageField : KycField
	{
		public int? TargetWidth => this.Metadata.TryGetValue("TargetWidth", out var v) ? v as int? : null;
		public int? TargetHeight => this.Metadata.TryGetValue("TargetHeight", out var v) ? v as int? : null;
		public double? AspectRatio => this.Metadata.TryGetValue("AspectRatio", out var v) ? v as double? : null;
		public ICommand? TakePhotoCommand { get; set; }
	}

	/// <summary>
	/// Represents a selectable option for picker, radio, country, etc.
	/// </summary>
	public class KycOption
	{
		public KycOption(string Value, KycLocalizedText Label)
		{
			this.Value = Value;
			this.Label = Label;
		}

		public string Value { get; }
		public KycLocalizedText Label { get; }
		public string GetLabel(string? Lang = null)
		{
			return this.Label.Get(Lang) ?? this.Value;
		}
	}

	/// <summary>
	/// Key/value mapping for external systems or viewmodels.
	/// </summary>
	public class KycMapping
	{
		public string Key { get; set; } = string.Empty;
		public string? Transform { get; set; }
	}

	/// <summary>
	/// Controls when a field is visible, based on other field values.
	/// </summary>
	public class KycCondition
	{
		public string FieldRef { get; set; } = string.Empty;
		public string? Equals { get; set; }

		public bool Evaluate(IDictionary<string, string?> Values)
		{
			return Values.TryGetValue(this.FieldRef, out string? V)
				&& (this.Equals is null ? !string.IsNullOrEmpty(V) : string.Equals(V, this.Equals, StringComparison.OrdinalIgnoreCase));
		}
	}
}
