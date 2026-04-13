using System;
using System.Globalization;
using System.Reflection;
using NeuroAccess.Nfc.TravelDocuments.Security.FieldTypes;
using NeuroAccess.Nfc.TravelDocuments.Security.PublicKeys;
using Waher.Events;
using Waher.Networking;
using Waher.Runtime.Collections;
using Waher.Runtime.Inventory;
using Waher.Security;
using Waher.Security.EllipticCurves;

namespace NeuroAccess.Nfc.TravelDocuments.Security.SignatureAlgorithms
{
	/// <summary>
	/// ECDSA Signature algorithm
	/// </summary>
	public abstract class EcdsaAlgorithm : SignatureAlgorithm
	{
		/// <summary>
		/// Verifies a digital signature.
		/// </summary>
		/// <param name="Data">Data being signed.</param>
		/// <param name="Signature">Digital signature.</param>
		/// <param name="PublicKeyKey">Public Key of the signing body.</param>
		/// <param name="Client">Optional client reference.</param>
		/// <returns>If the digital signature is correct.</returns>
		public override bool VerifySignature(byte[] Data, byte[] Signature, IPublicKey PublicKey,
			ICommunicationLayer? Client)
		{
			if (PublicKey is not EllipticCurvePublicKey ECPublicKey)
			{
				Client?.Error("No ECDSA public key provided or found.");
				return false;
			}

			return VerifySignature(Data, Signature, ECPublicKey, this.HashAlgorithm, Client);
		}

		/// <summary>
		/// Verifies a digital signature using the ECDSA algorithm.
		/// </summary>
		/// <param name="Data">Data being signed.</param>
		/// <param name="Signature">Digital signature.</param>
		/// <param name="PublicKey">Elliptic Curve Public Key</param>
		/// <param name="HashAlgorithm">Hash Algorithm to use</param>
		/// <param name="Client">Optional client reference.</param>
		/// <returns>If the digital signature is correct.</returns>
		public static bool VerifySignature(byte[] Data, byte[] Signature,
			EllipticCurvePublicKey PublicKey, HashFunctionArray HashAlgorithm,
			ICommunicationLayer? Client)
		{
			System.Numerics.BigInteger Order = PublicKey.Order;
			System.Numerics.BigInteger Cofactor = PublicKey.CoFactor;
			System.Numerics.BigInteger A = PublicKey.A;
			System.Numerics.BigInteger B = PublicKey.B;
			PointOnCurve BasePoint = PublicKey.BasePoint;
			PointOnCurve PublicKeyPoint = PublicKey.PublicKey;

			if (!ASN1.TryDecodeDer(Client, Signature, out object? Obj) ||
				Obj is not Vector SignatureVector ||
				SignatureVector.Length != 2 ||
				SignatureVector[0] is not System.Numerics.BigInteger R ||
				SignatureVector[1] is not System.Numerics.BigInteger S)
			{
				Client?.Error("Unable to parse signature.");
				return false;
			}

			PrimeFieldCurve? Selected = null;

			if (PublicKey.Field is PrimeField PrimeField)
			{
				System.Numerics.BigInteger Prime = PrimeField.Prime;

				foreach (EllipticCurve EC in curves)
				{
					if (EC is not PrimeFieldCurve PrimeFieldCurve)
						continue;

					if (PrimeFieldCurve.Prime != Prime ||
						PrimeFieldCurve.Cofactor != Cofactor ||
						PrimeFieldCurve.Order != Order ||
						PrimeFieldCurve.BasePoint.X != BasePoint.X ||
						PrimeFieldCurve.BasePoint.Y != BasePoint.Y)
					{
						continue;
					}

					if (EC is WeierstrassCurve WeierstrassCurve)
					{
						if (WeierstrassCurve.A != A || WeierstrassCurve.B != B)
							continue;
					}
					else if (EC is MontgomeryCurve MontgomeryCurve)
					{
						if (MontgomeryCurve.A != A)
							continue;
					}
					else if (EC is EdwardsCurve EdwardsCurve)
					{
						if (EdwardsCurve.D != A)
							continue;
					}
					else if (EC is EdwardsTwistedCurve EdwardsTwistedCurve)
					{
						if (EdwardsTwistedCurve.D != A)
							continue;
					}
					else
						continue;

					Selected = PrimeFieldCurve;
					break;
				}

				if (Selected is null)
				{
					Client?.Warning("Elliptic Curve not recognized.\r\n\r\nA: " +
						A.ToString(CultureInfo.InvariantCulture) +
						",\r\nB: " + B.ToString(CultureInfo.InvariantCulture) +
						",\r\np: " + Prime.ToString(CultureInfo.InvariantCulture) +
						",\r\nCofactor: " + Cofactor.ToString(CultureInfo.InvariantCulture) +
						",\r\nOrder: " + Order.ToString(CultureInfo.InvariantCulture) +
						",\r\nG.X: " + BasePoint.X.ToString(CultureInfo.InvariantCulture) +
						",\r\nG.Y: " + BasePoint.Y.ToString(CultureInfo.InvariantCulture));

					if (Cofactor < int.MinValue || Cofactor > int.MaxValue)
						return false;

					Selected = new CustomWeierstrassCurve("Custom", Prime, BasePoint, A, B,
						Order, (int)Cofactor);
				}
			}
			else
			{
				Client?.Error("Curve field type not recognized: " + PublicKey.Field.GetType().FullName);
				return false;
			}

			Client?.Information("Curve used for signature: " + Selected.GetType().FullName);

			if (!Selected.IsPoint(PublicKeyPoint))
			{
				Client?.Error("Public key not a point on curve.");
				return false;
			}

			return ECDSA.Verify(Data, PublicKeyPoint, HashAlgorithm, Selected, R, S);
		}

		private static readonly EllipticCurve[] curves = GetEllipticCurves();

		private static EllipticCurve[] GetEllipticCurves()
		{
			ChunkedList<EllipticCurve> EllipticCurves = [];

			foreach (Type T in Types.GetTypesImplementingInterface(typeof(Waher.Security.ISignatureAlgorithm)))
			{
				ConstructorInfo? CI = Types.GetDefaultConstructor(T);
				if (CI is null)
					continue;

				try
				{
					Waher.Security.ISignatureAlgorithm Algorithm = (Waher.Security.ISignatureAlgorithm)CI.Invoke(Types.NoParameters);
					if (Algorithm is EllipticCurve Curve)
						EllipticCurves.Add(Curve);
				}
				catch (Exception ex)
				{
					Log.Exception(ex);
				}
			}

			return [.. EllipticCurves];
		}

		/// <summary>
		/// Hash algorithm to use.
		/// </summary>
		public abstract HashFunctionArray HashAlgorithm { get; }
	}
}
