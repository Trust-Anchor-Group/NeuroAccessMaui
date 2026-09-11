using System.Security.Cryptography;
using System.Text;
using Waher.Runtime.Inventory;
using Waher.Security;
using Waher.Security.JWT;

namespace NeuroAccessMaui.Services.Crypto
{
	/// <summary>
	/// Cryptographic service that helps create passwords and other security related tasks.
	/// </summary>
	[Singleton]
	internal sealed class CryptoService : ICryptoService
	{
		private readonly string basePath;
		private readonly string deviceId;
		private readonly RandomNumberGenerator rnd;
		private JwtFactory? jwtFactory;

		/// <summary>
		/// Device ID
		/// </summary>
		public string DeviceID => this.deviceId;

		/// <summary>
		/// Cryptographic service that helps create passwords and other security related tasks.
		/// </summary>
		public CryptoService()
		{
			this.basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

			string? DeviceId = ServiceRef.PlatformSpecific.GetDeviceId();
			if (string.IsNullOrEmpty(DeviceId))
				throw new InvalidOperationException("The device identifier has not been initialized.");
			this.deviceId = DeviceId + "_";
			this.rnd = RandomNumberGenerator.Create();
		}

        /// <inheritdoc />
        public Task<KeyValuePair<byte[], byte[]>> GetCustomKey(string FileName) => this.GetCustomKeyAsync(FileName, true);

        /// <inheritdoc />
        public async Task<KeyValuePair<byte[], byte[]>> GetCustomKeyAsync(string FileName, bool AllowCreation)
        {
            string KeyName = this.deviceId + Path.GetRelativePath(this.basePath, FileName);
            string? Stored = await SecureStorage.GetAsync(KeyName);
            if (Stored is not null)
            {
                string[] Parts = Stored.Split(',');
                if (Parts.Length != 2)
                    throw new CryptographicException("Invalid persisted encryption material.");
                byte[] Key = Hashes.StringToBinary(Parts[0]);
                byte[] IV = Hashes.StringToBinary(Parts[1]);
                if (Key.Length != 32 || IV.Length != 16)
                    throw new CryptographicException("Invalid persisted encryption material.");
                return new KeyValuePair<byte[], byte[]>(Key, IV);
            }
            if (!AllowCreation)
                throw new CryptographicException("Encryption material for existing data is missing.");
            byte[] NewKey = this.GetBytes(32);
            byte[] NewIV = this.GetBytes(16);
            await SecureStorage.SetAsync(KeyName, Hashes.BinaryToString(NewKey) + "," + Hashes.BinaryToString(NewIV));
            return new KeyValuePair<byte[], byte[]>(NewKey, NewIV);
        }

		/// <summary>
		/// Generates a random password to use.
		/// </summary>
		/// <returns>Random password</returns>
		public string CreateRandomPassword()
		{
			return Hashes.BinaryToString(this.GetBytes(32));
		}

		private byte[] GetBytes(int nrBytes)
		{
			byte[] Result = new byte[nrBytes];

			lock (this.rnd)
			{
				this.rnd.GetBytes(Result);
			}

			return Result;
		}

		/// <summary>
		/// Initializes the JWT factory.
		/// </summary>
		public async Task InitializeJwtFactory()
		{
			KeyValuePair<byte[], byte[]> Keys = await this.GetCustomKey("factory.jwt");
			this.jwtFactory = JwtFactory.CreateHmacSha256(Keys.Key);
		}

		/// <summary>
		/// Generates a JWT token the app can send to third parties. The token and its claims can be parsed and
		/// validated using <see cref="ParseAndValidateJwtToken"/>.
		/// </summary>
		/// <param name="Claims">Set of claims to embed into token.</param>
		/// <returns>JWT token.</returns>
		public async Task<string> GenerateJwtToken(params KeyValuePair<string, object?>[] Claims)
		{
			if (this.jwtFactory is null)
				await this.InitializeJwtFactory();	// Can be called multiple times.

			return this.jwtFactory!.Create(Claims);
		}

		/// <summary>
		/// Vaidates a JWT token, that has been issued by the same app. (Tokens from other apps will not be valid.)
		/// </summary>
		/// <param name="Token">String representation of JWT token.</param>
		/// <returns>Parsed token, if valid, null if not valid.</returns>
		public async Task<JwtToken?> ParseAndValidateJwtToken(string Token)
		{
			try
			{
				if (this.jwtFactory is null)
					await this.InitializeJwtFactory();  // Can be called multiple times.

				JwtToken Parsed = new(Token);
				if (!this.jwtFactory!.IsValid(Parsed))
					return null;

				return Parsed;
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		public void Dispose()
		{
			this.jwtFactory?.Dispose();
			this.jwtFactory = null;
			this.rnd.Dispose();
		}
	}
}
