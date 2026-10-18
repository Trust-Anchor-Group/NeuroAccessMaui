using NeuroAccessMaui.Testing.Services;
using NeuroAccessMaui.UITests.Screens;
using NeuroAccessMaui.UITests.Screens.Kyc;

namespace NeuroAccessMaui.UITests.Workflows;

/// <summary>Revokes the identity through Settings and verifies the phone again once the old identity shows as Obsoleted.</summary>
internal sealed class RevocationWorkflow
{
	private readonly UiSession session;
	internal RevocationWorkflow(UiSession Session) => this.session = Session;

	internal async Task RevokeAndReverifyAsync(string IdentityId)
	{
		new ApplicationsScreen(this.session).ReturnHome();
		AccountScreen Account = new AccountScreen(this.session);
		Account.OpenSettings();
		Account.RevokeIdentity();
		Account.AssertPin();
		Account.EnterPin(this.session.Settings.Pin(), AutomationIds.ScreenOnboardingPhone);
		OnboardingScreen Onboarding = new OnboardingScreen(this.session);
		// Onboarding also opens after a refused revocation or a local reset, so check the old identity's state.
		Onboarding.AwaitProfileIdentity("Obsoleted", IdentityId);
		await this.ReverifyPhoneAsync(Onboarding);
	}

	private async Task ReverifyPhoneAsync(OnboardingScreen Onboarding)
	{
		string Phone = this.session.Settings.Phone();
		TestOtpClient Otp = new TestOtpClient(this.session.Settings.GetUri("OTP_ENDPOINT", false));
		for (int Attempt = 0; ; Attempt++)
		{
			if (Onboarding.AwaitPhoneOrFinish() == AutomationIds.ButtonOnboardingFinalizeContinue)
			{
				Onboarding.Finish();
				return;
			}
			Assert.IsTrue(Attempt < 2, "Phone verification returned to the phone step twice instead of finishing onboarding.");
			Onboarding.AssertPhone(SendEnabled: true);
			Onboarding.AssertPrefilledPhone(Phone);
			if (Onboarding.SendCodeAgain() == AutomationIds.ScreenSecurityVerifycode)
			{
				Onboarding.AssertCode();
				Onboarding.EnterCode(await Otp.FetchAsync("+1" + Phone));
				Onboarding.VerifyCode();
			}
		}
	}
}
