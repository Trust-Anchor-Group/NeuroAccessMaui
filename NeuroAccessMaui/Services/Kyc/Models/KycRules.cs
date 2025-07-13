using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace NeuroAccessMaui.Services.Kyc.Models
{
	public interface IKycRule
	{
		bool Validate(KycField Field, out string Error, string? Lang = null);
	}

	/// <summary>
	/// Ensures the field has a value if required.
	/// </summary>
	public class RequiredRule : IKycRule
	{
		public bool Validate(KycField Field, out string Error, string? Lang = null)
		{
			Error = string.Empty;
			if (!Field.Required)
				return true;

			string Label = Field.Label?.Get(Lang) ?? Field.Id;
			bool Missing = Field.FieldType switch
			{
				FieldType.Date => Field.DateValue is null,
				FieldType.Picker => Field.SelectedOption is null,
				FieldType.Radio => Field.SelectedOption is null,
				FieldType.Boolean => Field.BoolValue != true,
				FieldType.Checkbox => Field.SelectedOptions == null || Field.SelectedOptions.Count == 0,
				FieldType.File => Field.Value is not string s || string.IsNullOrEmpty(s), // file path or token
				FieldType.Email => string.IsNullOrEmpty(Field.Value),
				FieldType.Phone => string.IsNullOrEmpty(Field.Value),
				FieldType.Integer => Field.IntValue is null,
				FieldType.Decimal => Field.DecimalValue is null,
				FieldType.Country => string.IsNullOrEmpty(Field.CountryCode),
				FieldType.Label or FieldType.Info => false, // info/display only, never required
				_ => string.IsNullOrEmpty(Field.Value)
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

		public bool Validate(KycField Field, out string Error, string? Lang = null)
		{
			Error = string.Empty;
			if (Field.Value is string Text)
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

		public bool Validate(KycField Field, out string Error, string? Lang = null)
		{
			Error = string.Empty;
			if (Field.Value is string Text && !this.regex.IsMatch(Text))
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

		public bool Validate(KycField Field, out string Error, string? Lang = null)
		{
			Error = string.Empty;
			if (Field.DateValue is DateTime Date)
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

		public bool Validate(KycField Field, out string Error, string? Lang = null)
		{
			Error = string.Empty;
			decimal? val = Field.FieldType switch
			{
				FieldType.Integer => Field.IntValue,
				FieldType.Decimal => Field.DecimalValue,
				_ => null
			};
			if (val is decimal d)
			{
				if (this.min.HasValue && d < this.min.Value)
					Error = this.message ?? $"{Field.Label?.Get(Lang) ?? Field.Id} must be at least {this.min}";
				else if (this.max.HasValue && d > this.max.Value)
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
		private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

		private readonly string? message;

		public EmailRule(string? Message = null) => this.message = Message;

		public bool Validate(KycField Field, out string Error, string? Lang = null)
		{
			Error = string.Empty;
			if (Field.FieldType == FieldType.Email && Field.Value is string s && !string.IsNullOrEmpty(s))
			{
				if (!EmailRegex.IsMatch(s))
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

		public bool Validate(KycField Field, out string Error, string? Lang = null)
		{
			Error = string.Empty;
			if (Field.FieldType == FieldType.Phone && Field.Value is string s && !string.IsNullOrEmpty(s))
			{
				if (!phoneRegex.IsMatch(s))
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

		public bool Validate(KycField Field, out string Error, string? Lang = null)
		{
			Error = string.Empty;
			if (Field.FieldType == FieldType.Country && !string.IsNullOrEmpty(Field.CountryCode))
			{
				if (!this.validCountries.Contains(Field.CountryCode))
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

		public bool Validate(KycField Field, out string Error, string? Lang = null)
		{
			// File validation implementation goes here as needed (pseudo-code):
			// - check file exists, size, extension...
			Error = string.Empty;
			return true;
		}
	}
}
