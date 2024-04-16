namespace NeuroAccessMaui
{
	/// <summary>
	/// Purpose for requesting the user to authenticate itself.
	/// </summary>
	public enum AuthenticationPurpose
	{
		/// <summary>
		/// App is being started
		/// </summary>
		Start,

		/// <summary>
		/// App is resuming
		/// </summary>
		Resume,

		/// <summary>
		/// Petition for signature has been received
		/// </summary>
		PetitionForSignatureReceived,

		/// <summary>
		/// TAG Signature QR-code has been scanned
		/// </summary>
		TagSignature,

		/// <summary>
		/// An application has to be signed
		/// </summary>
		SignApplication,

		/// <summary>
		/// An application is to be removed.
		/// </summary>
		RevokeApplication,

		/// <summary>
		/// A contract is to be signed.
		/// </summary>
		SignContract,

		/// <summary>
		/// A contract is to be obsoleted.
		/// </summary>
		ObsoleteContract,

		/// <summary>
		/// A contract is to be deleted.
		/// </summary>
		DeleteContract,

		/// <summary>
		/// A petition is to be signed.
		/// </summary>
		SignPetition,

		/// <summary>
		/// Authentication method is to be changed.
		/// </summary>
		ChangeAuthenticationMethod,

		/// <summary>
		/// Screen capture is to be permitted.
		/// </summary>
		PermitScreenCapture,

		/// <summary>
		/// Screen capture is to be prohibited.
		/// </summary>
		ProhibitScreenCapture,

		/// <summary>
		/// Identity is to be revoked.
		/// </summary>
		RevokeIdentity,

		/// <summary>
		/// Identity is to be reported as compromized.
		/// </summary>
		ReportAsCompromized,

		/// <summary>
		/// Identity is to be transfered
		/// </summary>
		TransferIdentity,

		/// <summary>
		/// Petition request is to be accepted
		/// </summary>
		AcceptPetitionRequest,

		/// <summary>
		/// Reviewer is to be authenticated
		/// </summary>
		AuthenticateReviewer,

		/// <summary>
		/// Peer review is to be accepted
		/// </summary>
		AcceptPeerReview,

		/// <summary>
		/// Peer review is to be declined
		/// </summary>
		DeclinePeerReview,

		/// <summary>
		/// Thing about to be claimed.
		/// </summary>
		ClaimThing,

		/// <summary>
		/// Provisioning rules for thing to be deleted.
		/// </summary>
		DeleteRules,

		/// <summary>
		/// Thing is to be disowned
		/// </summary>
		DisownThing,

		/// <summary>
		/// Thing to be added to list of things.
		/// </summary>
		AddToListOfThings,

		/// <summary>
		/// Thing to be removed from list of things.
		/// </summary>
		RemoveFromListOfThings,

		/// <summary>
		/// Accept eDaler URI
		/// </summary>
		AcceptEDalerUri,

		/// <summary>
		/// Pay online
		/// </summary>
		PayOnline,

		/// <summary>
		/// Pay offline
		/// </summary>
		PayOffline,

		/// <summary>
		/// Submit eDaler URI (in case previous attempts were unsuccessful, or URI is from an offline payment).
		/// </summary>
		SubmitEDalerUri,

		/// <summary>
		/// Show eDaler URI as (scannable) QR code. (Should be treated as an offline payment.)
		/// </summary>
		ShowUriAsQr,

		/// <summary>
		/// Send payment
		/// </summary>
		SendPayment,

		/// <summary>
		/// Propose contract
		/// </summary>
		ProposeContract,

		/// <summary>
		/// NFC Tag detected
		/// </summary>
		NfcTagDetected,

		/// <summary>
		/// View ID
		/// </summary>
		ViewId,

		/// <summary>
		/// Petition Identity
		/// </summary>
		PetitionIdentity,

		/// <summary>
		/// Apply for a new Organizational ID
		/// </summary>
		ApplyForOrganizationalId,

		/// <summary>
		/// Apply for a new Personal ID
		/// </summary>
		ApplyForPersonalId
	}
}
