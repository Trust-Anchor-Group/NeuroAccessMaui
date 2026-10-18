namespace NeuroAccessMaui.UITests.Screens;

/// <summary>Opens production QR links using the scanner's manual-entry controls.</summary>
internal sealed class ScannerScreen
{
	private readonly UiSession session;
	private MobileUi Ui => this.session.Ui;
	internal ScannerScreen(UiSession Session) => this.session = Session;

	internal void OpenManual()
	{
		this.Ui.Find(AutomationIds.ScreenMainHome);
		this.Ui.Click(AutomationIds.ButtonMainHomeScanqr, AutomationIds.ScreenMainHome);
		this.Ui.Find(AutomationIds.ScreenMainQrscan);
		this.Ui.Click(AutomationIds.ButtonMainQrscanManualentry, AutomationIds.ScreenMainQrscan);
		this.Ui.Find(AutomationIds.ControlMainQrscanManual);
		this.Ui.Find(AutomationIds.LabelMainQrscanPayload, AutomationIds.ScreenMainQrscan);
		this.Ui.Input(AutomationIds.InputMainQrscanPayload, AutomationIds.ScreenMainQrscan);
	}

	internal void EnterLink(string Link)
	{
		this.Ui.Enter(AutomationIds.InputMainQrscanPayload, Link, AutomationIds.ScreenMainQrscan);
		this.Ui.Click(AutomationIds.ButtonMainQrscanOpen, AutomationIds.ScreenMainQrscan);
	}
}
