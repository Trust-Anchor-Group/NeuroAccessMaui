using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NeuroAccessMaui.Services.Kyc.Models;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Kyc
{
	/// <summary>
	/// Handles preview identity promotion into final non-preview identity applications.
	/// </summary>
	[DefaultImplementation(typeof(KycPreviewPromotionService))]
	public interface IKycPreviewPromotionService
	{
		/// <summary>
		/// Handles an approved preview identity and starts final promotion when applicable.
		/// </summary>
		/// <param name="Reference">KYC reference that tracks the preview identity.</param>
		/// <param name="Identity">Approved preview identity.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		/// <returns>The promotion result.</returns>
		Task<KycPreviewPromotionResult> HandleApprovedPreviewIdentityAsync(
			KycReference Reference,
			LegalIdentity Identity,
			CancellationToken CancellationToken = default);

		/// <summary>
		/// Promotes an approved preview identity into a final identity application.
		/// </summary>
		/// <param name="Reference">KYC reference that tracks the preview identity.</param>
		/// <param name="PreviewIdentityId">Preview identity identifier.</param>
		/// <param name="ApprovedPreviewIdentity">Approved preview identity, when already available.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		/// <returns>The promotion result.</returns>
		Task<KycPreviewPromotionResult> PromoteApprovedPreviewAsync(
			KycReference Reference,
			string PreviewIdentityId,
			LegalIdentity? ApprovedPreviewIdentity = null,
			CancellationToken CancellationToken = default);
	}

	/// <summary>
	/// Result returned from preview promotion attempts.
	/// </summary>
	public sealed class KycPreviewPromotionResult
	{
		private KycPreviewPromotionResult(bool Succeeded, LegalIdentity? FinalIdentity, string Reason)
		{
			this.Succeeded = Succeeded;
			this.FinalIdentity = FinalIdentity;
			this.Reason = Reason;
		}

		/// <summary>
		/// Gets a value indicating whether promotion changed local or server state.
		/// </summary>
		public bool Succeeded { get; }

		/// <summary>
		/// Gets the submitted final identity, when available.
		/// </summary>
		public LegalIdentity? FinalIdentity { get; }

		/// <summary>
		/// Gets a diagnostic reason for no-change results.
		/// </summary>
		public string Reason { get; }

		/// <summary>
		/// Creates a no-change result.
		/// </summary>
		/// <param name="Reason">Diagnostic reason.</param>
		/// <returns>A no-change result.</returns>
		public static KycPreviewPromotionResult NoChange(string Reason)
		{
			return new KycPreviewPromotionResult(false, null, Reason);
		}

		/// <summary>
		/// Creates a changed result.
		/// </summary>
		/// <param name="FinalIdentity">Submitted final identity, when available.</param>
		/// <returns>A changed result.</returns>
		public static KycPreviewPromotionResult Changed(LegalIdentity? FinalIdentity = null)
		{
			return new KycPreviewPromotionResult(true, FinalIdentity, string.Empty);
		}
	}

	/// <summary>
	/// Default preview promotion service.
	/// </summary>
	public sealed class KycPreviewPromotionService : IKycPreviewPromotionService, IDisposable
	{
		private readonly IKycService kycService = ServiceRef.KycService;
		private readonly KycContentPolicyService contentPolicyService = new KycContentPolicyService();
		private readonly SemaphoreSlim promotionLock = new SemaphoreSlim(1, 1);
		private bool isDisposed;

		/// <inheritdoc/>
		public async Task<KycPreviewPromotionResult> HandleApprovedPreviewIdentityAsync(
			KycReference Reference,
			LegalIdentity Identity,
			CancellationToken CancellationToken = default)
		{
			ObjectDisposedException.ThrowIf(this.isDisposed, this);

			if (Reference is null || Identity is null)
				return KycPreviewPromotionResult.NoChange("MissingReferenceOrIdentity");

			if (Identity.State != IdentityState.Approved || !Reference.IsPreviewIdentity(Identity.Id))
				return KycPreviewPromotionResult.NoChange("IdentityIsNotApprovedPreview");

			await this.kycService.MarkPreviewApprovedForFinalizationAsync(Reference, Identity).ConfigureAwait(false);
			return await this.PromoteApprovedPreviewAsync(Reference, Reference.PreviewIdentityId ?? Identity.Id, Identity, CancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public async Task<KycPreviewPromotionResult> PromoteApprovedPreviewAsync(
			KycReference Reference,
			string PreviewIdentityId,
			LegalIdentity? ApprovedPreviewIdentity = null,
			CancellationToken CancellationToken = default)
		{
			ObjectDisposedException.ThrowIf(this.isDisposed, this);

			if (Reference is null || string.IsNullOrWhiteSpace(PreviewIdentityId))
				return KycPreviewPromotionResult.NoChange("MissingReferenceOrPreviewIdentity");

			await this.promotionLock.WaitAsync(CancellationToken).ConfigureAwait(false);
			try
			{
				if (!Reference.IsPreviewIdentity(PreviewIdentityId))
					return KycPreviewPromotionResult.NoChange("PreviewIdentityMismatch");

				if (!string.IsNullOrWhiteSpace(Reference.FinalIdentityId))
					return KycPreviewPromotionResult.NoChange("FinalIdentityAlreadySubmitted");

				LegalIdentity? PreviewIdentity = ApprovedPreviewIdentity;
				if (PreviewIdentity is null)
				{
					PreviewIdentity = await ServiceRef.XmppService.GetLegalIdentity(PreviewIdentityId.Trim()).ConfigureAwait(false);
				}

				if (PreviewIdentity.State != IdentityState.Approved)
					return KycPreviewPromotionResult.NoChange("PreviewIdentityNotApproved");

				KycProcess? Process = await Reference.GetProcess().ConfigureAwait(false);
				if (Process is null || Process.ApplicationPolicy.Mode != KycApplicationMode.Preview)
					return KycPreviewPromotionResult.NoChange("ReferenceIsNotPreviewApplication");

				(IReadOnlyList<Property> Properties, IReadOnlyList<LegalIdentityAttachment> Attachments) =
					await this.kycService.PreparePropertiesAndAttachmentsAsync(Process, CancellationToken).ConfigureAwait(false);
				IReadOnlyList<Property> SubmissionProperties = BuildSubmissionProperties(Process, Properties);
				IReadOnlyList<LegalIdentityAttachment> SubmissionAttachments = BuildSubmissionAttachments(Process, Reference, Attachments);
				KycApplicationContentSet FinalContent = this.contentPolicyService.BuildFinalPromotionContent(
					Process.ApplicationPolicy,
					SubmissionProperties,
					SubmissionAttachments);
				if (FinalContent.Stage != KycApplicationContentStage.FinalPromotion)
					return KycPreviewPromotionResult.NoChange("UnexpectedPromotionContentStage");

				List<Property> FinalProperties = FinalContent.Properties.ToList();
				AddOrReplaceProperty(FinalProperties, Constants.XmppProperties.Preview, StripIdentityDomain(PreviewIdentity.Id));

				LegalIdentity FinalIdentity = await ServiceRef.XmppService.AddLegalIdentity(
					FinalProperties.ToArray(),
					false,
					FinalContent.Attachments.ToArray()).ConfigureAwait(false);

				await this.ApplyFinalIdentityToProfileAsync(FinalIdentity).ConfigureAwait(false);
				await this.kycService.ApplyFinalSubmissionAsync(Reference, FinalIdentity).ConfigureAwait(false);
				return KycPreviewPromotionResult.Changed(FinalIdentity);
			}
			finally
			{
				this.promotionLock.Release();
			}
		}

		/// <summary>
		/// Releases resources owned by the service.
		/// </summary>
		public void Dispose()
		{
			if (this.isDisposed)
				return;

			this.promotionLock.Dispose();
			this.isDisposed = true;
			GC.SuppressFinalize(this);
		}

		private async Task ApplyFinalIdentityToProfileAsync(LegalIdentity FinalIdentity)
		{
			if (FinalIdentity.State == IdentityState.Approved)
			{
				if (ServiceRef.TagProfile.LegalIdentity is null ||
					string.Equals(ServiceRef.TagProfile.LegalIdentity.Id, FinalIdentity.Id, StringComparison.OrdinalIgnoreCase))
				{
					await ServiceRef.TagProfile.SetLegalIdentity(FinalIdentity, false).ConfigureAwait(false);
				}

				await ServiceRef.TagProfile.SetIdentityApplication(null, false).ConfigureAwait(false);

				return;
			}

			await ServiceRef.TagProfile.SetIdentityApplication(FinalIdentity, false).ConfigureAwait(false);
		}

		private static void AddOrReplaceProperty(List<Property> Properties, string Name, string Value)
		{
			Properties.RemoveAll(Property => string.Equals(Property.Name, Name, StringComparison.OrdinalIgnoreCase));
			Properties.Add(new Property(Name, Value));
		}

		private static IReadOnlyList<Property> BuildSubmissionProperties(KycProcess Process, IEnumerable<Property> Properties)
		{
			List<Property> Result = Properties.ToList();
			string Jid = ServiceRef.TagProfile.LegalIdentity?.Properties.FirstOrDefault(Property => Property.Name == Constants.XmppProperties.Jid)?.Value ??
				ServiceRef.XmppService.BareJid ??
				string.Empty;
			string Phone = ServiceRef.TagProfile.LegalIdentity?.Properties.FirstOrDefault(Property => Property.Name == Constants.XmppProperties.Phone)?.Value ??
				ServiceRef.TagProfile.PhoneNumber ??
				string.Empty;
			string Email = ServiceRef.TagProfile.LegalIdentity?.Properties.FirstOrDefault(Property => Property.Name == Constants.XmppProperties.EMail)?.Value ??
				ServiceRef.TagProfile.EMail ??
				string.Empty;

			AddOrReplaceProperty(Result, Constants.XmppProperties.DeviceId, ServiceRef.PlatformSpecific.GetDeviceId());
			if (!Process.HasMapping(Constants.XmppProperties.Jid))
				AddOrReplaceProperty(Result, Constants.XmppProperties.Jid, Jid);
			if (!Process.HasMapping(Constants.XmppProperties.Phone))
				AddOrReplaceProperty(Result, Constants.XmppProperties.Phone, Phone);
			if (!Process.HasMapping(Constants.XmppProperties.EMail))
				AddOrReplaceProperty(Result, Constants.XmppProperties.EMail, Email);
			if (!Process.HasMapping(Constants.XmppProperties.Country) && !string.IsNullOrEmpty(ServiceRef.TagProfile.SelectedCountry))
				AddOrReplaceProperty(Result, Constants.XmppProperties.Country, ServiceRef.TagProfile.SelectedCountry);

			return Result;
		}

		private static IReadOnlyList<LegalIdentityAttachment> BuildSubmissionAttachments(
			KycProcess Process,
			KycReference Reference,
			IEnumerable<LegalIdentityAttachment> Attachments)
		{
			List<LegalIdentityAttachment> Result = Attachments.ToList();
			if (string.IsNullOrWhiteSpace(Reference.NfcReadoutXml))
				return Result;

			KycNfcEvidencePolicy NfcPolicy = Process.EvidencePolicy.TravelDocument.Nfc;
			string AttachmentName = string.IsNullOrWhiteSpace(NfcPolicy.AttachmentName)
				? KycNfcEvidencePolicy.DefaultAttachmentName
				: NfcPolicy.AttachmentName;
			string ContentType = string.IsNullOrWhiteSpace(NfcPolicy.ContentType)
				? KycNfcEvidencePolicy.DefaultContentType
				: NfcPolicy.ContentType;
			byte[] Data = Encoding.UTF8.GetBytes(Reference.NfcReadoutXml);

			LegalIdentityAttachment? Existing = Result.FirstOrDefault(Attachment =>
				string.Equals(Attachment.FileName, AttachmentName, StringComparison.OrdinalIgnoreCase));
			if (Existing is not null)
			{
				Existing.ContentType = ContentType;
				Existing.Data = Data;
				Existing.ContentLength = Data.Length;
				return Result;
			}

			Result.Add(new LegalIdentityAttachment(AttachmentName, ContentType, Data));
			return Result;
		}

		private static string StripIdentityDomain(string IdentityId)
		{
			string CleanIdentityId = IdentityId.Trim();
			int AtIndex = CleanIdentityId.IndexOf('@');
			return AtIndex >= 0 ? CleanIdentityId[..AtIndex] : CleanIdentityId;
		}
	}
}
