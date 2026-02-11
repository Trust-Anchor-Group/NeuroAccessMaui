using System;
using Waher.Security.EllipticCurves;

namespace NeuroAccess.Nfc.Extensions.PACE
{
	/// <summary>
	/// Abstract base class for PACE protocols using Elliptic Curve Cryptography (EEC).
	/// </summary>
	public abstract class PaceEecProtocol() : PaceProtocol()
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
		public override byte[] CreateNewEphemeralKey()
		{
			if (this.curve is null)
				throw new NotSupportedException("EEC Curve not configured.");

			this.curve.GenerateKeys();

			return this.curve.PublicKey;
		}

	}
}
