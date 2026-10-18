using NeuroAccessMaui.UITests.Workflows;

namespace NeuroAccessMaui.UITests.Tests.Onboarding;

/// <summary>Preserves the temporary-phone registration journey.</summary>
[TestClass, TestCategory("Registration")]
public sealed class RegistrationFlowTests : BaseUiTest
{
	/// <summary>Registers a real test account and checks the resulting Home actions.</summary>
	/// <returns>A task representing the registration scenario.</returns>
	[TestMethod]
	public async Task Test_01_CompleteRegistrationAsync()
	{
		RegistrationWorkflow.Validate(new TestSettings(this.TestContext));
		await new RegistrationWorkflow(this.Open()).RegisterAsync();
	}
}
