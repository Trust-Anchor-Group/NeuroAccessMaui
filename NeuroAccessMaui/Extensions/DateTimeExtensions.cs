using System;

namespace NeuroAccessMaui.Extensions
{
	/// <summary>
	/// Extensions for the <see cref="DateTime"/> class.
	/// </summary>
	public static class DateTimeExtensions
	{
		/// <summary>
		/// Returns the actual date, if it is non-null, or <c>null</c> if it is <see cref="DateTime.MinValue"/>.
		/// </summary>
		/// <param name="date">The date to check.</param>
		/// <returns>A DateTime, or null</returns>
		public static DateTime? GetDateOrNullIfMinValue(this DateTime? date)
		{
			if (!date.HasValue)
				return null;
			return GetDateOrNullIfMinValue(date.Value);
		}

		/// <summary>
		/// Returns the actual date if it has a valid value, or <c>null</c> if it is <see cref="DateTime.MinValue"/>.
		/// </summary>
		/// <param name="date">The date to check.</param>
		/// <returns>A DateTime, or null</returns>
		public static DateTime? GetDateOrNullIfMinValue(this DateTime date)
		{
			if (date == DateTime.MinValue)
				return null;
			return date;
		}

		/// <summary>
		/// Clamps a date to a safe range to avoid <see cref="System.ArgumentOutOfRangeException"/> when MAUI converts it to <see cref="System.DateTimeOffset"/> with a timezone offset.
		/// </summary>
		/// <param name="date">Input date.</param>
		/// <param name="safeMin">Lower bound (inclusive).</param>
		/// <param name="safeMax">Upper bound (inclusive).</param>
		/// <returns>Clamped date in range [safeMin, safeMax]. If input is <see cref="DateTime.MinValue"/> or <see cref="DateTime.MaxValue"/>, substitutes safe bounds.</returns>
		public static DateTime SanitizeForDatePicker(this DateTime date, DateTime safeMin, DateTime safeMax)
		{
			if (date <= safeMin || date == DateTime.MinValue)
				return safeMin;
			if (date >= safeMax || date == DateTime.MaxValue)
				return safeMax;
			return date;
		}

		/// <summary>
		/// Clamps a nullable date to a safe range, preserving <c>null</c> if no value is present.
		/// </summary>
		/// <param name="date">Nullable input date.</param>
		/// <param name="safeMin">Lower bound (inclusive).</param>
		/// <param name="safeMax">Upper bound (inclusive).</param>
		/// <returns>
		/// <c>null</c> if <paramref name="date"/> is <c>null</c>, otherwise a clamped date in range [safeMin, safeMax].
		/// </returns>
		public static DateTime? SanitizeForDatePicker(this DateTime? date, DateTime safeMin, DateTime safeMax)
		{
			if (!date.HasValue)
				return null;

			return date.Value.SanitizeForDatePicker(safeMin, safeMax);
		}
	}
}
