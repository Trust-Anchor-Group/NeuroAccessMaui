using NeuroAccessMaui.Services.Kyc.Models;

namespace NeuroAccessMaui.Services.Kyc.ViewModels
{
    /// <summary>
    /// Generic field for text, phone, email, etc.
    /// </summary>
    public class ObservableGenericField : ObservableKycField
    {
        public override string? StringValue
        {
            get => this.RawValue as string;
            set => this.RawValue = value;
        }
    }
}
