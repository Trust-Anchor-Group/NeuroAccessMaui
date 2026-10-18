using NeuroAccessMaui.UITests.Screens;
using NeuroAccessMaui.UITests.Services;

namespace NeuroAccessMaui.UITests.Tests.QuickLogin;

/// <summary>Compares the real mobile identity with the real Quick Login browser response.</summary>
[TestClass, TestCategory("QuickLogin")]
public sealed class QuickLoginFlowTests : BaseUiTest
{
	/// <summary>Signs in through manual QR entry and checks every required identity-table field.</summary>
	[TestMethod]
	public void Test_01_CompareMobileAndWebIdentity()
	{
		TestSettings Settings = new TestSettings(this.TestContext);
		string Pin = Settings.Pin();
		using QuickLoginBrowser Browser = new QuickLoginBrowser(Settings);
		UiSession Session = this.Open();
		AccountScreen Account = new AccountScreen(Session);
		Account.OpenIdentity();
		IdentityScreen Identity = new IdentityScreen(Session);
		Identity.AssertApproved();
		string Id = Identity.ReadId();
		Identity.Close(AutomationIds.ScreenMainHome);
		ScannerScreen Scanner = new ScannerScreen(Session);
		Scanner.OpenManual();
		Browser.Open();
		string Link = Browser.SignUrl();
		string Purpose = Browser.Purpose();
		Scanner.EnterLink(Link);
		string Petition = AutomationIds.ScreenPetitionsSignature;
		Session.Ui.Find(Petition);
		Session.Ui.Find(AutomationIds.LabelPetitionsSignatureTitle, Petition);
		Session.Ui.Until(() => Session.Ui.Read(AutomationIds.LabelPetitionsSignaturePurpose, Petition) == Purpose, $"the petition purpose '{Purpose}'");
		Session.Ui.Click(AutomationIds.ButtonPetitionsSignatureAccept, Petition);
		Account.AssertPin();
		Account.EnterPin(Pin, AutomationIds.ScreenMainHome);
		Browser.AssertApprovedIdentity(Id);
	}
}
