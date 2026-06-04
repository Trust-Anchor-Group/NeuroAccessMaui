using System;
using System.Globalization;
using System.Reflection;
using System.Text;
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

			return VerifySignature(Data, Signature, ECPublicKey, this.HashAlgorithm,
				this.HashAlgorithmStream, Client);
		}

		/// <summary>
		/// Verifies a digital signature using the ECDSA algorithm.
		/// </summary>
		/// <param name="Data">Data being signed.</param>
		/// <param name="Signature">Digital signature.</param>
		/// <param name="PublicKey">Elliptic Curve Public Key</param>
		/// <param name="HashAlgorithm">Hash algorithm to use for in-memory blocks of data.</param>
		/// <param name="HashAlgorithmStream">Hash algorithm to use for streams of data.</param>
		/// <param name="Client">Optional client reference.</param>
		/// <returns>If the digital signature is correct.</returns>
		public static bool VerifySignature(byte[] Data, byte[] Signature,
			EllipticCurvePublicKey PublicKey, HashFunctionArray HashAlgorithm,
			HashFunctionStream HashAlgorithmStream, ICommunicationLayer? Client)
		{
			System.Numerics.BigInteger Order = PublicKey.Order;
			System.Numerics.BigInteger Cofactor = PublicKey.CoFactor;
			System.Numerics.BigInteger A = PublicKey.A;
			System.Numerics.BigInteger B = PublicKey.B;
			PointOnCurve BasePoint = PublicKey.BasePoint;
			PointOnCurve PublicKeyPoint = PublicKey.PublicKey;
			bool HasSniffer = Client?.HasSniffers ?? false;

			if (!ASN1.TryDecodeDer(Client, Signature, out object? Obj) ||
				Obj is not Vector SignatureVector ||
				SignatureVector.Length != 2 ||
				SignatureVector.FirstElement is not System.Numerics.BigInteger R ||
				SignatureVector[1] is not System.Numerics.BigInteger S)
			{
				if (HasSniffer)
					Client!.Error("Unable to parse signature: " + Hashes.BinaryToString(Signature));

				return false;
			}

			StringBuilder? Msg = HasSniffer ? new StringBuilder() : null;

			if (HasSniffer)
			{
				Msg!.AppendLine("ECDSA signature verification parameters:");
				Msg.Append("A (dec): ");
				Msg.AppendLine(A.ToString(CultureInfo.InvariantCulture));
				Msg.Append("B (dec): ");
				Msg.AppendLine(B.ToString(CultureInfo.InvariantCulture));
				Msg.Append("Order (dec): ");
				Msg.AppendLine(Order.ToString(CultureInfo.InvariantCulture));
				Msg.Append("Cofactor (dec): ");
				Msg.AppendLine(Cofactor.ToString(CultureInfo.InvariantCulture));
				Msg.Append("BasePoint.X (dec): ");
				Msg.AppendLine(BasePoint.X.ToString(CultureInfo.InvariantCulture));
				Msg.Append("BasePoint.Y (dec): ");
				Msg.AppendLine(BasePoint.Y.ToString(CultureInfo.InvariantCulture));
				Msg.Append("PublicKey.X (dec): ");
				Msg.AppendLine(PublicKeyPoint.X.ToString(CultureInfo.InvariantCulture));
				Msg.Append("PublicKey.Y (dec): ");
				Msg.AppendLine(PublicKeyPoint.Y.ToString(CultureInfo.InvariantCulture));
				Msg.Append("S (Signature, hex): ");
				Msg.AppendLine(Hashes.BinaryToString(Signature));
				Msg.Append("R (dec): ");
				Msg.AppendLine(R.ToString(CultureInfo.InvariantCulture));
				Msg.Append("S (dec): ");
				Msg.AppendLine(S.ToString(CultureInfo.InvariantCulture));
				Msg.Append("Hash function: ");
				Msg.AppendLine(HashAlgorithm.Method.Name);
				Msg.Append("Message (hex): ");
				Msg.AppendLine(Hashes.BinaryToString(Data));
			}

			PrimeFieldCurve? Selected = null;

			if (PublicKey.HasNamedCurve)
			{
				if (PublicKey.NamedCurve is PrimeFieldCurve PrimeFieldCurve)
					Selected = PrimeFieldCurve;
				else
				{
					if (HasSniffer)
					{
						Client!.Information(Msg!.ToString());
						Client.Error("Named curve not supported by ECDSA: " + PublicKey.NamedCurve.CurveName);
					}

					return false;
				}
			}
			else if (PublicKey.Field is PrimeField PrimeField)
			{
				System.Numerics.BigInteger Prime = PrimeField.Prime;

				foreach (EllipticCurve EC in curves)
				{
					if (EC.Order != Order ||
						EC.Cofactor != Cofactor ||
						EC.BasePoint.X != BasePoint.X ||
						EC.BasePoint.Y != BasePoint.Y)
					{
						continue;
					}

					if (EC is not PrimeFieldCurve PrimeFieldCurve)
						continue;

					if (PrimeFieldCurve.Prime != Prime)
						continue;

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
					if (HasSniffer)
						Msg!.AppendLine("Elliptic Curve not recognized. A custom Weierstras curve will be used.");

					if (Cofactor < int.MinValue || Cofactor > int.MaxValue)
					{
						if (HasSniffer)
						{
							Client!.Information(Msg!.ToString());
							Client.Error("Unsupported cofactor.");
						}

						return false;
					}

					Selected = new CustomWeierstrassCurve("Custom", Prime, BasePoint, A, B,
						Order, (int)Cofactor, HashAlgorithm, HashAlgorithmStream);
				}
				else if (Selected.HashFunction != HashAlgorithm ||
					Selected.HashFunctionStream != HashAlgorithmStream)
				{
					if (HasSniffer)
					{
						Msg!.Append("Elliptic Curve Hash algorithm not standard: ");
						Msg.Append(HashAlgorithm.Method.Name);
						Msg.Append(" (instead of ");
						Msg.Append(Selected.HashFunction.Method.Name);
						Msg.AppendLine(")");
					}

					Selected = new CustomWeierstrassCurve(Selected.CurveName, Prime, BasePoint, A, B,
						Order, Selected.Cofactor, HashAlgorithm, HashAlgorithmStream);
				}
			}
			else
			{
				if (HasSniffer)
				{
					Client!.Information(Msg!.ToString());
					Client.Error("Curve field type not supported: " + PublicKey.Field.GetType().FullName);
				}

				return false;
			}

			ASN1.ReportAlgorithmUse(Selected);

			if (HasSniffer)
			{
				Msg!.Append("Curve used for signature: ");
				Msg.Append(Selected.CurveName);
				Msg.Append(" (implemented by ");
				Msg.Append(Selected.GetType().FullName);
				Msg.AppendLine(")");
			}

			if (!Selected.IsPoint(PublicKeyPoint))
			{
				if (HasSniffer)
				{
					Client!.Information(Msg!.ToString());
					Client.Error("Public key not a point on curve.");
				}

				return false;
			}

			if (!ECDSA.Verify(Data, PublicKeyPoint, HashAlgorithm, Selected, R, S))
			{
				if (HasSniffer)
				{
					Client!.Information(Msg!.ToString());
					Client.Error("ECDSA validation failed.");
				}

				return false;
			}

			if (HasSniffer)
				Client!.Information(Msg!.ToString());

			return true;
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
		/// Hash algorithm to use for in-memory blocks of data.
		/// </summary>
		public abstract HashFunctionArray HashAlgorithm { get; }

		/// <summary>
		/// Hash algorithm to use for streams of data.
		/// </summary>
		public abstract HashFunctionStream HashAlgorithmStream { get; }
	}
}
