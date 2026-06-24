using Waher.Security.EllipticCurves;

namespace NeuroAccess.Nfc.TravelDocuments.Security.NamedCurves
{
	/// <summary>
	/// Named elliptic curve secp521r1 / NIST P-521 / ansip521r1
	/// </summary>
	public class SecP521r1 : NamedCurve
	{
		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "1.3.132.0.35";

		/// <summary>
		/// Gets the Elliptic Curve associated with the named curve.
		/// </summary>
		/// <returns>The Elliptic Curve.</returns>
		public override EllipticCurve GetCurve()
		{
			return new NistP521();
		}
	}
}
