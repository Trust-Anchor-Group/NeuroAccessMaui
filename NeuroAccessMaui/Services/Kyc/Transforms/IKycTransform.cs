using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using NeuroAccessMaui.Services.Kyc.Models;
using NeuroAccessMaui.Services.Kyc.ViewModels;
using NeuroAccessMaui.Services.Data.PersonalNumbers;

namespace NeuroAccessMaui.Services.Kyc.Transforms
{
	/// <summary>
	/// Contract for KYC mapping transforms. Transforms can normalize, format or derive new values.
	/// </summary>
	public interface IKycTransform
	{
		/// <summary>
		/// Unique transform name (case-insensitive in registry).
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Applies the transform to the current value.
		/// </summary>
		/// <param name="field">Originating field.</param>
		/// <param name="process">Owning KYC process.</param>
		/// <param name="currentValue">Current pipeline value (never null, may be empty).</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>Transformed value (may be empty to indicate removal).</returns>
		Task<string> ApplyAsync(ObservableKycField field, KycProcess process, string currentValue, CancellationToken ct);
	}

	/// <summary>
	/// Transform registry (static). Register new transforms at startup or via static ctor.
	/// </summary>
	public static class KycTransformRegistry
	{
		private static readonly Dictionary<string, IKycTransform> transforms = new(StringComparer.OrdinalIgnoreCase);
		private static bool initialized;

		private static void EnsureInit()
		{
			if (initialized)
				return;

			initialized = true;
			// Built-ins
			Register(new TrimTransform());
			Register(new UppercaseTransform());
			Register(new LowercaseTransform());
			Register(new YearTransform());
			Register(new MonthTransform());
			Register(new DayTransform());
			Register(new PersonalNumberNormalizeTransform());
		}

		public static void Register(IKycTransform transform)
		{
			transforms[transform.Name] = transform;
		}

		public static bool TryGet(string name, out IKycTransform transform)
		{
			EnsureInit();
			return transforms.TryGetValue(name, out transform!);
		}
	}

	#region Built-in transforms

	internal abstract class SimpleFuncTransform : IKycTransform
	{
		protected SimpleFuncTransform(string name) => this.Name = name;
		public string Name { get; }
		public abstract Task<string> ApplyAsync(ObservableKycField field, KycProcess process, string currentValue, CancellationToken ct);
	}

	internal sealed class TrimTransform : SimpleFuncTransform
	{
		public TrimTransform() : base("trim") { }
		public override Task<string> ApplyAsync(ObservableKycField field, KycProcess process, string currentValue, CancellationToken ct) => Task.FromResult(currentValue.Trim());
	}

	internal sealed class UppercaseTransform : SimpleFuncTransform
	{
		public UppercaseTransform() : base("uppercase") { }
		public override Task<string> ApplyAsync(ObservableKycField field, KycProcess process, string currentValue, CancellationToken ct) => Task.FromResult(currentValue.ToUpperInvariant());
	}

	internal sealed class LowercaseTransform : SimpleFuncTransform
	{
		public LowercaseTransform() : base("lowercase") { }
		public override Task<string> ApplyAsync(ObservableKycField field, KycProcess process, string currentValue, CancellationToken ct) => Task.FromResult(currentValue.ToLowerInvariant());
	}

	internal sealed class YearTransform : SimpleFuncTransform
	{
		public YearTransform() : base("year") { }
		public override Task<string> ApplyAsync(ObservableKycField field, KycProcess process, string currentValue, CancellationToken ct)
		{
			return Task.FromResult(DateTime.TryParse(currentValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt) ? dt.Year.ToString(CultureInfo.InvariantCulture) : string.Empty);
		}
	}

	internal sealed class MonthTransform : SimpleFuncTransform
	{
		public MonthTransform() : base("month") { }
		public override Task<string> ApplyAsync(ObservableKycField field, KycProcess process, string currentValue, CancellationToken ct)
		{
			return Task.FromResult(DateTime.TryParse(currentValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt) ? dt.Month.ToString(CultureInfo.InvariantCulture) : string.Empty);
		}
	}

	internal sealed class DayTransform : SimpleFuncTransform
	{
		public DayTransform() : base("day") { }
		public override Task<string> ApplyAsync(ObservableKycField field, KycProcess process, string currentValue, CancellationToken ct)
		{
			return Task.FromResult(DateTime.TryParse(currentValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt) ? dt.Day.ToString(CultureInfo.InvariantCulture) : string.Empty);
		}
	}

	internal sealed class PersonalNumberNormalizeTransform : SimpleFuncTransform
	{
		public PersonalNumberNormalizeTransform() : base("personalNumberNormalize") { }
		public override async Task<string> ApplyAsync(ObservableKycField field, KycProcess process, string currentValue, CancellationToken ct)
		{
			if (string.IsNullOrWhiteSpace(currentValue))
				return currentValue;

			string countryCode = string.Empty;
			try
			{
				// Try explicit country field id patterns first
				string? fromValues = process.Values.Where(kv => kv.Key.Contains("country", StringComparison.OrdinalIgnoreCase))
					.Select(kv => kv.Value)
					.FirstOrDefault(v => !string.IsNullOrEmpty(v));
				if (!string.IsNullOrEmpty(fromValues))
					countryCode = fromValues!;
				if (string.IsNullOrEmpty(countryCode))
					countryCode = ServiceRef.TagProfile.SelectedCountry ?? ServiceRef.TagProfile.LegalIdentity?.GetPersonalInformation().Country ?? string.Empty;
			}
			catch (Exception)
			{
				countryCode = string.Empty;
			}

			if (string.IsNullOrEmpty(countryCode))
				return currentValue;

			try
			{
				NumberInformation info = await PersonalNumberSchemes.Validate(countryCode, currentValue);
				if (info.IsValid == true && !string.IsNullOrEmpty(info.PersonalNumber))
					return info.PersonalNumber;
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
			return currentValue; // fallback
		}
	}

	#endregion
}
