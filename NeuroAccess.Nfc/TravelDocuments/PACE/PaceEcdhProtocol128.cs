namespace NeuroAccess.Nfc.TravelDocuments.PACE
{
	/// <summary>
	/// Abstract base class for PACE protocols using Elliptic Curve Cryptography (EEC)
	/// having 128 bit security.
	/// </summary>
	public abstract class PaceEcdhProtocol128 : PaceEcdhProtocol
	{
		/// <summary>
		/// Bits of security provided by the protocol.
		/// </summary>
		public override int Bits => 128;
	}
}
