using System;
using Waher.Runtime.Inventory;
using Waher.Security.EllipticCurves;

namespace NeuroAccess.Nfc.TravelDocuments.PACE
{
	/// <summary>
	/// Abstract base class for PACE protocols using Elliptic Curve Cryptography (EEC).
	/// </summary>
	public abstract class PaceEcdhProtocol() : PaceProtocol()
	{
		private EllipticCurve? curve;

		/// <summary>
		/// Selected curve.
		/// </summary>
		public EllipticCurve? Curve => this.curve;

		/// <summary>
		/// If the protocol could be configured by the security information provided.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <returns>If the protocol could be configured, given the security information.</returns>
		public override bool Configure(Array SecurityInfo)
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
					return false;   // TODO: 1024 - bit MODP Group with 160 - bit Prime Order, RFC 5114

				case 1:
					return false;   // TODO: 2048 - bit MODP Group with 224 - bit Prime Order, RFC 5114

				case 2:
					return false;   // TODO: 2048 - bit MODP Group with 256 - bit Prime Order

				// 3-7: Reserved for future use

				case 8:
					this.curve = new NistP192();
					return true;

				case 9:
					this.curve = new BrainpoolP192();
					return true;

				case 10:
					this.curve = new NistP224();
					return true;

				case 11:
					this.curve = new BrainpoolP224();
					return true;

				case 12:
					this.curve = new NistP256();
					return true;

				case 13:
					this.curve = new BrainpoolP256();
					return true;

				case 14:
					this.curve = new BrainpoolP320();
					return true;

				case 15:
					this.curve = new NistP384();
					return true;

				case 16:
					this.curve = new BrainpoolP384();
					return true;

				case 17:
					this.curve = new BrainpoolP512();
					return true;

				case 18:
					this.curve = new NistP521();
					return true;

				// 19-31: Reserved for future use

				default:
					return false;
			}
		}

		/// <summary>
		/// Creates a new ephemeral key, used in the PACE protocol.
		/// </summary>
		/// <returns>Public part of the ephemeral key.</returns>
		public override byte[] CreateNewKey()
		{
			if (this.curve is null)
				throw new NotSupportedException("EEC Curve not configured.");

			this.curve.GenerateKeys();

			return this.curve.PublicKeyBigEndian;
		}

		/// <summary>
		/// Creates an ephemeral key using the same algorithm, cipher and configuration.
		/// </summary>
		/// <returns>Ephemeral key</returns>
		public override IPaceProtocol CreateEphemeralKey()
		{
			PaceEcdhProtocol Result = (PaceEcdhProtocol)Types.Instantiate(this.GetType());

			if (this.curve is not null)
				Result.curve = (EllipticCurve)Types.Instantiate(this.curve.GetType());

			return Result;
		}

		/// <summary>
		/// Sets the private key.
		/// </summary>
		/// <param name="PrivateKey">Private key.</param>
		public void SetPrivateKey(byte[] PrivateKey)
		{
			if (this.curve is null)
				throw new NotSupportedException("EEC Curve not configured.");

			this.curve.SetPrivateKey(PrivateKey);
		}

		/// <summary>
		/// Gets the shared secret, given the local private key previously generated using
		/// <see cref="CreateNewKey"/> and a remote public key.
		/// </summary>
		/// <param name="RemotePublicKey">Remote public key.</param>
		/// <returns>Shared secret</returns>
		public override byte[] GetSharedSecret(byte[] RemotePublicKey)
		{
			//return this.curve!.GetSharedKey(RemotePublicKey, this.HashFunction);

			PointOnCurve H = this.curve!.GetSharedPoint(RemotePublicKey, true);

			byte[] X = H.X.ToByteArray();   // Little endian
			byte[] Y = H.Y.ToByteArray();   // Little endian
			int c = this.curve.OrderBytes;

			if (X.Length != c)
				Array.Resize(ref X, c);

			if (Y.Length != c)
				Array.Resize(ref Y, c);

			Array.Reverse(X);   // Most significant byte first.
			Array.Reverse(Y);   // Most significant byte first.

			byte[] Result = new byte[c << 1];

			Buffer.BlockCopy(X, 0, Result, 0, c);
			Buffer.BlockCopy(Y, 0, Result, c, c);

			return Result;
		}

		/// <summary>
		/// Gets the map used in Generic Mapping.
		/// </summary>
		/// <param name="s">Decrypted nonce.</param>
		/// <param name="RemotePublicKey">Remote public key.</param>
		/// <returns>Generic Map G → Ĝ is defined as Ĝ = s×G+H, where H is calculated
		/// using ECDH.</returns>
		public PointOnCurve GetGenericMap(byte[] s, byte[] RemotePublicKey)
		{
			PointOnCurve G = this.curve!.BasePoint;
			PointOnCurve H = this.curve.GetSharedPoint(RemotePublicKey, true);

			byte[] s2 = (byte[])s.Clone();
			Array.Reverse(s2);

			PointOnCurve Ĝ = this.curve.ScalarMultiplication(s2, G, true);
			this.curve.AddTo(ref Ĝ, H);

			return Ĝ;
		}
	}
}
