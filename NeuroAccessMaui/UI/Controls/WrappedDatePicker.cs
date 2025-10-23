using System;
using Microsoft.Maui.Controls;
using NeuroAccessMaui.Extensions;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// DatePicker wrapper preventing redundant reentrant Date updates and guarding against invalid extreme dates that crash MAUI on some time zones.
	/// </summary>
	public class WrappedDatePicker : DatePicker, IDatePicker
	{
		private bool settingDate;

		// Safe bounds used to avoid DateTimeOffset validation exceptions (year0/ >10000 after offset math).
		private static readonly DateTime SafeMinDate = new DateTime(1900,1,1);
		private static readonly DateTime SafeMaxDate = new DateTime(2100,12,31);

		DateTime IDatePicker.Date
		{
			get => this.Date;
			set
			{
				if (this.settingDate)
					return;

				DateTime Sanitized = value.SanitizeForDatePicker(SafeMinDate, SafeMaxDate);
				if (this.Date == Sanitized)
					return; // No change needed; avoids useless handler cycles.

				this.settingDate = true;
				try
				{
					// Ensure bounds before assigning.
					if (this.MinimumDate < SafeMinDate)
						this.MinimumDate = SafeMinDate;
					if (this.MaximumDate > SafeMaxDate)
						this.MaximumDate = SafeMaxDate;
					if (Sanitized < this.MinimumDate)
						Sanitized = this.MinimumDate;
					else if (Sanitized > this.MaximumDate)
						Sanitized = this.MaximumDate;
					this.Date = Sanitized; // Triggers normal MAUI property notifications.
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
			if (this.MinimumDate < SafeMinDate || this.MinimumDate == DateTime.MinValue)
				this.MinimumDate = SafeMinDate;
			if (this.MaximumDate > SafeMaxDate || this.MaximumDate == DateTime.MaxValue)
				this.MaximumDate = SafeMaxDate;

			// Clamp current date to bounds.
			DateTime Current = this.Date.SanitizeForDatePicker(SafeMinDate, SafeMaxDate);
			if (Current != this.Date)
				this.Date = Current;
		}
	}
}
