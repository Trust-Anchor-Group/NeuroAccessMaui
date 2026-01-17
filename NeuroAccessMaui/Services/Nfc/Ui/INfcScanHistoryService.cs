using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NeuroAccessMaui.Services.Nfc;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Nfc.Ui
{
    /// <summary>
    /// Provides persistence and retrieval of NFC scan history.
    /// </summary>
    [DefaultImplementation(typeof(NfcScanHistoryService))]
    public interface INfcScanHistoryService
    {
        /// <summary>
        /// Occurs when the persisted history has changed.
        /// </summary>
        event EventHandler? HistoryChanged;

        /// <summary>
        /// Gets recent NFC scan history records.
        /// </summary>
        /// <param name="MaxCount">Maximum number of records to return.</param>
        /// <param name="CancellationToken">Cancellation token.</param>
        /// <returns>Recent records, ordered from newest to oldest.</returns>
        Task<IReadOnlyList<NfcScanHistoryRecord>> GetRecentAsync(int MaxCount, CancellationToken CancellationToken);

        /// <summary>
        /// Clears the NFC scan history.
        /// </summary>
        /// <param name="CancellationToken">Cancellation token.</param>
        Task ClearAsync(CancellationToken CancellationToken);
    }
}
