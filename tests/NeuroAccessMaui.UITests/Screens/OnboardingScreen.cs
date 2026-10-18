namespace NeuroAccessMaui.UITests.Screens;

/// <summary>Drives the onboarding pages: provider, phone, verification code, username, PIN and finish.</summary>
internal sealed class OnboardingScreen
{
	private readonly UiSession session;
	private MobileUi Ui => this.session.Ui;
	private AppTranslations Text => this.session.Text;

	internal OnboardingScreen(UiSession Session) => this.session = Session;

	internal void AssertProvider()
	{
		this.Ui.Find(AutomationIds.ScreenOnboardingProvider);
		this.Ui.Find(AutomationIds.ButtonOnboardingProviderSelect, AutomationIds.ScreenOnboardingProvider, Ready: true);
	}

	internal void SelectProvider()
	{
		this.Ui.Click(AutomationIds.ButtonOnboardingProviderSelect, AutomationIds.ScreenOnboardingProvider);
		this.Ui.Find(AutomationIds.ScreenOnboardingPhone);
	}

	internal void AssertPhone(bool SendEnabled = false)
	{
		string Screen = AutomationIds.ScreenOnboardingPhone;
		this.Ui.Find(Screen);
		this.Ui.Find(AutomationIds.LabelOnboardingPhoneTitle, Screen);
		this.Ui.Find(AutomationIds.LabelOnboardingPhoneNumber, Screen);
		this.Ui.Input(AutomationIds.InputOnboardingPhoneNumber, Screen);
		this.Ui.Enabled(AutomationIds.ButtonOnboardingPhoneSendCode, SendEnabled, Screen);
	}

	internal void AssertPrefilledPhone(string Phone)
	{
		this.Ui.Input(AutomationIds.InputOnboardingPhoneNumber, AutomationIds.ScreenOnboardingPhone, Expected: Phone);
		this.Ui.Action(AutomationIds.ButtonOnboardingPhoneCountry, "1", true, AutomationIds.ScreenOnboardingPhone);
	}

	internal void SelectUnitedStates()
	{
		this.Ui.Click(AutomationIds.ButtonOnboardingPhoneCountry, AutomationIds.ScreenOnboardingPhone);
		this.Ui.Find(AutomationIds.PopupSharedPhonecountry);
		this.Ui.Enter(AutomationIds.InputSharedPhonecountrySearch, "USA", AutomationIds.PopupSharedPhonecountry);
		this.Ui.Click(AutomationIds.ForKey(AutomationIds.ItemSharedPhonecountryOption, "US"), AutomationIds.PopupSharedPhonecountry);
		this.Ui.Hidden(AutomationIds.PopupSharedPhonecountry);
		this.Ui.Action(AutomationIds.ButtonOnboardingPhoneCountry, "1", true, AutomationIds.ScreenOnboardingPhone);
	}

	internal void EnterPhone(string Phone) => this.Ui.Enter(AutomationIds.InputOnboardingPhoneNumber, Phone, AutomationIds.ScreenOnboardingPhone);

	internal void SendCode()
	{
		this.Ui.Click(AutomationIds.ButtonOnboardingPhoneSendCode, AutomationIds.ScreenOnboardingPhone);
		this.Ui.Find(AutomationIds.ScreenSecurityVerifycode);
	}

	/// <summary>Sends a code to an already verified phone and returns the screen shown next: the code entry or the finish page.</summary>
	internal string SendCodeAgain()
	{
		this.Ui.Click(AutomationIds.ButtonOnboardingPhoneSendCode, AutomationIds.ScreenOnboardingPhone);
		return this.Ui.Any(AutomationIds.ScreenOnboardingFinalize, AutomationIds.ScreenSecurityVerifycode);
	}

	/// <summary>Waits until either the phone page or the finish page can be acted on and returns that action's ID.</summary>
	internal string AwaitPhoneOrFinish() =>
		// A phone page that is only passing through is not enough, so wait for an enabled action.
		this.Ui.AnyReady(AutomationIds.ButtonOnboardingFinalizeContinue, AutomationIds.ButtonOnboardingPhoneSendCode);

