using System.Globalization;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Converters;
using NeuroAccessMaui.UI.Pages.Contracts.ObjectModel;
using Waher.Content;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Contracts
{
	/// <summary>
	/// Short, plain texts about contract parameters: why a value isn't accepted, and which values are allowed.
	/// </summary>
	/// <remarks>
	/// The contracts library gives the reason a value was rejected, but leaves the error text empty for most reasons.
	/// These texts are built from the reason and the parameter's limits instead.
	/// </remarks>
	public static class ParameterMessages
	{
		/// <summary>
		/// Gets the text that says why the parameter's value isn't accepted.
		/// </summary>
		/// <param name="Parameter">A parameter that failed validation.</param>
		/// <returns>The localized text.</returns>
		public static string GetErrorText(Parameter Parameter)
		{
			Limits Limits = GetLimits(Parameter);

			switch (Parameter.ErrorReason)
			{
				case ParameterErrorReason.LacksValue:
					return ServiceRef.Localizer[nameof(AppResources.ParameterFillThisIn)];

				case ParameterErrorReason.BelowMin:
					if (Limits.Min is null)
						break;

					if (Limits.IsTime)
					{
						return Limits.MinIncluded
							? ServiceRef.Localizer[nameof(AppResources.ParameterPickFrom), Limits.Min]
							: ServiceRef.Localizer[nameof(AppResources.ParameterPickAfter), Limits.Min];
					}

					return Limits.MinIncluded
						? ServiceRef.Localizer[nameof(AppResources.ParameterMustBeAtLeast), Limits.Min]
						: ServiceRef.Localizer[nameof(AppResources.ParameterMustBeMoreThan), Limits.Min];

				case ParameterErrorReason.AboveMax:
					if (Limits.Max is null)
						break;

					if (Limits.IsTime)
					{
						return Limits.MaxIncluded
							? ServiceRef.Localizer[nameof(AppResources.ParameterPickUntil), Limits.Max]
							: ServiceRef.Localizer[nameof(AppResources.ParameterPickBefore), Limits.Max];
					}

					return Limits.MaxIncluded
						? ServiceRef.Localizer[nameof(AppResources.ParameterMustBeAtMost), Limits.Max]
						: ServiceRef.Localizer[nameof(AppResources.ParameterMustBeLessThan), Limits.Max];

				case ParameterErrorReason.TooShort:
					if (Parameter is StringParameter { MinLength: int MinLength })
						return ServiceRef.Localizer[nameof(AppResources.ParameterTooShort), MinLength];
					break;

				case ParameterErrorReason.TooLong:
					if (Parameter is StringParameter { MaxLength: int MaxLength })
						return ServiceRef.Localizer[nameof(AppResources.ParameterTooLong), MaxLength];
					break;

				case ParameterErrorReason.RegularExpressionRejection:
					return ServiceRef.Localizer[nameof(AppResources.ParameterWrongFormat)];

				case ParameterErrorReason.ScriptExpressionRejection:
					return ServiceRef.Localizer[nameof(AppResources.ParameterNotAllowed)];

				case ParameterErrorReason.Outside:
					return ServiceRef.Localizer[nameof(AppResources.ParameterOutsideArea)];

				case ParameterErrorReason.InvalidReference:
				case ParameterErrorReason.UnableToGetContract:
					return ServiceRef.Localizer[nameof(AppResources.ParameterContractNotFound)];

				case ParameterErrorReason.InvalidContractNamespace:
				case ParameterErrorReason.InvalidTemplateId:
				case ParameterErrorReason.InvalidProvider:
					return ServiceRef.Localizer[nameof(AppResources.ParameterWrongContractKind)];

				case ParameterErrorReason.ContractNotValid:
					return ServiceRef.Localizer[nameof(AppResources.ParameterContractNotValid)];

				case ParameterErrorReason.Exception:
					return Parameter is CalcParameter
						? ServiceRef.Localizer[nameof(AppResources.ParameterCantCalculate)]
						: ServiceRef.Localizer[nameof(AppResources.ParameterNotAllowed)];
			}

			// A reason without limits to name, or no reason at all.
			return ServiceRef.Localizer[nameof(AppResources.ParameterWrongFormat)];
		}

		/// <summary>
		/// Gets a short hint about which values are allowed, such as "0 to 100" or "Up to 20 characters".
		/// </summary>
		/// <param name="Parameter">The parameter.</param>
		/// <returns>The localized hint, or an empty string when the parameter has no limits to show.</returns>
		public static string GetHint(Parameter Parameter)
		{
			if (Parameter is StringParameter StringParameter)
				return GetLengthHint(StringParameter.MinLength, StringParameter.MaxLength);

			Limits Limits = GetLimits(Parameter);

			if (Limits.Min is not null && Limits.Max is not null)
				return ServiceRef.Localizer[nameof(AppResources.ParameterRangeBetween), Limits.Min, Limits.Max];

			if (Limits.Min is not null)
			{
				if (Limits.IsTime)
				{
					return Limits.MinIncluded
						? ServiceRef.Localizer[nameof(AppResources.ParameterRangeFrom), Limits.Min]
						: ServiceRef.Localizer[nameof(AppResources.ParameterRangeAfter), Limits.Min];
				}

				return Limits.MinIncluded
					? ServiceRef.Localizer[nameof(AppResources.ParameterRangeAtLeast), Limits.Min]
					: ServiceRef.Localizer[nameof(AppResources.ParameterRangeAbove), Limits.Min];
			}

			if (Limits.Max is not null)
			{
				if (Limits.IsTime)
				{
					return Limits.MaxIncluded
						? ServiceRef.Localizer[nameof(AppResources.ParameterRangeUntil), Limits.Max]
						: ServiceRef.Localizer[nameof(AppResources.ParameterRangeBefore), Limits.Max];
				}

				return Limits.MaxIncluded
					? ServiceRef.Localizer[nameof(AppResources.ParameterRangeAtMost), Limits.Max]
					: ServiceRef.Localizer[nameof(AppResources.ParameterRangeBelow), Limits.Max];
			}

			return string.Empty;
		}

		/// <summary>
		/// Gets the hint for the length of a text.
		/// </summary>
		/// <param name="MinLength">The shortest length allowed, if any.</param>
		/// <param name="MaxLength">The longest length allowed, if any.</param>
		/// <returns>The localized hint, or an empty string.</returns>
		private static string GetLengthHint(int? MinLength, int? MaxLength)
		{
			if (MinLength.HasValue && MaxLength.HasValue)
				return ServiceRef.Localizer[nameof(AppResources.ParameterLengthRange), MinLength.Value, MaxLength.Value];

			if (MaxLength.HasValue)
				return ServiceRef.Localizer[nameof(AppResources.ParameterLengthAtMost), MaxLength.Value];

			if (MinLength.HasValue && MinLength.Value > 0)
				return ServiceRef.Localizer[nameof(AppResources.ParameterLengthAtLeast), MinLength.Value];

			return string.Empty;
		}

		/// <summary>
		/// Gets the limits of a parameter, formatted for the user.
		/// </summary>
		/// <param name="Parameter">The parameter.</param>
		/// <returns>The limits. Both are null when the parameter has none.</returns>
		private static Limits GetLimits(Parameter Parameter)
		{
			CultureInfo Culture = CultureInfo.CurrentCulture;

			return Parameter switch
			{
				NumericalParameter P => new Limits(
					P.Min is decimal Min ? ObservableParameter.FormatNumber(Min) : null, P.MinIncluded,
					P.Max is decimal Max ? ObservableParameter.FormatNumber(Max) : null, P.MaxIncluded,
					false),

				DateParameter P => new Limits(
					P.Min?.ToString("d", Culture), P.MinIncluded,
					P.Max?.ToString("d", Culture), P.MaxIncluded,
					true),

				DateTimeParameter P => new Limits(
					P.Min.HasValue ? ToLocal(P.Min.Value).ToString("g", Culture) : null, P.MinIncluded,
					P.Max.HasValue ? ToLocal(P.Max.Value).ToString("g", Culture) : null, P.MaxIncluded,
					true),

				TimeParameter P => new Limits(
					P.Min.HasValue ? DateTime.Today.Add(P.Min.Value).ToString("t", Culture) : null, P.MinIncluded,
					P.Max.HasValue ? DateTime.Today.Add(P.Max.Value).ToString("t", Culture) : null, P.MaxIncluded,
					true),

				DurationParameter P => new Limits(
					P.Min is Duration Min ? DurationToString.ToString(Min) : null, P.MinIncluded,
					P.Max is Duration Max ? DurationToString.ToString(Max) : null, P.MaxIncluded,
					false),

				StringParameter P => new Limits(
					string.IsNullOrEmpty(P.Min) ? null : P.Min, P.MinIncluded,
					string.IsNullOrEmpty(P.Max) ? null : P.Max, P.MaxIncluded,
					false),

				_ => new Limits(null, false, null, false, false)
			};
		}

		/// <summary>
		/// Shows a time in local time, unless it is already local or has no kind.
		/// </summary>
		/// <param name="Value">The time.</param>
		/// <returns>The local time.</returns>
		private static DateTime ToLocal(DateTime Value)
		{
			return Value.Kind == DateTimeKind.Utc ? Value.ToLocalTime() : Value;
		}

		/// <summary>
		/// The limits of a parameter, formatted for the user.
		/// </summary>
		/// <param name="Min">The lowest value allowed, if any.</param>
		/// <param name="MinIncluded">If the lowest value is allowed itself.</param>
		/// <param name="Max">The highest value allowed, if any.</param>
		/// <param name="MaxIncluded">If the highest value is allowed itself.</param>
		/// <param name="IsTime">If the values are dates or times, which are described as earlier and later.</param>
		private sealed record Limits(string? Min, bool MinIncluded, string? Max, bool MaxIncluded, bool IsTime);
	}
}
