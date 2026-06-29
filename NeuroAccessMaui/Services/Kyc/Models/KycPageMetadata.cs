using System.Collections.ObjectModel;

namespace NeuroAccessMaui.Services.Kyc.Models
{
	/// <summary>
	/// Defines optional metadata that describes built-in behavior for a KYC page.
	/// </summary>
	public class KycPageMetadata
	{
		/// <summary>
		/// Gets or sets the built-in action name declared for the page.
		/// </summary>
		public string? ActionName { get; set; }

		/// <summary>
		/// Gets the evidence keys required by the page.
		/// </summary>
		public Collection<string> RequiredEvidenceKeys { get; } = new Collection<string>();
	}
}
