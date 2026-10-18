using NeuroAccessMaui.UITests.Screens;

namespace NeuroAccessMaui.UITests.Workflows;

/// <summary>Exchanges identities with a peer through QR links and petitions, and checks the saved contact.</summary>
internal sealed class ContactsWorkflow
{
	private readonly UiSession session;
	internal ContactsWorkflow(UiSession Session) => this.session = Session;

	/// <summary>Opens a peer's identity link and returns true when the peer has to accept a petition first.</summary>
	internal bool Request(string Link)
	{
		ScannerScreen Scanner = new ScannerScreen(this.session);
		Scanner.OpenManual();
		Scanner.EnterLink(Link);
		return new ContactsScreen(this.session).AwaitPetitionSent();
	}

	/// <summary>Opens an approved peer's identity link, checks the identity and saves it as a contact.</summary>
	internal void AddScanned(string Link, string ExpectedId)
	{
		Assert.IsFalse(this.Request(Link), "The peer has not accepted the identity petition.");
		IdentityScreen Identity = new IdentityScreen(this.session);
		Identity.AssertApproved();
		Assert.AreEqual(ExpectedId, Identity.ReadId(), "The scanned identity differs from the peer's.");
		new ContactsScreen(this.session).AddContact();
	}

	/// <summary>Accepts the peer's incoming identity petition with the PIN and saves the peer as a contact.</summary>
	internal void AcceptRequest()
	{
		ContactsScreen Contacts = new ContactsScreen(this.session);
		if (!Contacts.OpenPetition())
		{
			Contacts.AcceptPetition();
			new AccountScreen(this.session).EnterPin(this.session.Settings.Pin(), AutomationIds.ScreenPetitionsIdentity);
			Contacts.AwaitPetitionApproved();
		}
		Contacts.AddContact();
	}

	internal void VerifySaved(string Peer)
	{
		new ContactsScreen(this.session).OpenDetails(Peer);
		IdentityScreen Identity = new IdentityScreen(this.session);
		Identity.AssertApproved();
		Assert.AreEqual(Peer, Identity.ReadId(), "The saved contact shows another identity.");
	}
}
