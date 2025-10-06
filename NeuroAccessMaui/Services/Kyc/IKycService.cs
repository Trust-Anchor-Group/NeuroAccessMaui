using System.Threading;
using System.Collections.Generic;
using NeuroAccessMaui.Services.Kyc.Domain;
using NeuroAccessMaui.Services.Kyc.Models;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Kyc
{
	/// <summary>
	/// Service for loading KYC processes, performing validation, building identity artifacts and managing persistence snapshots.
	/// </summary>
	[DefaultImplementation(typeof(KycService))]
	public interface IKycService
	{
		/// <summary>
		/// Loads (or creates) the persisted KYC reference and ensures process XML is available.
		/// </summary>
		/// <param name="Lang">Optional language code.</param>
		Task<KycReference> LoadKycReferenceAsync(string? Lang = null, KycApplicationTemplate? Template = null);

		/// <summary>
		/// Persists the provided reference (insert or update).
		/// </summary>
		/// <param name="Reference">Reference to persist.</param>
		Task SaveKycReferenceAsync(KycReference Reference);

		/// <summary>
		/// Loads available KYC processes from server, falling back to bundled test KYC.
		/// </summary>
		/// <param name="Lang">Optional language code.</param>
		Task<IReadOnlyList<KycReference>> LoadAvailableKycReferencesAsync(string? Lang = null);

		/// <summary>
		/// Loads a page of KYC application templates using PubSub pagination, with bundled fallback when remote data is unavailable.
		/// </summary>
		/// <param name="After">Item identifier to request items after.</param>
		/// <param name="Before">Item identifier to request items before.</param>
		/// <param name="Index">Optional zero-based index of the first item to request.</param>
		/// <param name="Max">Maximum number of entries to request.</param>
		/// <param name="Lang">Optional language code used when parsing KYC names.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		Task<KycApplicationPage> LoadKycApplicationsPageAsync(string? After = null, string? Before = null, int? Index = null, int? Max = null, string? Lang = null, CancellationToken CancellationToken = default);

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

		/// <summary>
		/// Updates snapshot information and schedules an autosave.
		/// </summary>
		Task ScheduleSnapshotAsync(KycReference Reference, Kyc.Models.KycProcess Process, KycNavigationSnapshot Navigation, double Progress, string? CurrentPageId);

		/// <summary>
		/// Updates snapshot information and flushes any pending autosave.
		/// </summary>
		Task FlushSnapshotAsync(KycReference Reference, Kyc.Models.KycProcess Process, KycNavigationSnapshot Navigation, double Progress, string? CurrentPageId);

		/// <summary>
		/// Persists submission details after an application is sent.
		/// </summary>
		Task ApplySubmissionAsync(KycReference Reference, LegalIdentity Identity);

		/// <summary>
		/// Clears stored submission details for revoked or reset applications.
		/// </summary>
		Task ClearSubmissionAsync(KycReference Reference);

		/// <summary>
		/// Persists rejection metadata and invalid claim/photo information.
		/// </summary>
		Task ApplyRejectionAsync(KycReference Reference, string Message, string[] InvalidClaims, string[] InvalidPhotos, string? Code);

		/// <summary>
		/// Resets reference state and seeds optional field values for a fresh application session.
		/// </summary>
		Task PrepareReferenceForNewApplicationAsync(KycReference Reference, string? Language, IReadOnlyList<KycFieldValue>? SeedFields);

		/// <summary>
		/// Flushes any pending in-memory KYC snapshots to durable storage. Should be fast and idempotent.
		/// </summary>
		Task FlushAsync(CancellationToken CancellationToken = default);

		/// <summary>
		/// Performs an orderly shutdown of the KYC service, draining queues and releasing resources.
		/// Calls <see cref="FlushAsync"/> internally.
		/// </summary>
		Task ShutdownAsync(CancellationToken CancellationToken = default);

	}
}
