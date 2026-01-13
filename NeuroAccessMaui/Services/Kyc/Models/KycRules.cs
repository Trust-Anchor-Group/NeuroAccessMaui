using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.Data.PersonalNumbers;
using NeuroAccessMaui.Services.Kyc.ViewModels;

namespace NeuroAccessMaui.Services.Kyc.Models
{
    /// <summary>
    /// Contract for synchronous KYC field validation rules.
    /// </summary>
    public interface IKycRule
    {
        /// <summary>
        /// Validates a field and returns a result.
        /// </summary>
        /// <param name="Field">Field to validate.</param>
        /// <param name="process">Owning process, if any.</param>
        /// <param name="Error">Receives localized error message on failure.</param>
        /// <param name="Lang">Preferred language for error messages.</param>
        /// <returns>True if valid; otherwise false.</returns>
        bool Validate(ObservableKycField Field, KycProcess? process, out string Error, string? Lang = null);
    }

    /// <summary>
    /// Contract for asynchronous KYC field validation rules.
    /// </summary>
    public interface IAsyncKycRule: IKycRule
    {
        /// <summary>
        /// Asynchronously validates a field and returns a result and optional error message.
        /// </summary>
        /// <param name="field">Field to validate.</param>
        /// <param name="process">Owning process, if any.</param>
        /// <param name="lang">Preferred language for error messages.</param>
        /// <returns>Tuple containing validation result and optional error message.</returns>
        Task<(bool Ok, string? Error)> ValidateAsync(ObservableKycField field, KycProcess? process, string? lang = null);
    }

	/// <summary>
	/// Ensures the field has a value if required.
	/// </summary>
	public class RequiredRule : IKycRule
	{
		public bool Validate(ObservableKycField Field, KycProcess? process, out string Error, string? Lang = null)
		{
			Error = string.Empty;
			if (!Field.Required)
				return true;

			Lang ??= CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

			string Label = Field.Label?.Get(Lang) ?? Field.Id;
			bool Missing = Field.FieldType switch
			{
				FieldType.Date => Field is ObservableDateField DateField && DateField.DateValue is null,
				FieldType.Picker => Field is ObservablePickerField PickerField && PickerField.SelectedOption is null,
				FieldType.Gender => Field is ObservablePickerField GenderField && GenderField.SelectedOption is null,
				FieldType.Radio => Field is ObservableRadioField RadioField && RadioField.SelectedOption is null,
				FieldType.Boolean => Field is ObservableBooleanField BoolField && BoolField.BoolValue != true,
				// Consider both UI selection list and serialized StringValue (populated during deserialization).
				FieldType.Checkbox => Field is ObservableCheckboxField CheckboxField &&
					(CheckboxField.SelectedOptions == null || CheckboxField.SelectedOptions.Count == 0) && string.IsNullOrEmpty(Field.StringValue),
				FieldType.File => Field is ObservableFileField FileField && (FileField.StringValue is not string FileValue || string.IsNullOrEmpty(FileValue)),
				FieldType.Email or FieldType.Phone or FieldType.Text => string.IsNullOrEmpty(Field.StringValue),
				FieldType.Integer => Field is ObservableIntegerField IntField && IntField.IntValue is null,
				FieldType.Decimal => Field is ObservableDecimalField DecField && DecField.DecimalValue is null,
				FieldType.Country => Field is ObservableCountryField CountryField && string.IsNullOrEmpty(CountryField.CountryCode),
				FieldType.Label or FieldType.Info => false, // info/display only, never required
				_ => string.IsNullOrEmpty(Field.StringValue)
			};

			if (Missing)
				Error = ServiceRef.Localizer[nameof(AppResources.IsRequired), Label];
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

		public bool Validate(ObservableKycField Field, KycProcess? process, out string Error, string? Lang = null)
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

		public bool Validate(ObservableKycField Field, KycProcess? process, out string Error, string? Lang = null)
		{
			Error = string.Empty;
			string? Text = Field.StringValue;
			if (!string.IsNullOrEmpty(Text) && !this.regex.IsMatch(Text))
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

		public bool Validate(ObservableKycField Field, KycProcess? process, out string Error, string? Lang = null)
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

		public bool Validate(ObservableKycField Field, KycProcess? process, out string Error, string? Lang = null)
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

		public bool Validate(ObservableKycField Field, KycProcess? process, out string Error, string? Lang = null)
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

		public bool Validate(ObservableKycField Field, KycProcess? process, out string Error, string? Lang = null)
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
	/// Validates Personal number format (no normalization side-effects; normalization should be handled via transform).
	/// </summary>
	public partial class PersonalNumberRule(string? fieldRef, string? message) : IAsyncKycRule
	{
		private readonly string? fieldRef = fieldRef;
		private readonly string? message = message;

		public bool Validate(ObservableKycField Field, KycProcess? process, out string Error, string? Lang = null)
		{
			(bool Ok, string? Error) Result = this.ValidateAsync(Field, process, Lang).GetAwaiter().GetResult();
			Error = Result.Error ?? string.Empty;
			return Result.Ok;
		}

		public async Task<(bool Ok, string? Error)> ValidateAsync(ObservableKycField Field, KycProcess? Process, string? Lang = null)
		{
			string Error = string.Empty;
			string CountryCode;

			if (Process is null)
				return (true, null);

			if (Field.FieldType == FieldType.Text && Field.StringValue is string Pnr && !string.IsNullOrEmpty(Pnr))
			{
				if (!string.IsNullOrEmpty(this.fieldRef))
				{
					CountryCode = Process.Values.TryGetValue(this.fieldRef, out string? Cc) && !string.IsNullOrEmpty(Cc) ? Cc : string.Empty;
				}
				else
				{
					try
					{
						CountryCode = ServiceRef.TagProfile.SelectedCountry ??
						ServiceRef.TagProfile.LegalIdentity?.Properties?.FirstOrDefault(p =>p.Name.Equals(Constants.XmppProperties.Country, StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty;
					}
					catch (Exception)
					{
						CountryCode = string.Empty;
					}
				}

				if (string.IsNullOrEmpty(CountryCode))
				{
					return (true, string.Empty);
				}

				NumberInformation Info = await PersonalNumberSchemes.Validate(CountryCode, Pnr);

				if (Info.IsValid == false)
					Error = this.message ?? $"{Field.Label?.Get(Lang) ?? Field.Id} is not a valid personal number";
			}

			bool Ok = string.IsNullOrEmpty(Error);
			return (Ok, Error);
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

		public bool Validate(ObservableKycField Field, KycProcess? process, out string Error, string? Lang = null)
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

		public bool Validate(ObservableKycField Field, KycProcess? process, out string Error, string? Lang = null)
		{
			Error = string.Empty;
			return true;
		}
	}
}
