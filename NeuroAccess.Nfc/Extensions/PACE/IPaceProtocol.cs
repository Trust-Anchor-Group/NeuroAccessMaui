using System;
using Waher.Runtime.Inventory;

namespace NeuroAccess.Nfc.Extensions.PACE
{
	/// <summary>
	/// Basic interface for PACE protocols.
	/// </summary>
	public interface IPaceProtocol : IProcessingSupport<string>
	{
		/// <summary>
		/// OID identifying the PACE protocol.
		/// </summary>
		string Oid { get; }

		/// <summary>
		/// Security strength mapped as a grade.
		/// </summary>
		Grade SecurityStrength { get; }

		/// <summary>
		/// If Chip-Authentication-Mapping is supported by the protocol.
		/// </summary>
		bool ChipAuthenticationMapping { get; }

		/// <summary>
		/// If the protocol could be configured by the security information provided.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <returns>If the protocol could be configured, given the security information.</returns>
		bool Configure(Array SecurityInfo);
	}
}
