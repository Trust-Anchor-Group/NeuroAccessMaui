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
		Task<KycReference> LoadKycReferenceAsync(string? Lang = null);

		Task SaveKycReferenceAsync(KycReference Reference);

		/// <summary>
		/// Loads available KYC processes from server, falling back to bundled test KYC.
		/// </summary>
		/// <param name="Lang">Optional language code.</param>
		Task<IReadOnlyList<KycReference>> LoadAvailableKycReferencesAsync(string? Lang = null);
	}
}
