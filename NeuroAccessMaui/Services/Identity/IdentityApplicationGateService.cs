using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Services.Kyc;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Persistence.Filters;
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

		/// <summary>
		/// Finds the KYC reference of the current identity application, or else the most recently updated reference.
		/// </summary>
		/// <remarks>
		/// Uses the indexes on the identity identifier columns and on <see cref="KycReference.UpdatedUtc"/>, so the common cases
		/// do not read every reference with its KYC XML. A full scan remains only when an identity application exists and no
		/// reference matches its identifier exactly, since matching ignores case and the indexes do not.
		/// </remarks>
		/// <returns>The most relevant reference, or <c>null</c> if there is none or the lookup failed.</returns>
		private async Task<KycReference?> FindLatestReferenceAsync()
		{
			try
			{
				string? IdentityId = ServiceRef.TagProfile.IdentityApplication?.Id;
				if (string.IsNullOrWhiteSpace(IdentityId))
				{
					IEnumerable<KycReference> Latest = await Database.Find<KycReference>(0, 1, "-" + nameof(KycReference.UpdatedUtc)).ConfigureAwait(false);
					return Latest.FirstOrDefault();
				}

				IdentityId = IdentityId.Trim();

				KycReference? ExactMatch = SelectLatest(await FindByIdentityIdAsync(IdentityId).ConfigureAwait(false), IdentityId);
				if (ExactMatch is not null)
					return ExactMatch;

				IEnumerable<KycReference> References = await Database.Find<KycReference>().ConfigureAwait(false);
				return SelectLatest(References, IdentityId) ?? References
					.OrderByDescending(Reference => Reference.UpdatedUtc)
					.FirstOrDefault();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				return null;
			}
		}

		/// <summary>
		/// Finds the references whose identity identifier columns equal an identifier, using the column indexes.
		/// </summary>
		/// <param name="IdentityId">Trimmed identity identifier.</param>
		/// <returns>The matching references, each once.</returns>
		private static async Task<List<KycReference>> FindByIdentityIdAsync(string IdentityId)
		{
			string[] FieldNames =
			[
				nameof(KycReference.ReservedPreviewIdentityId),
				nameof(KycReference.PreviewIdentityId),
				nameof(KycReference.FinalIdentityId),
				nameof(KycReference.CreatedIdentityId)
			];

			List<KycReference> Matches = [];
			HashSet<string> ObjectIds = new(StringComparer.Ordinal);

			foreach (string FieldName in FieldNames)
			{
				IEnumerable<KycReference> Found = await Database.Find<KycReference>(new FilterFieldEqualTo(FieldName, IdentityId)).ConfigureAwait(false);
				foreach (KycReference Reference in Found)
				{
					if (Reference.ObjectId is null || ObjectIds.Add(Reference.ObjectId))
						Matches.Add(Reference);
				}
			}

			return Matches;
		}

		/// <summary>
		/// Selects the most recently updated reference that matches an identity identifier.
		/// </summary>
		/// <param name="References">Candidate references.</param>
		/// <param name="IdentityId">Identity identifier.</param>
		/// <returns>The matching reference with the latest update, or <c>null</c> if none matches.</returns>
		private static KycReference? SelectLatest(IEnumerable<KycReference> References, string IdentityId)
		{
			return References
				.Where(Reference => Reference.MatchesIdentityId(IdentityId))
				.OrderByDescending(Reference => Reference.UpdatedUtc)
				.FirstOrDefault();
		}
	}
}
