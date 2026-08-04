using System;
using System.Xml;
using NeuroAccess.Nfc.TravelDocuments.Security;
using Waher.Runtime.Inventory;
using Waher.Security.EllipticCurves;

namespace NeuroAccess.Nfc.TravelDocuments.PACE
{
	/// <summary>
	/// Abstract base class for PACE protocols using traditional finite group Diffie-Hellman.
	/// </summary>
	public abstract class PaceDhProtocol() : PaceProtocol()
	{
		private ModulusP? zP = null;
		private int primeOrder;

		/// <summary>
		/// Integers modulus p, used in the Diffie-Hellman protocol.
		/// </summary>
		public ModulusP? Zp => this.zP;

		/// <summary>
		/// If the object has been configured.
		/// </summary>
		public override bool IsConfigured => this.zP is not null;

		/// <summary>
		/// If the protocol could be configured by the security information provided.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <returns>If the protocol could be configured, given the security information.</returns>
		public override bool Configure(Vector SecurityInfo)
		{
			if (!base.Configure(SecurityInfo))
				return false;

			if (!this.ParameterId.HasValue ||
				this.ParameterId.Value < int.MinValue ||
				this.ParameterId.Value > int.MaxValue)
			{
				return false;
			}

			switch ((int)this.ParameterId.Value)
			{
				case 0:
					this.primeOrder = 160;
					return false;   // TODO: 1024 - bit MODP Group with 160 - bit Prime Order, RFC 5114

				case 1:
					this.primeOrder = 224;
					return false;   // TODO: 2048 - bit MODP Group with 224 - bit Prime Order, RFC 5114

				case 2:
					this.primeOrder = 256;
					return false;   // TODO: 2048 - bit MODP Group with 256 - bit Prime Order

				// 3-7: Reserved for future use

				default:
					return false;
			}
		}

		/// <summary>
		/// Creates a new ephemeral key, used in the PACE protocol.
		/// </summary>
		/// <param name="Seed">Optional seed value for key generation.</param>
		/// <param name="Index">Index value for key generation.</param>
		/// <returns>Public part of the ephemeral key.</returns>
		public override byte[] CreateNewKey(byte[]? Seed, ref int Index)
		{
			throw new NotImplementedException("DH not implemented.");  // TODO
		}

		/// <summary>
		/// Sets the private key.
		/// </summary>
		/// <param name="PrivateKey">Private key.</param>
		public void SetPrivateKey(byte[] PrivateKey)
		{
			throw new NotImplementedException("DH not implemented.");  // TODO
		}

		/// <summary>
		/// Imports the private key and public key from a previous export.
		/// </summary>
		/// <returns>Public part of imported ephemeral key.</returns>
		public override byte[] ImportKey(XmlDocument Xml)
		{
			throw new NotImplementedException("DH not implemented.");  // TODO
		}

		/// <summary>
		/// Gets the shared secret, given the local private key previously generated using
		/// <see cref="CreateNewKey"/> and a remote public key.
		/// </summary>
		/// <param name="RemotePublicKey">Remote public key.</param>
		/// <returns>Shared secret</returns>
		public override byte[] GetSharedSecret(byte[] RemotePublicKey)
		{
			throw new NotImplementedException("DH not implemented.");  // TODO
		}

		/// <summary>
		/// Creates an ephemeral key using the same algorithm, cipher and configuration.
		/// </summary>
		/// <returns>Ephemeral key</returns>
		public override IPaceProtocol CreateEphemeralKey()
		{
			return (IPaceProtocol)Types.Instantiate(this.GetType());
		}
	}
}
