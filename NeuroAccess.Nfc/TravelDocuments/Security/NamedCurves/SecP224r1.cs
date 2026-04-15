using Waher.Security.EllipticCurves;

namespace NeuroAccess.Nfc.TravelDocuments.Security.NamedCurves
{
	/// <summary>
	/// Named elliptic curve secp224r1 / NIST P-224 / ansip224r1
	/// </summary>
	public class AnsiP224r1 : NamedCurve
	{
		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "1.3.132.0.33";

		/// <summary>
		/// Gets the Elliptic Curve associated with the named curve.
		/// </summary>
		/// <returns>The Elliptic Curve.</returns>
		public override EllipticCurve GetCurve()
		{
			return new NistP224();
		}
	}
}
