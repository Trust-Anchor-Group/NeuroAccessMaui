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
			HashSet<string> invalid = new(System.StringComparer.OrdinalIgnoreCase);
			if (reference is null) return invalid;
			foreach (string claim in reference.InvalidClaims ?? System.Array.Empty<string>())
			{
				if (string.IsNullOrWhiteSpace(claim)) continue;
				string key = claim.Trim(); invalid.Add(key);
				if (key.Equals(Constants.XmppProperties.BirthDay, System.StringComparison.OrdinalIgnoreCase) || key.Equals(Constants.XmppProperties.BirthMonth, System.StringComparison.OrdinalIgnoreCase) || key.Equals(Constants.XmppProperties.BirthYear, System.StringComparison.OrdinalIgnoreCase) || key.Equals(Constants.CustomXmppProperties.BirthDate, System.StringComparison.OrdinalIgnoreCase)) invalid.Add(Constants.CustomXmppProperties.BirthDate);
				if (key.StartsWith("ORGREP", System.StringComparison.OrdinalIgnoreCase)) invalid.Add("ORGREPBDATE");
			}
			foreach (string photo in reference.InvalidPhotos ?? System.Array.Empty<string>())
			{
				if (string.IsNullOrWhiteSpace(photo)) continue; invalid.Add(photo.Trim());
			}
			return invalid;
		}

		/// <summary>
		/// Generates a summary model using existing identity summary formatter (kept impure due to dependency) but wrapped to return pure model.
		/// </summary>
		public static KycSummaryModel Generate(KycProcess process, IEnumerable<Property> mappedValues, IEnumerable<LegalIdentityAttachment> attachments, ISet<string> invalid)
		{
			IdentitySummaryFormatter.KycSummaryResult summary = IdentitySummaryFormatter.BuildKycSummaryFromProperties(mappedValues, process, attachments.Select(a => new IdentitySummaryFormatter.AttachmentInfo(a.FileName ?? string.Empty, a.ContentType)), CultureInfo.CurrentCulture, invalid);
			KycOrderingComparer Comparer = KycOrderingComparer.Create(process);
			summary.Personal.Sort(Comparer.DisplayComparer);
			summary.Address.Sort(Comparer.DisplayComparer);
			summary.Attachments.Sort(Comparer.DisplayComparer);
			summary.CompanyInfo.Sort(Comparer.DisplayComparer);
			summary.CompanyAddress.Sort(Comparer.DisplayComparer);
			summary.CompanyRepresentative.Sort(Comparer.DisplayComparer);
			List<KycSummarySection> sections = new List<KycSummarySection>(6)
			{
				new KycSummarySection(Personal, summary.Personal.ToList()),
				new KycSummarySection(Address, summary.Address.ToList()),
				new KycSummarySection(Attachments, summary.Attachments.ToList()),
				new KycSummarySection(CompanyInfo, summary.CompanyInfo.ToList()),
				new KycSummarySection(CompanyAddress, summary.CompanyAddress.ToList()),
				new KycSummarySection(CompanyRepresentative, summary.CompanyRepresentative.ToList())
			};
			return new KycSummaryModel(sections);
		}
	}
}
