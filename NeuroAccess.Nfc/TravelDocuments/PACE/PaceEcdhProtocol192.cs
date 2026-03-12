using Waher.Security;

namespace NeuroAccess.Nfc.TravelDocuments.PACE
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
		/// Hash function to use in the KDF function.
		/// </summary>
		public override HashFunctionArray KdfHashFunction => Hashes.ComputeSHA256Hash;

		/// <summary>
		/// Number of bytes of hash output to use as key material in the KDF function.
		/// </summary>
		public override int KdfHashKeyLength => 24;
	}
}
