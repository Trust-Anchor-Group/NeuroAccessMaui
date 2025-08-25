using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.Services.Identity
{
    /// <summary>
    /// Builds friendly, localized summaries for identity-like data from mapped properties and attachments.
    /// Designed to be used by both View Identity and KYC preview summaries.
    /// </summary>
    public static class IdentitySummaryFormatter
    {
        public sealed class DisplayPair
        {
            public string Label { get; }
            public string Value { get; }

            public DisplayPair(string Label, string Value)
            {
                this.Label = Label;
                this.Value = Value;
            }
        }

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
            public List<DisplayPair> Personal { get; } = new List<DisplayPair>();
            public List<DisplayPair> Address { get; } = new List<DisplayPair>();
            public List<DisplayPair> Attachments { get; } = new List<DisplayPair>();
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
            IEnumerable<AttachmentInfo>? Attachments = null,
            CultureInfo? Culture = null)
        {
            CultureInfo EffectiveCulture = Culture ?? CultureInfo.CurrentCulture;

            Dictionary<string, string> LabelMap = GetLabelMap();

            // Classification sets
            HashSet<string> PersonalKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Constants.XmppProperties.FullName,
                Constants.CustomXmppProperties.BirthDate,
                Constants.XmppProperties.PersonalNumber,
                Constants.XmppProperties.Nationality,
                Constants.XmppProperties.Gender,
                Constants.XmppProperties.Phone,
                Constants.XmppProperties.EMail
            };

            HashSet<string> AddressKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Constants.XmppProperties.Address,
                Constants.XmppProperties.Address2,
                Constants.XmppProperties.Area,
                Constants.XmppProperties.City,
                Constants.XmppProperties.ZipCode,
                Constants.XmppProperties.Region,
                Constants.XmppProperties.Country
            };

            // Prepare dictionary for quick lookup
            Dictionary<string, string> Dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
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
                    DateTime BirthDate = new DateTime(Year, Month, Day);
                    Dict[Constants.CustomXmppProperties.BirthDate] = BirthDate.ToString("d", EffectiveCulture);
                }
                catch (Exception Ex)
                {
                    ServiceRef.LogService.LogException(Ex);
                }
            }

            KycSummaryResult Result = new KycSummaryResult();

            // Personal
            foreach (string Key in PersonalKeys)
            {
                if (!Dict.TryGetValue(Key, out string? Val) || string.IsNullOrWhiteSpace(Val))
                    continue;

                string Label = GetLabel(LabelMap, Key);
                Result.Personal.Add(new DisplayPair(Label, Val));
            }

            // Address
            foreach (string Key in AddressKeys)
            {
                if (!Dict.TryGetValue(Key, out string? Val) || string.IsNullOrWhiteSpace(Val))
                    continue;

                string Label = GetLabel(LabelMap, Key);
                Result.Address.Add(new DisplayPair(Label, Val));
            }

            // Attachments
            if (Attachments is not null)
            {
                foreach (AttachmentInfo Att in Attachments)
                {
                    if (string.IsNullOrEmpty(Att.FileName))
                        continue;

                    string Base = Att.FileName.Split('.')[0];
                    string Description = Base switch
                    {
                        "Passport" => "Passport image", // TODO: Localize
                        "IdCardFront" => "Front of ID card",
                        "IdCardBack" => "Back of ID card",
                        "DriverLicenseFront" => "Front of driver's license",
                        "DriverLicenseBack" => "Back of driver's license",
                        "ProfilePhoto" => "Photo of face",
                        _ => Att.FileName
                    };

                    Result.Attachments.Add(new DisplayPair(Att.FileName, Description));
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
            HashSet<string> ReviewableKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
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
            HashSet<string> PersonalKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
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

            HashSet<string> OrgKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
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

            HashSet<string> TechnicalKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
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
            IdentityGroupsResult Result = new IdentityGroupsResult();
            HashSet<string> UsedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
            Dictionary<string, string> Map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { Constants.XmppProperties.FullName, "Full name" },
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
                { Constants.XmppProperties.Country, ServiceRef.Localizer[nameof(AppResources.Country)].Value }
            };

            return Map;
        }

        private static Dictionary<string, string> GetIdentityLabelMap()
        {
            Dictionary<string, string> Map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
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
            DisplayField Field = new DisplayField(Key, Label, Value, IsReviewable);

            if (PersonalKeys.Contains(Key)) Result.Personal.Add(Field);
            else if (OrgKeys.Contains(Key)) Result.Organization.Add(Field);
            else if (TechnicalKeys.Contains(Key)) Result.Technical.Add(Field);
            else Result.Other.Add(Field);
        }
    }
}
