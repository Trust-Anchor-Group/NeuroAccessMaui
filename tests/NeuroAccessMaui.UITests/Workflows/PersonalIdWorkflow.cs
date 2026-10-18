using NeuroAccessMaui.UITests.Models;
using NeuroAccessMaui.UITests.Screens;
using NeuroAccessMaui.UITests.Screens.Kyc;

namespace NeuroAccessMaui.UITests.Workflows;

/// <summary>Composes the shared application journey and validates prerequisites before account mutations.</summary>
internal sealed class PersonalIdWorkflow
{
	private readonly UiSession session;
	private readonly TestContext context;
	internal PersonalIdWorkflow(UiSession Session, TestContext Context)
	{
		this.session = Session;
		this.context = Context;
	}

	internal static PersonalIdentityData Validate(TestSettings Settings, bool NewApplication = true, bool ManualApproval = false, bool Revocation = false)
	{
		Settings.RequireOptIn("ALLOW_KYC_SUBMISSION");
		if (ManualApproval)
		{
			Settings.RequireOptIn("MANUAL_APPROVAL");
			_ = Settings.ApprovalTimeout;
		}
		if (Revocation)
		{
			Settings.RequireOptIn("REVOKE_IDENTITY");
			Settings.Phone();
			Settings.GetUri("OTP_ENDPOINT", false);
		}
		Assert.IsFalse(Settings.Flag("RESET_BEFORE_TEST"), "KYC requires a prepared account; RESET_BEFORE_TEST must be false.");
		Assert.IsTrue(Settings.Language == "en" && Settings.Get("SYSTEM_LANGUAGE_CODE", "en") == "en"
			&& Settings.Get("SYSTEM_LOCALE", "en-US") == "en-US", "The KYC fixture requires English app text and English US system pickers.");
		Settings.Pin();
		if (NewApplication)
		{
			Settings.Required("KYC_TEMPLATE_ITEM_ID");
			Settings.Required("KYC_TEMPLATE_NAME");
		}
		else
			Assert.IsTrue(Settings.Required("KYC_REVIEW_STAGE") is "pending-review" or "finalizing",
				"KYC_REVIEW_STAGE must be pending-review or finalizing when resuming an application.");
		return PersonalIdentityData.FromSettings(Settings);
	}

	internal void Submit(PersonalIdentityData Identity, bool NewApplication = true)
	{
		new ApplicationsScreen(this.session).OpenFromHome();
		this.SubmitFromApplications(Identity, NewApplication);
	}

	internal void SubmitFromApplications(PersonalIdentityData Identity, bool NewApplication = true)
	{
		ApplicationsScreen Applications = new ApplicationsScreen(this.session);
		if (NewApplication)
			Applications.OpenNew();
		else
			Applications.OpenExisting();
		KycProcessScreen Form = new KycProcessScreen(this.session);
		Form.NavigateToPersonalInformation();
		bool IncludesMiddleName = Form.EnterPersonalInformation(Identity);
		Form.ContinueWithoutDocument();
		Form.EnterAddress(Identity);
		string PhotoKey = Form.RevealPhoto();
		KycCameraScreen Camera = new KycCameraScreen(this.session);
		Camera.Open(PhotoKey);
		Camera.Capture();
		Camera.Retake();
		Camera.Capture();
		Camera.Accept();
		Form.ContinueWithPhoto(PhotoKey);
		Form.ContinueWithoutOrganization();
		new KycSummaryScreen(this.session).AssertMatches(Identity, IncludesMiddleName);
		Form.Submit();
		AccountScreen Account = new AccountScreen(this.session);
		Account.AssertPin();
		Account.EnterPin(this.session.Settings.Pin(), AutomationIds.ControlKycProcessSent);
		Form.AssertSent();
	}

	/// <summary>Waits for an administrator to approve the submitted application, then checks it shows as approved.</summary>
	internal void WaitForApproval(string Instruction, bool ExpectUnlockedDialog = true)
	{
		TimeSpan Timeout = this.session.Settings.ApprovalTimeout;
		this.context.WriteLine(Instruction);
		this.context.WriteLine($"Waiting up to {Timeout.TotalMinutes} minutes for an administrator to approve the application.");
		if (ExpectUnlockedDialog)
			this.session.Ui.Dialog(this.session.Text["Unlocked"], Value => Value == this.session.Text["YouCanNowScanIdCodes"],
				this.session.Text["Ok"], Timeout.TotalSeconds);
		else
			this.session.Ui.Until(() => this.session.Ui.Visible(AutomationIds.ScreenApplicationsCatalog),
				"the approved application to return to Applications", Timeout.TotalSeconds);
		new ApplicationsScreen(this.session).AssertApproved();
	}

	/// <summary>Opens the approved application's identity card from Applications, reads its ID and returns to Applications.</summary>
	internal string ReadApprovedId()
	{
		ApplicationsScreen Applications = new ApplicationsScreen(this.session);
		Applications.OpenApproved();
		IdentityScreen Card = new IdentityScreen(this.session);
		Card.AssertApproved();
		string Id = Card.ReadId();
		Card.Close(AutomationIds.ScreenApplicationsCatalog);
		Applications.AssertDisplayed();
		return Id;
	}
}
