namespace NeuroAccessMaui.UITests.Screens.Kyc;

/// <summary>Drives the Applications page: opens a template or the current application and checks its state.</summary>
internal sealed class ApplicationsScreen
{
	private readonly UiSession session;
	private MobileUi Ui => this.session.Ui;
	internal ApplicationsScreen(UiSession Session) => this.session = Session;

	internal void AssertDisplayed()
	{
		this.Ui.Find(AutomationIds.ScreenApplicationsCatalog);
		this.Ui.Find(AutomationIds.LabelApplicationsCatalogTitle, AutomationIds.ScreenApplicationsCatalog);
	}

	internal void OpenFromHome()
	{
		this.Ui.Find(AutomationIds.ScreenMainHome);
		this.Ui.Click(AutomationIds.ButtonMainHomeApplyidentity, AutomationIds.ScreenMainHome);
		this.AssertDisplayed();
	}

	internal void OpenNew()
	{
		this.AssertDisplayed();
		string Key = this.session.Settings.Required("KYC_TEMPLATE_ITEM_ID");
		string Item = AutomationIds.ForKey(AutomationIds.ItemApplicationsCatalogTemplate, Key);
		this.Ui.Reveal(Item, AutomationIds.ControlApplicationsCatalogScroll);
		this.Ui.Label(AutomationIds.ForKey(AutomationIds.LabelApplicationsCatalogTemplatename, Key),
			this.session.Settings.Required("KYC_TEMPLATE_NAME"), Item);
		this.Ui.Label(AutomationIds.ForKey(AutomationIds.LabelApplicationsCatalogTemplatetype, Key),
			this.session.Text["IdentityApplication"], Item);
		this.Ui.Click(Item, AutomationIds.ScreenApplicationsCatalog);
		// A document/NFC template must fail here; never bypass its evidence requirement.
		this.Ui.Find(AutomationIds.ScreenKycProcess);
	}

	internal void OpenExisting()
	{
		this.AssertDisplayed();
		string State = this.Ui.Read(AutomationIds.LabelApplicationsCatalogCurrentstate, AutomationIds.ScreenApplicationsCatalog);
		bool Review = State == this.session.Text["InReview"];
		Assert.IsTrue(Review || State == this.session.Text["InProgress"], $"Resuming requires a draft or pending-review application, but the current one is '{State}'.");
		this.Ui.Click(AutomationIds.ButtonApplicationsCatalogCurrent, AutomationIds.ScreenApplicationsCatalog);
		if (Review)
		{
			this.Ui.Find(AutomationIds.ScreenKycStatus);
			string Title = this.session.Settings.Required("KYC_REVIEW_STAGE") == "finalizing"
				? this.session.Text["IdentityApplication"] : this.session.Text["IdentityReviewRequest"];
			this.Ui.Label(AutomationIds.LabelKycStatusTitle, Title, AutomationIds.ScreenKycStatus);
			IWebElement Screen = this.Ui.Find(AutomationIds.ScreenKycStatus);
			int HeadingCount = this.Ui.TextElements(Value => Value == this.session.Text["IdentityApplication"], Screen).Count;
			Assert.AreEqual(Title == this.session.Text["IdentityApplication"] ? 2 : 1, HeadingCount, "Application status heading differs.");
			this.Ui.Action(AutomationIds.ButtonKycStatusApplications, this.session.Text["Applications"], true, AutomationIds.ScreenKycStatus);
			this.Ui.Action(AutomationIds.ButtonKycStatusOpen, this.session.Text["Open"], true, AutomationIds.ScreenKycStatus);
			this.Ui.Click(AutomationIds.ButtonKycStatusOpen, AutomationIds.ScreenKycStatus);
		}
		this.Ui.Find(AutomationIds.ScreenKycProcess);
	}

	internal void AssertApproved()
	{
		this.AssertDisplayed();
		this.Ui.Until(() => this.Ui.Read(AutomationIds.LabelApplicationsCatalogCurrentstate, AutomationIds.ScreenApplicationsCatalog)
			== this.session.Text["IdentityState_Approved"], "the current application to be approved");
		this.Ui.Find(AutomationIds.ButtonApplicationsCatalogCurrent, AutomationIds.ScreenApplicationsCatalog, Ready: true);
	}

	internal void OpenApproved()
	{
		this.AssertApproved();
		this.Ui.Click(AutomationIds.ButtonApplicationsCatalogCurrent, AutomationIds.ScreenApplicationsCatalog);
		this.Ui.Find(AutomationIds.ScreenIdentityCard);
	}

	internal void ReturnHome()
	{
		this.AssertDisplayed();
		this.Ui.Click(AutomationIds.ButtonApplicationsCatalogBack, AutomationIds.ScreenApplicationsCatalog);
		this.Ui.Hidden(AutomationIds.ScreenApplicationsCatalog);
		this.Ui.Find(AutomationIds.ScreenMainHome);
	}
}
