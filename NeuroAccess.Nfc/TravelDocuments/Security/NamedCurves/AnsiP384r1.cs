using Waher.Security.EllipticCurves;

namespace NeuroAccess.Nfc.TravelDocuments.Security.NamedCurves
{
	/// <summary>
	/// Named elliptic curve secp384r1 / NIST P-384 / ansip384r1
	/// </summary>
	public class AnsiP384r1 : NamedCurve
	{
		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "1.3.132.0.34";

		/// <summary>
		/// Gets the Elliptic Curve associated with the named curve.
		/// </summary>
		/// <returns>The Elliptic Curve.</returns>
		public override EllipticCurve GetCurve()
		{
			return new NistP384();
		}
	}
}
