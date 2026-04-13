using System.Globalization;
using System.Numerics;
using Waher.Security.EllipticCurves;

namespace NeuroAccess.Nfc.TravelDocuments.Security.EllipticCurves
{
	/// <summary>
	/// FTP256v1 elliptic curve, defined by French ANSSI.
	/// </summary>
	public class Frp256v1 : WeierstrassCurve
	{
		private static readonly BigInteger prime = BigInteger.Parse("0f1fd178c0b3ad58f10126de8ce42435b3961adbcabc8ca6de8fcf353d86e9c03", System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture);
		private static readonly BigInteger a = BigInteger.Parse("0f1fd178c0b3ad58f10126de8ce42435b3961adbcabc8ca6de8fcf353d86e9c00", System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture);
		private static readonly BigInteger b = BigInteger.Parse("0ee353fca5428a9300d4aba754a44c00fdfec0c9ae4b1a1803075ed967b7bb73f", System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture);
		private static readonly BigInteger gX = BigInteger.Parse("0b6b3d4c356c139eb31183d4749d423958c27d2dcaf98b70164c97a2dd98f5cff", System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture);
		private static readonly BigInteger gY = BigInteger.Parse("6142e0f7c8b204911f9271f0f3ecef8c2701c307e8e4c9e183115a1554062cfb", System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture);
		private static readonly BigInteger order = BigInteger.Parse("0f1fd178c0b3ad58f10126de8ce42435b53dc67e140d2bf941ffdd459c6d655e1", System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture);

		/// <summary>
		/// FTP256v1 elliptic curve, defined by French ANSSI.
		/// </summary>
		public Frp256v1()
			: base(prime, new PointOnCurve(gX, gY), a, b, order, 1)
		{
		}

		/// <summary>
		/// Name of curve.
		/// </summary>
		public override string CurveName => "FRP256v1";
	}
}
