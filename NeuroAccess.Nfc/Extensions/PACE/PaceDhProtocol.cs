using System;
using Waher.Runtime.Inventory;

namespace NeuroAccess.Nfc.Extensions.PACE
{
	/// <summary>
	/// Abstract base class for PACE protocols using traditional finite group Diffie-Hellman.
	/// </summary>
	public abstract class PaceDhProtocol() : PaceProtocol()
	{
		/// <summary>
		/// Creates a new ephemeral key, used in the PACE protocol.
		/// </summary>
		/// <returns>Public part of the ephemeral key.</returns>
		public override byte[] CreateNewKey()
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
