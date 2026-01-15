using System.Threading;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Nfc
{
    /// <summary>
    /// Provides a user-initiated NFC passport scan flow.
    /// </summary>
    /// <remarks>
    /// This service is responsible for initiating an NFC tag scan session (especially on iOS where NFC reads
    /// must be started by the app) and performing the initial ISO-DEP/APDU exchange needed for ePassport reading.
    /// </remarks>
    [DefaultImplementation(typeof(PassportNfcScanService))]
    public interface IPassportNfcScanService
    {
        /// <summary>
        /// Scans an ePassport chip using NFC.
        /// </summary>
        /// <param name="Prompt">Prompt text shown by the platform NFC UI.</param>
        /// <param name="CancellationToken">Cancellation token.</param>
        /// <returns>
        /// A task that returns <c>true</c> if a compatible tag was detected and the session completed without fatal errors;
        /// otherwise <c>false</c>.
        /// </returns>
        Task<bool> ScanPassportAsync(string Prompt, CancellationToken CancellationToken);
    }
}
