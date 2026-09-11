using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Persistence.Attributes;
using NeuroAccessMaui.Services.Identity;
using NeuroAccessMaui.Services.Kyc.Models;
using NeuroAccessMaui.Services.Kyc.ViewModels;

namespace NeuroAccessMaui.Services.Kyc
{
	/// <summary>
	/// Represents the current phase of an identity application created from a KYC reference.
	/// </summary>
	public enum KycIdentityApplicationStage
	{
		/// <summary>
		/// No identity application has been submitted.
		/// </summary>
		None = 0,

		/// <summary>
		/// A preview identity has been reserved but has not been submitted for review.
		/// </summary>
		ReservedPreview = 5,

		/// <summary>
		/// A preview identity has been submitted and is awaiting review.
		/// </summary>
		PreviewPendingReview = 1,

		/// <summary>
		/// The preview identity has been approved and final identity creation is in progress.
		/// </summary>
		FinalizationInProgress = 2,

		/// <summary>
		/// The final identity has been submitted and is awaiting approval.
		/// </summary>
		FinalPendingApproval = 3,

		/// <summary>
		/// The identity application flow completed with an approved identity.
		/// </summary>
		Completed = 4
	}

	/// <summary>
	/// Contains a local reference to a KYC process.
	/// </summary>
	[CollectionName("KycReferences")]
	[Index(nameof(UpdatedUtc), nameof(UpdatedUtc))]
	[Index(nameof(CreatedIdentityId))]
	[Index(nameof(ReservedPreviewIdentityId))]
	[Index(nameof(PreviewIdentityId))]
	[Index(nameof(FinalIdentityId))]
	public class KycReference
	{
		private KycProcess? process;
		private ApplicationReview? applicationReview;

		/// <summary>
		/// Gets the derived process value key that indicates whether travel-document NFC evidence is complete.
		/// </summary>
		public const string TravelDocumentNfcCompletedEvidenceFieldId = "evidence.travelDocument.nfc.completed";

		/// <summary>
		/// Object ID
		/// </summary>
		[ObjectId]
		public string? ObjectId { get; set; }

		/// <summary>
		/// XML describing the KYC process.
		/// </summary>
		public string? KycXml { get; set; }

		/// <summary>
		/// When the reference was created.
		/// </summary>
		public DateTime CreatedUtc { get; set; }

		/// <summary>
		/// When the reference was last updated.
		/// </summary>
		public DateTime UpdatedUtc { get; set; }

		/// <summary>
		/// When the process was last fetched.
		/// </summary>
		public DateTime FetchedUtc { get; set; }

		/// <summary>
		/// Monotonically increasing version for optimistic concurrency and snapshot ordering.
		/// Incremented whenever a new immutable snapshot of the reference state is captured.
		/// </summary>
		[DefaultValue(0)]
		public int Version { get; set; }

		/// <summary>
		/// Field values in the process.
		/// </summary>
		[DefaultValueNull]
		public KycFieldValue[]? Fields { get; set; }

		/// <summary>
		/// Optional friendly name.
		/// </summary>
		[DefaultValueStringEmpty]
		public string FriendlyName { get; set; } = string.Empty;

		/// <summary>
		/// The legal ID of the created identity (if any).
		/// </summary>
		[DefaultValueNull]
		public string? CreatedIdentityId { get; set; }

		/// <summary>
		/// Last known state of the created identity (if any), for quick status tagging offline.
		/// </summary>
		[DefaultValueNull]
		public IdentityState? CreatedIdentityState { get; set; }

		/// <summary>
		/// Gets or sets the legal identity identifier reserved for a preview identity before submission.
		/// </summary>
		[DefaultValueNull]
		public string? ReservedPreviewIdentityId { get; set; }

		/// <summary>
		/// Gets or sets the legal identity identifier for a submitted preview identity.
		/// </summary>
		[DefaultValueNull]
		public string? PreviewIdentityId { get; set; }

		/// <summary>
		/// Gets or sets the last known state of the preview identity.
		/// </summary>
		[DefaultValueNull]
		public IdentityState? PreviewIdentityState { get; set; }

