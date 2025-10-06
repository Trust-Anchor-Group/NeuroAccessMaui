using System;

namespace NeuroAccessMaui.Services.Kyc.Models
{
    /// <summary>
    /// Detailed reason info for an invalidated claim.
    /// </summary>
    public class KycInvalidClaim
    {
        public string Claim { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string ReasonLanguage { get; set; } = string.Empty;
        public string ReasonCode { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;

        public KycInvalidClaim()
        {
        }

        public KycInvalidClaim(string Claim, string Reason, string ReasonLanguage, string ReasonCode, string Service)
        {
            this.Claim = Claim ?? string.Empty;
            this.Reason = Reason ?? string.Empty;
            this.ReasonLanguage = ReasonLanguage ?? string.Empty;
            this.ReasonCode = ReasonCode ?? string.Empty;
            this.Service = Service ?? string.Empty;
        }
    }

    /// <summary>
    /// Detailed reason info for an invalidated photo/attachment.
    /// </summary>
    public class KycInvalidPhoto
    {
        public string FileName { get; set; } = string.Empty;
        public string Mapping { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string ReasonLanguage { get; set; } = string.Empty;
        public string ReasonCode { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;

        public KycInvalidPhoto()
        {
        }

        public KycInvalidPhoto(string FileName, string Mapping, string Reason, string ReasonLanguage, string ReasonCode, string Service)
        {
            this.FileName = FileName ?? string.Empty;
            this.Mapping = Mapping ?? string.Empty;
            this.Reason = Reason ?? string.Empty;
            this.ReasonLanguage = ReasonLanguage ?? string.Empty;
            this.ReasonCode = ReasonCode ?? string.Empty;
            this.Service = Service ?? string.Empty;
        }
    }
}

