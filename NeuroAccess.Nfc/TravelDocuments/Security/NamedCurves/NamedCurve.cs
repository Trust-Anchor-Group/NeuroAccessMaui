using Waher.Security.EllipticCurves;

namespace NeuroAccess.Nfc.TravelDocuments.Security.NamedCurves
{
	/// <summary>
	/// Abstract base class for named curves.
	/// </summary>
	public abstract class NamedCurve : SecurityObject, INamedCurve
	{
		private bool configured;

		/// <summary>
		/// If the object has been configured.
		/// </summary>
		public override bool IsConfigured => this.configured;

		/// <summary>
		/// If the object can be configured by the security information provided.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <returns>If the object can be configured, given the security information.</returns>
		public override bool Configure(Vector SecurityInfo)
		{
			this.configured = true;
			return true;
		}

		/// <summary>
		/// Gets the Elliptic Curve associated with the named curve.
		/// </summary>
		/// <returns>The Elliptic Curve.</returns>
		public abstract EllipticCurve GetCurve();
	}
}
