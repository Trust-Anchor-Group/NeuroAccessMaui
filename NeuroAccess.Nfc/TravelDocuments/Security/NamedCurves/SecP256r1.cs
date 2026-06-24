using Waher.Security.EllipticCurves;

namespace NeuroAccess.Nfc.TravelDocuments.Security.NamedCurves
{
	/// <summary>
	/// Named elliptic curve secp256r1 / NIST P-256 / ansip256r1
	/// </summary>
	public class AnsiP256r1 : NamedCurve
	{
		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "1.2.840.10045.3.1.7";

		/// <summary>
		/// Gets the Elliptic Curve associated with the named curve.
		/// </summary>
		/// <returns>The Elliptic Curve.</returns>
		public override EllipticCurve GetCurve()
		{
			return new NistP256();
		}
	}
}
