using NeuroAccessMaui.UITests.Screens;
using NeuroAccessMaui.UITests.Workflows;

namespace NeuroAccessMaui.UITests.Tests.Contacts;

/// <summary>Coordinates contact creation and messaging across two independently configured devices.</summary>
[TestClass, TestCategory("Contacts")]
public sealed class MutualContactsFlowTests : BaseUiTest
{
	/// <summary>Copies both real identity links, accepts petitions, verifies saved peers and exchanges unique messages.</summary>
	[TestMethod]
	public void Test_01_AddContactsAndExchangeMessages()
	{
		TestSettings SettingsA = new TestSettings(this.TestContext, "A");
		TestSettings SettingsB = new TestSettings(this.TestContext, "B");
		SettingsA.RequireOptIn("ALLOW_CONTACTS");
		Assert.IsTrue(SettingsA.Server != SettingsB.Server || SettingsA.DeviceId != SettingsB.DeviceId, "Contacts requires two distinct device targets.");
		Assert.IsTrue(SettingsA.Language == "en" && SettingsB.Language == "en", "The contacts scenario requires English app and system UI.");
		UiSession A = this.Open("A");
		UiSession B = this.Open("B");
		string IdA = ReadIdentity(A);
		string IdB = ReadIdentity(B);
		Assert.AreNotEqual(IdA, IdB, "Both devices use the same identity.");
		ContactsWorkflow ContactsA = new ContactsWorkflow(A);
		ContactsWorkflow ContactsB = new ContactsWorkflow(B);

		string LinkA = ExportIdentity(A);
		A.ColdStart();
		B.ColdStart();
		if (ContactsB.Request(LinkA))
			ContactsA.AcceptRequest();
		B.ColdStart();
		ContactsB.AddScanned(LinkA, IdA);

		string LinkB = ExportIdentity(B);
		A.ColdStart();
		B.ColdStart();
		if (ContactsA.Request(LinkB))
			ContactsB.AcceptRequest();
		A.ColdStart();
		ContactsA.AddScanned(LinkB, IdB);

		A.ColdStart();
		B.ColdStart();
		ContactsA.VerifySaved(IdB);
		ContactsB.VerifySaved(IdA);

		A.ColdStart();
		B.ColdStart();
		ContactsScreen ChatA = new ContactsScreen(A);
		ContactsScreen ChatB = new ContactsScreen(B);
		ChatA.OpenChat(IdB);
		ChatB.OpenChat(IdA);
		string RunId = Guid.NewGuid().ToString("N");
		string FromA = "NeuroAccess " + RunId + " A to B";
		string FromB = "NeuroAccess " + RunId + " B to A";
		ChatA.Send(FromA, IdB);
		ChatB.AwaitMessage(FromA, IdA);
		ChatB.Send(FromB, IdA);
		// Both sessions stay open until the initiator has received the reply.
		ChatA.AwaitMessage(FromB, IdB);
	}

	private static string ReadIdentity(UiSession Session)
	{
		new AccountScreen(Session).OpenIdentity();
		IdentityScreen Identity = new IdentityScreen(Session);
		Identity.AssertApproved();
		return Identity.ReadId();
	}

	private static string ExportIdentity(UiSession Session)
	{
		Session.ColdStart();
		new AccountScreen(Session).OpenIdentity();
		return new IdentityScreen(Session).CopyLink();
	}
}
