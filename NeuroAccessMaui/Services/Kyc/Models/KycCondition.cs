using System;
using System.Collections.Generic;

namespace NeuroAccessMaui.Services.Kyc.Models
{
    public class KycCondition
    {
        public string FieldRef { get; set; } = string.Empty;
        public new string? Equals { get; set; }

        public bool Evaluate(IDictionary<string, string?> Values)
        {
            string? FieldValue;
            return Values.TryGetValue(this.FieldRef, out FieldValue)
                && (this.Equals is null ? !string.IsNullOrEmpty(FieldValue) : string.Equals(FieldValue, this.Equals, StringComparison.OrdinalIgnoreCase));
        }
    }
}
