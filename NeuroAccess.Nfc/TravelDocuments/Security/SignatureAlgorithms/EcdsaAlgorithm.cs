using System;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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
		/// <param name="Certificate">Certificate of the signing body.</param>
		/// <param name="Client">Optional client reference.</param>
		/// <returns>If the digital signature is correct.</returns>
		public override bool VerifySignature(byte[] Data, byte[] Signature, X509Certificate2 Certificate,
			ICommunicationLayer? Client)
		{
			using ECDsa? Ecdsa = Certificate.GetECDsaPublicKey();
			if (Ecdsa is null)
			{
				Client?.Error("Unable to get ECDSA public key from certificate.");
				return false;
			}

			ECParameters Parameters = Ecdsa.ExportParameters(false);
			ECCurve Curve = Parameters.Curve;

			if (Parameters.Q.X is null || Parameters.Q.Y is null)
			{
				Client?.Error("Unable to get coordinates of ECDSA public key.");
				return false;
			}

			if (Parameters.Curve.Order is null)
			{
				Client?.Error("Unable to get order of ECDSA public key curve.");
				return false;
			}

			if (Parameters.Curve.Cofactor is null)
			{
				Client?.Error("Unable to get cofactor of ECDSA public key curve.");
				return false;
			}

			System.Numerics.BigInteger Order = EllipticCurve.ToInt(Parameters.Curve.Order, true);
			System.Numerics.BigInteger Cofactor = EllipticCurve.ToInt(Parameters.Curve.Cofactor, true);
			System.Numerics.BigInteger GeneratorX = EllipticCurve.ToInt(Parameters.Curve.G.X, true);
			System.Numerics.BigInteger GeneratorY = EllipticCurve.ToInt(Parameters.Curve.G.Y, true);
			System.Numerics.BigInteger CertPubX = EllipticCurve.ToInt(Parameters.Q.X, true);
			System.Numerics.BigInteger CertPubY = EllipticCurve.ToInt(Parameters.Q.Y, true);

			if (!TravelDocumentsClient.TryDecodeDER(Signature, out object? Obj) ||
				Obj is not Vector SignatureVector ||
				SignatureVector.Elements.Length != 2 ||
				SignatureVector.Elements.GetValue(0) is not System.Numerics.BigInteger R ||
				SignatureVector.Elements.GetValue(1) is not System.Numerics.BigInteger S)
			{
				return false;
			}
			bool Result;

			// The Ecdsa.VerifyData() method works differently on different operating systems,
			// for some ICAO certificates, even if input is binary exactly the same. This might (?)
			// be caused by imperfect signature and/or coordinate encodings, where Windows is more
			// tolerant than Android, for example, or support for different curves (?). By providing
			// a complete platform-independent managed implementation, validation becomes portable.

			PrimeFieldCurve? Selected = null;

			switch (Curve.CurveType)
			{
				case ECCurve.ECCurveType.PrimeShortWeierstrass:
					if (Parameters.Curve.A is null)
					{
						Client?.Error("Unable to get a-coefficient of ECDSA public key Weierstrass curve.");
						return false;
					}

					if (Parameters.Curve.B is null)
					{
						Client?.Error("Unable to get b-coefficient of ECDSA public key Weierstrass curve.");
						return false;
					}

					if (Parameters.Curve.Prime is null)
					{
						Client?.Error("Unable to get prime of ECDSA public key Weierstrass curve.");
						return false;
					}

					System.Numerics.BigInteger CurveA = EllipticCurve.ToInt(Parameters.Curve.A, true);
					System.Numerics.BigInteger CurveB = EllipticCurve.ToInt(Parameters.Curve.B, true);
					System.Numerics.BigInteger Prime = EllipticCurve.ToInt(Parameters.Curve.Prime, true);

					foreach (EllipticCurve EC in curves)
					{
						if (EC is WeierstrassCurve WeierstrassCurve2)
						{
							if (WeierstrassCurve2.A == CurveA &&
								WeierstrassCurve2.B == CurveB &&
								WeierstrassCurve2.Prime == Prime &&
								WeierstrassCurve2.Cofactor == Cofactor &&
								WeierstrassCurve2.Order == Order &&
								WeierstrassCurve2.BasePoint.X == GeneratorX &&
								WeierstrassCurve2.BasePoint.Y == GeneratorY)
							{
								Selected = WeierstrassCurve2;
								break;
							}
						}
					}

					if (Selected is null)
					{
						Client?.Warning("Wirestrass Curve not recognized.\r\n\r\nA: " +
							CurveA.ToString(CultureInfo.InvariantCulture) +
							",\r\nB: " + CurveB.ToString(CultureInfo.InvariantCulture) +
							",\r\np: " + Prime.ToString(CultureInfo.InvariantCulture) +
							",\r\nCofactor: " + Cofactor.ToString(CultureInfo.InvariantCulture) +
							",\r\nOrder: " + Order.ToString(CultureInfo.InvariantCulture) +
							",\r\nG.X: " + GeneratorX.ToString(CultureInfo.InvariantCulture) +
							",\r\nG.Y: " + GeneratorY.ToString(CultureInfo.InvariantCulture));
					}
					break;

				case ECCurve.ECCurveType.PrimeMontgomery:
					if (Parameters.Curve.A is null)
					{
						Client?.Error("Unable to get a-coefficient of ECDSA public key Montgomery curve.");
						return false;
					}

					if (Parameters.Curve.Prime is null)
					{
						Client?.Error("Unable to get prime of ECDSA public key Montgomery curve.");
						return false;
					}

					CurveA = EllipticCurve.ToInt(Parameters.Curve.A, true);
					Prime = EllipticCurve.ToInt(Parameters.Curve.Prime, true);

					foreach (EllipticCurve EC in curves)
					{
						if (EC is MontgomeryCurve MontgomeryCurve2)
						{
							if (MontgomeryCurve2.A == CurveA &&
								MontgomeryCurve2.Prime == Prime &&
								MontgomeryCurve2.Cofactor == Cofactor &&
								MontgomeryCurve2.Order == Order &&
								MontgomeryCurve2.BasePoint.X == GeneratorX &&
								MontgomeryCurve2.BasePoint.Y == GeneratorY)
							{
								Selected = MontgomeryCurve2;
								break;
							}
						}
					}

					if (Selected is null)
					{
						Client?.Warning("Montgomery Curve not recognized.\r\n\r\nA: " +
							CurveA.ToString(CultureInfo.InvariantCulture) +
							",\r\np: " + Prime.ToString(CultureInfo.InvariantCulture) +
							",\r\nCofactor: " + Cofactor.ToString(CultureInfo.InvariantCulture) +
							",\r\nOrder: " + Order.ToString(CultureInfo.InvariantCulture) +
							",\r\nG.X: " + GeneratorX.ToString(CultureInfo.InvariantCulture) +
							",\r\nG.Y: " + GeneratorY.ToString(CultureInfo.InvariantCulture));
					}
					break;

				case ECCurve.ECCurveType.PrimeTwistedEdwards:
					if (Parameters.Curve.Prime is null)
					{
						Client?.Error("Unable to get prime of ECDSA public key (Twisted) Edwards curve.");
						return false;
					}

					Prime = EllipticCurve.ToInt(Parameters.Curve.Prime, true);

					foreach (EllipticCurve EC in curves)
					{
						if (EC is EdwardsCurveBase EdwardsCurve2)
						{
							if (EdwardsCurve2.Prime == Prime &&
								EdwardsCurve2.Cofactor == Cofactor &&
								EdwardsCurve2.Order == Order &&
								EdwardsCurve2.BasePoint.X == GeneratorX &&
								EdwardsCurve2.BasePoint.Y == GeneratorY)
							{
								Selected = EdwardsCurve2;
								break;
							}
						}
					}

					if (Selected is null)
					{
						Client?.Warning("(Twisted) Edwards Curve not recognized.\r\n\r\nA: " +
							",\r\np: " + Prime.ToString(CultureInfo.InvariantCulture) +
							",\r\nCofactor: " + Cofactor.ToString(CultureInfo.InvariantCulture) +
							",\r\nOrder: " + Order.ToString(CultureInfo.InvariantCulture) +
							",\r\nG.X: " + GeneratorX.ToString(CultureInfo.InvariantCulture) +
							",\r\nG.Y: " + GeneratorY.ToString(CultureInfo.InvariantCulture));
					}
					break;

				case ECCurve.ECCurveType.Named:
				case ECCurve.ECCurveType.Characteristic2:
				case ECCurve.ECCurveType.Implicit:
				default:
					Client?.Warning("Elliptic Curve not recognized.\r\n\r\nA: " +
						Convert.ToBase64String(Parameters.Curve.A ?? []) +
						",\r\nB: " + Convert.ToBase64String(Parameters.Curve.B ?? []) +
						",\r\np: " + Convert.ToBase64String(Parameters.Curve.Prime ?? []) +
						",\r\nCofactor: " + Cofactor.ToString(CultureInfo.InvariantCulture) +
						",\r\nOrder: " + Order.ToString(CultureInfo.InvariantCulture) +
						",\r\nG.X: " + GeneratorX.ToString(CultureInfo.InvariantCulture) +
						",\r\nG.Y: " + GeneratorY.ToString(CultureInfo.InvariantCulture));
					break;
			}

			if (Selected is null)
				Result = Ecdsa.VerifyData(Data, Signature, this.HashAlgorithmName, DSASignatureFormat.Rfc3279DerSequence);
			else
			{
				Client?.Information("Curve used for signature: " + Selected.GetType().FullName);

				PointOnCurve CertPub = new(CertPubX, CertPubY);
				if (!Selected.IsPoint(CertPub))
				{
					Client?.Error("Public key not a point on curve.");
					return false;
				}

				byte[] CertPubBin = Selected.Encode(CertPub, true);
				byte[] CertSignature = Selected.Encode(new PointOnCurve(R, S), true);
				HashFunctionArray? HashFunction = ToHashFunction(this.HashAlgorithmName);

				if (HashFunction is null)
				{
					Client?.Warning("Hash function not supported: " + this.HashAlgorithmName.ToString());
					Result = Ecdsa.VerifyData(Data, Signature, this.HashAlgorithmName, DSASignatureFormat.Rfc3279DerSequence);
				}
				else
				{
					Result = ECDSA.Verify(Data, CertPubBin, true, HashFunction,
						Selected.OrderBytes, Selected.BigIntegerBytes, Selected.MsbOrderMask,
						Selected, CertSignature);
				}
			}

			if (!Result && (Client?.HasSniffers ?? false))
			{
				Client?.Warning("Invalid signature.\r\n\r\nAlgorithm Type: " + this.GetType().FullName +
					"\r\nPublic Key: " + Convert.ToBase64String(Ecdsa.ExportSubjectPublicKeyInfo()) +
				"\r\nSignature: " + Convert.ToBase64String(Signature) +
				"\r\nData: " + Convert.ToBase64String(Data) +
				"\r\nValid: " + Result.ToString());
			}

			return Result;
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
		public abstract HashAlgorithmName HashAlgorithmName { get; }

		private static HashFunctionArray? ToHashFunction(HashAlgorithmName AlgorithmName)
		{
			return AlgorithmName.Name switch
			{
				"MD5" => Hashes.ComputeMD5Hash,
				"SHA1" => Hashes.ComputeSHA1Hash,
				"SHA256" => Hashes.ComputeSHA256Hash,
				"SHA384" => Hashes.ComputeSHA384Hash,
				"SHA512" => Hashes.ComputeSHA512Hash,
				"SHA3-256" => SHA3_256.Create().ComputeHash,
				"SHA3-384" => SHA3_384.Create().ComputeHash,
				"SHA3-512" => SHA3_512.Create().ComputeHash,
				_ => null,
			};
		}
	}
}
