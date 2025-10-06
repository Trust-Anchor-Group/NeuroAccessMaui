using NeuroAccessMaui.Services.Kyc.Models;

namespace NeuroAccessMaui.UI.Controls
{
    public class SelectableKycOption : SelectableOption<KycOption>
    {
        public SelectableKycOption(KycOption option, bool isSelected = false, Action<SelectableOption<KycOption>>? toggleAction = null)
            : base(option, isSelected, toggleAction) { }
    }
}
