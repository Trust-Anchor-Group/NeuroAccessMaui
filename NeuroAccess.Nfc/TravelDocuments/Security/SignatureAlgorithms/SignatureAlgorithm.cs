using NeuroAccess.Nfc.TravelDocuments.Certificates;
using Waher.Networking;

namespace NeuroAccess.Nfc.TravelDocuments.Security.SignatureAlgorithms
{
	/// <summary>
	/// Abstract base class for signature algorithms, as defined in RFC 5280.
	/// </summary>
	public abstract class SignatureAlgorithm : SecurityObject, ISignatureAlgorithm
	{
		private bool configured;

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
			return TryDecode(AlgorithmIdentifier, null);
		}

		/// <summary>
		/// Tries to decode an ASN.1-decoded algorithm identifier into an instance of the
		/// corresponding signature algorithm.
		/// </summary>
		/// <param name="AlgorithmIdentifier">Algorithm identifier.</param>
		/// <returns>Instance of signature algorithm, if found.</returns>
		public static ISignatureAlgorithm? TryDecode(Vector AlgorithmIdentifier, ICommunicationLayer? Client)
		{
			if (AlgorithmIdentifier.Length == 0)
			{
				Client?.Error("No signature algorithm identifier provided.");
				return null;
			}

			if (AlgorithmIdentifier[0] is not ISignatureAlgorithm Algorithm)
			{
				Client?.Error("Signature algorithm not supported. " + AlgorithmIdentifier[0]?.ToString());
				return null;
			}

			if (!Algorithm.IsConfigured && !Algorithm.Configure(AlgorithmIdentifier))
			{
				Client?.Error("Unable to configure signature algorithm.");
				return null;
			}

			return Algorithm;
		}

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
		/// Verifies a digital signature.
		/// </summary>
		/// <param name="Data">Data being signed.</param>
		/// <param name="Signature">Digital signature.</param>
		/// <param name="Certificate">Certificate of the signing body.</param>
		/// <param name="Client">Optional client reference.</param>
		/// <returns>If the digital signature is correct.</returns>
		public abstract bool VerifySignature(byte[] Data, byte[] Signature, Certificate Certificate,
			ICommunicationLayer? Client);

		/// <inheritdoc/>
		public override string ToString()
		{
			return this.GetType().Name;
		}
	}
}
