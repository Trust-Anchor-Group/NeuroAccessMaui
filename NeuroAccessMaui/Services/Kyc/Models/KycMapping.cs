using System.Collections.Generic;

namespace NeuroAccessMaui.Services.Kyc.Models
{
    /// <summary>
    /// Mapping from a field value to an identity property, including optional transform pipeline.
    /// </summary>
    public class KycMapping
    {
        /// <summary>
        /// Target identity property key.
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Ordered list of transform identifiers to apply. Empty means no transforms.
        /// </summary>
        public List<string> TransformNames { get; } = new List<string>();
    }
}
