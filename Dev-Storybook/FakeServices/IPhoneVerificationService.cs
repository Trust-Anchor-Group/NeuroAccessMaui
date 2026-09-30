namespace DevStorybook.FakeServices
{
	/// <summary>
	/// Defines the backend boundary used by Phone Verification stories.
	/// </summary>
	public interface IPhoneVerificationService
	{
		/// <summary>
		/// Sends a verification code using only deterministic fake behavior.
		/// </summary>
		/// <param name="PhoneNumber">The non-production phone number displayed in the story.</param>
		/// <param name="Outcome">The controlled result to return.</param>
		/// <param name="CancellationToken">A token that cancels a persistent loading state.</param>
		/// <returns>The controlled send result.</returns>
		Task<PhoneVerificationResult> SendCodeAsync(
			string PhoneNumber,
			PhoneVerificationOutcome Outcome,
			CancellationToken CancellationToken);
	}
}
