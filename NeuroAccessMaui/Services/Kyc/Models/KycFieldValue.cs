using System;
using Waher.Persistence.Attributes;

namespace NeuroAccessMaui.Services.Kyc.Models
{
    /// <summary>
    /// Represents a field value in a KYC process reference.
    /// </summary>
    public class KycFieldValue
    {
        /// <summary>
        /// Gets or sets the field identifier (unique within the process).
        /// </summary>
        [DefaultValueStringEmpty]
        public string FieldId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the value of the field as a string.
        /// </summary>
        [DefaultValueNull]
        public string? Value { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="KycFieldValue"/> class.
        /// </summary>
        public KycFieldValue() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="KycFieldValue"/> class with the specified field ID and value.
        /// </summary>
        /// <param name="fieldId">The field identifier.</param>
        /// <param name="value">The field value.</param>
        public KycFieldValue(string fieldId, string? value)
        {
            this.FieldId = fieldId;
            this.Value = value;
        }
    }
}
