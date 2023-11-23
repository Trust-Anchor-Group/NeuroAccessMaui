using System.Security.Cryptography;
using System.Text;
using Waher.Runtime.Inventory;
using Waher.Security;

namespace NeuroAccessMaui.Services.Crypto;

/// <summary>
/// Cryptographic service that helps create passwords and other security related tasks.
/// </summary>
[Singleton]
internal sealed class CryptoService : ICryptoService
{
	private readonly string basePath;
	private readonly string deviceId;
	private readonly RandomNumberGenerator rnd;

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
		this.rnd = RandomNumberGenerator.Create();

		try
		{
			this.deviceId = ServiceRef.PlatformSpecific.GetDeviceId() + "_";
		}
		catch (Exception ex)
		{
			ServiceRef.LogService.LogException(ex);
			this.deviceId = "UNKNOWN_";
		}
	}

	/// <summary>
	/// Returns a cryptographic authorization key for the given filename.
	/// </summary>
	/// <param name="fileName">The filename to get a key for.</param>
	/// <returns>A cryptographic key.</returns>
	public async Task<KeyValuePair<byte[], byte[]>> GetCustomKey(string fileName)
	{
		byte[] key;
		byte[] iv;
		string s;
		int i;

		string FileNameHash = this.deviceId + Path.GetRelativePath(this.basePath, fileName);

		try
		{
			s = await SecureStorage.GetAsync(FileNameHash);
		}
		catch (TypeInitializationException ex)
		{
			ServiceRef.LogService.LogException(ex);
			// No secure storage available.

			key = Hashes.ComputeSHA256Hash(Encoding.UTF8.GetBytes(fileName + ".Key"));
			iv = Hashes.ComputeSHA256Hash(Encoding.UTF8.GetBytes(fileName + ".IV"));
			Array.Resize<byte>(ref iv, 16);

			return new KeyValuePair<byte[], byte[]>(key, iv);
		}

		if (!string.IsNullOrWhiteSpace(s) && (i = s.IndexOf(',')) > 0)
		{
			key = Hashes.StringToBinary(s[..i]);
			iv = Hashes.StringToBinary(s[(i + 1)..]);
		}
		else
		{
			key = new byte[32];
			iv = new byte[16];

			lock (this.rnd)
			{
				this.rnd.GetBytes(key);
				this.rnd.GetBytes(iv);
			}

			s = Hashes.BinaryToString(key) + "," + Hashes.BinaryToString(iv);

			try
			{
				await SecureStorage.SetAsync(FileNameHash, s);
			}
			catch(Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiSerializer.DisplayException(ex);
			}
		}

		return new KeyValuePair<byte[], byte[]>(key, iv);
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
		byte[] result = new byte[nrBytes];

		lock (this.rnd)
		{
			this.rnd.GetBytes(result);
		}

		return result;
	}
}
