using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using NeuroAccessMaui.Services.Kyc.ViewModels;

namespace NeuroAccessMaui.Services.Kyc.Models
{
	public interface IKycRule
	{
		bool Validate(ObservableKycField Field, out string Error, string? Lang = null);
	}

	public interface IAsyncKycRule: IKycRule
	{
		Task<(bool Ok, string? Error)> ValidateAsync(ObservableKycField field, string? lang = null);
	}

	/// <summary>
	/// Ensures the field has a value if required.
	/// </summary>
	public class RequiredRule : IKycRule
	{
		public bool Validate(ObservableKycField Field, out string Error, string? Lang = null)
		{
			Error = string.Empty;
			if (!Field.Required)
				return true;

			string Label = Field.Label?.Get(Lang) ?? Field.Id;
			bool Missing = Field.FieldType switch
			{
				FieldType.Date => Field is ObservableDateField DateField && DateField.DateValue is null,
				FieldType.Picker => Field is ObservablePickerField PickerField && PickerField.SelectedOption is null,
				FieldType.Radio => Field is ObservableRadioField RadioField && RadioField.SelectedOption is null,
				FieldType.Boolean => Field is ObservableBooleanField BoolField && BoolField.BoolValue != true,
				FieldType.Checkbox => Field is ObservableCheckboxField CheckboxField && (CheckboxField.SelectedOptions == null || CheckboxField.SelectedOptions.Count == 0),
				FieldType.File => Field is ObservableFileField FileField && (FileField.StringValue is not string FileValue || string.IsNullOrEmpty(FileValue)),
				FieldType.Email or FieldType.Phone or FieldType.Text => string.IsNullOrEmpty(Field.StringValue),
				FieldType.Integer => Field is ObservableIntegerField IntField && IntField.IntValue is null,
				FieldType.Decimal => Field is ObservableDecimalField DecField && DecField.DecimalValue is null,
				FieldType.Country => Field is ObservableCountryField CountryField && string.IsNullOrEmpty(CountryField.CountryCode),
				FieldType.Label or FieldType.Info => false, // info/display only, never required
				_ => string.IsNullOrEmpty(Field.StringValue)
			};

			if (Missing)
				Error = $"{Label} is required";
			return string.IsNullOrEmpty(Error);
		}
	}

	/// <summary>
	/// Validates text length for string fields.
	/// </summary>
	public class LengthRule : IKycRule
	{
		private readonly int? min;
		private readonly int? max;
		private readonly string? message;

		public LengthRule(int? Min, int? Max, string? Message)
		{
			this.min = Min;
			this.max = Max;
			this.message = Message;
		}

		public bool Validate(ObservableKycField Field, out string Error, string? Lang = null)
		{
			Error = string.Empty;
			string? Text = Field.StringValue;
			if (Text is not null)
			{
				if (this.min.HasValue && Text.Length < this.min.Value)
					Error = this.message ?? $"{Field.Label?.Get(Lang) ?? Field.Id} must be at least {this.min.Value} characters";
				else if (this.max.HasValue && Text.Length > this.max.Value)
					Error = this.message ?? $"{Field.Label?.Get(Lang) ?? Field.Id} must be at most {this.max.Value} characters";
			}
			return string.IsNullOrEmpty(Error);
		}
	}

	/// <summary>
	/// Validates string input against a regex.
	/// </summary>
	public class RegexRule : IKycRule
	{
		private readonly Regex regex;
		private readonly string? message;

		public RegexRule(string Pattern, string? Message)
		{
			this.regex = new Regex(Pattern, RegexOptions.Compiled);
			this.message = Message;
		}

		public bool Validate(ObservableKycField Field, out string Error, string? Lang = null)
		{
			Error = string.Empty;
			string? Text = Field.StringValue;
			if (Text is not null && !this.regex.IsMatch(Text))
				Error = this.message ?? $"{Field.Label?.Get(Lang) ?? Field.Id} format is invalid";
			return string.IsNullOrEmpty(Error);
		}
	}

	/// <summary>
	/// Validates if date input is within a range.
	/// </summary>
	public class DateRangeRule : IKycRule
	{
		private readonly DateTime? min;
		private readonly DateTime? max;
		private readonly string? message;

		public DateRangeRule(DateTime? Min, DateTime? Max, string? Message)
		{
			this.min = Min;
			this.max = Max;
			this.message = Message;
		}

		public bool Validate(ObservableKycField Field, out string Error, string? Lang = null)
		{
			Error = string.Empty;
			if (Field is ObservableDateField DateField && DateField.DateValue is DateTime Date)
			{
				if (this.min.HasValue && Date < this.min.Value)
					Error = this.message ?? $"{Field.Label?.Get(Lang) ?? Field.Id} must be on or after {this.min:yyyy-MM-dd}";
				else if (this.max.HasValue && Date > this.max.Value)
					Error = this.message ?? $"{Field.Label?.Get(Lang) ?? Field.Id} must be on or before {this.max:yyyy-MM-dd}";
			}
			return string.IsNullOrEmpty(Error);
		}
	}

