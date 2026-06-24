namespace NeuroAccess.Nfc.TravelDocuments.RevocationLists
{
	/// <summary>
	/// Reason for revoking a certificate
	/// </summary>
	public enum RevokedReason
	{
		/// <summary>
		/// Unspecified reason
		/// </summary>
		Unspecified = 0,

		/// <summary>
		/// Keys compromized
		/// </summary>
		KeyCompromise = 1,

		/// <summary>
		/// Certificate Authority Compromized
		/// </summary>
		CACompromise = 2,

		/// <summary>
		/// Affiliation Changed
		/// </summary>
		AffiliationChanged = 3,

		/// <summary>
		/// Superseded
		/// </summary>
		Superseded = 4,

		/// <summary>
		/// Cessation of Operation
		/// </summary>
		CessationOfOperation = 5,

		/// <summary>
		/// Certificate placed on temporary hold
		/// </summary>
		CertificateHold = 6,

		/// <summary>
		/// Certificate has been reinstated
		/// </summary>
		RemoveFromCRL = 8,

		/// <summary>
		/// Privileges for the certificate has been withdrawn by the issuing authority.
		/// </summary>
		PrivilegeWithdrawn = 9,

		/// <summary>
		/// Attribute Authority Compromized
		/// </summary>
		AACompromise = 10
	}
}
