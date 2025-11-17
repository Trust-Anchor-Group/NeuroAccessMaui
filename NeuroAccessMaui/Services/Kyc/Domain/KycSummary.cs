using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using NeuroAccessMaui.Services.Identity;
using NeuroAccessMaui.Services.Kyc.Models;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.Services.Kyc.Domain
{
	/// <summary>
	/// Summary section representation (pure domain model).
	/// </summary>
	internal sealed record KycSummarySection(string Id, IReadOnlyList<DisplayQuad> Items);

	/// <summary>
	/// Root summary model composed of ordered sections.
	/// </summary>
	internal sealed record KycSummaryModel(IReadOnlyList<KycSummarySection> Sections)
	{
		public KycSummarySection? Get(string id) => this.Sections.FirstOrDefault(s => string.Equals(s.Id, id, System.StringComparison.OrdinalIgnoreCase));
	}

	internal static class KycSummary
	{
		// Section identifiers (match previous ViewModel collections)
		public const string Personal = "Personal";
		public const string Address = "Address";
		public const string Attachments = "Attachments";
		public const string CompanyInfo = "CompanyInfo";
		public const string CompanyAddress = "CompanyAddress";
		public const string CompanyRepresentative = "CompanyRepresentative";

		/// <summary>
		/// Builds invalid mapping key set from a <see cref="KycReference"/>.
		/// </summary>
		public static ISet<string> BuildInvalidMappingSet(KycReference? reference)
		{
			HashSet<string> Invalid = new(System.StringComparer.OrdinalIgnoreCase);
			if (reference is null) return Invalid;
			ApplicationReview? Review = reference.ApplicationReview;
			foreach (string Claim in Review?.InvalidClaims ?? System.Array.Empty<string>())
			{
				if (string.IsNullOrWhiteSpace(Claim)) continue;
				string Key = Claim.Trim(); Invalid.Add(Key);
				if (Key.Equals(Constants.XmppProperties.BirthDay, System.StringComparison.OrdinalIgnoreCase) || Key.Equals(Constants.XmppProperties.BirthMonth, System.StringComparison.OrdinalIgnoreCase) || Key.Equals(Constants.XmppProperties.BirthYear, System.StringComparison.OrdinalIgnoreCase) || Key.Equals(Constants.CustomXmppProperties.BirthDate, System.StringComparison.OrdinalIgnoreCase)) Invalid.Add(Constants.CustomXmppProperties.BirthDate);
				if (Key.StartsWith("ORGREP", System.StringComparison.OrdinalIgnoreCase)) Invalid.Add("ORGREPBDATE");
			}
			foreach (string Photo in Review?.InvalidPhotos ?? System.Array.Empty<string>())
			{
				if (string.IsNullOrWhiteSpace(Photo)) continue; Invalid.Add(Photo.Trim());
			}
			return Invalid;
		}

		/// <summary>
		/// Generates a summary model using existing identity summary formatter (kept impure due to dependency) but wrapped to return pure model.
		/// </summary>
		public static KycSummaryModel Generate(KycProcess process, IEnumerable<Property> mappedValues, IEnumerable<LegalIdentityAttachment> attachments, ISet<string> invalid)
		{
			IdentitySummaryFormatter.KycSummaryResult Summary = IdentitySummaryFormatter.BuildKycSummaryFromProperties(mappedValues, process, attachments.Select(a => new IdentitySummaryFormatter.AttachmentInfo(a.FileName ?? string.Empty, a.ContentType)), CultureInfo.CurrentCulture, invalid);
			KycOrderingComparer Comparer = KycOrderingComparer.Create(process);
			Summary.Personal.Sort(Comparer.DisplayComparer);
			Summary.Address.Sort(Comparer.DisplayComparer);
			Summary.Attachments.Sort(Comparer.DisplayComparer);
			Summary.CompanyInfo.Sort(Comparer.DisplayComparer);
			Summary.CompanyAddress.Sort(Comparer.DisplayComparer);
			Summary.CompanyRepresentative.Sort(Comparer.DisplayComparer);
			List<KycSummarySection> Sections = new List<KycSummarySection>(6)
			{
				new KycSummarySection(Personal, Summary.Personal.ToList()),
				new KycSummarySection(Address, Summary.Address.ToList()),
				new KycSummarySection(Attachments, Summary.Attachments.ToList()),
				new KycSummarySection(CompanyInfo, Summary.CompanyInfo.ToList()),
				new KycSummarySection(CompanyAddress, Summary.CompanyAddress.ToList()),
				new KycSummarySection(CompanyRepresentative, Summary.CompanyRepresentative.ToList())
			};
			return new KycSummaryModel(Sections);
		}
	}
}
