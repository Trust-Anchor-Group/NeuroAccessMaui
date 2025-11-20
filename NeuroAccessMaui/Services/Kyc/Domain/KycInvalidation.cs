using System;
using System.Collections.Generic;
using System.Linq;
using NeuroAccessMaui.Services.Identity;
using NeuroAccessMaui.Services.Kyc.Models;
using NeuroAccessMaui.Services.Kyc.ViewModels;

namespace NeuroAccessMaui.Services.Kyc.Domain
{
	/// <summary>
	/// Applies invalidation information from a <see cref="ApplicationReview"/> onto an in-memory <see cref="KycProcess"/>, marking fields invalid and setting validation messages.
	/// </summary>
	internal static class KycInvalidation
	{
		public static void ApplyInvalidations(KycProcess? process, ApplicationReview? review, string? fallbackReason)
		{
			if (process is null || review is null)
				return;
			IEnumerable<string> invalidClaims = review.InvalidClaims ?? Array.Empty<string>();
			if (!invalidClaims.Any() && !(review.InvalidPhotos?.Any() ?? false))
				return;
			HashSet<string> invalidSet = new(invalidClaims, StringComparer.OrdinalIgnoreCase);
			Dictionary<string, string> reasons = BuildInvalidReasonsByMapping(review, fallbackReason);
			foreach (KycPage Page in process.Pages)
			{
				foreach (ObservableKycField Field in Page.AllFields)
				{
					if (IsInvalid(Field, invalidSet))
						Apply(Field, reasons, fallbackReason);
				}
				foreach (KycSection Section in Page.AllSections)
				{
					foreach (ObservableKycField Field in Section.AllFields)
					{
						if (IsInvalid(Field, invalidSet))
							Apply(Field, reasons, fallbackReason);
					}
				}
			}
		}

		private static bool IsInvalid(ObservableKycField Field, HashSet<string> InvalidSet)
		{
			return Field.Mappings.Any(m => InvalidSet.Contains(m.Key) || IsGroupedDateMatch(m.Key, InvalidSet));
		}

		private static void Apply(ObservableKycField Field, Dictionary<string, string> Reasons, string? Fallback)
		{
			Field.IsValid = false;
			string? Reason = Field.Mappings.Select(m => Reasons.TryGetValue(m.Key, out string? R) ? R : null).FirstOrDefault(r => r is not null);
			Field.ValidationText = !string.IsNullOrWhiteSpace(Reason) ? Reason : (Fallback ?? string.Empty);
		}

		private static Dictionary<string, string> BuildInvalidReasonsByMapping(ApplicationReview Review, string? Fallback)
		{
			Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase);
			foreach (ApplicationReviewClaimDetail Detail in Review.InvalidClaimDetails ?? Array.Empty<ApplicationReviewClaimDetail>())
			{
				if (Detail is null || string.IsNullOrWhiteSpace(Detail.Claim))
					continue;
				string Key = Detail.Claim.Trim();
				string Reason = string.IsNullOrWhiteSpace(Detail.Reason) ? Fallback ?? string.Empty : Detail.Reason;
				Map[Key] = Reason;
				if (Key.Equals(Constants.XmppProperties.BirthDay, StringComparison.OrdinalIgnoreCase) || Key.Equals(Constants.XmppProperties.BirthMonth, StringComparison.OrdinalIgnoreCase) || Key.Equals(Constants.XmppProperties.BirthYear, StringComparison.OrdinalIgnoreCase) || Key.Equals("BDATE", StringComparison.OrdinalIgnoreCase)) Map[Constants.CustomXmppProperties.BirthDate] = Reason;
				if (Key.StartsWith("ORGREP", StringComparison.OrdinalIgnoreCase)) Map["ORGREPBDATE"] = Reason;
			}
			foreach (ApplicationReviewPhotoDetail Detail in Review.InvalidPhotoDetails ?? Array.Empty<ApplicationReviewPhotoDetail>())
			{
				if (Detail is null)
					continue;
				string Key = string.IsNullOrWhiteSpace(Detail.DisplayName) ? (Detail.FileName ?? string.Empty) : Detail.DisplayName;
				if (string.IsNullOrWhiteSpace(Key))
					continue;
				string Reason = string.IsNullOrWhiteSpace(Detail.Reason) ? Fallback ?? string.Empty : Detail.Reason;
				Map[Key.Trim()] = Reason;
			}
			return Map;
		}

		private static bool IsGroupedDateMatch(string MappingKey, ISet<string> InvalidSet)
		{
			if (MappingKey.Equals(Constants.XmppProperties.BirthDay, StringComparison.OrdinalIgnoreCase) || MappingKey.Equals(Constants.XmppProperties.BirthMonth, StringComparison.OrdinalIgnoreCase) || MappingKey.Equals(Constants.XmppProperties.BirthYear, StringComparison.OrdinalIgnoreCase))
				return InvalidSet.Contains("BDATE") || InvalidSet.Contains(Constants.XmppProperties.BirthDay) || InvalidSet.Contains(Constants.XmppProperties.BirthMonth) || InvalidSet.Contains(Constants.XmppProperties.BirthYear);
			if (MappingKey.StartsWith("ORGREP", StringComparison.OrdinalIgnoreCase))
				return InvalidSet.Contains("ORGREPBDATE") || InvalidSet.Contains("ORGREPBDAY") || InvalidSet.Contains("ORGREPBMONTH") || InvalidSet.Contains("ORGREPBYEAR");
			return false;
		}
	}
}
