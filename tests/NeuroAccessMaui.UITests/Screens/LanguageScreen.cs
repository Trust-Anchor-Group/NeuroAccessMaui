namespace NeuroAccessMaui.UITests.Screens;

/// <summary>Selects a language by its stable code and verifies representative localized controls.</summary>
internal sealed class LanguageScreen
{
	private readonly UiSession session;
	private MobileUi Ui => this.session.Ui;
	internal LanguageScreen(UiSession Session) => this.session = Session;

	internal void Select(AppTranslations Language)
	{
		this.Ui.Click(AutomationIds.ButtonOnboardingRootLanguage, AutomationIds.ScreenOnboardingRoot);
		this.Ui.Find(AutomationIds.PopupSettingsLanguage);
		string Option = AutomationIds.ForKey(AutomationIds.ItemSettingsLanguageOption, Language.Code);
		this.Ui.Reveal(Option, AutomationIds.PopupSettingsLanguage);
		this.Ui.Click(Option, AutomationIds.PopupSettingsLanguage);
		this.Ui.DismissInformationDialog();
		this.Ui.Hidden(AutomationIds.PopupSettingsLanguage);
		this.session.Text = Language;
	}

	internal void AssertLanguage(AppTranslations Language)
	{
		string Screen = AutomationIds.ScreenOnboardingProvider;
		this.Ui.Find(Screen);
		Assert.AreEqual(Language["ActivateYourDigitalIdentity"], this.Ui.Read(AutomationIds.LabelOnboardingProviderTitle, Screen),
			$"{Language.Code}: provider heading did not use the requested culture.");
		Assert.AreEqual(Language["SelectForMe"], this.Ui.Read(AutomationIds.ButtonOnboardingProviderSelect, Screen),
			$"{Language.Code}: provider action did not use the requested culture.");
		this.Ui.Find(AutomationIds.ButtonOnboardingProviderSelect, Screen, Ready: true);
	}
}
