namespace DevStorybook.FakeServices
{
	/// <summary>
	/// Simulates Phone Verification outcomes without network, storage, credentials, or user data.
	/// </summary>
	public sealed class FakePhoneVerificationService : IPhoneVerificationService
	{
		/// <inheritdoc/>
		public async Task<PhoneVerificationResult> SendCodeAsync(
			string PhoneNumber,
			PhoneVerificationOutcome Outcome,
			CancellationToken CancellationToken)
		{
			if (Outcome == PhoneVerificationOutcome.Loading)
				await Task.Delay(Timeout.InfiniteTimeSpan, CancellationToken);

			await Task.Delay(TimeSpan.FromMilliseconds(250), CancellationToken);

			return Outcome switch
			{
				PhoneVerificationOutcome.Success => new PhoneVerificationResult(true, "Verification code sent by the fake service."),
				PhoneVerificationOutcome.BackendError => new PhoneVerificationResult(false, "Controlled backend error: the code was not sent."),
				PhoneVerificationOutcome.NetworkError => new PhoneVerificationResult(false, "Controlled network error: no connection is required for this story."),
				_ => new PhoneVerificationResult(false, "The fake operation was cancelled.")
			};
		}
	}
}
