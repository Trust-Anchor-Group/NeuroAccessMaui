using System;
using System.Numerics;
using Waher.Runtime.Inventory;

namespace NeuroAccess.Nfc.Extensions.PACE
{
	/// <summary>
	/// Abstract base class for PACE protocols.
	/// </summary>
	public abstract class PaceProtocol() : IPaceProtocol
	{
		private BigInteger version;
		private BigInteger? parameterId;

		/// <summary>
		/// OID identifying the PACE protocol.
		/// </summary>
		public abstract string Oid { get; }

		/// <summary>
		/// Security strength mapped as a grade.
		/// </summary>
		public abstract Grade SecurityStrength { get; }

		/// <summary>
		/// If Chip-Authentication-Mapping is supported by the protocol.
		/// </summary>
		public virtual bool ChipAuthenticationMapping => false;

		/// <summary>
		/// Required protocol version.
		/// </summary>
		public BigInteger Version => this.version;

		/// <summary>
		/// Optional Parameter ID.
		/// </summary>
		public BigInteger? ParameterId => this.parameterId;

		/// <summary>
		/// If the interface understands objects such as Object.
		/// </summary>
		/// <param name="Object">OID</param>
		/// <returns>How well objects of this type are supported.</returns>
		public Grade Supports(string Object)
		{
			return Object == this.Oid ? this.SecurityStrength : Grade.NotAtAll;
		}

		/// <summary>
		/// If the protocol could be configured by the security information provided.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <returns>If the protocol could be configured, given the security information.</returns>
		public virtual bool Configure(Array SecurityInfo)
		{
			if (SecurityInfo.Length < 2)
				return false;

			if (SecurityInfo.GetValue(1) is not BigInteger Version)
				return false;

			this.version = Version;

			if (SecurityInfo.Length > 3)
				return false;
			else if (SecurityInfo.Length == 2)
				return true;
			else if (SecurityInfo.GetValue(2) is not BigInteger ParameterId)
				return false;
			else
			{
				this.parameterId = ParameterId;
				return true;
			}
		}

		/// <summary>
		/// Creates a new ephemeral key, used in the PACE protocol.
		/// </summary>
		/// <returns>Public part of the ephemeral key.</returns>
		public virtual byte[] CreateNewEphemeralKey()
		{
			// TODO
			throw new NotImplementedException("This protocol does not implement ephemeral key generation.");
		}
	}
}
