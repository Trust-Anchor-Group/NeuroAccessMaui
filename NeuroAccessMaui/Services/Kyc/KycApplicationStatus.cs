using System;
using NeuroAccessMaui.Extensions;
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
		/// The application is creating or waiting for the final approved identity.
		/// </summary>
		Finalizing,

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
		public bool IsPending => this.Kind == KycApplicationStatusKind.PendingReview;

		/// <summary>
		/// Gets a value indicating whether final identity creation or approval is in progress.
		/// </summary>
		public bool IsFinalizing => this.Kind == KycApplicationStatusKind.Finalizing;

		/// <summary>
		/// Gets a value indicating whether the application has an invalid terminal outcome.
		/// </summary>
		public bool IsRejectedOrInvalid => this.Kind is KycApplicationStatusKind.Rejected or
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
			IdentityState? EffectiveApplicationState = Reference?.GetEffectiveApplicationIdentityState() ?? IdentityApplication?.State;
			string? ActiveIdentityId = Reference?.GetActiveApplicationIdentityId() ?? IdentityApplication?.Id;

			return new KycApplicationStatus
			{
				Kind = ResolveKind(Reference, LegalIdentity, EffectiveApplicationState),
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

			if (Reference is not null)
			{
				switch (Reference.IdentityStage)
				{
					case KycIdentityApplicationStage.ReservedPreview:
						return KycApplicationStatusKind.ReservedPreview;

					case KycIdentityApplicationStage.FinalizationInProgress:
					case KycIdentityApplicationStage.FinalPendingApproval:
						return ResolveSubmittedKind(EffectiveApplicationState, KycApplicationStatusKind.Finalizing);

					case KycIdentityApplicationStage.Completed:
						return KycApplicationStatusKind.Approved;

					case KycIdentityApplicationStage.PreviewPendingReview:
						return ResolveSubmittedKind(EffectiveApplicationState, KycApplicationStatusKind.PendingReview);
				}
			}

			if (LegalIdentity?.State == IdentityState.Approved && LegalIdentity.To < DateTime.Now)
				return KycApplicationStatusKind.Expired;

			if (LegalIdentity?.IsApproved() == true)
				return KycApplicationStatusKind.Approved;

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
	}
}
