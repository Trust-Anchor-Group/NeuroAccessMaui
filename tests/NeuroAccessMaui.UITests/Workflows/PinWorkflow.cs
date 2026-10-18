using NeuroAccessMaui.UITests.Screens;

namespace NeuroAccessMaui.UITests.Workflows;

/// <summary>Changes the PIN through Settings and checks that only the new PIN unlocks the identity.</summary>
internal sealed class PinWorkflow
{
	private readonly UiSession session;
	internal PinWorkflow(UiSession Session) => this.session = Session;

	internal void Change()
	{
		string Current = this.session.Settings.Pin();
		string New = this.session.Settings.Pin(New: true);
		AccountScreen Account = new AccountScreen(this.session);
		IdentityScreen Identity = new IdentityScreen(this.session);
		Account.OpenIdentity();
		Identity.AssertApproved();
		Identity.Close(AutomationIds.ScreenMainHome);
		Account.OpenSettings();
		Account.SettingsAction(AutomationIds.ButtonMainSettingsChangepassword);
		Account.AssertPin();
		Account.EnterPin(Current, AutomationIds.ScreenOnboardingPassword);
		OnboardingScreen Onboarding = new OnboardingScreen(this.session);
		Onboarding.CreatePin(New, false);
		Onboarding.Finish();
		Account.OpenIdentity(Authenticated: true);
	}

	internal void VerifyChanged()
	{
		string Old = this.session.Settings.Pin();
		string New = this.session.Settings.Pin(New: true);
		AccountScreen Account = new AccountScreen(this.session);
		Account.OpenIdentity(RequirePin: true);
		Account.RejectPin(Old);
		Account.EnterPin(New, AutomationIds.ScreenIdentityCard);
	}
}
