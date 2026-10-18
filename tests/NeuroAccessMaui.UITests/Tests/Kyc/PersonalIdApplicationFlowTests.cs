using NeuroAccessMaui.UITests.Models;
using NeuroAccessMaui.UITests.Workflows;

namespace NeuroAccessMaui.UITests.Tests.Kyc;

/// <summary>Submits a personal identity application.</summary>
[TestClass, TestCategory("Kyc"), TestCategory("KycApplication")]
public sealed class PersonalIdApplicationFlowTests : BaseUiTest
{
	/// <summary>Fills in and submits a personal identity application, new or resumed.</summary>
	[TestMethod]
	public void Test_01_ApplyForPersonalId()
	{
		TestSettings Settings = new TestSettings(this.TestContext);
		bool NewApplication = Settings.Flag("KYC_NEW_APPLICATION", true);
		PersonalIdentityData Identity = PersonalIdWorkflow.Validate(Settings, NewApplication);
		new PersonalIdWorkflow(this.Open(), this.TestContext).Submit(Identity, NewApplication);
	}
}
