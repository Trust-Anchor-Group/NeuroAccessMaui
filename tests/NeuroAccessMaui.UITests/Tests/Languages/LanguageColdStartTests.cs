using NeuroAccessMaui.UITests.Workflows;

namespace NeuroAccessMaui.UITests.Tests.Languages;

/// <summary>Checks language switching and persistence across a process restart for every supported locale.</summary>
[TestClass, TestCategory("Languages")]
public sealed class LanguageColdStartTests : BaseUiTest
{
	/// <summary>Verifies that a language choice survives application termination and activation.</summary>
	/// <param name="Code">The independently asserted locale code.</param>
	[TestMethod]
	[DataRow("en"), DataRow("sv"), DataRow("es"), DataRow("fr"), DataRow("de"), DataRow("da")]
	[DataRow("no"), DataRow("fi"), DataRow("sr"), DataRow("pt"), DataRow("ro"), DataRow("ru")]
	public void Test_01_PersistLanguageAfterColdStart(string Code)
	{
		UiSession Session = this.Open();
		Session.ClearApp();
		new LanguageWorkflow(Session).VerifySwitchAndPersistence(new AppTranslations(Code));
	}
}
