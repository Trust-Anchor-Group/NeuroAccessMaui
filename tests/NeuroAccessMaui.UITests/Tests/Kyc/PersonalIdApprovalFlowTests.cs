using NeuroAccessMaui.UITests.Models;
using NeuroAccessMaui.UITests.Workflows;

namespace NeuroAccessMaui.UITests.Tests.Kyc;

/// <summary>Submits an identity and waits for real manual approval and the exact Unlocked notification.</summary>
[TestClass, TestCategory("Kyc"), TestCategory("KycApproval")]
public sealed class PersonalIdApprovalFlowTests : BaseUiTest
{
	/// <summary>Submits an identity and waits for real manual approval and the exact Unlocked notification.</summary>
	[TestMethod]
	public void Test_01_SubmitAndWaitForManualApproval()
	{
		TestSettings Settings = new TestSettings(this.TestContext);
		bool NewApplication = Settings.Flag("KYC_NEW_APPLICATION", true);
		PersonalIdentityData Identity = PersonalIdWorkflow.Validate(Settings, NewApplication, ManualApproval: true);
		PersonalIdWorkflow Workflow = new PersonalIdWorkflow(this.Open(), this.TestContext);
		Workflow.Submit(Identity, NewApplication);
		Workflow.WaitForApproval("Application submitted. Approve it in admin.");
	}
}
