using System;
using NeuroAccessMaui.Services.Identity;
using NeuroAccessMaui.Services.Kyc.Models;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.Services.Kyc
{
	/// <summary>
	/// Immutable snapshot of the state of a <see cref="KycReference"/> at a point in time.
	/// Used to persist ordered, atomic views without exposing live mutable objects to background tasks.
	/// </summary>
	public sealed class KycReferenceSnapshot
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="KycReferenceSnapshot"/> class.
		/// </summary>
		/// <param name="objectId">Persisted object identifier.</param>
		/// <param name="version">Snapshot version.</param>
		/// <param name="fields">Persisted field values.</param>
		/// <param name="createdIdentityId">Legacy or active created identity identifier.</param>
		/// <param name="createdIdentityState">Legacy or active created identity state.</param>
		/// <param name="reservedPreviewIdentityId">Reserved preview identity identifier.</param>
		/// <param name="previewIdentityId">Submitted preview identity identifier.</param>
		/// <param name="previewIdentityState">Submitted preview identity state.</param>
		/// <param name="finalIdentityId">Submitted final identity identifier.</param>
		/// <param name="finalIdentityState">Submitted final identity state.</param>
		/// <param name="identityStage">Current identity application stage.</param>
		/// <param name="progress">KYC flow progress.</param>
		/// <param name="lastVisitedPageId">Last visited page identifier.</param>
		/// <param name="lastVisitedMode">Last visited mode.</param>
		/// <param name="applicationReview">Latest application review.</param>
		/// <param name="createdUtc">Creation time.</param>
		/// <param name="updatedUtc">Updated time.</param>
		public KycReferenceSnapshot(
			string? objectId,
			int version,
			KycFieldValue[]? fields,
			string? createdIdentityId,
			IdentityState? createdIdentityState,
			string? reservedPreviewIdentityId,
			string? previewIdentityId,
			IdentityState? previewIdentityState,
			string? finalIdentityId,
			IdentityState? finalIdentityState,
			KycIdentityApplicationStage identityStage,
			double progress,
			string? lastVisitedPageId,
			string lastVisitedMode,
			ApplicationReview? applicationReview,
			DateTime createdUtc,
			DateTime updatedUtc)
		{
			this.ObjectId = objectId;
			this.Version = version;
			this.Fields = fields;
			this.CreatedIdentityId = createdIdentityId;
			this.CreatedIdentityState = createdIdentityState;
			this.ReservedPreviewIdentityId = reservedPreviewIdentityId;
			this.PreviewIdentityId = previewIdentityId;
			this.PreviewIdentityState = previewIdentityState;
			this.FinalIdentityId = finalIdentityId;
			this.FinalIdentityState = finalIdentityState;
			this.IdentityStage = identityStage;
			this.Progress = progress;
			this.LastVisitedPageId = lastVisitedPageId;
			this.LastVisitedMode = lastVisitedMode;
			this.ApplicationReview = applicationReview;
			this.CreatedUtc = createdUtc;
			this.UpdatedUtc = updatedUtc;
		}

		/// <summary>
		/// Gets the persisted object identifier.
		/// </summary>
		public string? ObjectId { get; }

		/// <summary>
		/// Gets the snapshot version.
		/// </summary>
		public int Version { get; }

		/// <summary>
		/// Gets the persisted field values.
		/// </summary>
		public KycFieldValue[]? Fields { get; }

		/// <summary>
		/// Gets the legacy or active created identity identifier.
		/// </summary>
		public string? CreatedIdentityId { get; }

		/// <summary>
		/// Gets the legacy or active created identity state.
		/// </summary>
		public IdentityState? CreatedIdentityState { get; }

		/// <summary>
		/// Gets the reserved preview identity identifier.
		/// </summary>
		public string? ReservedPreviewIdentityId { get; }

		/// <summary>
		/// Gets the submitted preview identity identifier.
		/// </summary>
		public string? PreviewIdentityId { get; }

		/// <summary>
		/// Gets the submitted preview identity state.
		/// </summary>
		public IdentityState? PreviewIdentityState { get; }

		/// <summary>
		/// Gets the submitted final identity identifier.
		/// </summary>
		public string? FinalIdentityId { get; }

		/// <summary>
		/// Gets the submitted final identity state.
		/// </summary>
		public IdentityState? FinalIdentityState { get; }

		/// <summary>
		/// Gets the current identity application stage.
		/// </summary>
		public KycIdentityApplicationStage IdentityStage { get; }

		/// <summary>
		/// Gets the KYC flow progress.
		/// </summary>
		public double Progress { get; }

		/// <summary>
		/// Gets the last visited page identifier.
		/// </summary>
		public string? LastVisitedPageId { get; }

		/// <summary>
		/// Gets the last visited mode.
		/// </summary>
		public string LastVisitedMode { get; }

		/// <summary>
		/// Gets the latest application review.
		/// </summary>
		public ApplicationReview? ApplicationReview { get; }

		/// <summary>
		/// Gets the creation time.
		/// </summary>
		public DateTime CreatedUtc { get; }

		/// <summary>
		/// Gets the updated time.
		/// </summary>
		public DateTime UpdatedUtc { get; }
	}
}
