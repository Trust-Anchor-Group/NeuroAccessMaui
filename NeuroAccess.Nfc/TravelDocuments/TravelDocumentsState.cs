namespace NeuroAccess.Nfc.TravelDocuments
{
	/// <summary>
	/// State of travel documents interface.
	/// </summary>
	public enum TravelDocumentsState
	{
		/// <summary>
		/// Travel documents interface is detected. This is the initial state.
		/// </summary>
		Detected,

		/// <summary>
		/// A file is being selected. Associated data will contain a <see cref="ushort"/> value
		/// identifying the file being selected.
		/// </summary>
		SelectingFile,

		/// <summary>
		/// Binary data is being read from the selected file. Associated data will contain a
		/// <see cref="ushort"/> value indicating the current offset being read.
		/// </summary>
		ReadingBinary,

		/// <summary>
		/// Matching PACE cipher implementations with OIDs presented by chip.
		/// </summary>
		FindingCipher,

		/// <summary>
		/// Selecting cipher in chip.
		/// </summary>
		SelectingCipher,

		/// <summary>
		/// Retrieving PACE nonce to use for authentication.
		/// </summary>
		GettingNonce,

		/// <summary>
		/// Getting PACE public key to use for authentication.
		/// </summary>
		GettingPublicKey,

		/// <summary>
		/// Getting PACE additional ephemeral public key to use for authentication.
		/// </summary>
		GettingEphemeralPublicKey,

		/// <summary>
		/// Getting PACE verification token to validate authentication.
		/// </summary>
		GettingVerificationToken,

		/// <summary>
		/// Getting BAC challenge (BAC is obsolete, but still an option for older documents).
		/// </summary>
		GettingChallenge,

		/// <summary>
		/// Responding to BAC challenge (BAC is obsolete, but still an option for older documents).
		/// </summary>
		RespondingToChallenge
	}
}
