using Waher.Runtime.Inventory;
using Waher.Security.JWT;

namespace NeuroAccessMaui.Services.Crypto
{
	/// <summary>
	/// Cryptographic service that helps create passwords and other security related tasks.
	/// </summary>
	[DefaultImplementation(typeof(CryptoService))]
	public interface ICryptoService : IDisposable
	{
		/// <summary>
		/// Device ID
		/// </summary>
		string DeviceID { get; }

		/// <summary>
		/// Returns a cryptographic authorization key for the given filename.
		/// </summary>
		/// <param name="fileName">The filename to get a key for.</param>
		/// <returns>A cryptographic key.</returns>
		Task<KeyValuePair<byte[], byte[]>> GetCustomKey(string fileName);

		/// <summary>
		/// Generates a random password to use.
		/// </summary>
		/// <returns>Random password</returns>
		string CreateRandomPassword();

		/// <summary>
		/// Initializes the JWT factory.
		/// </summary>
		Task InitializeJwtFactory();

		/// <summary>
		/// Generates a JWT token the app can send to third parties. The token and its claims can be parsed and
		/// validated using <see cref="ParseAndValidateJwtToken"/>.
		/// </summary>
		/// <param name="Claims">Set of claims to embed into token.</param>
		/// <returns>JWT token.</returns>
		Task<string> GenerateJwtToken(params KeyValuePair<string, object?>[] Claims);

		/// <summary>
		/// Vaidates a JWT token, that has been issued by the same app. (Tokens from other apps will not be valid.)
		/// </summary>
		/// <param name="Token">String representation of JWT token.</param>
		/// <returns>Parsed token, if valid, null if not valid.</returns>
		Task<JwtToken?> ParseAndValidateJwtToken(string Token);
	}
}
