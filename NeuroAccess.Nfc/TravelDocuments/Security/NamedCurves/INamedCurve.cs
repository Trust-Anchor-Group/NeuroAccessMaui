using Waher.Security.EllipticCurves;

namespace NeuroAccess.Nfc.TravelDocuments.Security.NamedCurves
{
	/// <summary>
	/// Interface for named curves.
	/// </summary>
	public interface INamedCurve : ISecurityObject
	{
		/// <summary>
		/// Gets the Elliptic Curve associated with the named curve.
		/// </summary>
		/// <returns>The Elliptic Curve.</returns>
		EllipticCurve GetCurve();
	}
}