		/// <summary>
		/// Gets or sets the legal identity identifier for the final identity created from this application.
		/// </summary>
		[DefaultValueNull]
		public string? FinalIdentityId { get; set; }

		/// <summary>
		/// Gets or sets the last known state of the final identity.
		/// </summary>
		[DefaultValueNull]
		public IdentityState? FinalIdentityState { get; set; }

		/// <summary>
		/// Gets or sets the current phase of the identity application flow.
		/// </summary>
		[DefaultValue(KycIdentityApplicationStage.None)]
		public KycIdentityApplicationStage IdentityStage { get; set; }

		/// <summary>
		/// Gets or sets the latest travel-document MRZ captured for this application.
		/// </summary>
		[DefaultValueNull]
		public string? TravelDocumentMrz { get; set; }

		/// <summary>
		/// Gets a value indicating whether the stored travel-document MRZ has a full MRZ shape.
		/// </summary>
		[IgnoreMember]
		public bool HasFullTravelDocumentMrz => KycReference.IsFullTravelDocumentMrz(this.TravelDocumentMrz);

		/// <summary>
		/// Gets or sets when the latest travel-document MRZ was captured.
		/// </summary>
		[DefaultValueNull]
		public DateTime? TravelDocumentMrzUpdatedUtc { get; set; }

		/// <summary>
		/// Gets or sets the latest NFC travel-document readout XML captured for this application.
		/// </summary>
		[DefaultValueNull]
		public string? NfcReadoutXml { get; set; }

		/// <summary>
		/// Gets or sets when the latest NFC travel-document readout was captured.
		/// </summary>
		[DefaultValueNull]
		public DateTime? NfcReadoutUpdatedUtc { get; set; }

		/// <summary>
		/// Gets or sets the field identifiers populated from a successfully verified NFC readout.
		/// </summary>
		/// <remarks>
		/// Null indicates missing verification metadata; an empty array records a readout with no matching fields.
		/// </remarks>
		[DefaultValueNull]
		public string[]? NfcVerifiedFieldIds { get; set; }

		/// <summary>
		/// Gets whether an unsubmitted NFC draft can be rescanned to recover missing field-verification metadata.
		/// </summary>
		[IgnoreMember]
		public bool CanRescanLegacyNfcReadout =>
			!string.IsNullOrWhiteSpace(this.NfcReadoutXml) &&
			this.NfcVerifiedFieldIds is null &&
			(this.IdentityStage is KycIdentityApplicationStage.None or KycIdentityApplicationStage.ReservedPreview) &&
			string.IsNullOrWhiteSpace(this.PreviewIdentityId) &&
			string.IsNullOrWhiteSpace(this.FinalIdentityId) &&
			(string.IsNullOrWhiteSpace(this.CreatedIdentityId) || this.IsReservedPreviewIdentity(this.CreatedIdentityId));

		/// <summary>
		/// Gets or sets the server template identifier for the active KYC process.
		/// </summary>
		[DefaultValueNull]
		public string? KycTemplateId { get; set; }

		/// <summary>
		/// Gets or sets the template identifier captured when the current application was submitted.
		/// </summary>
		[DefaultValueNull]
		public string? SubmittedKycTemplateId { get; set; }

		/// <summary>
		/// Gets or sets the verification method captured when the current application was submitted.
		/// </summary>
		[DefaultValueNull]
		public string? SubmittedVerificationMethod { get; set; }

		/// <summary>
		/// Progress of the KYC process (0.0–1.0), persisted for UI display.
		/// </summary>
		[DefaultValue(0.0)]
		public double Progress { get; set; }

		/// <summary>
		/// Last visited page identifier to support resuming.
		/// </summary>
		[DefaultValueNull]
		public string? LastVisitedPageId { get; set; }

		/// <summary>
		/// Last visited mode: "Form" or "Summary". Default is "Form".
		/// </summary>
		[DefaultValueStringEmpty]
		public string LastVisitedMode { get; set; } = "Form";

		/// <summary>
		/// Latest backend review for this application, if any.
		/// </summary>
		[DefaultValueNull]
		public ApplicationReview? ApplicationReview
		{
			get
			{
				if (this.applicationReview is null)
					this.TryMigrateLegacyReview();

				return this.applicationReview;
			}

			set => this.applicationReview = value;
		}

