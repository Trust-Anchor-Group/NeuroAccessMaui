namespace DevStorybook.FakeServices
{
	/// <summary>
	/// Identifies the deterministic result returned by the fake Phone Verification service.
	/// </summary>
	public enum PhoneVerificationOutcome
	{
		/// <summary>The operation succeeds.</summary>
		Success,
		/// <summary>The operation remains in progress until cancelled.</summary>
		Loading,
		/// <summary>The backend rejects the operation.</summary>
		BackendError,
		/// <summary>The operation reports a missing network.</summary>
		NetworkError
	}
}
