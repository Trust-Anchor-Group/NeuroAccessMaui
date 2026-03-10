using Waher.Security;

namespace NeuroAccess.Nfc.Extensions.PACE
{
	/// <summary>
	/// Abstract base class for PACE DH-based protocols having 256 bit security.
	/// </summary>
	public abstract class PaceDhProtocol256 : PaceDhProtocol
	{
		/// <summary>
		/// Bits of security provided by the protocol.
		/// </summary>
		public override int Bits => 256;

		/// <summary>
		/// Hash function to use when deriving keys from the shared secret.
		/// </summary>
		/// <param name="Data">Data to hash.</param>
		/// <returns>Hash digest.</returns>
		public override byte[] HashFunction(byte[] Data) => Hashes.ComputeSHA256Hash(Data);
	}
}
