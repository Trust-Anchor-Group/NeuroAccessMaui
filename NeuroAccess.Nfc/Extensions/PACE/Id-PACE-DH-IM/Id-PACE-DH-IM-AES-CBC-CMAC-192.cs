using Waher.Runtime.Inventory;

namespace NeuroAccess.Nfc.Extensions.PACE.Id_PACE_DH_IM
{
	/// <summary>
	/// PACE protocol id-PACE-DH-IM-AES-CBC-CMAC-192
	/// Integrated Mapping, Diffie-Hellman, AES-CBC with CMAC, 192 bit key.
	/// </summary>
	public class Id_PACE_DH_IM_AES_CBC_CMAC_192() : PaceProtocol192()
	{
		/// <summary>
		/// OID identifying the PACE protocol.
		/// </summary>
		public override string Oid => "0.4.0.127.0.7.2.2.4.3.3";

		/// <summary>
		/// Security strength mapped as a grade.
		/// </summary>
		public override Grade SecurityStrength => Grade.Ok;     // RSA cannot have a higher than Ok grade.
	}
}
