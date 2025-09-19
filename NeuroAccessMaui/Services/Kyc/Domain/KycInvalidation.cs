using System;
using System.Collections.Generic;
using System.Linq;
using NeuroAccessMaui.Services.Kyc.Models;
using NeuroAccessMaui.Services.Kyc.ViewModels;

namespace NeuroAccessMaui.Services.Kyc.Domain
{
	/// <summary>
	/// Applies invalidation information from a <see cref="KycReference"/> onto an in-memory <see cref="KycProcess"/>, marking fields invalid and setting validation messages.
	/// </summary>
	internal static class KycInvalidation
	{
		public static void ApplyInvalidations(KycProcess? process, KycReference? reference, string? fallbackReason)
		{
			if (process is null || reference is null) return;
			IEnumerable<string> invalidClaims = reference.InvalidClaims ?? Array.Empty<string>();
			if (!invalidClaims.Any() && !(reference.InvalidPhotos?.Any() ?? false)) return;
			HashSet<string> invalidSet = new(invalidClaims, StringComparer.OrdinalIgnoreCase);
			Dictionary<string, string> reasons = BuildInvalidReasonsByMapping(reference, fallbackReason);
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

		private static bool IsInvalid(ObservableKycField Field, ISet<string> InvalidSet)
		{
			return Field.Mappings.Any(m => InvalidSet.Contains(m.Key) || IsGroupedDateMatch(m.Key, InvalidSet));
		}

		private static void Apply(ObservableKycField Field, Dictionary<string, string> Reasons, string? Fallback)
		{
			Field.IsValid = false;
			string? Reason = Field.Mappings.Select(m => Reasons.TryGetValue(m.Key, out string R) ? R : null).FirstOrDefault(r => r is not null);
			Field.ValidationText = !string.IsNullOrWhiteSpace(Reason) ? Reason : (Fallback ?? string.Empty);
		}

		private static Dictionary<string, string> BuildInvalidReasonsByMapping(KycReference Reference, string? Fallback)
		{
			Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase);
			foreach (KycInvalidClaim C in Reference.InvalidClaimDetails ?? Array.Empty<KycInvalidClaim>())
			{
				if (C is null || string.IsNullOrWhiteSpace(C.Claim)) continue;
				string Key = C.Claim.Trim(); string Reason = string.IsNullOrWhiteSpace(C.Reason) ? Fallback ?? string.Empty : C.Reason; Map[Key] = Reason;
				if (Key.Equals(Constants.XmppProperties.BirthDay, StringComparison.OrdinalIgnoreCase) || Key.Equals(Constants.XmppProperties.BirthMonth, StringComparison.OrdinalIgnoreCase) || Key.Equals(Constants.XmppProperties.BirthYear, StringComparison.OrdinalIgnoreCase) || Key.Equals("BDATE", StringComparison.OrdinalIgnoreCase)) Map[Constants.CustomXmppProperties.BirthDate] = Reason;
				if (Key.StartsWith("ORGREP", StringComparison.OrdinalIgnoreCase)) Map["ORGREPBDATE"] = Reason;
			}
			foreach (KycInvalidPhoto P in Reference.InvalidPhotoDetails ?? Array.Empty<KycInvalidPhoto>())
			{
				if (P is null) continue; string Key = string.IsNullOrWhiteSpace(P.Mapping) ? (P.FileName ?? string.Empty) : P.Mapping; if (string.IsNullOrWhiteSpace(Key)) continue; string Reason = string.IsNullOrWhiteSpace(P.Reason) ? Fallback ?? string.Empty : P.Reason; Map[Key.Trim()] = Reason;
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
