using Waher.Runtime.Collections;

namespace NeuroAccess.Nfc.TravelDocuments.Security.Properties.CertificateExtensions
{
	/// <summary>
	/// CRL Distribution Points
	/// </summary>
	public class DistributionPoints : SecurityObject
	{
		private DistributionPoint[]? distributionPoints;

		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "2.5.29.31";

		/// <summary>
		/// If the object has been configured.
		/// </summary>
		public override bool IsConfigured => this.distributionPoints is not null;

		/// <summary>
		/// If the object can be configured by the security information provided.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <returns>If the object can be configured, given the security information.</returns>
		public override bool Configure(Vector SecurityInfo)
		{
			if (SecurityInfo.LastElementNested is not Vector DistributionPoints)
				return false;

			if (DistributionPoints.Length == 1 &&
				DistributionPoints.FirstElement is Vector v &&
				(v.SubSection[0] & 0x80) == 0)
			{
				DistributionPoints = v;
			}

			ChunkedList<DistributionPoint> Points = [];

			foreach (object? Element in DistributionPoints)
			{
				if (Element is not Vector DistributionPointVector)
					return false;

				if (DistributionPoint.TryCreate(DistributionPointVector, out DistributionPoint? Point))
					Points.Add(Point);
			}

			if (!Points.HasFirstItem)
				return false;

			this.distributionPoints = [.. Points];

			return true;
		}

		/// <summary>
		/// Distribution points.
		/// </summary>
		public DistributionPoint[] Points => this.distributionPoints!;
	}
}
