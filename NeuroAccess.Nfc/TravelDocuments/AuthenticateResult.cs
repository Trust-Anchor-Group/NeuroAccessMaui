namespace NeuroAccess.Nfc.TravelDocuments
{
	/// <summary>
	/// Enumerations of possible results when authenticating the app with the travel document.
	/// </summary>
	public enum AuthenticateResult
	{
		/// <summary>
		/// Authentication successful.
		/// </summary>
		Success,

		/// <summary>
		/// Already authenticated with the document. Create new client and try again.
		/// </summary>
		AlreadyEncrypted,


		/// <summary>
		/// Unable to initialize PACE.
		/// (Incompatibility, missing support; suggest sending log to support for troubleshooting if problem persists.)
		/// </summary>
		UnableToInitializePace,

		/// <summary>
		/// Unable to authenticate using the selected PACE protocol.
		/// (Incompatibility, missing support; suggest sending log to support for troubleshooting if problem persists.)
		/// </summary>
		UnableToAuthenticatePace,

		/// <summary>
		/// Unable to get BAC challenge. (Probably not a valid/working travel document.)
		/// </summary>
		UnableToGetBacChallenge,

		/// <summary>
		/// Old Travel Document requiring BAC, which is not supported.
		/// </summary>
		BacNotImplemented
	}
}
