using Waher.Runtime.Inventory;

namespace NeuroAccess.Nfc.Extensions.PACE.Id_PACE_ECDH_IM
{
	/// <summary>
	/// PACE protocol id-PACE-ECDH-IM-AES-CBC-CMAC-192
	/// Integrated Mapping, Elliptic Curve Diffie-Hellman, AES-CBC with CMAC, 192 bit key.
	/// </summary>
	public class Id_PACE_ECDH_IM_AES_CBC_CMAC_192() : PaceProtocol()
	{
		/// <summary>
		/// OID identifying the PACE protocol.
		/// </summary>
		public override string Oid => "0.4.0.127.0.7.2.2.4.4.3";

		/// <summary>
		/// Security strength mapped as a grade.
		/// </summary>
		public override Grade SecurityStrength => Grade.Excellent;
	}
}
