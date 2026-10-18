using NeuroAccessMaui.UITests.Models;
using NeuroAccessMaui.UITests.Workflows;

namespace NeuroAccessMaui.UITests.Tests.Kyc;

/// <summary>Checks real revocation, phone reverification and approval of a different replacement identity.</summary>
[TestClass, TestCategory("Kyc"), TestCategory("KycReplacement")]
public sealed class PersonalIdReplacementFlowTests : BaseUiTest
{
	/// <summary>Checks real revocation, phone reverification and approval of a different replacement identity.</summary>
	/// <returns>A task representing the complete KYC scenario.</returns>
	[TestMethod]
	public async Task Test_01_RevokeAndApproveReplacementAsync()
	{
		TestSettings Settings = new TestSettings(this.TestContext);
		PersonalIdentityData Identity = PersonalIdWorkflow.Validate(Settings, ManualApproval: true, Revocation: true);
		UiSession Session = this.Open();
		PersonalIdWorkflow Workflow = new PersonalIdWorkflow(Session, this.TestContext);
		Workflow.Submit(Identity);
		Workflow.WaitForApproval("First application submitted. Approve it in admin.");
		string OriginalId = Workflow.ReadApprovedId();
		this.TestContext.WriteLine($"Identity {OriginalId} approved. Revoking it and waiting for the Obsoleted state.");
		await new RevocationWorkflow(Session).RevokeAndReverifyAsync(OriginalId);
		Workflow.Submit(Identity);
		Workflow.WaitForApproval("Replacement application submitted. Approve this second application in admin.");
		Assert.AreNotEqual(OriginalId, Workflow.ReadApprovedId(), "The approved replacement must differ from the revoked identity.");
	}
}
