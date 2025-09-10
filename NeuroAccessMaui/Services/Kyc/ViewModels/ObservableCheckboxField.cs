using System.Linq;
using NeuroAccessMaui.Services.Kyc.Models;

namespace NeuroAccessMaui.Services.Kyc.ViewModels
{
    /// <summary>
    /// Checkbox field (multi-selection)
    /// </summary>
    public class ObservableCheckboxField : ObservableKycField
    {
        /// <summary>
        /// Gets or sets the value as a comma-separated list of selected option values.
        /// </summary>
        public override string? StringValue
        {
            get => string.Join(",", this.SelectedOptions.Select(o => o.Value));
            set
            {
                this.SelectedOptions.Clear();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    string[] OptionValues = value.Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);
                    foreach (string OptionValue in OptionValues)
                    {
                        KycOption? Opt = this.Options.FirstOrDefault(o => o.Value == OptionValue);
						MainThread.BeginInvokeOnMainThread(()=>
						{
							if (Opt is not null)
								this.SelectedOptions.Add(Opt);
						});
					}
                }
            }
        }
    }
}
