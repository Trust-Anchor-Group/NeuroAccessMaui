using NeuroAccessMaui.UITests.Models;
using NeuroAccessMaui.UITests.Workflows;

namespace NeuroAccessMaui.UITests.Tests.Suites;

/// <summary>Runs the whole app journey in order on one device, from a clean install to a changed PIN.</summary>
[TestClass, TestCategory("FullSuite")]
public sealed class FullSuiteTests : BaseUiTest
{
	/// <summary>Checks every language, registers, submits an identity application and changes the PIN, restarting the app between steps.</summary>
	/// <returns>A task representing the complete ordered run.</returns>
	[TestMethod]
	public async Task Test_01_RunSuiteAsync()
	{
		TestSettings Settings = new TestSettings(this.TestContext);
		RegistrationWorkflow.Validate(Settings);
		PersonalIdentityData Identity = PersonalIdWorkflow.Validate(Settings);
		Settings.RequireOptIn("ALLOW_PIN_CHANGE");
		UiSession Session = this.Open();
		foreach (string Code in AppTranslations.SupportedCodes)
		{
			Session.ClearApp();
			new LanguageWorkflow(Session).VerifySwitchAndPersistence(new AppTranslations(Code));
		}
		Session.ClearApp();
		Session.Text = new AppTranslations(Settings.Language);
		await new RegistrationWorkflow(Session).RegisterAsync();
		Session.ColdStart();
		new PersonalIdWorkflow(Session, this.TestContext).Submit(Identity);
		PinWorkflow Pin = new PinWorkflow(Session);
		Session.ColdStart();
		Pin.Change();
		Session.ColdStart();
		Pin.VerifyChanged();
	}
}
