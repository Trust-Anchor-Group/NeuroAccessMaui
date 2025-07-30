using System.Linq;
using NeuroAccessMaui.Services.Kyc.Models;

namespace NeuroAccessMaui.Services.Kyc.ViewModels
{
    /// <summary>
    /// Checkbox field (multi-selection)
    /// </summary>
    public class ObservableCheckboxField : ObservableKycField
    {
        public override string? StringValue
        {
            get => string.Join(",", this.Options.Select(o => o.Value));
            set
            {
                this.SelectedOptions.Clear();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    string[] OptionValues = value.Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);
					for(int i = 0; i < OptionValues.Length; i++)
					{
						if (this.Options[i].Value == OptionValues[i]) this.SelectedOptions.Add(this.Options[i]);
					}
                }
            }
        }
    }
}
