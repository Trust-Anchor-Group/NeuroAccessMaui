namespace NeuroAccessMaui.UITests.Screens.Kyc;

/// <summary>Drives the KYC photo capture and review page; automation builds use a fixed photo instead of the camera.</summary>
internal sealed class KycCameraScreen
{
	private readonly MobileUi ui;

	internal KycCameraScreen(UiSession Session) => this.ui = Session.Ui;

	internal void Open(string FieldId)
	{
		this.ui.Click(AutomationIds.ForKey(AutomationIds.ButtonKycImageCapture, FieldId));
		this.AssertPreview();
	}

	internal void AssertPreview()
	{
		this.ui.Find(AutomationIds.ScreenKycCamera);
		this.ui.Find(AutomationIds.ButtonKycCameraCapture, Ready: true);
		Assert.IsFalse(this.ui.Visible(AutomationIds.ButtonKycCameraAccept), "A photo cannot be accepted before capture.");
		Assert.IsFalse(this.ui.Visible(AutomationIds.ImageKycCameraReview), "Review must be hidden before capture.");
	}

	internal void Capture()
	{
		this.ui.Click(AutomationIds.ButtonKycCameraCapture);
		this.ui.Image(AutomationIds.ImageKycCameraReview);
		this.ui.Find(AutomationIds.ButtonKycCameraAccept, Ready: true);
		this.ui.Find(AutomationIds.ButtonKycCameraRetake, Ready: true);
		Assert.IsFalse(this.ui.Visible(AutomationIds.ButtonKycCameraCapture), "Capture controls must be hidden during review.");
	}

	internal void Retake()
	{
		this.ui.Click(AutomationIds.ButtonKycCameraRetake);
		this.AssertPreview();
	}

	internal void Accept()
	{
		this.ui.Click(AutomationIds.ButtonKycCameraAccept);
		this.ui.Hidden(AutomationIds.ScreenKycCamera);
	}

	internal void Cancel()
	{
		this.ui.Click(AutomationIds.ButtonKycCameraCancel);
		this.ui.Hidden(AutomationIds.ScreenKycCamera);
	}
}
