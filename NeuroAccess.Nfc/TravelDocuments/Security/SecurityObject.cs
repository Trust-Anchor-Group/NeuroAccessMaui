using System;
using Waher.Runtime.Inventory;

namespace NeuroAccess.Nfc.TravelDocuments.Security
{
	/// <summary>
	/// Abstract base class for security objects.
	/// </summary>
	public abstract class SecurityObject : ISecurityObject
	{
		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public abstract string Oid { get; }

		/// <summary>
		/// If the object can be configured by the security information provided.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <returns>If the object can be configured, given the security information.</returns>
		public abstract bool Configure(Array SecurityInfo);

		/// <summary>
		/// If the interface understands objects such as Object.
		/// </summary>
		/// <param name="Object">OID</param>
		/// <returns>How well objects of this type are supported.</returns>
		public virtual Grade Supports(string Object)
		{
			return Object == this.Oid ? Grade.Ok : Grade.NotAtAll;
		}
	}
}
