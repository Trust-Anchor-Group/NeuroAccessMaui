using System.Globalization;
using NeuroAccessMaui.Services.Kyc.Models;

namespace NeuroAccessMaui.Services.Kyc.ViewModels
{
    /// <summary>
    /// Integer field
    /// </summary>
    public class ObservableIntegerField : ObservableKycField
    {
        public override string? StringValue
        {
            get => this.IntValue?.ToString(CultureInfo.InvariantCulture);
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    this.IntValue = null;
                }
                else if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int iv))
                {
                    this.IntValue = iv;
                }
            }
        }

        public int? IntValue
        {
            get => this.RawValue as int?;
            set => this.RawValue = value;
        }
    }
}
