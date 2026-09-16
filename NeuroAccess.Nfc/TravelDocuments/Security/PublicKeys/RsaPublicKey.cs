using System.Collections.Generic;
using System.Numerics;
using NeuroAccess.Nfc.TravelDocuments.Security.SignatureAlgorithms;
using Waher.Networking;

namespace NeuroAccess.Nfc.TravelDocuments.Security.PublicKeys
{
	/// <summary>
	/// The RSA Public Key object does not have an OID of its own. Instead, it uses the
	/// OID of rsaEncryption (1.2.840.113549.1.1.1), which is later instantiated and configured
	/// when parsing the certificate. (See RFCs 8017 and 5280).
	/// </summary>
	public class RsaPublicKey : PublicKey, ISignatureAlgorithm
	{
		private BigInteger? modulus;
		private BigInteger? exponent;

		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "1.2.840.113549.1.1.1";

		/// <summary>
		/// If the object has been configured.
		/// </summary>
		/// <remarks>
		/// The RSA public key is configured at a later stage in the process, when the
		/// public key is set. Configuration parameters provided are therefore either missing
		/// or only NULL, and are ignored.
		/// </remarks>
		public override bool IsConfigured => true;

		/// <summary>
		/// Modulus
		/// </summary>
		public BigInteger Modulus => this.modulus!.Value;

		/// <summary>
		/// Exponent
		/// </summary>
		public BigInteger Exponent => this.exponent!.Value;

		/// <summary>
		/// If the object can be configured by the security information provided.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <returns>If the object can be configured, given the security information.</returns>
		public override bool Configure(Vector SecurityInfo)
		{
			return true;
		}

		/// <summary>
		/// Sets the public key, based on the security information provided.
		/// </summary>
		/// <param name="PublicKey">Security information used to set the public key.</param>
		/// <returns>If successful in setting the public key.</returns>
		public override bool SetPublicKey(object? PublicKey)
		{
			if (PublicKey is not Vector RsaParameters)
			{
				if (PublicKey is not byte[] KeyData)
					return false;

				if (!ASN1.TryDecodeDer(KeyData, out object? Parsed))
					return false;

				if (Parsed is not Vector v)
					return false;

				RsaParameters = v;
			}

			if (RsaParameters.Length != 2 ||
				RsaParameters.FirstElement is not BigInteger Modulus ||
				RsaParameters[1] is not BigInteger Exponent)
			{
				return false;
			}

			this.modulus = Modulus;
			this.exponent = Exponent;

			return true;
		}

		/// <summary>
		/// Gets parsed parameters from the public key definition, if available.
		/// </summary>
		/// <param name="Parameters">Dictionary to receive parsed parameters.</param>
		public override void GetParsedParameters(Dictionary<string, object?> Parameters)
		{
			Parameters["Modulus"] = this.modulus;
			Parameters["Exponent"] = this.exponent;
		}

		/// <summary>
		/// Verifies a digital signature.
		/// </summary>
		/// <param name="Data">Data being signed.</param>
		/// <param name="Signature">Digital signature.</param>
		/// <param name="PublicKey">Public Key of the signing body.</param>
		/// <param name="Client">Optional client reference.</param>
		/// <returns>If the digital signature is correct.</returns>
		public bool VerifySignature(byte[] Data, byte[] Signature, IPublicKey PublicKey,
			ICommunicationLayer? Client)
		{
			// Hash algorithm not known at this stage, so testing different algorithms to
			// see if one works.

			return
				new RsaSha1().VerifySignature(Data, Signature, PublicKey, Client) ||
				new RsaSha256().VerifySignature(Data, Signature, PublicKey, Client) ||
				new RsaSha384().VerifySignature(Data, Signature, PublicKey, Client) ||
				new RsaSha512().VerifySignature(Data, Signature, PublicKey, Client) ||
				new RsaPss().VerifySignature(Data, Signature, PublicKey, Client);
		}

	}
}
