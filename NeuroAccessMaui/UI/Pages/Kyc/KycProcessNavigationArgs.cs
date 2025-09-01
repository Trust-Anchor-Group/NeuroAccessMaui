using NeuroAccessMaui.Services.Kyc;
using NeuroAccessMaui.Services.UI;

namespace NeuroAccessMaui.UI.Pages.Kyc
{
    /// <summary>
    /// Navigation arguments for the KYC process page.
    /// Carries the KycReference to edit/continue.
    /// </summary>
    public class KycProcessNavigationArgs : NavigationArgs
    {
        public KycProcessNavigationArgs()
        {
        }

        public KycProcessNavigationArgs(KycReference reference)
        {
            this.Reference = reference;
        }

        /// <summary>
        /// KYC reference to load.
        /// </summary>
        public KycReference? Reference { get; }
    }
}

