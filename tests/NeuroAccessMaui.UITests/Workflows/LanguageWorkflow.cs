using NeuroAccessMaui.UITests.Screens;

namespace NeuroAccessMaui.UITests.Workflows;

/// <summary>Verifies an actual language transition and persistence across a process restart.</summary>
internal sealed class LanguageWorkflow
{
	private readonly UiSession session;

	internal LanguageWorkflow(UiSession Session) => this.session = Session;

	internal void VerifySwitchAndPersistence(AppTranslations Target)
	{
		LanguageScreen Screen = new LanguageScreen(this.session);
		AppTranslations Anchor = new AppTranslations(Target.Code == "en" ? "sv" : "en");
		Screen.Select(Anchor);
		Screen.AssertLanguage(Anchor);
		Screen.Select(Target);
		Screen.AssertLanguage(Target);
		this.session.ColdStart();
		Screen.AssertLanguage(Target);
	}
}
