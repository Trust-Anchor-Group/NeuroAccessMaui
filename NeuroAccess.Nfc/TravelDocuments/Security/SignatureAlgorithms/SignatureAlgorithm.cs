using System;
using System.Security.Cryptography.X509Certificates;

namespace NeuroAccess.Nfc.TravelDocuments.Security.SignatureAlgorithms
{
	/// <summary>
	/// Abstract base class for signature algorithms, as defined in RFC 5280.
	/// </summary>
	public abstract class SignatureAlgorithm : SecurityObject, ISignatureAlgorithm
	{
		/// <summary>
		/// Abstract base class for signature algorithms, as defined in RFC 5280.
		/// </summary>
		public SignatureAlgorithm()
			: base()
		{
		}

		/// <summary>
		/// Tries to decode an ASN.1-decoded algorithm identifier into an instance of the
		/// corresponding signature algorithm.
		/// </summary>
		/// <param name="AlgorithmIdentifier">Algorithm identifier.</param>
		/// <returns>Instance of signature algorithm, if found.</returns>
		public static ISignatureAlgorithm? TryDecode(Vector AlgorithmIdentifier)
		{
			if (AlgorithmIdentifier.Elements.Length == 0)
				return null;

			if (AlgorithmIdentifier.Elements.GetValue(0) is not ISignatureAlgorithm Algorithm)
				return null;

			if (!Algorithm.Configure(AlgorithmIdentifier.Elements))
				return null;

			return Algorithm;
		}

		/// <summary>
		/// If the object can be configured by the security information provided.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <returns>If the object can be configured, given the security information.</returns>
		public override bool Configure(Array SecurityInfo)
		{
			return true;
		}

		/// <summary>
		/// Verifies a digital signature.
		/// </summary>
		/// <param name="Data">Data being signed.</param>
		/// <param name="Signature">Digital signature.</param>
		/// <param name="Certificate">Certificate of the signing body.</param>
		/// <returns>If the digital signature is correct.</returns>
		public abstract bool VerifySignature(byte[] Data, byte[] Signature, X509Certificate2 Certificate);

	}
}
