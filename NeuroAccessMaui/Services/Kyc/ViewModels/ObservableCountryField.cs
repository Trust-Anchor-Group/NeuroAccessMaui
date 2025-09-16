using NeuroAccessMaui.Services.Data;
using NeuroAccessMaui.Services.Kyc.Models;

namespace NeuroAccessMaui.Services.Kyc.ViewModels
{
    /// <summary>
    /// Country field (ISO code)
    /// </summary>
    public class ObservableCountryField : ObservableKycField
    {
        public override string? StringValue
        {
            get => this.CountryCode;
            set => this.CountryCode = value;
        }

        public string? CountryCode
        {
            get
			{
				if (this.RawValue is KycOption Option)
					return Option.Value;

				return null;
			}
			set => this.RawValue = value;
		}
    }
}
