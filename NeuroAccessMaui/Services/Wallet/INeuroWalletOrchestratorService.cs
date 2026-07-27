using Waher.Runtime.Inventory;

using NeuroFeatures;

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

        /// <summary>
        /// Opens a token through the canonical token-detail navigation path.
        /// </summary>
        /// <param name="TokenId">Identifier of the token to open.</param>
        /// <param name="InitialToken">Optional token data that has already been loaded.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task OpenTokenAsync(string TokenId, Token? InitialToken = null);
    }
}
