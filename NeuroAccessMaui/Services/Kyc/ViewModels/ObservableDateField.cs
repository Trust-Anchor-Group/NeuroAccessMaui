using System;
using System.Globalization;
using NeuroAccessMaui.Services.Kyc.Models;

namespace NeuroAccessMaui.Services.Kyc.ViewModels
{
    /// <summary>
    /// Date field
    /// </summary>
    public class ObservableDateField : ObservableKycField
    {
        public override string? StringValue
        {
            get => this.DateValue?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    this.DateValue = null;
                }
                else if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime Dt))
                {
                    this.DateValue = Dt;
                }
            }
        }

        public DateTime? DateValue
        {
            get => this.RawValue as DateTime?;
            set => this.RawValue = value;
        }
    }
}
