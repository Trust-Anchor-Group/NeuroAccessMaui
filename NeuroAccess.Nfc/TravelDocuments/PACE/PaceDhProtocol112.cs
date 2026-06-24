namespace NeuroAccess.Nfc.TravelDocuments.PACE
{
	/// <summary>
	/// Abstract base class for PACE DH-based protocols having 112 bit security.
	/// </summary>
	public abstract class PaceDhProtocol112 : PaceDhProtocol
	{
		/// <summary>
		/// Bits of security provided by the protocol.
		/// </summary>
		public override int Bits => 112;
	}
}
