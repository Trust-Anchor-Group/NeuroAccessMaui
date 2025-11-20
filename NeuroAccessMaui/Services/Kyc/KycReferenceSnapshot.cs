using System;
using NeuroAccessMaui.Services.Identity;
using NeuroAccessMaui.Services.Kyc.Models;

namespace NeuroAccessMaui.Services.Kyc
{
	/// <summary>
	/// Immutable snapshot of the state of a <see cref="KycReference"/> at a point in time.
	/// Used to persist ordered, atomic views without exposing live mutable objects to background tasks.
	/// </summary>
	public sealed class KycReferenceSnapshot
	{
		public KycReferenceSnapshot(
			string? objectId,
			int version,
			KycFieldValue[]? fields,
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
			this.Progress = progress;
			this.LastVisitedPageId = lastVisitedPageId;
			this.LastVisitedMode = lastVisitedMode;
			this.ApplicationReview = applicationReview;
			this.CreatedUtc = createdUtc;
			this.UpdatedUtc = updatedUtc;
		}

		public string? ObjectId { get; }
		public int Version { get; }
		public KycFieldValue[]? Fields { get; }
		public double Progress { get; }
		public string? LastVisitedPageId { get; }
		public string LastVisitedMode { get; }
		public ApplicationReview? ApplicationReview { get; }
		public DateTime CreatedUtc { get; }
		public DateTime UpdatedUtc { get; }
	}
}
