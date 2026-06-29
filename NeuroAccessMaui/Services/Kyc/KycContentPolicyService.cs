using System.Collections.Generic;
using System.IO;
using System.Linq;
using NeuroAccessMaui.Services.Kyc.Models;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.Services.Kyc
{
	/// <summary>
	/// Computes identity application content from a KYC content routing policy.
	/// </summary>
	public class KycContentPolicyService
	{
		private static readonly string[] appRequiredPropertyKeys = new string[]
		{
			Constants.XmppProperties.DeviceId,
			Constants.XmppProperties.Jid,
			Constants.XmppProperties.Phone,
			Constants.XmppProperties.EMail,
			Constants.XmppProperties.Country
		};

		/// <summary>
		/// Builds the property and attachment set for the first application.
		/// </summary>
		/// <param name="Policy">The application policy to apply.</param>
		/// <param name="Properties">The prepared identity properties.</param>
		/// <param name="Attachments">The prepared identity attachments.</param>
		/// <returns>The content that should be sent with the first application.</returns>
		public KycApplicationContentSet BuildFirstApplicationContent(
			KycApplicationPolicy Policy,
			IEnumerable<Property> Properties,
			IEnumerable<LegalIdentityAttachment> Attachments)
		{
			KycContentPolicy Content = Policy.Content;

			if (!HasRoutingRules(Content))
			{
				return new KycApplicationContentSet(
					KycApplicationContentStage.FirstApplication,
					Properties.ToList(),
					Attachments.ToList());
			}

			HashSet<string> PropertyKeys = CreateKeySet(Content.IncludePropertyKeys, Content.KeepPropertyKeys, appRequiredPropertyKeys);
			HashSet<string> AttachmentNames = CreateKeySet(Content.IncludeAttachmentNames, Content.KeepAttachmentNames);
			HashSet<string> AttachmentTags = CreateKeySet(Content.IncludeAttachmentTags, Content.KeepAttachmentTags);

			return new KycApplicationContentSet(
				KycApplicationContentStage.FirstApplication,
				FilterProperties(Properties, PropertyKeys),
				FilterAttachments(Attachments, AttachmentNames, AttachmentTags));
		}

		/// <summary>
		/// Builds the property and attachment set for a final promoted application.
		/// </summary>
		/// <param name="Policy">The application policy to apply.</param>
		/// <param name="Properties">The prepared identity properties.</param>
		/// <param name="Attachments">The prepared identity attachments.</param>
		/// <returns>The content that should survive into the final promoted application.</returns>
		public KycApplicationContentSet BuildFinalPromotionContent(
			KycApplicationPolicy Policy,
			IEnumerable<Property> Properties,
			IEnumerable<LegalIdentityAttachment> Attachments)
		{
			KycContentPolicy Content = Policy.Content;
			HashSet<string> PropertyKeys = CreateKeySet(Content.KeepPropertyKeys, appRequiredPropertyKeys);
			HashSet<string> AttachmentNames = CreateKeySet(Content.KeepAttachmentNames);
			HashSet<string> AttachmentTags = CreateKeySet(Content.KeepAttachmentTags);

			return new KycApplicationContentSet(
				KycApplicationContentStage.FinalPromotion,
				FilterProperties(Properties, PropertyKeys),
				FilterAttachments(Attachments, AttachmentNames, AttachmentTags));
		}

		/// <summary>
		/// Gets the app-owned property keys that should be kept available for application submission.
		/// </summary>
		/// <returns>The required application property keys.</returns>
		public IReadOnlyList<string> GetAppRequiredPropertyKeys()
		{
			return appRequiredPropertyKeys;
		}

		private static bool HasRoutingRules(KycContentPolicy Content)
		{
			return Content.IncludePropertyKeys.Count > 0 ||
				Content.KeepPropertyKeys.Count > 0 ||
				Content.IncludeAttachmentNames.Count > 0 ||
				Content.IncludeAttachmentTags.Count > 0 ||
				Content.KeepAttachmentNames.Count > 0 ||
				Content.KeepAttachmentTags.Count > 0;
		}

		private static HashSet<string> CreateKeySet(params IEnumerable<string>[] Sources)
		{
			HashSet<string> Result = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

			foreach (IEnumerable<string> Source in Sources)
			{
				foreach (string Value in Source)
				{
					if (!string.IsNullOrWhiteSpace(Value))
					{
						Result.Add(Value.Trim());
					}
				}
			}

			return Result;
		}

		private static IReadOnlyList<Property> FilterProperties(IEnumerable<Property> Properties, HashSet<string> PropertyKeys)
		{
			if (PropertyKeys.Count == 0)
			{
				return new List<Property>();
			}

			return Properties
				.Where(Property => Property is not null &&
					!string.IsNullOrWhiteSpace(Property.Name) &&
					PropertyKeys.Contains(Property.Name.Trim()))
				.ToList();
		}

		private static IReadOnlyList<LegalIdentityAttachment> FilterAttachments(
			IEnumerable<LegalIdentityAttachment> Attachments,
			HashSet<string> AttachmentNames,
			HashSet<string> AttachmentTags)
		{
			if (AttachmentNames.Count == 0 && AttachmentTags.Count == 0)
			{
				return new List<LegalIdentityAttachment>();
			}

			return Attachments
				.Where(Attachment => Attachment is not null &&
					(MatchesAttachmentName(Attachment, AttachmentNames) ||
					MatchesAttachmentTag(Attachment, AttachmentTags)))
				.ToList();
		}

		private static bool MatchesAttachmentName(LegalIdentityAttachment Attachment, HashSet<string> AttachmentNames)
		{
			string FileName = Attachment.FileName?.Trim() ?? string.Empty;

			return !string.IsNullOrWhiteSpace(FileName) && AttachmentNames.Contains(FileName);
		}

		private static bool MatchesAttachmentTag(LegalIdentityAttachment Attachment, HashSet<string> AttachmentTags)
		{
			string FileName = Attachment.FileName?.Trim() ?? string.Empty;
			string FileStem = Path.GetFileNameWithoutExtension(FileName) ?? FileName;

			return !string.IsNullOrWhiteSpace(FileStem) && AttachmentTags.Contains(FileStem.Trim());
		}
	}

	/// <summary>
	/// Identifies the application stage a content set was selected for.
	/// </summary>
	public enum KycApplicationContentStage
	{
		/// <summary>
		/// Content selected for the first identity application.
		/// </summary>
		FirstApplication,

		/// <summary>
		/// Content selected for the final identity application promoted from an approved preview.
		/// </summary>
		FinalPromotion
	}

	/// <summary>
	/// Represents the properties and attachments selected for an identity application stage.
	/// </summary>
	public class KycApplicationContentSet
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="KycApplicationContentSet"/> class.
		/// </summary>
		/// <param name="Stage">The application stage the content was selected for.</param>
		/// <param name="Properties">The selected identity properties.</param>
		/// <param name="Attachments">The selected identity attachments.</param>
		public KycApplicationContentSet(
			KycApplicationContentStage Stage,
			IReadOnlyList<Property> Properties,
			IReadOnlyList<LegalIdentityAttachment> Attachments)
		{
			this.Stage = Stage;
			this.Properties = Properties;
			this.Attachments = Attachments;
		}

		/// <summary>
		/// Gets the application stage the content was selected for.
		/// </summary>
		public KycApplicationContentStage Stage { get; }

		/// <summary>
		/// Gets the selected identity properties.
		/// </summary>
		public IReadOnlyList<Property> Properties { get; }

		/// <summary>
		/// Gets the selected identity attachments.
		/// </summary>
		public IReadOnlyList<LegalIdentityAttachment> Attachments { get; }
	}
}
