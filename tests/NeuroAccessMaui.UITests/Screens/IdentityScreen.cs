using System.Text.RegularExpressions;

namespace NeuroAccessMaui.UITests.Screens;

/// <summary>Checks the production identity card, including its expandable details and QR link.</summary>
internal sealed class IdentityScreen
{
	private readonly UiSession session;
	private MobileUi Ui => this.session.Ui;
	internal IdentityScreen(UiSession Session) => this.session = Session;

	internal void AssertApproved()
	{
		this.Ui.Find(AutomationIds.ScreenIdentityCard);
		this.Ui.Label(AutomationIds.LabelIdentityCardState, this.session.Text["IdentityState_Approved"], AutomationIds.ScreenIdentityCard);
	}

	internal string ReadId()
	{
		if (!this.Ui.Visible(AutomationIds.ControlIdentityCardScroll))
		{
			this.Ui.Find(AutomationIds.LabelIdentityCardDetails, AutomationIds.ScreenIdentityCard);
			IWebElement Header = this.Ui.Find(AutomationIds.LabelIdentityCardDetails);
			int PreviousTop = Header.Location.Y;
			int Height = Header.Size.Height;
			Header.Click();
			try
			{
				this.Ui.Until(() => this.Ui.Visible(AutomationIds.ControlIdentityCardScroll)
					&& this.Ui.Find(AutomationIds.LabelIdentityCardDetails).Location.Y <= PreviousTop - Height,
					"expanded identity details", 2);
			}
			catch (AssertFailedException)
			{
				Header = this.Ui.Find(AutomationIds.LabelIdentityCardDetails);
				this.Ui.Drag(Header.Location.X + Header.Size.Width / 2, Header.Location.Y + Header.Size.Height / 2,
					Header.Location.X + Header.Size.Width / 2, this.session.Driver.Manage().Window.Size.Height / 5);
				this.Ui.Until(() => this.Ui.Visible(AutomationIds.ControlIdentityCardScroll)
					&& this.Ui.Find(AutomationIds.LabelIdentityCardDetails).Location.Y <= PreviousTop - Height,
					"expanded identity details");
			}
		}
		string Name = AutomationIds.ForKey(AutomationIds.LabelIdentityCardFieldname, "IDENTITY_ID");
		string Value = AutomationIds.ForKey(AutomationIds.LabelIdentityCardFieldvalue, "IDENTITY_ID");
		this.Ui.Reveal(Value, AutomationIds.ControlIdentityCardScroll);
		this.Ui.Find(Name, AutomationIds.ScreenIdentityCard);
		string IdentityId = this.Ui.Read(Value, AutomationIds.ScreenIdentityCard).Trim();
		Assert.IsTrue(Regex.IsMatch(IdentityId, @"^[^\s@]+@[^\s@]+$"), $"'{IdentityId}' is not a valid identity ID.");
		return IdentityId;
	}

	internal void AssertIdentity(string FirstName, string ExpectedId)
	{
		this.AssertApproved();
		this.Ui.LabelMatches(AutomationIds.LabelIdentityCardName,
			Value => Value == FirstName || Value.StartsWith(FirstName + " ", StringComparison.Ordinal), $"a name starting with '{FirstName}'", AutomationIds.ScreenIdentityCard);
		Assert.AreEqual(ExpectedId, this.ReadId(), "The identity card shows another identity.");
	}

	internal void Close(string Destination)
	{
		this.Ui.Click(AutomationIds.ButtonIdentityCardBack, AutomationIds.ScreenIdentityCard);
		this.Ui.Hidden(AutomationIds.ScreenIdentityCard);
		this.Ui.Find(Destination);
	}

	internal string CopyLink()
	{
		this.AssertApproved();
		this.session.SetClipboard("");
		this.Ui.Image(AutomationIds.ImageIdentityCardQr, AutomationIds.ScreenIdentityCard);
		this.Ui.Click(AutomationIds.ImageIdentityCardQr, AutomationIds.ScreenIdentityCard);
		this.Ui.Text(this.session.Text["IdCopiedSuccessfully"]);
		string Link = this.session.GetClipboard();
		Assert.IsTrue(Uri.TryCreate(Link, UriKind.Absolute, out _) && !Link.Any(char.IsWhiteSpace), $"Copied identity link '{Link}' is not a valid URI.");
		this.Ui.Text(this.session.Text["Ok"]).Click();
		this.Ui.Until(() => this.Ui.TextElements(Value => Value == this.session.Text["IdCopiedSuccessfully"]).Count == 0, "clipboard dialog dismissal");
		return Link;
	}
}
