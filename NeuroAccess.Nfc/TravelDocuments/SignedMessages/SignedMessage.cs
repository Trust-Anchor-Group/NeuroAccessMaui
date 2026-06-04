using System.Diagnostics.CodeAnalysis;
using NeuroAccess.Nfc.TravelDocuments.Security;
using Waher.Networking;

namespace NeuroAccess.Nfc.TravelDocuments.SignedMessages
{
	/// <summary>
	/// Signed Data, as defined in RFC 5652, §5.1.
	/// </summary>
	public class SignedMessage
	{
		/// <summary>
		/// Signed Data, as defined in RFC 5652, §5.1.
		/// </summary>
		/// <param name="Asn1Vector">ASN.1 decoded vector.</param>
		private SignedMessage(SignedData Data)
		{
			this.Data = Data;
		}

		/// <summary>
		/// Tries to parse an ASN.1-encoded Signed Data structure, as defined in RFC 5652, §5.1.
		/// </summary>
		/// <param name="RawCertificate">ASN.1 DER encoded Signed Data object.</param>
		/// <param name="Parsed">Parsed object.</param>
		/// <returns>If successful.</returns>
		public static bool TryParse(byte[] RawCertificate, [NotNullWhen(true)] out SignedMessage? Parsed)
		{
			Parsed = null;

			if (!ASN1.TryDecodeDer(RawCertificate, out object? Content))
				return false;

			return TryParse(Content, out Parsed);
		}

		/// <summary>
		/// Tries to parse an ASN.1-encoded Signed Data structure, as defined in RFC 5652, §5.1.
		/// </summary>
		/// <param name="RawCertificate">ASN.1 DER encoded Signed Data object.</param>
		/// <param name="Parsed">Parsed object.</param>
		/// <returns>If successful.</returns>
		public static bool TryParse(object? Content, [NotNullWhen(true)] out SignedMessage? Parsed)
		{
			Parsed = null;

			if (Content is Vector SignedDataVector)
				return TryParse(SignedDataVector, out Parsed);
			else if (Content is SignedData SignedData)
			{
				Parsed = new SignedMessage(SignedData);
				return true;
			}
			else
				return false;
		}

		/// <summary>
		/// Tries to parse an ASN.1-encoded Signed Data structure, as defined in RFC 5652, §5.1.
		/// </summary>
		/// <param name="SignedDataVector">Decoded Signed Data object.</param>
		/// <param name="Parsed">Parsed object.</param>
		/// <returns>If successful.</returns>
		public static bool TryParse(Vector SignedDataVector, [NotNullWhen(true)] out SignedMessage? Parsed)
		{
			foreach (object Item in SignedDataVector.Elements)
			{
				if (TryParse(Item, out Parsed))
					return true;
			}

			Parsed = null;
			return false;
		}

		/// <summary>
		/// Signed Data
		/// </summary>
		public SignedData Data { get; }

		/// <summary>
		/// Checks if the signature is valid.
		/// </summary>
		/// <param name="VerifyCertificates">If certificates should be verified also.</param>
		/// <returns>If signature, and optional embedded certificates, are valid.</returns>
		public bool CheckSignature(bool VerifyCertificates)
		{
			return this.CheckSignature(VerifyCertificates, null);
		}

		/// <summary>
		/// Checks if the signature is valid.
		/// </summary>
		/// <param name="VerifyCertificates">If certificates should be verified also.</param>
		/// <param name="Client">Optional communication layer, for informative communication logs.</param>
		/// <returns>If signature, and optional embedded certificates, are valid.</returns>
		public bool CheckSignature(bool VerifyCertificates, ICommunicationLayer? Client)
		{
			return this.Data.CheckSignature(VerifyCertificates, Client);
		}
	}
}
