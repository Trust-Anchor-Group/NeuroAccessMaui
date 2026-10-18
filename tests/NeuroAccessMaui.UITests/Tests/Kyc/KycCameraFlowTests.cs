using NeuroAccessMaui.UITests.Screens.Kyc;

namespace NeuroAccessMaui.UITests.Tests.Kyc;

/// <summary>Exercises the KYC photo capture page against a prepared draft that has no photo yet.</summary>
[TestClass, TestCategory("KycCamera")]
public sealed class KycCameraFlowTests : BaseUiTest
{
	/// <summary>Checks cancel, retake and accept. Automation builds capture a fixed photo; normal builds use the device camera.</summary>
	[TestMethod]
	public void Test_01_CaptureReviewAndAccept()
	{
		TestSettings Settings = new TestSettings(this.TestContext);
		Settings.RequireOptIn("ALLOW_KYC_DRAFT_UPDATE");
		string FieldId = Settings.Get("KYC_PHOTO_FIELD_ID", "selfieUpload");
		UiSession Session = this.Open();
		MobileUi Ui = Session.Ui;
		new ApplicationsScreen(Session).OpenFromHome();
		Ui.Label(AutomationIds.LabelApplicationsCatalogCurrentstate, Session.Text["InProgress"]);
		Ui.Click(AutomationIds.ButtonApplicationsCatalogCurrent);
		Ui.Find(AutomationIds.ScreenKycProcess);
		string Card = AutomationIds.ForKey(AutomationIds.ButtonKycImageCapture, FieldId);
		string Photo = AutomationIds.ForKey(AutomationIds.ImageKycFieldPreview, FieldId);
		Ui.Find(Card);
		Assert.IsFalse(Ui.Visible(Photo), "Prepare a dedicated draft that resumes at its selfie step with no accepted photo.");

		KycCameraScreen Camera = new KycCameraScreen(Session);
		Camera.Open(FieldId);
		Camera.Cancel();
		Ui.Find(Card);
		Assert.IsFalse(Ui.Visible(Photo), "Cancelling before capture must leave the photo empty.");

		Camera.Open(FieldId);
		Camera.Capture();
		Camera.Cancel();
		Ui.Find(Card);
		Assert.IsFalse(Ui.Visible(Photo), "Cancelling review must not attach the unaccepted photo.");

		Camera.Open(FieldId);
		Camera.Capture();
		Camera.Retake();
		Camera.Capture();
		Camera.Accept();
		Ui.Image(Photo);
		Ui.Find(Card);
	}
}