	/// <summary>
	/// Validates integer and decimal ranges.
	/// </summary>
	public class NumericRangeRule : IKycRule
	{
		private readonly decimal? min;
		private readonly decimal? max;
		private readonly string? message;

		public NumericRangeRule(decimal? Min, decimal? Max, string? Message)
		{
			this.min = Min;
			this.max = Max;
			this.message = Message;
		}

		public bool Validate(ObservableKycField Field, out string Error, string? Lang = null)
		{
			Error = string.Empty;
			decimal? Value = Field.FieldType switch
			{
				FieldType.Integer => Field is ObservableIntegerField IntField ? IntField.IntValue : null,
				FieldType.Decimal => Field is ObservableDecimalField DecField ? DecField.DecimalValue : null,
				_ => null
			};
			if (Value is decimal Dec)
			{
				if (this.min.HasValue && Dec < this.min.Value)
					Error = this.message ?? $"{Field.Label?.Get(Lang) ?? Field.Id} must be at least {this.min}";
				else if (this.max.HasValue && Dec > this.max.Value)
					Error = this.message ?? $"{Field.Label?.Get(Lang) ?? Field.Id} must be at most {this.max}";
			}
			return string.IsNullOrEmpty(Error);
		}
	}

	/// <summary>
	/// Validates email format using a simple regex.
	/// </summary>
	public class EmailRule : IKycRule
	{
		private static readonly Regex emailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

		private readonly string? message;

		public EmailRule(string? Message = null) => this.message = Message;

		public bool Validate(ObservableKycField Field, out string Error, string? Lang = null)
		{
			Error = string.Empty;
			if (Field.FieldType == FieldType.Email && Field.StringValue is string Email && !string.IsNullOrEmpty(Email))
			{
				if (!emailRegex.IsMatch(Email))
					Error = this.message ?? $"{Field.Label?.Get(Lang) ?? Field.Id} must be a valid email address";
			}
			return string.IsNullOrEmpty(Error);
		}
	}

	/// <summary>
	/// Validates phone format using a simple international regex.
	/// </summary>
	public partial class PhoneRule : IKycRule
	{
		// You can use a more complex regex/library for real world
		private static readonly Regex phoneRegex = new(@"^\+?[1-9]\d{7,15}$", RegexOptions.Compiled);
		private readonly string? message;

		public PhoneRule(string? Message = null) => this.message = Message;

		public bool Validate(ObservableKycField Field, out string Error, string? Lang = null)
		{
			Error = string.Empty;
			if (Field.FieldType == FieldType.Phone && Field.StringValue is string Phone && !string.IsNullOrEmpty(Phone))
			{
				if (!phoneRegex.IsMatch(Phone))
					Error = this.message ?? $"{Field.Label?.Get(Lang) ?? Field.Id} must be a valid phone number";
			}
			return string.IsNullOrEmpty(Error);
		}
	}

	/// <summary>
	/// Ensures the value is a supported country code.
	/// </summary>
	public class CountryRule : IKycRule
	{
		private readonly HashSet<string> validCountries;
		private readonly string? message;

		public CountryRule(IEnumerable<string> AllowedCountries, string? Message = null)
		{
			this.validCountries = new HashSet<string>(AllowedCountries, StringComparer.OrdinalIgnoreCase);
			this.message = Message;
		}

		public bool Validate(ObservableKycField Field, out string Error, string? Lang = null)
		{
			Error = string.Empty;
			if (Field.FieldType == FieldType.Country && Field is ObservableCountryField CountryField && !string.IsNullOrEmpty(CountryField.CountryCode))
			{
				if (!this.validCountries.Contains(CountryField.CountryCode))
					Error = this.message ?? $"{Field.Label?.Get(Lang) ?? Field.Id} is not a valid country";
			}
			return string.IsNullOrEmpty(Error);
		}
	}

	/// <summary>
	/// Validates file field (size/type) - implement details as needed.
	/// </summary>
	public class FileRule : IKycRule
	{
		private readonly long? maxSizeBytes;
		private readonly string[] allowedExtensions;
		private readonly string? message;

		public FileRule(long? MaxSizeBytes, string[] AllowedExtensions, string? Message = null)
		{
			this.maxSizeBytes = MaxSizeBytes;
			this.allowedExtensions = AllowedExtensions ?? Array.Empty<string>();
			this.message = Message;
		}

		public bool Validate(ObservableKycField Field, out string Error, string? Lang = null)
		{
			// File validation implementation goes here as needed (pseudo-code):
			// - check file exists, size, extension...
			Error = string.Empty;
			return true;
		}
	}
}
