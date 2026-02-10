using Waher.Runtime.Inventory;

namespace NeuroAccess.Nfc.Extensions.PACE.Id_PACE_ECDH_IM
{
	/// <summary>
	/// PACE protocol id-PACE-ECDH-IM-3DES-CBC-CBC
	/// Integrated Mapping, Elliptic Curve Diffie-Hellman, 3DES in CBC mode for encryption, and 3DES in CBC mode for message authentication.
	/// </summary>
	public class Id_PACE_ECDH_IM_3DES_CBC_CBC() : PaceProtocol()
	{
		/// <summary>
		/// OID identifying the PACE protocol.
		/// </summary>
		public override string Oid => "0.4.0.127.0.7.2.2.4.4.1";

		/// <summary>
		/// Security strength mapped as a grade.
		/// </summary>
		public override Grade SecurityStrength => Grade.Barely;
	}
}
