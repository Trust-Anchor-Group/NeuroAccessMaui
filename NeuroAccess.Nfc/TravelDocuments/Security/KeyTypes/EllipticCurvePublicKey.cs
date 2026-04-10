using System;
using System.Numerics;
using NeuroAccess.Nfc.TravelDocuments.Security.FieldTypes;
using Waher.Security.EllipticCurves;

namespace NeuroAccess.Nfc.TravelDocuments.Security.KeyTypes
{
	/// <summary>
	/// Abstract base class for key types.
	/// </summary>
	public class EllipticCurvePublicKey : KeyType
	{
		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "1.2.840.10045.2.1";

		private int version;
		private FieldType? field;
		private BigInteger a;
		private BigInteger b;
		private BigInteger order;
		private BigInteger coFactor;
		private PointOnCurve basePoint;

		/// <summary>
		/// If the object has been configured.
		/// </summary>
		public override bool IsConfigured => this.field is not null;

		/// <summary>
		/// If the object can be configured by the security information provided.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <returns>If the object can be configured, given the security information.</returns>
		public override bool Configure(Vector SecurityInfo)
		{
			if (SecurityInfo.Length != 2 ||
				SecurityInfo.LastElement is not Vector EcParameters ||
				EcParameters.Length != 6 ||
				EcParameters[0] is not BigInteger Version ||
				Version < int.MinValue ||
				Version > int.MaxValue ||
				EcParameters[1] is not FieldType Field ||
				EcParameters[2] is not Vector Curve ||
				Curve.Length != 2 ||
				Curve[0] is not byte[] A ||
				Curve[1] is not byte[] B ||
				EcParameters[3] is not byte[] BasePoint ||
				EcParameters[4] is not BigInteger Order ||
				EcParameters[5] is not BigInteger h)
			{
				return false;
			}

			if (BasePoint.Length == 0)
				return false;

			PointOnCurve G;

			switch (BasePoint[0])
			{
				case 0:	// Infinity
					G = new PointOnCurve();
					break;

				case 4:
				case 6:
				case 7:
					int c = (BasePoint.Length - 1) / 2;
					byte[] Gx = new byte[c];
					byte[] Gy = new byte[c];

					Buffer.BlockCopy(BasePoint, 1, Gx, 0, c);
					Buffer.BlockCopy(BasePoint, 1 + c, Gy, 0, c);

					G = new PointOnCurve(
						EllipticCurve.ToInt(Gx, true),
						EllipticCurve.ToInt(Gy, true));

					break;

				case 2:	// Compressed, Y is even
					c = BasePoint.Length - 1;
					Gx = new byte[c];

					Buffer.BlockCopy(BasePoint, 1, Gx, 0, c);

					G = new PointOnCurve(
						EllipticCurve.ToInt(Gx, true),
						0);

					break;

				case 3: // Compressed, Y is odd
					c = BasePoint.Length - 1;
					Gx = new byte[c];

					Buffer.BlockCopy(BasePoint, 1, Gx, 0, c);

					G = new PointOnCurve(
						EllipticCurve.ToInt(Gx, true),
						1);

					break;

				default:
					return false;
			}

			this.version = (int)Version;
			this.field = Field;
			this.a = EllipticCurve.ToInt(A, true);
			this.b = EllipticCurve.ToInt(B, true);
			this.order = Order;
			this.coFactor = h;
			this.basePoint = G;

			return true;
		}

		/// <summary>
		/// Version
		/// </summary>
		public int Version => this.version;

		/// <summary>
		/// Field
		/// </summary>
		public FieldType Field => this.field!;

		/// <summary>
		/// Elliptic Curve coefficient A.
		/// </summary>
		public BigInteger A => this.a;

		/// <summary>
		/// Elliptic Curve coefficient B.
		/// </summary>
		public BigInteger B => this.b;

		/// <summary>
		/// Elliptic Curve Order
		/// </summary>
		public BigInteger Order => this.order;

		/// <summary>
		/// Elliptic Curve co-factor
		/// </summary>
		public BigInteger CoFactor => this.coFactor;

		/// <summary>
		/// Elliptic Curve base point G
		/// </summary>
		public PointOnCurve BasePoint => this.basePoint;
	}
}
