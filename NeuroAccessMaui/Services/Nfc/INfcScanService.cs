using System.Threading;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Nfc
{
    /// <summary>
    /// Provides a user-initiated NFC scanning workflow.
    /// </summary>
    /// <remarks>
    /// This service is used to unify NFC and QR ingestion paths: UI flows can request a scan and await a URI.
    /// Platform-specific implementations may either start an active reader session (iOS) or rely on platform
    /// intent delivery (Android) and complete the pending scan when a matching NFC tag is detected.
    /// </remarks>
    [DefaultImplementation(typeof(NfcScanService))]
    public interface INfcScanService
    {
        /// <summary>
        /// Gets a value indicating whether an NFC scan is currently active.
        /// </summary>
        bool HasActiveScan { get; }

        /// <summary>
        /// Starts an NFC scan and returns the first detected URI matching an optional allow-list.
        /// </summary>
        /// <param name="Prompt">User-facing prompt to show while scanning.</param>
        /// <param name="AllowedSchemes">Optional URI schemes to accept. If null or empty, any absolute URI is accepted.</param>
        /// <param name="CancellationToken">Cancellation token.</param>
        /// <returns>The detected absolute URI, or null if the scan was canceled or no supported tag was found.</returns>
        Task<string?> ScanUriAsync(string Prompt, string[]? AllowedSchemes, CancellationToken CancellationToken);

        /// <summary>
        /// Offers a detected URI to any currently active scan.
        /// </summary>
        /// <param name="Uri">Detected absolute URI.</param>
        /// <returns>True if the URI was accepted and completed an active scan; otherwise false.</returns>
        bool TryHandleDetectedUri(string Uri);
    }
}