		/// <summary>
		/// Gets the identity identifier that should currently be treated as the active application.
		/// </summary>
		[IgnoreMember]
		public string? ActiveApplicationIdentityId => this.GetActiveApplicationIdentityId();

		/// <summary>
		/// Gets the effective identity state used by application status and resume logic.
		/// </summary>
		[IgnoreMember]
		public IdentityState? EffectiveApplicationIdentityState => this.GetEffectiveApplicationIdentityState();

		/// <summary>
		/// Legacy rejection message retained for persistence migration. Do not use directly.
		/// </summary>
		[DefaultValueNull]
		[Obsolete("Use ApplicationReview instead.")]
		public string? RejectionMessage { get; set; }

		/// <summary>
		/// Legacy rejection code retained for persistence migration. Do not use directly.
		/// </summary>
		[DefaultValueNull]
		[Obsolete("Use ApplicationReview instead.")]
		public string? RejectionCode { get; set; }

		/// <summary>
		/// Legacy invalid claim collection retained for persistence migration. Do not use directly.
		/// </summary>
		[DefaultValueNull]
		[Obsolete("Use ApplicationReview instead.")]
		public string[]? InvalidClaims { get; set; }

		/// <summary>
		/// Legacy invalid photo collection retained for persistence migration. Do not use directly.
		/// </summary>
		[DefaultValueNull]
		[Obsolete("Use ApplicationReview instead.")]
		public string[]? InvalidPhotos { get; set; }

		/// <summary>
		/// Legacy invalid claim detail payload retained for persistence migration. Do not use directly.
		/// </summary>
		[DefaultValueNull]
		[Obsolete("Use ApplicationReview instead.")]
		public KycInvalidClaim[]? InvalidClaimDetails { get; set; }

		/// <summary>
		/// Legacy invalid photo detail payload retained for persistence migration. Do not use directly.
		/// </summary>
		[DefaultValueNull]
		[Obsolete("Use ApplicationReview instead.")]
		public KycInvalidPhoto[]? InvalidPhotoDetails { get; set; }

		/// <summary>
		/// Gets a parsed KYC process, populating its fields from the reference.
		/// </summary>
		/// <param name="Lang">Optional language.</param>
		/// <returns>The parsed and populated KYC process, or null if XML is missing.</returns>
		public async Task<KycProcess?> GetProcess(string? Lang = null)
		{
			if (this.process is null && this.KycXml is not null)
			{
				this.process = await KycProcessParser.LoadProcessAsync(this.KycXml, Lang).ConfigureAwait(false);

				if (this.Fields is not null)
				{
					foreach (KycFieldValue Field in this.Fields)
					{
						if (KycReference.IsDerivedProcessValue(Field.FieldId))
							continue;

						this.process.Values[Field.FieldId] = Field.Value;
					}

					foreach (KycPage Page in this.process.Pages)
					{
						foreach (ObservableKycField Field in Page.AllFields)
							this.ApplyFieldValue(Field);

						foreach (KycSection Section in Page.AllSections)
							foreach (ObservableKycField Field in Section.AllFields)
								this.ApplyFieldValue(Field);
					}
				}
			}

			this.ApplyEvidenceStateToProcess(this.process);
			if (this.process is not null)
			{
				foreach (KycPage Page in this.process.Pages)
					Page.UpdateVisibilities(this.process.Values);
			}

			return this.process;
		}

		/// <summary>
		/// Stores a parsed KYC process into the reference.
		/// </summary>
		/// <param name="Process">KYC process.</param>
		/// <param name="Xml">Process XML.</param>
		public void SetProcess(KycProcess Process, string Xml)
		{
			this.SetProcess(Process, Xml, DateTime.UtcNow, DateTime.UtcNow);
		}

