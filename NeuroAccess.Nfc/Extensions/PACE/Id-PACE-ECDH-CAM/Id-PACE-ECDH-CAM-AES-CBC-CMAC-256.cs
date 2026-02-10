using Waher.Runtime.Inventory;

namespace NeuroAccess.Nfc.Extensions.PACE.Id_PACE_ECDH_CAM
{
	/// <summary>
	/// PACE protocol id-PACE-ECDH-CAM-AES-CBC-CMAC-256
	/// </summary>
	public class Id_PACE_ECDH_CAM_AES_CBC_CMAC_256() : PaceProtocol()
	{
		/// <summary>
		/// OID identifying the PACE protocol.
		/// </summary>
		public override string Oid => "0.4.0.127.0.7.2.2.4.6.4";

		/// <summary>
		/// Security strength mapped as a grade.
		/// </summary>
		public override Grade SecurityStrength => Grade.Excellent;
	}
}