	internal void AssertCode()
	{
		string Screen = AutomationIds.ScreenSecurityVerifycode;
		this.Ui.Find(Screen);
		this.Ui.Find(AutomationIds.LabelSecurityVerifycodeTitle, Screen);
		this.Ui.Find(AutomationIds.LabelSecurityVerifycodeDigits, Screen);
		this.AssertDigits();
		this.Ui.Enabled(AutomationIds.ButtonSecurityVerifycodeVerify, false, Screen);
		string Resend = this.Ui.Read(AutomationIds.ButtonSecurityVerifycodeResend, Screen);
		bool Ready = Resend == this.Text["ResendCode"];
		Assert.IsTrue(Ready || this.Text.MatchesNumber("ResendCodeSeconds", Resend), $"Unexpected resend label '{Resend}'.");
		this.Ui.Enabled(AutomationIds.ButtonSecurityVerifycodeResend, Ready, Screen);
		this.Ui.Find(AutomationIds.ButtonSecurityVerifycodeBack, Screen, Ready: true);
	}

	internal void EnterCode(string Code)
	{
		this.AssertDigits();
		this.Ui.Click(AutomationIds.ControlSecurityVerifycodeDigits, AutomationIds.ScreenSecurityVerifycode);
		IWebElement Input = this.session.Driver.SwitchTo().ActiveElement();
		Assert.IsTrue(this.Ui.IsInput(Input) && Input.Enabled && string.IsNullOrEmpty(this.Ui.Value(Input)),
			"The verification code input should be focused, enabled and empty.");
		this.session.Platform.TypeDigits(this.session, Input, Code);
		this.Ui.HideKeyboard();
	}

	internal void VerifyCode()
	{
		this.Ui.Click(AutomationIds.ButtonSecurityVerifycodeVerify, AutomationIds.ScreenSecurityVerifycode);
		this.Ui.Hidden(AutomationIds.ScreenSecurityVerifycode);
	}

	internal void EnterUsername(string Username)
	{
		Assert.AreEqual(AutomationIds.ScreenOnboardingUsername, this.Ui.Any(AutomationIds.ScreenOnboardingEmail, AutomationIds.ScreenOnboardingUsername),
			"The server did not choose the temporary-phone path.");
		string Screen = AutomationIds.ScreenOnboardingUsername;
		this.Ui.Find(AutomationIds.LabelOnboardingUsernameTitle, Screen);
		this.Ui.Input(AutomationIds.InputOnboardingUsernameValue, Screen);
		this.Ui.Enabled(AutomationIds.ButtonOnboardingUsernameContinue, false, Screen);
		this.Ui.Enter(AutomationIds.InputOnboardingUsernameValue, Username, Screen);
		this.Ui.Click(AutomationIds.ButtonOnboardingUsernameContinue, Screen);
		if (this.Ui.Any(AutomationIds.ScreenOnboardingAccount, AutomationIds.ScreenOnboardingPassword) == AutomationIds.ScreenOnboardingAccount)
			this.Ui.Find(AutomationIds.LabelOnboardingAccountTitle, AutomationIds.ScreenOnboardingAccount);
		this.Ui.Find(AutomationIds.ScreenOnboardingPassword);
		this.Ui.Hidden(Screen);
	}

