namespace NeuroAccessMaui.UITests.Screens;

/// <summary>Drives Home, the PIN popup and Settings.</summary>
internal sealed class AccountScreen
{
	private readonly UiSession session;
	private MobileUi Ui => this.session.Ui;
	private AppTranslations Text => this.session.Text;
	internal AccountScreen(UiSession Session) => this.session = Session;

	internal void Home() => this.Ui.Find(AutomationIds.ScreenMainHome);

	internal void OpenIdentity(bool RequirePin = false, bool Authenticated = false)
	{
		this.Home();
		this.Ui.Click(AutomationIds.ButtonMainHomeShowidentity, AutomationIds.ScreenMainHome);
		if (RequirePin)
			this.AssertPin();
		else if (Authenticated)
			this.Ui.Find(AutomationIds.ScreenIdentityCard);
		else if (this.Ui.Any(AutomationIds.ScreenIdentityCard, AutomationIds.PopupSecurityPassword) == AutomationIds.PopupSecurityPassword)
			this.EnterPin(this.session.Settings.Pin(), AutomationIds.ScreenIdentityCard);
	}

	internal void AssertPin()
	{
		string Popup = AutomationIds.PopupSecurityPassword;
		this.Ui.Find(Popup);
		this.Ui.Find(AutomationIds.LabelSecurityPasswordTitle, Popup);
		this.Ui.Input(AutomationIds.InputSecurityPasswordValue, Popup, Expected: "");
		this.Ui.Enabled(AutomationIds.ButtonSecurityPasswordEnter, false, Popup);
		this.Ui.Find(AutomationIds.ButtonSecurityPasswordCancel, Popup, Ready: true);
	}

	internal void EnterPin(string Pin, string Destination)
	{
		this.Ui.Enter(AutomationIds.InputSecurityPasswordValue, Pin, AutomationIds.PopupSecurityPassword);
		this.Ui.Click(AutomationIds.ButtonSecurityPasswordEnter, AutomationIds.PopupSecurityPassword);
		this.Ui.Hidden(AutomationIds.PopupSecurityPassword);
		this.Ui.Find(Destination);
	}

	internal void RejectPin(string Pin)
	{
		this.Ui.Enter(AutomationIds.InputSecurityPasswordValue, Pin, AutomationIds.PopupSecurityPassword);
		this.Ui.Click(AutomationIds.ButtonSecurityPasswordEnter, AutomationIds.PopupSecurityPassword);
		this.Ui.Dialog(this.Text["ErrorTitle"], Value => this.Text.MatchesNumber("PasswordIsInvalid", Value), this.Text["Ok"]);
		this.AssertPin();
		Assert.IsTrue(this.Ui.FindAll(AutomationIds.ScreenIdentityCard, Visible: false).Count == 0,
			"Identity must not open after a rejected password.");
	}

	internal void OpenSettings()
	{
		this.Home();
		this.Ui.Click(AutomationIds.ButtonMainHomeSettings, AutomationIds.ScreenMainHome);
		this.Ui.Find(AutomationIds.ScreenMainSettings);
		this.Ui.Find(AutomationIds.LabelMainSettingsTitle, AutomationIds.ScreenMainSettings);
	}

	internal void SettingsAction(string Id)
	{
		this.Ui.Reveal(Id, AutomationIds.ScreenMainSettings);
		this.Ui.Click(Id, AutomationIds.ScreenMainSettings);
	}

	internal void RevokeIdentity()
	{
		this.SettingsAction(AutomationIds.ButtonMainSettingsRevokeidentity);
		this.Ui.Confirm(this.Text["Confirm"], this.Text["AreYouSureYouWantToRevokeYourLegalIdentity"], this.Text["Yes"], this.Text["No"]);
	}
}
