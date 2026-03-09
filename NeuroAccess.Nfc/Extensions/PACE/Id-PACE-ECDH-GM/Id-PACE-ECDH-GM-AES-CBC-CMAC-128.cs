using Waher.Runtime.Inventory;

namespace NeuroAccess.Nfc.Extensions.PACE.Id_PACE_ECDH_GM
{
	/// <summary>
	/// PACE protocol id-PACE-ECDH-GM-AES-CBC-CMAC-128
	/// Generic Mapping, Elliptic Curve Diffie-Hellman, AES-CBC with CMAC, 128 bit key.
	/// </summary>
	public class Id_PACE_ECDH_GM_AES_CBC_CMAC_128() : PaceEecProtocol128()
	{
		/// <summary>
		/// OID identifying the PACE protocol.
		/// </summary>
		public override string Oid => "0.4.0.127.0.7.2.2.4.2.2";

		/// <summary>
		/// Security strength mapped as a grade.
		/// </summary>
		public override Grade SecurityStrength => Grade.Ok;
	}
}
