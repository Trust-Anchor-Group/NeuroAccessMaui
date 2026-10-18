using NeuroAccessMaui.UITests.Models;
using NeuroAccessMaui.UITests.Workflows;
using NeuroAccessMaui.UITests.Screens;
using NeuroAccessMaui.UITests.Screens.Kyc;

namespace NeuroAccessMaui.UITests.Tests.Kyc;

/// <summary>Approves Oscar, then Albin, and checks that Home shows the newly approved identity.</summary>
[TestClass, TestCategory("Kyc"), TestCategory("KycUpdate")]
public sealed class PersonalIdUpdateFlowTests : BaseUiTest
{
	/// <summary>Approves Oscar, then Albin, and checks that Home shows the newly approved identity.</summary>
	[TestMethod]
	public void Test_01_ApproveOscarThenAlbin()
	{
		TestSettings Settings = new TestSettings(this.TestContext);
		PersonalIdentityData Oscar = PersonalIdWorkflow.Validate(Settings, ManualApproval: true) with { FirstName = "Oscar" };
		PersonalIdentityData Albin = Oscar with { FirstName = "Albin" };
		UiSession Session = this.Open();
		PersonalIdWorkflow Workflow = new PersonalIdWorkflow(Session, this.TestContext);
		ApplicationsScreen Applications = new ApplicationsScreen(Session);
		IdentityScreen Card = new IdentityScreen(Session);
		Workflow.Submit(Oscar);
		Workflow.WaitForApproval("Approve the first application for Oscar in admin.");
		string OriginalId = Workflow.ReadApprovedId();
		Applications.OpenApproved();
		Card.AssertIdentity(Oscar.FirstName, OriginalId);
		Card.Close(AutomationIds.ScreenApplicationsCatalog);
		Workflow.SubmitFromApplications(Albin);
		Workflow.WaitForApproval("Approve the second application for Albin in admin.", ExpectUnlockedDialog: false);
		string UpdatedId = Workflow.ReadApprovedId();
		Assert.AreNotEqual(OriginalId, UpdatedId, "The approved second application must create a different identity ID.");
		Applications.ReturnHome();
		new AccountScreen(Session).OpenIdentity();
		Card.AssertIdentity(Albin.FirstName, UpdatedId);
		Card.Close(AutomationIds.ScreenMainHome);
	}
}
