using Waher.Security;

namespace NeuroAccess.Nfc.Extensions.PACE
{
	/// <summary>
	/// Abstract base class for PACE protocols using Elliptic Curve Cryptography (EEC)
	/// having 192 bit security.
	/// </summary>
	public abstract class PaceEcdhProtocol192 : PaceEcdhProtocol
	{
		/// <summary>
		/// Bits of security provided by the protocol.
		/// </summary>
		public override int Bits => 192;

		/// <summary>
		/// Hash function to use when deriving keys from the shared secret.
		/// </summary>
		/// <param name="Data">Data to hash.</param>
		/// <returns>Hash digest.</returns>
		public override byte[] HashFunction(byte[] Data) => Hashes.ComputeSHA256Hash(Data);
	}
}
