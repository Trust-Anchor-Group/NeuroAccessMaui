using Waher.Security.EllipticCurves;

namespace NeuroAccess.Nfc.TravelDocuments.Security.NamedCurves
{
	/// <summary>
	/// Named elliptic curve secp192r1 / NIST P-192 / ansip192r1
	/// </summary>
	public class AnsiP192r1 : NamedCurve
	{
		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "1.2.840.10045.3.1.1";

		/// <summary>
		/// Gets the Elliptic Curve associated with the named curve.
		/// </summary>
		/// <returns>The Elliptic Curve.</returns>
		public override EllipticCurve GetCurve()
		{
			return new NistP192();
		}
	}
}
