namespace NeuroAccessMaui.Services.Kyc.Models
{
    public class KycOption
    {
        public KycOption(string value, KycLocalizedText label)
        {
            this.Value = value;
            this.Label = label;
        }

        public string Value { get; }
        public KycLocalizedText Label { get; }
        public string GetLabel(string? lang = null)
        {
            return this.Label.Get(lang) ?? this.Value;
        }
    }
}
