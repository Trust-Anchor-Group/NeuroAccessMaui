using System;
using Microsoft.Maui.Controls;
using NeuroAccessMaui.Extensions;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// DatePicker wrapper preventing redundant reentrant Date updates and guarding against invalid extreme dates
	/// that crash MAUI on some time zones. Updated for nullable DateTime? in .NET MAUI 10.
	/// </summary>
	public class WrappedDatePicker : DatePicker, IDatePicker
	{
		private bool settingDate;

		// Safe bounds used to avoid DateTimeOffset validation exceptions (year 0 / >10000 after offset math).
		private static readonly DateTime SafeMinDate = new DateTime(1900, 1, 1);
		private static readonly DateTime SafeMaxDate = new DateTime(2100, 12, 31);

		// NOTE: IDatePicker.Date is now DateTime?
		DateTime? IDatePicker.Date
		{
			get => this.Date;
			set
			{
				if (this.settingDate)
					return;

				// Allow "no selection"
				if (value is null)
				{
					if (this.Date is null)
						return; // no change

					this.settingDate = true;
					try
					{
						this.Date = null;
					}
					finally
					{
						this.settingDate = false;
					}

					return;
				}

				// Non-null value -> sanitize & clamp
				DateTime sanitized = value.Value.SanitizeForDatePicker(SafeMinDate, SafeMaxDate);

				// If current date has same non-null value, skip
				if (this.Date.HasValue && this.Date.Value == sanitized)
					return;

				this.settingDate = true;
				try
				{
					// Ensure bounds before assigning.
					if (!this.MinimumDate.HasValue || this.MinimumDate.Value < SafeMinDate)
						this.MinimumDate = SafeMinDate;

					if (!this.MaximumDate.HasValue || this.MaximumDate.Value > SafeMaxDate)
						this.MaximumDate = SafeMaxDate;

					if (sanitized < this.MinimumDate)
						sanitized = this.MinimumDate.Value;
					else if (sanitized > this.MaximumDate)
						sanitized = this.MaximumDate.Value;

					this.Date = sanitized; // nullable DateTime?
				}
				finally
				{
					this.settingDate = false;
				}
			}
		}

		/// <summary>
		/// When handler is created, normalize min/max to safe values to prevent implicit DateTimeOffset construction errors.
		/// </summary>
		protected override void OnHandlerChanged()
		{
			base.OnHandlerChanged();

			// Clamp bounds once handler exists.
			if (!this.MinimumDate.HasValue || this.MinimumDate == DateTime.MinValue || this.MinimumDate < SafeMinDate)
				this.MinimumDate = SafeMinDate;

			if (!this.MaximumDate.HasValue || this.MaximumDate == DateTime.MaxValue || this.MaximumDate > SafeMaxDate)
				this.MaximumDate = SafeMaxDate;

			// Clamp current date to bounds, if present.
			if (this.Date is DateTime currentValue)
			{
				DateTime current = currentValue.SanitizeForDatePicker(SafeMinDate, SafeMaxDate);

				if (current < this.MinimumDate)
					current = this.MinimumDate.Value;
				else if (current > this.MaximumDate)
					current = this.MaximumDate.Value;

				if (current != currentValue)
					this.Date = current;
			}
		}
	}
}
