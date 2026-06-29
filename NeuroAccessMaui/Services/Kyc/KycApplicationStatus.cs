using System;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Services.Identity;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.Services.Kyc
{
	/// <summary>
	/// Represents the normalized local status of a KYC identity application.
	/// </summary>
	public enum KycApplicationStatusKind
	{
		/// <summary>
		/// No application state is known.
		/// </summary>
		None,

		/// <summary>
		/// A preview identity has been reserved but not submitted.
		/// </summary>
		ReservedPreview,

		/// <summary>
		/// An identity application has been submitted and is awaiting review.
		/// </summary>
		PendingReview,

		/// <summary>
		/// A preview identity has been submitted and is awaiting review.
		/// </summary>
		PreviewPendingReview,

		/// <summary>
		/// A preview identity was rejected before final promotion.
		/// </summary>
		PreviewRejected,

		/// <summary>
		/// The application is creating or waiting for the final approved identity.
		/// </summary>
		Finalizing,

		/// <summary>
		/// The approved preview identity is ready for final identity creation.
		/// </summary>
		FinalizationInProgress,

		/// <summary>
		/// The final identity has been submitted and is awaiting approval.
		/// </summary>
		FinalPendingApproval,

		/// <summary>
		/// The application completed with an approved identity.
		/// </summary>
		Approved,

		/// <summary>
		/// The application was rejected.
		/// </summary>
		Rejected,

		/// <summary>
		/// The application identity was obsoleted.
		/// </summary>
		Obsoleted,

		/// <summary>
		/// The application identity was compromised.
		/// </summary>
		Compromised,

		/// <summary>
		/// The approved identity has expired.
		/// </summary>
		Expired
	}

	/// <summary>
	/// Contains a normalized interpretation of a local KYC identity application.
	/// </summary>
	public sealed class KycApplicationStatus
	{
		/// <summary>
		/// Gets the normalized status kind.
		/// </summary>
		public KycApplicationStatusKind Kind { get; init; }

		/// <summary>
		/// Gets the active application identity identifier, if known.
		/// </summary>
		public string? ActiveIdentityId { get; init; }

		/// <summary>
		/// Gets the effective server identity state used for application decisions.
		/// </summary>
		public IdentityState? ServerState { get; init; }

		/// <summary>
		/// Gets a value indicating whether the application is waiting for review or approval.
		/// </summary>
		public bool IsPending => this.Kind is KycApplicationStatusKind.PendingReview or
			KycApplicationStatusKind.PreviewPendingReview or
			KycApplicationStatusKind.FinalPendingApproval;

		/// <summary>
		/// Gets a value indicating whether final identity creation or approval is in progress.
		/// </summary>
		public bool IsFinalizing => this.Kind is KycApplicationStatusKind.Finalizing or
			KycApplicationStatusKind.FinalizationInProgress or
			KycApplicationStatusKind.FinalPendingApproval;

		/// <summary>
		/// Gets a value indicating whether the application has an invalid terminal outcome.
		/// </summary>
		public bool IsRejectedOrInvalid => this.Kind is KycApplicationStatusKind.PreviewRejected or
			KycApplicationStatusKind.Rejected or
			KycApplicationStatusKind.Obsoleted or
			KycApplicationStatusKind.Compromised;

		/// <summary>
		/// Gets a value indicating whether the application completed with an approved identity.
		/// </summary>
		public bool IsApproved => this.Kind == KycApplicationStatusKind.Approved;
	}

	/// <summary>
	/// Resolves normalized KYC identity application status from local references and profile state.
	/// </summary>
	public static class KycApplicationStatusResolver
	{
		/// <summary>
		/// Resolves the current status for a KYC identity application.
		/// </summary>
		/// <param name="Reference">Latest KYC reference, if available.</param>
		/// <param name="LegalIdentity">Current approved or local legal identity, if available.</param>
		/// <param name="IdentityApplication">Current pending identity application, if available.</param>
		/// <returns>The normalized KYC application status.</returns>
		public static KycApplicationStatus Resolve(
			KycReference? Reference,
			LegalIdentity? LegalIdentity,
			LegalIdentity? IdentityApplication)
		{
			bool IsUnsubmittedReservedPreviewApplication =
				Reference?.IsUnsubmittedReservedPreviewIdentity(IdentityApplication?.Id) == true;
			IdentityState? EffectiveApplicationState = Reference?.GetEffectiveApplicationIdentityState() ??
				(IsUnsubmittedReservedPreviewApplication ? null : IdentityApplication?.State);
			if ((EffectiveApplicationState == IdentityState.Created || EffectiveApplicationState is null) &&
				IsTerminalApplicationReview(Reference?.ApplicationReview))
			{
				EffectiveApplicationState = IdentityState.Rejected;
			}

			KycApplicationStatusKind Kind = ResolveKind(Reference, LegalIdentity, EffectiveApplicationState);
			string? ActiveIdentityId = Kind == KycApplicationStatusKind.Approved && LegalIdentity?.IsApproved() == true
				? LegalIdentity.Id
				: Reference?.GetActiveApplicationIdentityId() ??
				(IsUnsubmittedReservedPreviewApplication ? null : IdentityApplication?.Id);

			return new KycApplicationStatus
			{
				Kind = Kind,
				ActiveIdentityId = ActiveIdentityId,
				ServerState = EffectiveApplicationState
			};
		}

		private static KycApplicationStatusKind ResolveKind(
			KycReference? Reference,
			LegalIdentity? LegalIdentity,
			IdentityState? EffectiveApplicationState)
		{
			if (LegalIdentity is not null && LegalIdentity.State == IdentityState.Compromised)
				return KycApplicationStatusKind.Compromised;

			if (LegalIdentity is not null && LegalIdentity.State == IdentityState.Obsoleted)
				return KycApplicationStatusKind.Obsoleted;

			if (LegalIdentity?.State == IdentityState.Approved && LegalIdentity.To < DateTime.Now)
				return KycApplicationStatusKind.Expired;

			if (LegalIdentity?.IsApproved() == true)
				return KycApplicationStatusKind.Approved;

			if (Reference is not null &&
				(Reference.IdentityStage == KycIdentityApplicationStage.ReservedPreview ||
				Reference.IdentityStage == KycIdentityApplicationStage.None) &&
				!string.IsNullOrWhiteSpace(Reference.ReservedPreviewIdentityId) &&
				string.IsNullOrWhiteSpace(Reference.PreviewIdentityId) &&
				string.IsNullOrWhiteSpace(Reference.FinalIdentityId))
			{
				return KycApplicationStatusKind.ReservedPreview;
			}

			if (Reference is not null)
			{
				switch (Reference.IdentityStage)
				{
					case KycIdentityApplicationStage.ReservedPreview:
						return KycApplicationStatusKind.ReservedPreview;

					case KycIdentityApplicationStage.FinalizationInProgress:
						return EffectiveApplicationState switch
						{
							IdentityState.Rejected => KycApplicationStatusKind.Rejected,
							IdentityState.Compromised => KycApplicationStatusKind.Compromised,
							IdentityState.Obsoleted => KycApplicationStatusKind.Obsoleted,
							_ => KycApplicationStatusKind.FinalizationInProgress
						};

					case KycIdentityApplicationStage.FinalPendingApproval:
						return EffectiveApplicationState switch
						{
							IdentityState.Approved => KycApplicationStatusKind.Approved,
							IdentityState.Rejected => KycApplicationStatusKind.Rejected,
							IdentityState.Compromised => KycApplicationStatusKind.Compromised,
							IdentityState.Obsoleted => KycApplicationStatusKind.Obsoleted,
							_ => KycApplicationStatusKind.FinalPendingApproval
						};

					case KycIdentityApplicationStage.Completed:
						return KycApplicationStatusKind.Approved;

					case KycIdentityApplicationStage.PreviewPendingReview:
						return EffectiveApplicationState switch
						{
							IdentityState.Approved => KycApplicationStatusKind.FinalizationInProgress,
							IdentityState.Rejected => KycApplicationStatusKind.PreviewRejected,
							IdentityState.Compromised => KycApplicationStatusKind.Compromised,
							IdentityState.Obsoleted => KycApplicationStatusKind.Obsoleted,
							_ => KycApplicationStatusKind.PreviewPendingReview
						};
				}
			}

			return ResolveSubmittedKind(EffectiveApplicationState, KycApplicationStatusKind.None);
		}

		private static KycApplicationStatusKind ResolveSubmittedKind(IdentityState? EffectiveApplicationState, KycApplicationStatusKind DefaultKind)
		{
			return EffectiveApplicationState switch
			{
				IdentityState.Created => DefaultKind == KycApplicationStatusKind.None ? KycApplicationStatusKind.PendingReview : DefaultKind,
				IdentityState.Rejected => KycApplicationStatusKind.Rejected,
				IdentityState.Approved => KycApplicationStatusKind.Approved,
				IdentityState.Obsoleted => KycApplicationStatusKind.Obsoleted,
				IdentityState.Compromised => KycApplicationStatusKind.Compromised,
				_ => DefaultKind
			};
		}

		private static bool IsTerminalApplicationReview(ApplicationReview? Review)
		{
			return Review is not null &&
				!string.IsNullOrWhiteSpace(Review.Code) &&
				!string.Equals(Review.Code, "ManualReview", StringComparison.OrdinalIgnoreCase);
		}
	}
}
