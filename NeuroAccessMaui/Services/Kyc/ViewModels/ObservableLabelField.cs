using NeuroAccessMaui.Services.Kyc.Models;

namespace NeuroAccessMaui.Services.Kyc.ViewModels
{
    /// <summary>
    /// Label/info field (non-input)
    /// </summary>
    public class ObservableLabelField : ObservableKycField
    {
        public override string? StringValue
        {
            get => null;
            set { /* ignore */ }
        }
    }
}
