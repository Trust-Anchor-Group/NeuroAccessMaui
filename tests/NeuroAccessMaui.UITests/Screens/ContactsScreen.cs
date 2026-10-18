using System.Text.RegularExpressions;

namespace NeuroAccessMaui.UITests.Screens;

/// <summary>Drives the contact list, chats, identity petitions and the add-contact action on a peer's identity.</summary>
internal sealed class ContactsScreen
{
	private readonly UiSession session;
	private MobileUi Ui => this.session.Ui;
	private AppTranslations Text => this.session.Text;
	internal ContactsScreen(UiSession Session) => this.session = Session;

	internal void OpenList()
	{
		this.Ui.Find(AutomationIds.ScreenMainHome);
		this.Ui.Text(this.Text["Apps"]).Click();
		this.Ui.Find(AutomationIds.ScreenMainApps);
		this.Ui.Click(AutomationIds.ButtonMainAppsContacts, AutomationIds.ScreenMainApps);
		this.Ui.Find(AutomationIds.ScreenContactsList);
	}

	internal void OpenDetails(string Peer)
	{
		this.OpenList();
		string Row = AutomationIds.ForKey(AutomationIds.ItemContactsListContact, Peer);
		this.Ui.Reveal(Row, AutomationIds.ControlContactsListScroll, 30);
		this.Ui.Click(AutomationIds.ForKey(AutomationIds.ButtonContactsListDetails, Peer), Row);
	}

	internal void OpenChat(string Peer)
	{
		this.OpenList();
		string Row = AutomationIds.ForKey(AutomationIds.ItemContactsListContact, Peer);
		this.Ui.Reveal(Row, AutomationIds.ControlContactsListScroll, 30);
		this.Ui.Click(AutomationIds.ForKey(AutomationIds.ButtonContactsListChat, Peer), Row);
		this.AssertChat(Peer);
	}

	internal void AssertChat(string Peer)
	{
		this.Ui.Find(AutomationIds.ScreenContactsChat);
		this.Ui.Find(AutomationIds.ForKey(AutomationIds.LabelContactsChatPeer, Peer), AutomationIds.ScreenContactsChat);
	}

	internal void Send(string Message, string Peer)
	{
		this.AssertChat(Peer);
		this.Ui.Enter(AutomationIds.InputContactsChatMessage, Message, AutomationIds.ScreenContactsChat);
		this.Ui.Click(AutomationIds.ButtonContactsChatSend, AutomationIds.ScreenContactsChat);
		this.AwaitMessage(Message, Peer, true);
	}

	internal void AwaitMessage(string Message, string Peer, bool Sent = false)
	{
		this.AssertChat(Peer);
		By Bubbles = this.session.Platform.LocatorStartingWith(
			(Sent ? AutomationIds.ItemContactsChatSent : AutomationIds.ItemContactsChatReceived) + "_key_", this.session.Settings.AppId);
		this.Ui.Until(() =>
		{
			IWebElement Scroll = this.Ui.Find(AutomationIds.ControlContactsChatScroll, AutomationIds.ScreenContactsChat);
			int Matches = Scroll.FindElements(Bubbles).Sum(Bubble => this.Ui.TextElements(Value => Value.Trim() == Message, Bubble).Count);
			Assert.IsTrue(Matches <= 1, $"'{Message}' appears {Matches} times in the chat.");
			return Matches == 1;
		}, Sent ? $"the sent message '{Message}'" : $"the received message '{Message}'", Sent ? 30 : 120);
	}

	/// <summary>Waits for the result of opening a peer's identity link and returns true when a petition was sent instead of the identity opening.</summary>
	internal bool AwaitPetitionSent()
	{
		string Result = this.Ui.Wait(() => this.Ui.TextElements(Value => Value == this.Text["PetitionSent"]).Count > 0
			? "sent" : this.Ui.Visible(AutomationIds.ScreenIdentityCard) ? "approved" : null, "the petition to be sent or the identity to open", 120);
		if (Result == "sent")
			this.Ui.Text(this.Text["Ok"]).Click();
		return Result == "sent";
	}

	/// <summary>Opens an incoming identity petition, which the app usually opens by itself, and returns whether it is already approved.</summary>
	internal bool OpenPetition()
	{
		string Petition = AutomationIds.ScreenPetitionsIdentity;
		try { this.Ui.Until(() => this.Ui.Visible(Petition), "the identity petition to open", 120); }
		catch (AssertFailedException)
		{
			this.Ui.Click(AutomationIds.ButtonMainHomeNotifications, AutomationIds.ScreenMainHome);
			this.Ui.Find(AutomationIds.ScreenMainNotifications);
			string Pattern = "\\A" + Regex.Escape(this.Text["NotificationPetitionIdentityTitle"]).Replace(Regex.Escape("{0}"), ".+") + "\\z";
			IWebElement Request = this.Ui.Wait(() => this.Ui.TextElements(Value => Regex.IsMatch(Value, Pattern),
				this.Ui.Find(AutomationIds.ScreenMainNotifications)).FirstOrDefault(), "the identity petition notification");
			Request.Click();
			this.Ui.Find(Petition);
		}
		return this.Ui.Read(AutomationIds.LabelPetitionsIdentityState, Petition) == this.Text["IdentityState_Approved"];
	}

	/// <summary>Taps Accept on the open petition; the app then asks for the PIN.</summary>
	internal void AcceptPetition()
	{
		this.Ui.Reveal(AutomationIds.ButtonPetitionsIdentityAccept, AutomationIds.ScreenPetitionsIdentity, 15);
		this.Ui.Click(AutomationIds.ButtonPetitionsIdentityAccept, AutomationIds.ScreenPetitionsIdentity);
	}

	internal void AwaitPetitionApproved() =>
		this.Ui.Until(() => this.Ui.Read(AutomationIds.LabelPetitionsIdentityState, AutomationIds.ScreenPetitionsIdentity)
			== this.Text["IdentityState_Approved"], "the petition identity to be approved");

	/// <summary>Saves the peer shown on the open identity or petition screen as a contact, unless it already is one.</summary>
	internal void AddContact()
	{
		for (int i = 0; i < 15; i++)
		{
			if (this.Ui.TextElements(Value => Value == this.Text["RemoveContact"]).Count > 0)
				return;
			if (this.Ui.TextElements(Value => Value == this.Text["AddContact"]).Count > 0)
			{
				this.Ui.Text(this.Text["AddContact"]).Click();
				this.Ui.Text(this.Text["RemoveContact"]);
				Assert.IsTrue(this.Ui.TextElements(Value => Value == this.Text["AddContact"]).Count == 0, "Add Contact remains visible after saving.");
				return;
			}
			this.Ui.Scroll();
		}
		Assert.Fail("Contact actions could not be reached by scrolling.");
	}
}
