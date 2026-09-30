namespace DevStorybook.FakeServices
{
	/// <summary>
	/// Contains a deterministic result from the fake Phone Verification service.
	/// </summary>
	/// <param name="Succeeded">Whether the verification code was sent.</param>
	/// <param name="Message">The controlled status message.</param>
	public sealed record PhoneVerificationResult(bool Succeeded, string Message);
}