		/// <summary>
		/// Stores a parsed KYC process into the reference with explicit timestamps.
		/// </summary>
		/// <param name="Process">KYC process.</param>
		/// <param name="Xml">Process XML.</param>
		/// <param name="Created">Created time.</param>
		/// <param name="Updated">Updated time.</param>
		public void SetProcess(KycProcess Process, string Xml, DateTime Created, DateTime Updated)
		{
			this.process = Process;
			this.KycXml = Xml;
			this.CreatedUtc = Created;
			this.UpdatedUtc = Updated;
			this.FetchedUtc = DateTime.UtcNow;
			this.Fields = KycReference.CreatePersistentFields(Process);
		}

		/// <summary>
		/// Applies the stored value for a field to the supplied observable field.
		/// </summary>
		/// <param name="Field">The observable field to update.</param>
		public void ApplyFieldValue(ObservableKycField Field)
		{
			if (this.process is null || !this.process.Values.TryGetValue(Field.Id, out string? Val) || Val is null)
				return;
			Field.StringValue = Val;
		}

		// SetFieldValue removed; logic is now in Field.StringValue

		/// <summary>
		/// Determines whether this reference tracks the specified identity identifier.
		/// </summary>
		/// <param name="IdentityId">Identity identifier to compare.</param>
		/// <returns><c>true</c> if the identifier matches a reserved preview, preview, final, or legacy identity id; otherwise, <c>false</c>.</returns>
		public bool MatchesIdentityId(string? IdentityId)
		{
			if (string.IsNullOrWhiteSpace(IdentityId))
				return false;

			string NormalizedIdentityId = IdentityId.Trim();
			return string.Equals(this.ReservedPreviewIdentityId, NormalizedIdentityId, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(this.PreviewIdentityId, NormalizedIdentityId, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(this.FinalIdentityId, NormalizedIdentityId, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(this.CreatedIdentityId, NormalizedIdentityId, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Determines whether a final identity belongs to this application, including its preview promotion link.
		/// </summary>
		/// <param name="Identity">The final identity to match.</param>
		/// <returns>True if the identity matches a stored final identifier or the submitted preview link.</returns>
		public bool MatchesFinalIdentity(LegalIdentity? Identity)
		{
			if (Identity is null || string.IsNullOrWhiteSpace(Identity.Id) ||
				this.IsReservedPreviewIdentity(Identity.Id) || this.IsPreviewIdentity(Identity.Id))
			{
				return false;
			}

			if (this.IsFinalIdentity(Identity.Id))
				return true;

			if (!string.IsNullOrWhiteSpace(this.FinalIdentityId) &&
				(Identity.State != IdentityState.Approved ||
					this.FinalIdentityState is not (null or IdentityState.Created)))
			{
				return false;
			}

			if (this.MatchesIdentityId(Identity.Id))
				return true;

			if (string.IsNullOrWhiteSpace(this.PreviewIdentityId))
				return false;

			string PreviewId = this.PreviewIdentityId.Trim();
			string PreviewLink = Identity[Constants.XmppProperties.Preview]?.Trim() ?? string.Empty;
			if (string.Equals(PreviewLink, PreviewId, StringComparison.OrdinalIgnoreCase))
				return true;

			int DomainSeparator = PreviewId.IndexOf('@');
			return DomainSeparator > 0 &&
				Identity.Id.EndsWith(PreviewId[DomainSeparator..], StringComparison.OrdinalIgnoreCase) &&
				string.Equals(PreviewLink, PreviewId[..DomainSeparator], StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Determines whether the specified identity identifier matches the preview identity.
		/// </summary>
		/// <param name="IdentityId">Identity identifier to compare.</param>
		/// <returns><c>true</c> if the identifier matches the preview identity; otherwise, <c>false</c>.</returns>
		public bool IsPreviewIdentity(string? IdentityId)
		{
			return !string.IsNullOrWhiteSpace(IdentityId) &&
				!string.IsNullOrWhiteSpace(this.PreviewIdentityId) &&
				string.Equals(this.PreviewIdentityId, IdentityId.Trim(), StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Determines whether the specified identity identifier matches the reserved preview identity.
		/// </summary>
		/// <param name="IdentityId">Identity identifier to compare.</param>
		/// <returns><c>true</c> if the identifier matches the reserved preview identity; otherwise, <c>false</c>.</returns>
		public bool IsReservedPreviewIdentity(string? IdentityId)
		{
			return !string.IsNullOrWhiteSpace(IdentityId) &&
				!string.IsNullOrWhiteSpace(this.ReservedPreviewIdentityId) &&
				string.Equals(this.ReservedPreviewIdentityId, IdentityId.Trim(), StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Determines whether the specified identity identifier matches the final identity.
		/// </summary>
		/// <param name="IdentityId">Identity identifier to compare.</param>
		/// <returns><c>true</c> if the identifier matches the final identity; otherwise, <c>false</c>.</returns>
		public bool IsFinalIdentity(string? IdentityId)
		{
			return !string.IsNullOrWhiteSpace(IdentityId) &&
				!string.IsNullOrWhiteSpace(this.FinalIdentityId) &&
				string.Equals(this.FinalIdentityId, IdentityId.Trim(), StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Determines whether this reference tracks only a reserved preview identity that has not been submitted.
		/// </summary>
		/// <param name="IdentityId">Identity identifier to compare.</param>
		/// <returns><c>true</c> if the identifier matches an unsubmitted reserved preview identity; otherwise, <c>false</c>.</returns>
		public bool IsUnsubmittedReservedPreviewIdentity(string? IdentityId)
		{
			return (this.IdentityStage == KycIdentityApplicationStage.ReservedPreview ||
				this.IdentityStage == KycIdentityApplicationStage.None) &&
				this.IsReservedPreviewIdentity(IdentityId) &&
				!this.IsPreviewIdentity(IdentityId) &&
				!this.IsFinalIdentity(IdentityId);
		}

		/// <summary>
		/// Gets the identity identifier that should currently be treated as the active application.
		/// </summary>
		/// <returns>The active application identity identifier, or <c>null</c> if no application identity is tracked.</returns>
		public string? GetActiveApplicationIdentityId()
		{
			if (this.IdentityStage == KycIdentityApplicationStage.ReservedPreview && !string.IsNullOrWhiteSpace(this.ReservedPreviewIdentityId))
				return this.ReservedPreviewIdentityId;

			if ((this.IdentityStage == KycIdentityApplicationStage.FinalPendingApproval ||
				this.IdentityStage == KycIdentityApplicationStage.Completed) &&
				!string.IsNullOrWhiteSpace(this.FinalIdentityId))
			{
				return this.FinalIdentityId;
			}

			if ((this.IdentityStage == KycIdentityApplicationStage.PreviewPendingReview ||
				this.IdentityStage == KycIdentityApplicationStage.FinalizationInProgress) &&
				!string.IsNullOrWhiteSpace(this.PreviewIdentityId))
			{
				return this.PreviewIdentityId;
			}

			if (!string.IsNullOrWhiteSpace(this.FinalIdentityId))
				return this.FinalIdentityId;

			if (!string.IsNullOrWhiteSpace(this.PreviewIdentityId))
				return this.PreviewIdentityId;

			return this.CreatedIdentityId;
		}

		/// <summary>
		/// Gets the effective identity state used by application status and resume logic.
		/// </summary>
		/// <returns>The effective application identity state, or <c>null</c> if no state is tracked.</returns>
		public IdentityState? GetEffectiveApplicationIdentityState()
		{
			return this.IdentityStage switch
			{
				KycIdentityApplicationStage.ReservedPreview => null,
				KycIdentityApplicationStage.FinalizationInProgress => IdentityState.Created,
				KycIdentityApplicationStage.FinalPendingApproval => this.FinalIdentityState ?? IdentityState.Created,
				KycIdentityApplicationStage.Completed => this.FinalIdentityState ?? IdentityState.Approved,
				KycIdentityApplicationStage.PreviewPendingReview => this.PreviewIdentityState ?? this.CreatedIdentityState,
				KycIdentityApplicationStage.None when this.IsReservedPreviewIdentity(this.CreatedIdentityId) => this.FinalIdentityState ?? this.PreviewIdentityState,
				_ => this.FinalIdentityState ?? this.PreviewIdentityState ?? this.CreatedIdentityState
			};
		}

		/// <summary>
		/// Creates a KycReference from a KycProcess, serializing its field values.
		/// </summary>
		/// <param name="Process">The KYC process to serialize.</param>
		/// <param name="Xml">The process XML.</param>
		/// <param name="FriendlyName">Optional friendly name.</param>
		/// <returns>A new KycReference instance.</returns>
		public static KycReference FromProcess(KycProcess Process, string Xml, string? FriendlyName = null)
		{
			KycReference Reference = new KycReference
			{
				process = Process,
				KycXml = Xml,
				CreatedUtc = DateTime.UtcNow,
				UpdatedUtc = DateTime.UtcNow,
				FetchedUtc = DateTime.UtcNow,
				Fields = KycReference.CreatePersistentFields(Process),
				FriendlyName = FriendlyName ?? string.Empty
			};
			return Reference;
		}

		/// <summary>
		/// Ensures the current process reflects values in <see cref="Fields"/> by applying them
		/// into the cached process instance (creating it if needed) and propagating values to
		/// visible fields. Useful when <see cref="Fields"/> has been updated after a prior
		/// call to <see cref="GetProcess(string?)"/> created the cached process.
		/// </summary>
		/// <param name="Lang">Optional language when creating a new process.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		public async Task ApplyFieldsToProcessAsync(string? Lang = null)
		{
			KycProcess? Proc = await this.GetProcess(Lang).ConfigureAwait(false);

			if (Proc is null)
				return;

			if (this.Fields is not null)
			{
				foreach (KycFieldValue Field in this.Fields)
				{
					if (KycReference.IsDerivedProcessValue(Field.FieldId))
						continue;

					Proc.Values[Field.FieldId] = Field.Value;
				}
			}

			this.ApplyEvidenceStateToProcess(Proc);

			foreach (KycPage Page in Proc.Pages)
			{
				foreach (ObservableKycField Field in Page.AllFields)
					this.ApplyFieldValue(Field);

				foreach (KycSection Section in Page.AllSections)
					foreach (ObservableKycField Field in Section.AllFields)
						this.ApplyFieldValue(Field);

				Page.UpdateVisibilities(Proc.Values);
			}
		}

		/// <summary>
		/// Creates a KycProcess from this reference, populating its fields.
		/// </summary>
		/// <param name="Lang">Optional language.</param>
		/// <returns>The populated KycProcess, or null if XML is missing.</returns>
		public async Task<KycProcess?> ToProcess(string? Lang = null)
		{
			return await this.GetProcess(Lang);
		}

		/// <summary>
		/// Applies derived evidence values to a process without persisting them as user-entered fields.
		/// </summary>
		/// <param name="Process">The process to update.</param>
		public void ApplyEvidenceStateToProcess(KycProcess? Process)
		{
			if (Process is null)
				return;

			bool HasNfcReadout = !string.IsNullOrWhiteSpace(this.NfcReadoutXml);
			Process.Values[KycReference.TravelDocumentNfcCompletedEvidenceFieldId] = HasNfcReadout ? "true" : "false";
			HashSet<string> VerifiedFieldIds = HasNfcReadout
				? new HashSet<string>(this.NfcVerifiedFieldIds ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase)
				: new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (KycPage Page in Process.Pages)
			{
				foreach (ObservableKycField Field in Page.AllFields)
					Field.IsReadOnly = VerifiedFieldIds.Contains(Field.Id);

				foreach (KycSection Section in Page.AllSections)
				{
					foreach (ObservableKycField Field in Section.AllFields)
						Field.IsReadOnly = VerifiedFieldIds.Contains(Field.Id);
				}
			}
		}

		/// <summary>
		/// Determines whether a process value is derived from reference evidence rather than entered by the user.
		/// </summary>
		/// <param name="FieldId">The process value key.</param>
		/// <returns><c>true</c> if the key identifies derived evidence state; otherwise, <c>false</c>.</returns>
		public static bool IsDerivedProcessValue(string? FieldId)
		{
			return string.Equals(FieldId, KycReference.TravelDocumentNfcCompletedEvidenceFieldId, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Creates the persisted field snapshot for a process, excluding derived evidence state.
		/// </summary>
		/// <param name="Process">The process containing current values.</param>
		/// <returns>The field values that should be stored with the reference.</returns>
		public static KycFieldValue[] CreatePersistentFields(KycProcess Process)
		{
			return [.. Process.Values
				.Where(Pair => !KycReference.IsDerivedProcessValue(Pair.Key))
				.Select(Pair => new KycFieldValue(Pair.Key, Pair.Value))];
		}

		/// <summary>
		/// Determines if a travel-document MRZ value has the shape of a full printed MRZ.
		/// </summary>
		/// <param name="MrzText">The MRZ text to inspect.</param>
		/// <returns>True if the value has a full printed MRZ shape; otherwise false.</returns>
		public static bool IsFullTravelDocumentMrz(string? MrzText)
		{
			string Normalized = MrzText?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(Normalized))
				return false;

			string Compact = new string(
				Normalized
					.Where(Character => Character != '\r' && Character != '\n' && !char.IsWhiteSpace(Character))
					.ToArray());
			return Compact.Length == 72 ||
				Compact.Length == 88 ||
				Compact.Length == 90;
		}

		private void TryMigrateLegacyReview()
		{
			if (this.applicationReview is not null)
				return;

			bool hasLegacyData =
				!string.IsNullOrWhiteSpace(this.RejectionMessage) ||
				!string.IsNullOrWhiteSpace(this.RejectionCode) ||
				(this.InvalidClaims?.Length ?? 0) > 0 ||
				(this.InvalidPhotos?.Length ?? 0) > 0 ||
				(this.InvalidClaimDetails?.Length ?? 0) > 0 ||
				(this.InvalidPhotoDetails?.Length ?? 0) > 0;

			if (!hasLegacyData)
				return;

			ApplicationReview Migrated = new ApplicationReview
			{
				Message = this.RejectionMessage ?? string.Empty,
				Code = this.RejectionCode,
				ReceivedUtc = this.UpdatedUtc
			};

			string[]? invalidClaims = this.InvalidClaims?
				.Where(s => !string.IsNullOrWhiteSpace(s))
				.Select(s => s.Trim())
				.ToArray();
			string[]? invalidPhotos = this.InvalidPhotos?
				.Where(s => !string.IsNullOrWhiteSpace(s))
				.Select(s => s.Trim())
				.ToArray();

			Migrated.InvalidClaims = invalidClaims is { Length: > 0 } ? invalidClaims : Array.Empty<string>();
			Migrated.InvalidPhotos = invalidPhotos is { Length: > 0 } ? invalidPhotos : Array.Empty<string>();

			ApplicationReviewClaimDetail[] claimDetails = this.InvalidClaimDetails?
				.Where(c => c is not null && !string.IsNullOrWhiteSpace(c.Claim))
				.Select(c =>
				{
					string Claim = c.Claim.Trim();
					string Reason = c.Reason ?? string.Empty;
					return new ApplicationReviewClaimDetail(Claim, Reason, c.ReasonLanguage, c.ReasonCode, c.Service);
				})
				.ToArray() ?? Array.Empty<ApplicationReviewClaimDetail>();

			ApplicationReviewPhotoDetail[] photoDetails = this.InvalidPhotoDetails?
				.Where(p => p is not null && (!string.IsNullOrWhiteSpace(p.Mapping) || !string.IsNullOrWhiteSpace(p.FileName)))
				.Select(p =>
				{
					string FileName = (p.FileName ?? string.Empty).Trim();
					string DisplayName = !string.IsNullOrWhiteSpace(p.Mapping) ? p.Mapping.Trim() : FileName;
					return new ApplicationReviewPhotoDetail(FileName, DisplayName, p.Reason ?? string.Empty, p.ReasonLanguage, p.ReasonCode, p.Service);
				})
				.ToArray() ?? Array.Empty<ApplicationReviewPhotoDetail>();

			Migrated.InvalidClaimDetails = claimDetails;
			Migrated.InvalidPhotoDetails = photoDetails;

			this.applicationReview = Migrated;
			this.RejectionMessage = null;
			this.RejectionCode = null;
			this.InvalidClaims = null;
			this.InvalidPhotos = null;
			this.InvalidClaimDetails = null;
			this.InvalidPhotoDetails = null;
		}
	}
}
