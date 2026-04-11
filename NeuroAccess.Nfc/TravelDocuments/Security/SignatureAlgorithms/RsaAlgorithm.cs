using System.Numerics;
using NeuroAccess.Nfc.TravelDocuments.Security.PublicKeys;
using Waher.Networking;
using Waher.Security;

namespace NeuroAccess.Nfc.TravelDocuments.Security.SignatureAlgorithms
{
	/// <summary>
	/// RSA Signature algorithm
	/// </summary>
	public abstract class RsaAlgorithm : SignatureAlgorithm
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
			if (PublicKey is not RsaPublicKey RsaParameters)
			{
				Client?.Error("No RSA public key provided or found.");
				return false;
			}

			return VerifySignatureRsaPkcs1(Data, Signature,
				RsaParameters.Modulus, RsaParameters.Exponent,
				this.HashAlgorithm ?? RsaParameters.HashFunction);
		}

		/// <summary>
		/// Verifies a digital signature, using RSA PKCS#1 v1.5.
		/// </summary>
		/// <param name="Data">Data being signed.</param>
		/// <param name="Signature">Digital signature.</param>
		/// <param name="Modulus">Modulus parameter.</param>
		/// <param name="Exponent">Exponent parameter.</param>
		/// <param name="HashFunction">Hash function to use, if defined.</param>
		/// <returns>If the digital signature is correct.</returns>
		public static bool VerifySignatureRsaPkcs1(byte[] Data, byte[] Signature, BigInteger Modulus,
			BigInteger Exponent, HashFunctionArray? HashFunction)
		{
			return false;
		}

		/// <summary>
		/// Hash algorithm to use.
		/// </summary>
		public abstract HashFunctionArray HashAlgorithm { get; }
	}
}
