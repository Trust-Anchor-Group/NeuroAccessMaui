using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Wallet
{
    /// <summary>
    /// Orchestrates eDaler operations.
    /// </summary>
    [DefaultImplementation(typeof(NeuroWalletOrchestratorService))]
    public interface INeuroWalletOrchestratorService : ILoadableService
    {
        /// <summary>
        /// Opens the eDaler wallet
        /// </summary>
        Task OpenEDalerWallet();

        /// <summary>
        /// eDaler URI scanned.
        /// </summary>
        /// <param name="Uri">eDaler URI.</param>
        Task OpenEDalerUri(string Uri);

        /// <summary>
        /// Neuro-Feature URI scanned.
        /// </summary>
        /// <param name="Uri">Neuro-Feature URI.</param>
        Task OpenNeuroFeatureUri(string Uri);
    }
}
