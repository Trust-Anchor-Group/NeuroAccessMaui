using NeuroAccessMaui.UITests.Workflows;

namespace NeuroAccessMaui.UITests.Tests.Security;

/// <summary>Checks PIN changes and authentication across a process restart on an approved test identity.</summary>
[TestClass, TestCategory("Pin")]
public sealed class PinColdStartTests : BaseUiTest
{
	/// <summary>Changes the PIN, restarts the application, and verifies rejection and acceptance.</summary>
	[TestMethod]
	public void Test_01_ChangePinAndVerifyAfterColdStart()
	{
		new TestSettings(this.TestContext).RequireOptIn("ALLOW_PIN_CHANGE");
		UiSession Session = this.Open();
		PinWorkflow Pin = new PinWorkflow(Session);
		Pin.Change();
		Session.ColdStart();
		Pin.VerifyChanged();
	}
}
