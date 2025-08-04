using NeuroAccessMaui.Services.Kyc.Models;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Kyc
{
	/// <summary>
	/// Service for loading KYC processes.
	/// </summary>
	[DefaultImplementation(typeof(KycService))]
	public interface IKycService
	{
		/// <summary>
		/// Loads and parses a KYC process.
		/// </summary>
		/// <param name="Resource">Embedded resource containing the process XML.</param>
		/// <param name="Lang">Optional language code.</param>
		Task<KycReference> LoadKycReferenceAsync(string Resource, string? Lang = null);

		Task SaveKycReference(KycReference Reference);
	}
}
