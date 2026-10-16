using NeuroFeatures;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Wallet
{
    /// <summary>
    /// Orchestrates eDaler and token operations.
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
        /// Opens a token from outside the wallet, such as a chat message, a link or a QR code. The token is always fetched
        /// from its Neuron first. When that fails, a shared copy is shown as an unverified copy, and without a copy the
        /// user is told why the token couldn't be opened.
        /// </summary>
        /// <param name="TokenId">Token ID.</param>
        /// <param name="SharedCopy">A copy of the token that someone shared, if any.</param>
        /// <returns>If token details were opened.</returns>
        Task<bool> OpenTokenAsync(string TokenId, Token? SharedCopy);

        /// <summary>
        /// Opens the token a notification is about. A token the user owns opens in token details. Otherwise My Tokens
        /// opens, with a message saying why the token isn't shown.
        /// </summary>
        /// <param name="TokenId">Token ID, if the notification has one.</param>
        Task OpenTokenFromNotificationAsync(string? TokenId);
    }
}
