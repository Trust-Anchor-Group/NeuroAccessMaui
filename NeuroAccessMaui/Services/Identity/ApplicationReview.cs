using System;

namespace NeuroAccessMaui.Services.Identity
{
	/// <summary>
	/// Captures the result of an application review returned from backend services.
	/// </summary>
	public sealed class ApplicationReview
	{
		/// <summary>
		/// The localized or raw message describing the review result.
		/// </summary>
		public string Message { get; set; } = string.Empty;

		/// <summary>
		/// Optional machine readable code identifying the review reason.
		/// </summary>
		public string? Code { get; set; }

		/// <summary>
		/// Moment when the review was received.
		/// </summary>
		public DateTime ReceivedUtc { get; set; } = DateTime.UtcNow;

		/// <summary>
		/// Invalid claim identifiers reported by the service.
		/// </summary>
		public string[] InvalidClaims { get; set; } = Array.Empty<string>();

		/// <summary>
		/// Optional details about invalid claims.
		/// </summary>
		public ApplicationReviewClaimDetail[] InvalidClaimDetails { get; set; } = Array.Empty<ApplicationReviewClaimDetail>();

		/// <summary>
		/// Invalid photo identifiers reported by the service.
		/// </summary>
		public string[] InvalidPhotos { get; set; } = Array.Empty<string>();

		/// <summary>
		/// Optional details about invalid photos.
		/// </summary>
		public ApplicationReviewPhotoDetail[] InvalidPhotoDetails { get; set; } = Array.Empty<ApplicationReviewPhotoDetail>();

		/// <summary>
		/// Claims still pending validation.
		/// </summary>
		public string[] UnvalidatedClaims { get; set; } = Array.Empty<string>();

		/// <summary>
		/// Photos still pending validation.
		/// </summary>
		public string[] UnvalidatedPhotos { get; set; } = Array.Empty<string>();
	}

	/// <summary>
	/// Contains additional data about an invalid claim.
	/// </summary>
	public sealed class ApplicationReviewClaimDetail
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ApplicationReviewClaimDetail"/> class.
		/// </summary>
		public ApplicationReviewClaimDetail()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ApplicationReviewClaimDetail"/> class.
		/// </summary>
		/// <param name="claim">The claim identifier.</param>
		/// <param name="reason">The textual reason explaining the invalidation.</param>
		/// <param name="reasonLanguage">Language code of the reason, if provided.</param>
		/// <param name="reasonCode">Machine readable reason code, if provided.</param>
		/// <param name="service">Source service of the review.</param>
		public ApplicationReviewClaimDetail(string claim, string reason, string? reasonLanguage, string? reasonCode, string? service)
		{
			this.Claim = claim;
			this.DisplayName = claim;
			this.Reason = reason;
			this.ReasonLanguage = reasonLanguage;
			this.ReasonCode = reasonCode;
			this.Service = service;
		}

		/// <summary>
		/// The claim identifier.
		/// </summary>
		public string Claim { get; set; } = string.Empty;

		/// <summary>
		/// Localized display name for the claim.
		/// </summary>
		public string DisplayName { get; set; } = string.Empty;

		/// <summary>
		/// The textual reason explaining the invalidation.
		/// </summary>
		public string Reason { get; set; } = string.Empty;

		/// <summary>
		/// Language code of the reason, if provided.
		/// </summary>
		public string? ReasonLanguage { get; set; }

		/// <summary>
		/// Machine readable reason code, if provided.
		/// </summary>
		public string? ReasonCode { get; set; }

		/// <summary>
		/// Source service of the review.
		/// </summary>
		public string? Service { get; set; }
	}

	/// <summary>
	/// Contains additional data about an invalid photo.
	/// </summary>
	public sealed class ApplicationReviewPhotoDetail
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ApplicationReviewPhotoDetail"/> class.
		/// </summary>
		public ApplicationReviewPhotoDetail()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ApplicationReviewPhotoDetail"/> class.
		/// </summary>
		/// <param name="fileName">Original file name of the photo.</param>
		/// <param name="displayName">Display name presented to users.</param>
		/// <param name="reason">Textual reason for the invalidation.</param>
		/// <param name="reasonLanguage">Language code of the reason, if provided.</param>
		/// <param name="reasonCode">Machine readable reason code, if provided.</param>
		/// <param name="service">Source service of the review.</param>
		public ApplicationReviewPhotoDetail(string fileName, string displayName, string reason, string? reasonLanguage, string? reasonCode, string? service)
		{
			this.FileName = fileName;
			this.DisplayName = displayName;
			this.Reason = reason;
			this.ReasonLanguage = reasonLanguage;
			this.ReasonCode = reasonCode;
			this.Service = service;
		}

		/// <summary>
		/// Original file name of the photo.
		/// </summary>
		public string FileName { get; set; } = string.Empty;

		/// <summary>
		/// Display name presented to users.
		/// </summary>
		public string DisplayName { get; set; } = string.Empty;

		/// <summary>
		/// Textual reason for the invalidation.
		/// </summary>
		public string Reason { get; set; } = string.Empty;

		/// <summary>
		/// Language code of the reason, if provided.
		/// </summary>
		public string? ReasonLanguage { get; set; }

		/// <summary>
		/// Machine readable reason code, if provided.
		/// </summary>
		public string? ReasonCode { get; set; }

		/// <summary>
		/// Source service of the review.
		/// </summary>
		public string? Service { get; set; }
	}
}
