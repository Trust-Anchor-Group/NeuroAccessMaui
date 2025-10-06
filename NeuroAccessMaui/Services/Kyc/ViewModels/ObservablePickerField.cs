using NeuroAccessMaui.Services.Kyc.Models;
using System.Linq;

namespace NeuroAccessMaui.Services.Kyc.ViewModels
{
    /// <summary>
    /// Picker field (single option selection)
    /// </summary>
    public class ObservablePickerField : ObservableKycField
    {
        public override string? StringValue
        {
            get => this.SelectedOption?.Value;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    this.SelectedOption = null;
                }
                else
                {
                    this.SelectedOption = this.Options.FirstOrDefault(o => o.Value == value);
                }
            }
        }

        public KycOption? SelectedOption
        {
            get => this.RawValue as KycOption;
            set
            {
                if (value is not null)
                    this.RawValue = value;
            }
        }
    }
}
