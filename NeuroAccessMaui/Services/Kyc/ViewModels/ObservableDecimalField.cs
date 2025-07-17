using System.Globalization;
using NeuroAccessMaui.Services.Kyc.Models;

namespace NeuroAccessMaui.Services.Kyc.ViewModels
{
    /// <summary>
    /// Decimal field
    /// </summary>
    public class ObservableDecimalField : ObservableKycField
    {
        public override string? StringValue
        {
            get => this.DecimalValue?.ToString(CultureInfo.InvariantCulture);
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    this.DecimalValue = null;
                }
                else if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal dv))
                {
                    this.DecimalValue = dv;
                }
            }
        }

        public decimal? DecimalValue
        {
            get => this.RawValue as decimal?;
            set => this.RawValue = value;
        }
    }
}
