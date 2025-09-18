using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Data;
using NeuroAccessMaui.Services.Kyc.Models;
using NeuroAccessMaui.Services.Kyc.ViewModels;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.Services.Identity
{
	/// <summary>
	/// Represents a displayable set of three related strings: a label, a value, and an optional mapping.
	/// </summary>
	public sealed class DisplayQuad
	{
		public string Label { get; }
		public string Value { get; }
		public string? Mapping { get; }
		public bool FromField { get; }
		public bool IsInvalid { get; }

		public DisplayQuad(string Label, string Value, string? Mapping, bool FromField, bool IsInvalid = false)
		{
			this.Label = Label;
			this.Value = Value;
			this.Mapping = Mapping;
			this.FromField = FromField;
			this.IsInvalid = IsInvalid;
		}
	}

	/// <summary>
	/// Builds friendly, localized summaries for identity-like data from mapped properties and attachments.
	/// Designed to be used by both View Identity and KYC preview summaries.
	/// </summary>
	public static class IdentitySummaryFormatter
    {
        public sealed class AttachmentInfo
        {
            public string FileName { get; }
            public string? ContentType { get; }

            public AttachmentInfo(string FileName, string? ContentType)
            {
                this.FileName = FileName;
                this.ContentType = ContentType;
            }
        }

        public sealed class KycSummaryResult
        {
            public List<DisplayQuad> Personal { get; } = new List<DisplayQuad>();
            public List<DisplayQuad> Address { get; } = new List<DisplayQuad>();
            public List<DisplayQuad> Attachments { get; } = new List<DisplayQuad>();
			public List<DisplayQuad> CompanyInfo { get; } = new List<DisplayQuad>();
			public List<DisplayQuad> CompanyAddress { get; } = new List<DisplayQuad>();
			public List<DisplayQuad> CompanyRepresentative { get; } = new List<DisplayQuad>();
		}

        public sealed class DisplayField
        {
            public string Key { get; }
            public string Label { get; }
            public string Value { get; }
            public bool IsReviewable { get; }

            public DisplayField(string Key, string Label, string Value, bool IsReviewable)
            {
                this.Key = Key;
                this.Label = Label;
                this.Value = Value;
                this.IsReviewable = IsReviewable;
            }
        }

        public sealed class IdentityGroupsResult
        {
            public List<DisplayField> Personal { get; } = new List<DisplayField>();
            public List<DisplayField> Organization { get; } = new List<DisplayField>();
            public List<DisplayField> Technical { get; } = new List<DisplayField>();
            public List<DisplayField> Other { get; } = new List<DisplayField>();
        }

        /// <summary>
        /// Build a KYC-oriented summary (Personal, Address, Attachments) from mapped properties.
        /// </summary>
        public static KycSummaryResult BuildKycSummaryFromProperties(IEnumerable<Property> Properties,
			KycProcess Process,
            IEnumerable<AttachmentInfo>? Attachments = null,
            CultureInfo? Culture = null,
            ISet<string>? InvalidMappings = null)
        {
            CultureInfo EffectiveCulture = Culture ?? CultureInfo.CurrentCulture;

            Dictionary<string, string> LabelMap = GetLabelMap();

            // Classification sets
			HashSet<string> PersonalKeys = new(StringComparer.OrdinalIgnoreCase)
			{
				Constants.XmppProperties.FullName,
				Constants.CustomXmppProperties.BirthDate,
				Constants.XmppProperties.PersonalNumber,
				Constants.XmppProperties.BirthDay,
				Constants.XmppProperties.BirthMonth,
				Constants.XmppProperties.BirthYear,
				Constants.XmppProperties.Nationality,
				Constants.XmppProperties.Gender,
				Constants.XmppProperties.Phone,
                Constants.XmppProperties.EMail
            };

            HashSet<string> AddressKeys = new(StringComparer.OrdinalIgnoreCase)
            {
                Constants.XmppProperties.Address,
                Constants.XmppProperties.Address2,
                Constants.XmppProperties.Area,
                Constants.XmppProperties.City,
                Constants.XmppProperties.ZipCode,
                Constants.XmppProperties.Region,
                Constants.XmppProperties.Country
            };

			HashSet<string> CompanyInfoKeys = new(StringComparer.OrdinalIgnoreCase)
			{
				Constants.XmppProperties.OrgNumber,
				Constants.XmppProperties.OrgName,
				"ORGTRADENAME"
			};

			HashSet<string> CompanyAddressKeys = new(StringComparer.OrdinalIgnoreCase)
			{
				Constants.XmppProperties.OrgAddress,
				Constants.XmppProperties.OrgAddress2,
				Constants.XmppProperties.OrgZipCode,
				Constants.XmppProperties.OrgArea,
				Constants.XmppProperties.OrgCity,
				Constants.XmppProperties.OrgRegion,
				Constants.XmppProperties.OrgCountry
			};

			HashSet<string> CompanyRepresentativeKeys = new(StringComparer.OrdinalIgnoreCase)
			{
				"ORGREPNAME",
				"ORGREPCPF",
				"ORGREPBDATE",
				"ORGREPRG",
				"ORGEMAIL",
				"ORGPHONE"
			};

			// Prepare dictionary for quick lookup
			Dictionary<string, string> Dict = new(StringComparer.OrdinalIgnoreCase);
            foreach (Property P in Properties ?? Array.Empty<Property>())
            {
                if (P is null)
                    continue;

                string Name = P.Name ?? string.Empty;
                string Value = P.Value ?? string.Empty;
                if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Value))
                    continue;

                Dict[Name] = Value.Trim();
            }

            // Compose BirthDate if we have parts
			string DayStr = Get(Dict, Constants.XmppProperties.BirthDay);
			string MonthStr = Get(Dict, Constants.XmppProperties.BirthMonth);
			string YearStr = Get(Dict, Constants.XmppProperties.BirthYear);

			if (int.TryParse(DayStr, out int Day) && int.TryParse(MonthStr, out int Month) && int.TryParse(YearStr, out int Year))
			{
				try
				{
					DateTime BirthDate = new(Year, Month, Day);
					Dict[Constants.CustomXmppProperties.BirthDate] = BirthDate.ToString("D", EffectiveCulture);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}
			}

			// Compose Company Representative BirthDate if we have parts
			DayStr = Get(Dict, "ORGREPBDAY");
			MonthStr = Get(Dict, "ORGREPBMONTH");
			YearStr = Get(Dict, "ORGREPBYEAR");

			if (int.TryParse(DayStr, out Day) && int.TryParse(MonthStr, out Month) && int.TryParse(YearStr, out Year))
			{
				try
				{
					DateTime BirthDate = new(Year, Month, Day);
					Dict["ORGREPBDATE"] = BirthDate.ToString("d", EffectiveCulture);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}
			}

			KycSummaryResult Result = new();

            // Personal
			foreach (string Key in PersonalKeys)
			{
				if (Key.Equals(Constants.XmppProperties.BirthDay, StringComparison.OrdinalIgnoreCase) ||
					Key.Equals(Constants.XmppProperties.BirthMonth, StringComparison.OrdinalIgnoreCase) ||
					Key.Equals(Constants.XmppProperties.BirthYear, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				if (!Dict.TryGetValue(Key, out string? Val) || string.IsNullOrWhiteSpace(Val))
					continue;

				string Label = GetLabel(LabelMap, Key);

				if (Key.Equals(Constants.XmppProperties.Nationality, StringComparison.OrdinalIgnoreCase))
					Val = ISO_3166_1.ToName(Val) ?? Val;

				if (Key.Equals(Constants.XmppProperties.Gender, StringComparison.OrdinalIgnoreCase))
				{
					string Normalized = Val.Trim().ToUpperInvariant();
					string ResourceKey = Normalized switch
					{
						"M" or "MALE" => nameof(AppResources.Male),
						"F" or "FEMALE" => nameof(AppResources.Female),
						_ => nameof(AppResources.Other)
					};
					Microsoft.Extensions.Localization.LocalizedString Localized = ServiceRef.Localizer[ResourceKey, false];
					if (!Localized.ResourceNotFound)
						Val = Localized.Value;
				}

				bool IsInvalid = InvalidMappings?.Contains(Key) ?? false;
				Result.Personal.Add(new DisplayQuad(Label, Val, Key, Process.HasMapping(Key), IsInvalid));
			}

            // Address
            foreach (string Key in AddressKeys)
            {
                if (!Dict.TryGetValue(Key, out string? Val) || string.IsNullOrWhiteSpace(Val))
                    continue;


                string Label = GetLabel(LabelMap, Key);

				if (Key.Equals(Constants.XmppProperties.Country, StringComparison.OrdinalIgnoreCase))
					Val = ISO_3166_1.ToName(Val) ?? Val;

				bool IsInvalid = InvalidMappings?.Contains(Key) ?? false;
				Result.Address.Add(new DisplayQuad(Label, Val, Key, Process.HasMapping(Key), IsInvalid));
            }

			// Company Info
			foreach (string Key in CompanyInfoKeys)
			{
				if (!Dict.TryGetValue(Key, out string? Val) || string.IsNullOrWhiteSpace(Val))
					continue;

				string Label = GetLabel(LabelMap, Key);
				bool IsInvalid = InvalidMappings?.Contains(Key) ?? false;
				Result.CompanyInfo.Add(new DisplayQuad(Label, Val, Key, Process.HasMapping(Key), IsInvalid));
			}

			// Company Address
			foreach (string Key in CompanyAddressKeys)
			{
				if (!Dict.TryGetValue(Key, out string? Val) || string.IsNullOrWhiteSpace(Val))
					continue;

				string Label = GetLabel(LabelMap, Key);
				bool IsInvalid = InvalidMappings?.Contains(Key) ?? false;
				Result.CompanyAddress.Add(new DisplayQuad(Label, Val, Key, Process.HasMapping(Key), IsInvalid));
			}

			// Company Representative
			foreach (string Key in CompanyRepresentativeKeys)
			{
				if (!Dict.TryGetValue(Key, out string? Val) || string.IsNullOrWhiteSpace(Val))
					continue;
				string Label = GetLabel(LabelMap, Key);
				bool IsInvalid = InvalidMappings?.Contains(Key) ?? false;
				Result.CompanyRepresentative.Add(new DisplayQuad(Label, Val, Key, Process.HasMapping(Key), IsInvalid));
			}

			// Attachments
			if (Attachments is not null)
            {
                foreach (AttachmentInfo Att in Attachments)
                {
                    if (string.IsNullOrEmpty(Att.FileName))
                        continue;

                    string Base = Att.FileName.Split('.')[0];

                    string Description;
                    switch (Base)
                    {
                        case "Passport":
                            {
                                Microsoft.Extensions.Localization.LocalizedString L = ServiceRef.Localizer[nameof(AppResources.Attachment_Passport), false];
                                Description = L.ResourceNotFound ? Base : L.Value;
                                break;
                            }
                        case "IdCardFront":
                            {
                                Microsoft.Extensions.Localization.LocalizedString L = ServiceRef.Localizer[nameof(AppResources.Attachment_IdCardFront), false];
                                Description = L.ResourceNotFound ? Base : L.Value;
                                break;
                            }
                        case "IdCardBack":
                            {
                                Microsoft.Extensions.Localization.LocalizedString L = ServiceRef.Localizer[nameof(AppResources.Attachment_IdCardBack), false];
                                Description = L.ResourceNotFound ? Base : L.Value;
                                break;
                            }
                        case "DriverLicenseFront":
                            {
                                Microsoft.Extensions.Localization.LocalizedString L = ServiceRef.Localizer[nameof(AppResources.Attachment_DriverLicenseFront), false];
                                Description = L.ResourceNotFound ? Base : L.Value;
                                break;
                            }
                        case "DriverLicenseBack":
                            {
                                Microsoft.Extensions.Localization.LocalizedString L = ServiceRef.Localizer[nameof(AppResources.Attachment_DriverLicenseBack), false];
                                Description = L.ResourceNotFound ? Base : L.Value;
                                break;
                            }
                        case "ProfilePhoto":
                            {
                                Microsoft.Extensions.Localization.LocalizedString L = ServiceRef.Localizer[nameof(AppResources.Attachment_ProfilePhoto), false];
                                Description = L.ResourceNotFound ? Base : L.Value;
                                break;
                            }
						case "ORGAOA":
							{
								Microsoft.Extensions.Localization.LocalizedString L = ServiceRef.Localizer[nameof(AppResources.Attachment_ArticleOfAssociation), false];
								Description = L.ResourceNotFound ? Base : L.Value;
								break;
							}
                        default:
                            Description = Att.FileName;
                            break;
                    }

                    bool IsInvalid = InvalidMappings?.Contains(Base) ?? false;
                    Result.Attachments.Add(new DisplayQuad(Att.FileName, Description, Base, Process.HasMapping(Base), IsInvalid));
                }
            }

            return Result;
        }

		/// <summary>
		/// Build grouped identity fields (Personal/Organization/Technical/Other) from a LegalIdentity.
		/// Includes custom/composed fields and metadata fields similar to the ViewIdentityViewModel.
		/// </summary>
		public static IdentityGroupsResult BuildIdentityGroups(LegalIdentity Identity, CultureInfo? Culture = null)
        {
            CultureInfo EffectiveCulture = Culture ?? CultureInfo.CurrentCulture;

            // Reviewable keys set
            HashSet<string> ReviewableKeys = new(StringComparer.OrdinalIgnoreCase)
            {
                Constants.XmppProperties.FirstName,
                Constants.XmppProperties.MiddleNames,
                Constants.XmppProperties.LastNames,
                Constants.XmppProperties.PersonalNumber,
                Constants.XmppProperties.Address,
                Constants.XmppProperties.Address2,
                Constants.XmppProperties.ZipCode,
                Constants.XmppProperties.Area,
                Constants.XmppProperties.City,
                Constants.XmppProperties.Region,
                Constants.XmppProperties.Country,
                Constants.XmppProperties.BirthDay,
                Constants.XmppProperties.BirthMonth,
                Constants.XmppProperties.BirthYear,
                Constants.XmppProperties.OrgName,
                Constants.XmppProperties.OrgDepartment,
                Constants.XmppProperties.OrgRole,
                Constants.XmppProperties.OrgAddress,
                Constants.XmppProperties.OrgAddress2,
                Constants.XmppProperties.OrgZipCode,
                Constants.XmppProperties.OrgArea,
                Constants.XmppProperties.OrgCity,
                Constants.XmppProperties.OrgRegion,
                Constants.XmppProperties.OrgCountry,
                Constants.XmppProperties.OrgNumber
            };

            // Classification sets
            HashSet<string> PersonalKeys = new(StringComparer.OrdinalIgnoreCase)
            {
                Constants.XmppProperties.FirstName,
                Constants.XmppProperties.MiddleNames,
                Constants.XmppProperties.LastNames,
                Constants.CustomXmppProperties.BirthDate,
                Constants.XmppProperties.BirthDay,
                Constants.XmppProperties.BirthMonth,
                Constants.XmppProperties.BirthYear,
                Constants.XmppProperties.Address,
                Constants.XmppProperties.Address2,
                Constants.XmppProperties.ZipCode,
                Constants.XmppProperties.Area,
                Constants.XmppProperties.City,
                Constants.XmppProperties.Region,
                Constants.XmppProperties.Country,
                Constants.XmppProperties.PersonalNumber,
                Constants.XmppProperties.Nationality,
                Constants.XmppProperties.Gender,
                Constants.XmppProperties.Phone,
                Constants.XmppProperties.EMail
            };

            HashSet<string> OrgKeys = new(StringComparer.OrdinalIgnoreCase)
            {
                Constants.XmppProperties.OrgName,
                Constants.XmppProperties.OrgDepartment,
                Constants.XmppProperties.OrgRole,
                Constants.XmppProperties.OrgAddress,
                Constants.XmppProperties.OrgAddress2,
                Constants.XmppProperties.OrgZipCode,
                Constants.XmppProperties.OrgArea,
                Constants.XmppProperties.OrgCity,
                Constants.XmppProperties.OrgRegion,
                Constants.XmppProperties.OrgCountry,
                Constants.XmppProperties.OrgNumber
            };

            HashSet<string> TechnicalKeys = new(StringComparer.OrdinalIgnoreCase)
            {
                Constants.XmppProperties.Jid,
                Constants.CustomXmppProperties.Neuro_Id,
                Constants.CustomXmppProperties.Provider,
                Constants.CustomXmppProperties.State,
                Constants.CustomXmppProperties.Created,
                Constants.CustomXmppProperties.Updated,
                Constants.CustomXmppProperties.From,
                Constants.CustomXmppProperties.To,
                Constants.XmppProperties.DeviceId
            };

            Dictionary<string, string> LabelMap = GetIdentityLabelMap();
            IdentityGroupsResult Result = new();
            HashSet<string> UsedKeys = new(StringComparer.OrdinalIgnoreCase);

            // Custom/composed BirthDate
            string D = Identity[Constants.XmppProperties.BirthDay];
            string M = Identity[Constants.XmppProperties.BirthMonth];
            string Y = Identity[Constants.XmppProperties.BirthYear];
            if (int.TryParse(D, out int Day) && int.TryParse(M, out int Month) && int.TryParse(Y, out int Year))
            {
                try
                {
                    string BirthDateStr = new DateTime(Year, Month, Day).ToString("d", EffectiveCulture);
                    AddField(Result, PersonalKeys, OrgKeys, TechnicalKeys, ReviewableKeys,
                        Constants.CustomXmppProperties.BirthDate,
                        GetLabel(LabelMap, Constants.CustomXmppProperties.BirthDate),
                        BirthDateStr);
                    UsedKeys.Add(Constants.XmppProperties.BirthDay);
                    UsedKeys.Add(Constants.XmppProperties.BirthMonth);
                    UsedKeys.Add(Constants.XmppProperties.BirthYear);
                }
                catch (Exception Ex)
                {
                    ServiceRef.LogService.LogException(Ex);
                }
            }

            // Iterate raw properties
            foreach (Property? Prop in Identity.Properties ?? Array.Empty<Property>())
            {
                if (Prop is null || UsedKeys.Contains(Prop.Name))
                    continue;

                string Key = Prop.Name;
                string Value = Prop.Value ?? string.Empty;
                if (string.IsNullOrWhiteSpace(Value))
                    continue;

                AddField(Result, PersonalKeys, OrgKeys, TechnicalKeys, ReviewableKeys,
                    Key, GetLabel(LabelMap, Key), Value);
                UsedKeys.Add(Key);
            }

            // Extra metadata fields
            AddField(Result, PersonalKeys, OrgKeys, TechnicalKeys, ReviewableKeys,
                Constants.CustomXmppProperties.Neuro_Id,
                GetLabel(LabelMap, Constants.CustomXmppProperties.Neuro_Id), Identity.Id ?? string.Empty);

            AddField(Result, PersonalKeys, OrgKeys, TechnicalKeys, ReviewableKeys,
                Constants.CustomXmppProperties.Provider,
                GetLabel(LabelMap, Constants.CustomXmppProperties.Provider), Identity.Provider ?? string.Empty);

            AddField(Result, PersonalKeys, OrgKeys, TechnicalKeys, ReviewableKeys,
                Constants.CustomXmppProperties.State,
                GetLabel(LabelMap, Constants.CustomXmppProperties.State), ServiceRef.Localizer["IdentityState_" + Identity.State.ToString()]);

            AddField(Result, PersonalKeys, OrgKeys, TechnicalKeys, ReviewableKeys,
                Constants.CustomXmppProperties.Created,
                GetLabel(LabelMap, Constants.CustomXmppProperties.Created), Identity.Created.ToString("g", EffectiveCulture));

            AddField(Result, PersonalKeys, OrgKeys, TechnicalKeys, ReviewableKeys,
                Constants.CustomXmppProperties.Updated,
                GetLabel(LabelMap, Constants.CustomXmppProperties.Updated), Identity.Updated.ToString("g", EffectiveCulture));

            AddField(Result, PersonalKeys, OrgKeys, TechnicalKeys, ReviewableKeys,
                Constants.CustomXmppProperties.From,
                GetLabel(LabelMap, Constants.CustomXmppProperties.From), Identity.From.ToString("d", EffectiveCulture));

            AddField(Result, PersonalKeys, OrgKeys, TechnicalKeys, ReviewableKeys,
                Constants.CustomXmppProperties.To,
                GetLabel(LabelMap, Constants.CustomXmppProperties.To), Identity.To.ToString("d", EffectiveCulture));

            return Result;
        }

        private static string Get(Dictionary<string, string> Dict, string Key)
        {
            if (Dict.TryGetValue(Key, out string? Value))
                return Value;
            return string.Empty;
        }

        private static string GetLabel(Dictionary<string, string> LabelMap, string Key)
        {
            if (LabelMap.TryGetValue(Key, out string? Label) && !string.IsNullOrEmpty(Label))
                return Label;
            return Key;
        }

        private static Dictionary<string, string> GetLabelMap()
        {
            // Use ServiceRef.Localizer for labels used in KYC summary
            Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
            {
                { Constants.XmppProperties.FullName, ServiceRef.Localizer[nameof(AppResources.FullName)].Value },
                { Constants.CustomXmppProperties.BirthDate, ServiceRef.Localizer[nameof(AppResources.BirthDate)].Value },
                { Constants.XmppProperties.PersonalNumber, ServiceRef.Localizer[nameof(AppResources.PersonalNumber)].Value },
                { Constants.XmppProperties.Nationality, ServiceRef.Localizer[nameof(AppResources.Nationality)].Value },
                { Constants.XmppProperties.Gender, ServiceRef.Localizer[nameof(AppResources.Gender)].Value },
                { Constants.XmppProperties.Phone, ServiceRef.Localizer[nameof(AppResources.PhoneNr)].Value },
                { Constants.XmppProperties.EMail, ServiceRef.Localizer[nameof(AppResources.EMail)].Value },
                { Constants.XmppProperties.Address, ServiceRef.Localizer[nameof(AppResources.Address)].Value },
                { Constants.XmppProperties.Address2, ServiceRef.Localizer[nameof(AppResources.Address2)].Value },
                { Constants.XmppProperties.Area, ServiceRef.Localizer[nameof(AppResources.Area)].Value },
                { Constants.XmppProperties.City, ServiceRef.Localizer[nameof(AppResources.City)].Value },
                { Constants.XmppProperties.ZipCode, ServiceRef.Localizer[nameof(AppResources.ZipCode)].Value },
                { Constants.XmppProperties.Region, ServiceRef.Localizer[nameof(AppResources.Region)].Value },
                { Constants.XmppProperties.Country, ServiceRef.Localizer[nameof(AppResources.Country)].Value },

				// Organization fields
				{ Constants.XmppProperties.OrgNumber, ServiceRef.Localizer[nameof(AppResources.OrgNumber)].Value },
				{ Constants.XmppProperties.OrgName, ServiceRef.Localizer[nameof(AppResources.OrgName)].Value },
				{ "ORGTRADENAME", ServiceRef.Localizer[nameof(AppResources.TradeName)].Value },
				{ Constants.XmppProperties.OrgAddress, ServiceRef.Localizer[nameof(AppResources.OrgAddress)].Value },
				{ Constants.XmppProperties.OrgAddress2, ServiceRef.Localizer[nameof(AppResources.OrgAddress2)].Value },
				{ Constants.XmppProperties.OrgArea, ServiceRef.Localizer[nameof(AppResources.OrgArea)].Value },
				{ Constants.XmppProperties.OrgCity, ServiceRef.Localizer[nameof(AppResources.OrgCity)].Value },
				{ Constants.XmppProperties.OrgZipCode, ServiceRef.Localizer[nameof(AppResources.OrgZipCode)].Value },
				{ Constants.XmppProperties.OrgRegion, ServiceRef.Localizer[nameof(AppResources.OrgRegion)].Value },
				{ Constants.XmppProperties.OrgCountry, ServiceRef.Localizer[nameof(AppResources.OrgCountry)].Value },
				{ "ORGREPNAME", ServiceRef.Localizer[nameof(AppResources.OrgRepName)].Value },
				{ "ORGREPCPF", ServiceRef.Localizer[nameof(AppResources.OrgRepCPF)].Value },
				{ "ORGREPBDATE", ServiceRef.Localizer[nameof(AppResources.OrgRepBirthDate)].Value },
				{ "ORGREPRG", ServiceRef.Localizer[nameof(AppResources.OrgRepRG)].Value },
				{ "ORGEMAIL", ServiceRef.Localizer[nameof(AppResources.OrgEMail)].Value },
				{ "ORGPHONE", ServiceRef.Localizer[nameof(AppResources.OrgPhone)].Value }
			};

            return Map;
        }

        private static Dictionary<string, string> GetIdentityLabelMap()
        {
            Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
            {
                { Constants.XmppProperties.FirstName,   ServiceRef.Localizer[nameof(AppResources.FirstName)].Value},
                { Constants.XmppProperties.MiddleNames, ServiceRef.Localizer[nameof(AppResources.MiddleNames)].Value},
                { Constants.XmppProperties.LastNames,   ServiceRef.Localizer[nameof(AppResources.LastNames)].Value},
                { Constants.CustomXmppProperties.BirthDate, ServiceRef.Localizer[nameof(AppResources.BirthDate)].Value},
                { Constants.XmppProperties.Address,     ServiceRef.Localizer[nameof(AppResources.Address)].Value},
                { Constants.XmppProperties.Address2,    ServiceRef.Localizer[nameof(AppResources.Address2)].Value},
                { Constants.XmppProperties.ZipCode,     ServiceRef.Localizer[nameof(AppResources.ZipCode)].Value},
                { Constants.XmppProperties.Area,        ServiceRef.Localizer[nameof(AppResources.Area)].Value},
                { Constants.XmppProperties.City,        ServiceRef.Localizer[nameof(AppResources.City)].Value},
                { Constants.XmppProperties.Region,      ServiceRef.Localizer[nameof(AppResources.Region)].Value},
                { Constants.XmppProperties.Country,     ServiceRef.Localizer[nameof(AppResources.Country)].Value},
                { Constants.XmppProperties.Nationality, ServiceRef.Localizer[nameof(AppResources.Nationality)].Value},
                { Constants.XmppProperties.PersonalNumber, ServiceRef.Localizer[nameof(AppResources.PersonalNumber)].Value},
                { Constants.XmppProperties.Gender, ServiceRef.Localizer[nameof(AppResources.Gender)].Value},
                { Constants.XmppProperties.Phone, ServiceRef.Localizer[nameof(AppResources.PhoneNr)].Value},
                { Constants.XmppProperties.EMail, ServiceRef.Localizer[nameof(AppResources.EMail)].Value},
                { Constants.XmppProperties.OrgName,    ServiceRef.Localizer[nameof(AppResources.OrgName)].Value},
                { Constants.XmppProperties.OrgDepartment, ServiceRef.Localizer[nameof(AppResources.OrgDepartment)].Value},
                { Constants.XmppProperties.OrgRole,     ServiceRef.Localizer[nameof(AppResources.OrgRole)].Value},
                { Constants.XmppProperties.OrgAddress,  ServiceRef.Localizer[nameof(AppResources.OrgAddress)].Value},
                { Constants.XmppProperties.OrgAddress2, ServiceRef.Localizer[nameof(AppResources.OrgAddress2)].Value},
                { Constants.XmppProperties.OrgZipCode,  ServiceRef.Localizer[nameof(AppResources.OrgZipCode)].Value},
                { Constants.XmppProperties.OrgArea,     ServiceRef.Localizer[nameof(AppResources.OrgArea)].Value},
                { Constants.XmppProperties.OrgCity,     ServiceRef.Localizer[nameof(AppResources.OrgCity)].Value},
                { Constants.XmppProperties.OrgRegion,   ServiceRef.Localizer[nameof(AppResources.OrgRegion)].Value},
                { Constants.XmppProperties.OrgCountry,  ServiceRef.Localizer[nameof(AppResources.OrgCountry)].Value},
                { Constants.XmppProperties.OrgNumber,   ServiceRef.Localizer[nameof(AppResources.OrgNumber)].Value},
                { Constants.XmppProperties.Jid,   ServiceRef.Localizer[nameof(AppResources.NetworkID)].Value},
                { Constants.XmppProperties.DeviceId,   ServiceRef.Localizer[nameof(AppResources.DeviceID)].Value},
                { Constants.CustomXmppProperties.Neuro_Id,   ServiceRef.Localizer[nameof(AppResources.NeuroID)].Value},
                { Constants.CustomXmppProperties.Provider,   ServiceRef.Localizer[nameof(AppResources.Provider)].Value},
                { Constants.CustomXmppProperties.State,   ServiceRef.Localizer[nameof(AppResources.Status)].Value},
                { Constants.CustomXmppProperties.Created,   ServiceRef.Localizer[nameof(AppResources.Created)].Value},
                { Constants.CustomXmppProperties.Updated,   ServiceRef.Localizer[nameof(AppResources.Updated)].Value},
                { Constants.CustomXmppProperties.From,   ServiceRef.Localizer[nameof(AppResources.Issued)].Value},
                { Constants.CustomXmppProperties.To,   ServiceRef.Localizer[nameof(AppResources.Expires)].Value},
            };

            return Map;
        }

        private static void AddField(IdentityGroupsResult Result,
            HashSet<string> PersonalKeys,
            HashSet<string> OrgKeys,
            HashSet<string> TechnicalKeys,
            HashSet<string> ReviewableKeys,
            string Key,
            string Label,
            string Value)
        {
            bool IsReviewable = ReviewableKeys.Contains(Key);
            DisplayField Field = new(Key, Label, Value, IsReviewable);

            if (PersonalKeys.Contains(Key)) Result.Personal.Add(Field);
            else if (OrgKeys.Contains(Key)) Result.Organization.Add(Field);
            else if (TechnicalKeys.Contains(Key)) Result.Technical.Add(Field);
            else Result.Other.Add(Field);
        }
    }
}
