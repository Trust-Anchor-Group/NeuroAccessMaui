using Waher.Runtime.Inventory;

namespace NeuroAccess.Nfc.Extensions.PACE.Id_PACE_DH_GM
{
	/// <summary>
	/// PACE protocol id-PACE-DH-GM-AES-CBC-CMAC-256
	/// Generic Mapping, Diffie-Hellman, AES-CBC with CMAC, 256 bit key.
	/// </summary>
	public class Id_PACE_DH_GM_AES_CBC_CMAC_256() : PaceProtocol()
	{
		/// <summary>
		/// OID identifying the PACE protocol.
		/// </summary>
		public override string Oid => "0.4.0.127.0.7.2.2.4.1.4";

		/// <summary>
		/// Security strength mapped as a grade.
		/// </summary>
		public override Grade SecurityStrength => Grade.Ok;     // RSA cannot have a higher than Ok grade.
	}
}
