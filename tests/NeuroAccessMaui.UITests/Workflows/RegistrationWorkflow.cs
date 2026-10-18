using NeuroAccessMaui.Testing.Services;
using NeuroAccessMaui.UITests.Screens;

namespace NeuroAccessMaui.UITests.Workflows;

/// <summary>Owns the temporary-phone registration journey and its account prerequisites.</summary>
internal sealed class RegistrationWorkflow
{
	private readonly UiSession session;
	internal RegistrationWorkflow(UiSession Session) => this.session = Session;

	internal static void Validate(TestSettings Settings)
	{
		Settings.RequireOptIn("ALLOW_REGISTRATION");
		Assert.IsTrue(Settings.Required("REGISTRATION_MODE") == "temporary-phone",
			"This registration scenario requires temporary-phone mode.");
		Settings.Phone();
		Settings.Pin(Creating: true);
		Settings.Username();
		Settings.GetUri("OTP_ENDPOINT", false);
	}

	internal async Task RegisterAsync()
	{
		Validate(this.session.Settings);
		string Phone = this.session.Settings.Phone();
		string Pin = this.session.Settings.Pin(Creating: true);
		string Username = this.session.Settings.Username();
		TestOtpClient Otp = new TestOtpClient(this.session.Settings.GetUri("OTP_ENDPOINT", false));
		OnboardingScreen Onboarding = new OnboardingScreen(this.session);
		new LanguageScreen(this.session).Select(this.session.Text);
		Onboarding.AssertProvider();
		Onboarding.SelectProvider();
		Onboarding.AssertPhone();
		Onboarding.SelectUnitedStates();
		Onboarding.EnterPhone(Phone);
		Onboarding.SendCode();
		Onboarding.AssertCode();
		Onboarding.EnterCode(await Otp.FetchAsync("+1" + Phone));
		Onboarding.VerifyCode();
		Onboarding.EnterUsername(Username);
		Onboarding.CreatePin(Pin, true);
		Onboarding.Finish(false);
		Onboarding.DismissNotifications();
		Onboarding.AssertRegisteredHome();
	}
}
