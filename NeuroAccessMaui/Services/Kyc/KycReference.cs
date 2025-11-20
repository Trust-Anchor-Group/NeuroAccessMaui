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
	/// Contains a local reference to a KYC process.
	/// </summary>
	[CollectionName("KycReferences")]
	[Index(nameof(UpdatedUtc), nameof(UpdatedUtc))]
	[Index(nameof(CreatedIdentityId))]
	public class KycReference
	{
		private KycProcess? process;
		private ApplicationReview? applicationReview;

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
		/// <param name="lang">Optional language.</param>
		/// <returns>The parsed and populated KYC process, or null if XML is missing.</returns>
		public async Task<KycProcess?> GetProcess(string? lang = null)
		{
			if (this.process is null && this.KycXml is not null)
			{
				this.process = await KycProcessParser.LoadProcessAsync(this.KycXml, lang).ConfigureAwait(false);

				if (this.Fields is not null)
				{
					foreach (KycFieldValue Field in this.Fields)
						this.process.Values[Field.FieldId] = Field.Value;

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

			return this.process;
		}

		/// <summary>
		/// Stores a parsed KYC process into the reference.
		/// </summary>
		/// <param name="process">KYC process.</param>
		/// <param name="xml">Process XML.</param>
		public void SetProcess(KycProcess process, string xml)
		{
			this.SetProcess(process, xml, DateTime.UtcNow, DateTime.UtcNow);
		}

		/// <summary>
		/// Stores a parsed KYC process into the reference with explicit timestamps.
		/// </summary>
		/// <param name="process">KYC process.</param>
		/// <param name="xml">Process XML.</param>
		/// <param name="created">Created time.</param>
		/// <param name="updated">Updated time.</param>
		public void SetProcess(KycProcess process, string xml, DateTime created, DateTime updated)
		{
			this.process = process;
			this.KycXml = xml;
			this.CreatedUtc = created;
			this.UpdatedUtc = updated;
			this.FetchedUtc = DateTime.UtcNow;
			this.Fields = process.Values.Select(p => new KycFieldValue(p.Key, p.Value)).ToArray();
		}

		public void ApplyFieldValue(ObservableKycField Field)
		{
			if (this.process is null || !this.process.Values.TryGetValue(Field.Id, out string? Val) || Val is null)
				return;
			Field.StringValue = Val;
		}

		// SetFieldValue removed; logic is now in Field.StringValue

		/// <summary>
		/// Creates a KycReference from a KycProcess, serializing its field values.
		/// </summary>
		/// <param name="process">The KYC process to serialize.</param>
		/// <param name="xml">The process XML.</param>
		/// <param name="friendlyName">Optional friendly name.</param>
		/// <returns>A new KycReference instance.</returns>
		public static KycReference FromProcess(KycProcess process, string xml, string? friendlyName = null)
		{
			KycReference Reference = new KycReference
			{
				process = process,
				KycXml = xml,
				CreatedUtc = DateTime.UtcNow,
				UpdatedUtc = DateTime.UtcNow,
				FetchedUtc = DateTime.UtcNow,
				Fields = process.Values.Select(P => new KycFieldValue(P.Key, P.Value)).ToArray(),
				FriendlyName = friendlyName ?? string.Empty
			};
			return Reference;
		}

		/// <summary>
		/// Ensures the current process reflects values in <see cref="Fields"/> by applying them
		/// into the cached process instance (creating it if needed) and propagating values to
		/// visible fields. Useful when <see cref="Fields"/> has been updated after a prior
		/// call to <see cref="GetProcess(string?)"/> created the cached process.
		/// </summary>
		/// <param name="lang">Optional language when creating a new process.</param>
		public async Task ApplyFieldsToProcessAsync(string? lang = null)
		{
			KycProcess? Proc = await this.GetProcess(lang).ConfigureAwait(false);

			if (Proc is null || this.Fields is null)
				return;

			foreach (KycFieldValue Field in this.Fields)
				Proc.Values[Field.FieldId] = Field.Value;

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
		/// <param name="lang">Optional language.</param>
		/// <returns>The populated KycProcess, or null if XML is missing.</returns>
		public async Task<KycProcess?> ToProcess(string? lang = null)
		{
			return await this.GetProcess(lang);
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
