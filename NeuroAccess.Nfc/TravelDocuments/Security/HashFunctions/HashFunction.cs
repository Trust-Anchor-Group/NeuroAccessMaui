using System;

namespace NeuroAccess.Nfc.TravelDocuments.Security.HashFunctions
{
	/// <summary>
	/// Abstract base class for hash functions.
	/// </summary>
	public abstract class HashFunction : SecurityObject
	{
		/// <summary>
		/// If the object can be configured by the security information provided.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <returns>If the object can be configured, given the security information.</returns>
		public override bool Configure(Array SecurityInfo)
		{
			return true;
		}

		/// <inheritdoc/>
		public override string ToString()
		{
			return this.GetType().Name;
		}

		/// <summary>
		/// Computes a Hash Digest from binary data.
		/// </summary>
		/// <param name="Data">Binary data.</param>
		/// <returns>Hash Digest of binary data.</returns>
		public abstract byte[] ComputeHash(byte[] Data);
	}
}
