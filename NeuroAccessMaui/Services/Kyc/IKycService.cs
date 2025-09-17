using System.Threading;
using System.Collections.Generic;
using NeuroAccessMaui.Services.Kyc.Models;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Kyc
{
	/// <summary>Service for loading KYC processes and performing validation &amp; data preparation.</summary>
	[DefaultImplementation(typeof(KycService))]
	public interface IKycService
	{
		/// <summary>Loads (or creates) the persisted KYC reference and ensures process XML is available.</summary>
		/// <param name="Lang">Optional language code.</param>
		Task<KycReference> LoadKycReferenceAsync(string? Lang = null);

		Task SaveKycReferenceAsync(KycReference Reference);

		/// <summary>
		/// Loads available KYC processes from server, falling back to bundled test KYC.
		/// </summary>
		/// <param name="Lang">Optional language code.</param>
		Task<IReadOnlyList<KycReference>> LoadAvailableKycReferencesAsync(string? Lang = null);

		/// <summary>
		/// Validates a single visible KYC page (synchronous + asynchronous rules).
		/// </summary>
		/// <param name="Page">Page to validate.</param>
		/// <returns>True if valid, otherwise false.</returns>
		Task<bool> ValidatePageAsync(Kyc.Models.KycPage Page);

		/// <summary>
		/// Returns index of first invalid visible page, or -1 if all visible pages are valid.
		/// </summary>
		/// <param name="Process">KYC process.</param>
		Task<int> GetFirstInvalidVisiblePageIndexAsync(Kyc.Models.KycProcess Process);

		/// <summary>
		/// Prepares mapped properties and attachments (image compression &amp; transforms applied).
		/// </summary>
		/// <param name="Process">KYC process.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		/// <returns>Tuple of mapped properties and attachments.</returns>
		Task<(IReadOnlyList<Property> Properties, IReadOnlyList<LegalIdentityAttachment> Attachments)> PreparePropertiesAndAttachmentsAsync(Kyc.Models.KycProcess Process, CancellationToken CancellationToken);

		/// <summary>Updates mutable reference fields (field values snapshot, progress, timestamps) from current process and navigation snapshot.</summary>
		/// <param name="Reference">Reference to update.</param>
		/// <param name="Process">Current KYC process.</param>
		/// <param name="Navigation">Navigation snapshot representing current page and state.</param>
		/// <param name="Progress">Current progress ratio (0..1).</param>
		void UpdateReferenceFields(KycReference Reference, Kyc.Models.KycProcess Process, NeuroAccessMaui.Services.Kyc.Domain.KycNavigationSnapshot Navigation, double Progress);

		/// <summary>
		/// Schedules an autosave of the reference after a debounce delay. Subsequent calls reset the timer.
		/// </summary>
		/// <param name="Reference">Reference to persist.</param>
		/// <returns>Task handle.</returns>
		Task ScheduleAutosaveAsync(KycReference Reference);

		/// <summary>
		/// Flushes any pending autosave immediately.
		/// </summary>
		/// <param name="Reference">Reference to persist now.</param>
		Task FlushAutosaveAsync(KycReference Reference);
	}
}
