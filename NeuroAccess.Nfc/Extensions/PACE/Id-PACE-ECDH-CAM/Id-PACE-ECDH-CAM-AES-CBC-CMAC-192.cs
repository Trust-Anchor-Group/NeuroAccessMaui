using Waher.Runtime.Inventory;

namespace NeuroAccess.Nfc.Extensions.PACE.Id_PACE_ECDH_CAM
{
	/// <summary>
	/// PACE protocol id-PACE-ECDH-CAM-AES-CBC-CMAC-192
	/// Chip Authentication Mapping, Elliptic Curve Diffie-Hellman, AES-CBC with CMAC, 192 bit key.
	/// </summary>
	public class Id_PACE_ECDH_CAM_AES_CBC_CMAC_192() : PaceEcdhProtocol192()
	{
		/// <summary>
		/// OID identifying the PACE protocol.
		/// </summary>
		public override string Oid => "0.4.0.127.0.7.2.2.4.6.3";

		/// <summary>
		/// Security strength mapped as a grade.
		/// </summary>
		public override Grade SecurityStrength => Grade.NotAtAll;   // TODO: When implemented: Grade.Excellent;

		/// <summary>
		/// If Chip-Authentication-Mapping is supported by the protocol.
		/// </summary>
		public override bool ChipAuthenticationMapping => true;
	}
}
