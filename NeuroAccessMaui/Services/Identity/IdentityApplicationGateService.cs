using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Services.Kyc;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Identity
{
	/// <summary>
	/// Identifies the next destination in the identity application experience.
	/// </summary>
	public enum IdentityApplicationRoute
	{
		/// <summary>
		/// The user must complete account onboarding before identity application can continue.
		/// </summary>
		NeedsOnboarding,

		/// <summary>
		/// The user can choose or resume a KYC application.
		/// </summary>
		StartOrResumeApplication,

		/// <summary>
		/// The user has an identity application waiting for provider review.
		/// </summary>
		ApplicationPending,

		/// <summary>
		/// The user has an application that needs correction or reapplication.
		/// </summary>
		ApplicationNeedsAttention,

		/// <summary>
		/// The user already has an approved identity that should be shown.
		/// </summary>
		ShowApprovedIdentity
	}

	/// <summary>
	/// Represents the resolved identity application route for the current user.
	/// </summary>
	public sealed class IdentityApplicationGateDecision
	{
		/// <summary>
		/// Gets or sets the route the UI should take next.
		/// </summary>
		public IdentityApplicationRoute Route { get; init; }

		/// <summary>
		/// Gets or sets the most relevant KYC reference, if one exists.
		/// </summary>
		public KycReference? Reference { get; init; }

		/// <summary>
		/// Gets or sets the approved legal identity, if one exists.
		/// </summary>
		public LegalIdentity? ApprovedIdentity { get; init; }

		/// <summary>
		/// Gets or sets the active application status.
		/// </summary>
		public KycApplicationStatus? ApplicationStatus { get; init; }
	}

	/// <summary>
	/// Resolves where the user should go next in the identity application experience.
	/// </summary>
	[DefaultImplementation(typeof(IdentityApplicationGateService))]
	public interface IIdentityApplicationGateService
	{
		/// <summary>
		/// Evaluates the current profile and persisted KYC state.
		/// </summary>
		/// <returns>The route decision for the current user.</returns>
		Task<IdentityApplicationGateDecision> EvaluateAsync();
	}

	/// <summary>
	/// Default identity application gate service.
	/// </summary>
	public sealed class IdentityApplicationGateService : IIdentityApplicationGateService
	{
		/// <inheritdoc/>
		public async Task<IdentityApplicationGateDecision> EvaluateAsync()
		{
			KycReference? Reference = await this.FindLatestReferenceAsync().ConfigureAwait(false);
			LegalIdentity? ApprovedIdentity = ServiceRef.TagProfile.LegalIdentity;
			LegalIdentity? IdentityApplication = ServiceRef.TagProfile.IdentityApplication;
			KycApplicationStatus Status = KycApplicationStatusResolver.Resolve(Reference, ApprovedIdentity, IdentityApplication);

			if (string.IsNullOrWhiteSpace(ServiceRef.TagProfile.Account) || !ServiceRef.TagProfile.HasLocalPassword)
			{
				return new IdentityApplicationGateDecision
				{
					Route = IdentityApplicationRoute.NeedsOnboarding,
					Reference = Reference,
					ApplicationStatus = Status
				};
			}

			if (ApprovedIdentity?.HasApprovedName() == true)
			{
				return new IdentityApplicationGateDecision
				{
					Route = IdentityApplicationRoute.ShowApprovedIdentity,
					Reference = Reference,
					ApprovedIdentity = ApprovedIdentity,
					ApplicationStatus = Status
				};
			}

			if (Status.IsPending || Status.IsFinalizing)
			{
				return new IdentityApplicationGateDecision
				{
					Route = IdentityApplicationRoute.ApplicationPending,
					Reference = Reference,
					ApplicationStatus = Status
				};
			}

			if (Status.IsRejectedOrInvalid)
			{
				return new IdentityApplicationGateDecision
				{
					Route = IdentityApplicationRoute.ApplicationNeedsAttention,
					Reference = Reference,
					ApplicationStatus = Status
				};
			}

			return new IdentityApplicationGateDecision
			{
				Route = IdentityApplicationRoute.StartOrResumeApplication,
				Reference = Reference,
				ApplicationStatus = Status
			};
		}

		private async Task<KycReference?> FindLatestReferenceAsync()
		{
			try
			{
				IEnumerable<KycReference> References = await Database.Find<KycReference>().ConfigureAwait(false);
				LegalIdentity? IdentityApplication = ServiceRef.TagProfile.IdentityApplication;
				if (IdentityApplication is not null)
				{
					KycReference? Match = References
						.Where(Reference => Reference.MatchesIdentityId(IdentityApplication.Id))
						.OrderByDescending(Reference => Reference.UpdatedUtc)
						.FirstOrDefault();
					if (Match is not null)
						return Match;
				}

				return References
					.OrderByDescending(Reference => Reference.UpdatedUtc)
					.FirstOrDefault();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				return null;
			}
		}
	}
}