	internal void CreatePin(string Pin, bool TemporaryWarning)
	{
		string Screen = AutomationIds.ScreenOnboardingPassword;
		this.Ui.Find(Screen);
		this.Ui.Find(AutomationIds.LabelOnboardingPasswordTitle, Screen);
		this.Ui.Find(AutomationIds.LabelOnboardingPasswordValue, Screen);
		this.Ui.Find(AutomationIds.LabelOnboardingPasswordConfirmation, Screen);
		this.Ui.Input(AutomationIds.InputOnboardingPasswordValue, Screen);
		this.Ui.Input(AutomationIds.InputOnboardingPasswordConfirmation, Screen);
		this.Ui.Enabled(AutomationIds.ButtonOnboardingPasswordCreate, false, Screen);
		this.Ui.Enter(AutomationIds.InputOnboardingPasswordValue, Pin, Screen);
		this.Ui.Enter(AutomationIds.InputOnboardingPasswordConfirmation, Pin, Screen);
		this.Ui.Click(AutomationIds.ButtonOnboardingPasswordCreate, Screen);
		if (TemporaryWarning)
			this.Ui.Dialog(this.Text["WarningTitle"], Value => Value == this.Text["TestOtpUsed"], this.Text["Ok"]);
		if (this.Ui.Any(AutomationIds.ScreenOnboardingBiometrics, AutomationIds.ScreenOnboardingFinalize) == AutomationIds.ScreenOnboardingBiometrics)
		{
			this.Ui.Find(AutomationIds.LabelOnboardingBiometricsTitle, AutomationIds.ScreenOnboardingBiometrics);
			this.Ui.Click(AutomationIds.ButtonOnboardingBiometricsLater, AutomationIds.ScreenOnboardingBiometrics);
			this.Ui.Find(AutomationIds.ScreenOnboardingFinalize);
		}
	}

	internal void Finish(bool WaitForHome = true)
	{
		string Screen = AutomationIds.ScreenOnboardingFinalize;
		this.Ui.Find(Screen);
		this.Ui.Find(AutomationIds.LabelOnboardingFinalizeTitle, Screen);
		this.Ui.Click(AutomationIds.ButtonOnboardingFinalizeContinue, Screen);
		if (WaitForHome)
			this.Ui.Find(AutomationIds.ScreenMainHome);
	}

	/// <summary>Waits for the automation-only profile marker to show the identity in the given state.</summary>
	internal void AwaitProfileIdentity(string State, string IdentityId)
	{
		string Marker = AutomationIds.ForKey(AutomationIds.ControlOnboardingProfileIdentity, State, IdentityId);
		this.Ui.Until(() => this.Ui.Visible(Marker), $"identity {IdentityId} to reach the {State} state");
	}

	internal void DismissNotifications()
	{
		bool NativeHandled = false;
		this.Ui.Until(() =>
		{
			NativeHandled = this.Ui.AllowNativeNotifications(NativeHandled) || NativeHandled;
			if (this.Ui.Visible(AutomationIds.PopupSystemPermission))
			{
				this.Ui.Find(AutomationIds.LabelSystemPermissionTitle, AutomationIds.PopupSystemPermission);
				this.Ui.Find(AutomationIds.LabelSystemPermissionDescription, AutomationIds.PopupSystemPermission);
				this.Ui.Reveal(AutomationIds.ButtonSystemPermissionSkip, AutomationIds.PopupSystemPermission);
				this.Ui.Click(AutomationIds.ButtonSystemPermissionSkip, AutomationIds.PopupSystemPermission);
				this.Ui.Hidden(AutomationIds.PopupSystemPermission);
			}
			return this.Ui.Visible(AutomationIds.ImageMainHomeBanner) && !this.Ui.Visible(AutomationIds.PopupSystemPermission);
		}, "first Home notification and theme initialization", 120);
		this.Ui.Image(AutomationIds.ImageMainHomeBanner, AutomationIds.ScreenMainHome);
	}

	internal void AssertRegisteredHome()
	{
		string Screen = AutomationIds.ScreenMainHome;
		this.Ui.Find(Screen);
		this.Ui.Find(AutomationIds.ButtonMainHomeShowidentity, Screen, Ready: true);
		this.Ui.Find(AutomationIds.ButtonMainHomeScanqr, Screen, Ready: true);
		this.Ui.Find(AutomationIds.ButtonMainHomeApplyidentity, Screen, Ready: true);
	}

	private void AssertDigits()
	{
		IWebElement Root = this.Ui.Find(AutomationIds.ControlSecurityVerifycodeDigits, AutomationIds.ScreenSecurityVerifycode);
		int Cells = this.Ui.Labels(Root).Count;
		Assert.IsTrue(Root.Enabled && Cells == 6, $"The verification code should show six enabled digit cells, found {Cells}.");
	}
}
