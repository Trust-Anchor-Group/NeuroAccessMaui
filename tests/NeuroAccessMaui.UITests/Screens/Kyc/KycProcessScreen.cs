using System.Globalization;
using NeuroAccessMaui.UITests.Models;

namespace NeuroAccessMaui.UITests.Screens.Kyc;

/// <summary>Fills the photo-based KYC schema using real inputs and navigation commands.</summary>
internal sealed class KycProcessScreen
{
	private readonly UiSession session;
	private MobileUi Ui => this.session.Ui;
	internal KycProcessScreen(UiSession Session) => this.session = Session;

	internal static string Page(string Key) => AutomationIds.ForKey(AutomationIds.ControlKycProcessPage, Key);
	private static string Field(string Key) => AutomationIds.ForKey(AutomationIds.InputKycProcessField, Key);

	internal void NavigateToPersonalInformation()
	{
		this.Ui.Find(AutomationIds.ScreenKycProcess);
		string[] Pages = [Page("prepareDocuments"), Page("personalInfo"), Page("identityDocuments"),
			Page("addressInformation"), Page("selfiePage"), Page("isOrgPage"), Page("orgInfo"),
			AutomationIds.ControlKycProcessContents, AutomationIds.ControlKycProcessSummary];
		for (int i = 0; i < Pages.Length; i++)
		{
			string Current = this.Ui.Any(Pages);
			if (Current == Page("personalInfo"))
				return;
			if (Current == Page("prepareDocuments"))
				this.Next(Current, Page("personalInfo"));
			else
			{
				this.Ui.Click(AutomationIds.ButtonKycProcessBack);
				this.Ui.Hidden(Current);
			}
		}
		Assert.Fail("KYC navigation did not reach personal information within the known schema steps.");
	}

	internal bool EnterPersonalInformation(PersonalIdentityData Data)
	{
		this.Enter("FIRST", Data.FirstName);
		bool IncludesMiddleName = this.TryRevealField("MIDDLE");
		if (IncludesMiddleName)
			this.Ui.Enter(Field("MIDDLE"), Data.MiddleName);
		this.Enter("LAST", Data.LastName);
		this.Enter("PNR", Data.PersonalNumber);
		this.RevealField("BDATE");
		this.Ui.Input(Field("BDATE")).Click();
		this.session.Platform.SelectDate(this.session, Data.DateOfBirth);
		string ActualDate = this.Ui.Value(this.Ui.Input(Field("BDATE")));
		Assert.IsTrue(DateOnly.TryParse(ActualDate, CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.None, out DateOnly Actual)
			&& Actual == Data.DateOfBirth, $"Birth date shows '{ActualDate}' instead of {Data.DateOfBirth:yyyy-MM-dd}.");
		if (this.TryRevealField("COUNTRY"))
			this.Select("COUNTRY", Data.Nationality);
		this.Select("NATIONALITY", Data.Nationality);
		this.Select("GENDER", Data.Gender);
		this.Next(Page("personalInfo"), Page("identityDocuments"));
		return IncludesMiddleName;
	}

	internal void ContinueWithoutDocument()
	{
		this.Select("documentType", "None");
		this.Next(Page("identityDocuments"), Page("addressInformation"));
	}

	internal void EnterAddress(PersonalIdentityData Data)
	{
		this.Enter("ZIP", Data.ZipCode);
		this.Enter("ADDR", Data.Address);
		this.Enter("ADDR2", Data.Apartment);
		this.Enter("AREA", Data.Area);
		this.Enter("CITY", Data.City);
		this.Enter("REGION", Data.Region);
		this.Next(Page("addressInformation"), Page("selfiePage"));
	}

	/// <summary>Scrolls the photo step to its capture button and returns the photo field's key.</summary>
	internal string RevealPhoto()
	{
		this.Ui.Find(Page("selfiePage"));
		string Key = this.session.Settings.Get("KYC_PHOTO_FIELD_ID", "selfieUpload");
		this.Ui.Reveal(AutomationIds.ForKey(AutomationIds.ButtonKycImageCapture, Key), AutomationIds.ControlKycProcessForm);
		return Key;
	}

	internal void ContinueWithPhoto(string Key)
	{
		this.Ui.Image(AutomationIds.ForKey(AutomationIds.ImageKycFieldPreview, Key));
		this.Next(Page("selfiePage"), Page("isOrgPage"));
	}

	internal void ContinueWithoutOrganization()
	{
		// Preserve the fixture's default unchecked organization choice; an enabled organization branch must fail.
		this.Ui.Find(Page("isOrgPage"));
		this.Ui.Click(AutomationIds.ButtonKycProcessNext);
		this.Ui.Hidden(Page("isOrgPage"));
		string Next = this.Ui.Any(AutomationIds.ControlKycProcessSummary, AutomationIds.ControlKycProcessContents, Page("orgInfo"));
		Assert.AreNotEqual(Page("orgInfo"), Next, "The personal identity application must not include an organization.");
		if (Next == AutomationIds.ControlKycProcessContents)
			this.Next(Next, AutomationIds.ControlKycProcessSummary);
	}

	/// <summary>Sends the application from the summary and confirms; the app then asks for the PIN.</summary>
	internal void Submit()
	{
		this.Ui.Find(AutomationIds.ControlKycProcessSummary);
		this.Ui.Click(AutomationIds.ButtonKycProcessNext);
		this.Ui.Confirm(this.session.Text["Confirm"], this.session.Text["AreYouSureYouWantToSendThisIdApplication"],
			this.session.Text["Yes"], this.session.Text["No"]);
	}

	internal void AssertSent() => this.Ui.Find(AutomationIds.ControlKycProcessSent, AutomationIds.ControlKycProcessSummary);

	private void Next(string Current, string Destination)
	{
		this.Ui.Click(AutomationIds.ButtonKycProcessNext);
		this.Ui.Hidden(Current);
		this.Ui.Find(Destination);
	}

	private void Enter(string Key, string Value)
	{
		this.RevealField(Key);
		this.Ui.Enter(Field(Key), Value);
	}

	private void Select(string Key, string Value)
	{
		this.RevealField(Key);
		this.Ui.Input(Field(Key)).Click();
		this.session.Platform.SelectPickerValue(this.session, Value);
		string Actual = this.Ui.Value(this.Ui.Input(Field(Key)));
		Assert.IsTrue(Actual.Contains(Value, StringComparison.Ordinal), $"{Key} shows '{Actual}' instead of '{Value}'.");
	}

	private void RevealField(string Key) => Assert.IsTrue(this.TryRevealField(Key), "Required KYC field is missing: " + Key + ".");

	private bool TryRevealField(string Key)
	{
		// Search both directions: an absent optional middle name must not strand the next required field above the viewport.
		string Container = AutomationIds.ControlKycProcessForm;
		bool Present = this.Ui.FindAll(Field(Key), Container, Visible: false).Count > 0;
		if (this.Ui.InputVisible(Field(Key), Container))
			return true;
		for (int i = 0; i < 8; i++)
		{
			this.Ui.Scroll(Container, Down: false);
			Present |= this.Ui.Visible(Field(Key), Container);
			if (this.Ui.InputVisible(Field(Key), Container))
				return true;
		}
		for (int i = 0; i <= 8; i++)
		{
			Present |= this.Ui.Visible(Field(Key), Container);
			if (this.Ui.InputVisible(Field(Key), Container))
				return true;
			if (i < 8)
				this.Ui.Scroll(Container);
		}
		Assert.IsFalse(Present, "KYC field exists but its input could not be reached: " + Key + ".");
		return false;
	}
}
