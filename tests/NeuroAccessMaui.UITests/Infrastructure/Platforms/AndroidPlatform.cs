using System.Globalization;
using System.Text;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;

namespace NeuroAccessMaui.UITests.Infrastructure.Platforms;

/// <summary>Implements the platform operations with UiAutomator2 on Android.</summary>
internal sealed class AndroidPlatform : MobilePlatform
{
	internal override void Configure(AppiumOptions Options, TestSettings Settings)
	{
		Options.AutomationName = "UiAutomator2";
		Options.AddAdditionalAppiumOption("appPackage", Settings.AppId);
		Options.AddAdditionalAppiumOption("appActivity", Settings.Get("APP_ACTIVITY", "com.tag.NeuroAccess.MainActivity"));
		Options.AddAdditionalAppiumOption("systemPort", Settings.Number("SYSTEM_PORT", 8200, 1024, 65535));
		Options.AddAdditionalAppiumOption("settings[disableIdLocatorAutocompletion]", true);
		Options.AddAdditionalAppiumOption("settings[waitForIdleTimeout]", 0);
	}

	internal override AppiumDriver Connect(Uri Server, AppiumOptions Options) => new AndroidDriver(Server, Options, TimeSpan.FromMinutes(3));
	internal override By Locator(string Id, string AppId) => By.Id(Id.Contains(":id/", StringComparison.Ordinal) ? Id : AppId + ":id/" + Id);
	internal override By LocatorStartingWith(string Prefix, string AppId) => By.XPath($".//*[starts-with(@resource-id,'{AppId}:id/{Prefix}')]");
	internal override Dictionary<string, object> AppArguments(string AppId) => new Dictionary<string, object> { ["appId"] = AppId };

	internal override string TypeAttribute => "class";
	internal override string[] InputTypes { get; } = ["android.widget.EditText"];
	internal override string[] LabelTypes { get; } = ["android.widget.TextView", "android.widget.CheckedTextView", "android.widget.Button"];
	internal override string ImageType => "android.widget.ImageView";
	internal override string Value(IWebElement Element) => Element.Text;
	internal override bool IsSecure(IWebElement Element) => Element.GetAttribute("password") == "true";
	internal override void HideKeyboard(UiSession Session) => Session.Execute("hideKeyboard");

	internal override void TypeDigits(UiSession Session, IWebElement Input, string Digits)
	{
		// Android key codes for 0-9 start at 7.
		foreach (char Digit in Digits)
			Session.Execute("pressKey", new Dictionary<string, object> { ["keycode"] = 7 + Digit - '0' });
	}

	internal override (ISearchContext? Scope, IReadOnlyList<IWebElement> Buttons) Alert(UiSession Session, double Seconds)
	{
		IWebElement Message = Session.Ui.Wait(() => Session.Ui.FindAll("android:id/message").SingleOrDefault(), "native dialog", Seconds);
		Assert.AreEqual(Session.Settings.AppId, Message.GetAttribute("package"), "The dialog belongs to another app.");
		// UiAutomator2 reads only the focused window, which is the dialog while it is open, so its texts are searched screen-wide.
		IWebElement[] Buttons = new[] { "android:id/button1", "android:id/button2", "android:id/button3" }
			.SelectMany(Id => Session.Ui.FindAll(Id)).ToArray();
		return (null, Buttons);
	}

	internal override (IWebElement Message, IWebElement? Allow)? NotificationPrompt(UiSession Session)
	{
		foreach (string Package in new[] { "com.android.permissioncontroller", "com.google.android.permissioncontroller" })
		{
			IWebElement? Message = Session.Ui.FindAll(Package + ":id/permission_message").SingleOrDefault();
			if (Message is not null)
				return (Message, Session.Ui.FindAll(Package + ":id/permission_allow_button").SingleOrDefault());
		}
		return null;
	}

	internal override void ClearApp(AppiumDriver Driver, TestSettings Settings) => Execute(Driver, "clearApp", this.AppArguments(Settings.AppId));

	internal override void SetClipboard(AppiumDriver Driver, TestSettings Settings, string Text) =>
		Execute(Driver, "setClipboard", new Dictionary<string, object>
		{
			["content"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(Text)), ["contentType"] = "plaintext"
		});

	internal override string GetClipboard(AppiumDriver Driver, TestSettings Settings)
	{
		string Content = Convert.ToString(Execute(Driver, "getClipboard",
			new Dictionary<string, object> { ["contentType"] = "plaintext" }), CultureInfo.InvariantCulture) ?? "";
		return Encoding.UTF8.GetString(Convert.FromBase64String(Content));
	}

	internal override void SelectPickerValue(UiSession Session, string Value)
	{
		MobileUi Ui = Session.Ui;
		IWebElement List = Ui.Wait(() => Session.Driver.FindElements(By.ClassName("android.widget.ListView"))
			.SingleOrDefault(Element => Element.Displayed), "native picker options");
		Assert.IsTrue(List.GetAttribute("package") == Session.Settings.AppId, "Native picker does not belong to the target application.");
		foreach (bool Down in new[] { false, true })
		{
			string? Previous = null;
			for (int i = 0; i <= 80; i++)
			{
				IReadOnlyList<IWebElement> Matches = Ui.TextElements(Text => Text.Contains(Value, StringComparison.Ordinal), List);
				Assert.IsTrue(Matches.Count <= 1, "Native picker option is ambiguous.");
				if (Matches.Count == 1)
				{
					Assert.IsTrue(Matches[0].Enabled, "Native picker option is disabled.");
					Matches[0].Click();
					Ui.Until(() => !Session.Driver.FindElements(By.ClassName("android.widget.ListView")).Any(Element => Element.Displayed), "native picker dismissal");
					return;
				}
				string Current = string.Join("\n", Ui.TextElements(_ => true, List).Select(Ui.Value));
				if (Current == Previous)
					break;
				Previous = Current;
				if (i < 80)
					Ui.Scroll(List, Down);
			}
		}
		Assert.Fail($"Option '{Value}' was not found in the native picker.");
	}

	internal override void SelectDate(UiSession Session, DateOnly Date)
	{
		MobileUi Ui = Session.Ui;
		Ui.Find("android:id/datePicker");
		Ui.Click("android:id/date_picker_header_year");
		string Year = Date.Year.ToString(CultureInfo.InvariantCulture);
		bool Selected = false;
		string? PreviousYears = null;
		for (int i = 0; i < 80; i++)
		{
			IWebElement Years = Ui.Find("android:id/date_picker_year_picker");
			IReadOnlyList<IWebElement> Labels = Ui.TextElements(Text => int.TryParse(Text, out _), Years);
			IWebElement? Target = Labels.SingleOrDefault(Element => Ui.Value(Element) == Year);
			if (Target is not null)
			{
				Target.Click();
				Selected = true;
				break;
			}
			Assert.IsTrue(Labels.Count > 0, "Native date picker has no visible years.");
			string CurrentYears = string.Join(",", Labels.Select(Ui.Value));
			if (CurrentYears == PreviousYears)
				break;
			PreviousYears = CurrentYears;
			int First = int.Parse(Ui.Value(Labels[0]), CultureInfo.InvariantCulture);
			Ui.Scroll(Years, Down: Date.Year > First);
		}
		Assert.IsTrue(Selected, "Birth year is unavailable in the native picker. The 1890 fixture is outside the app's current 1900 minimum; no substitute is allowed.");
		Ui.Hidden("android:id/date_picker_year_picker");
		// Every age-group fixture uses January 1; the year selection retains the current month.
		Assert.IsTrue(Date.Month == 1 && Date.Day == 1, "The preserved KYC date fixture must be January 1.");
		for (int i = 0; i < 12; i++)
		{
			IWebElement? Day = Ui.Find("android:id/datePicker").FindElements(By.ClassName("android.view.View"))
				.Where(Element => Element.Displayed).SingleOrDefault(Element =>
				{
					string Description = Element.GetAttribute("content-desc") ?? "";
					return DateOnly.TryParse(Description, CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.AllowWhiteSpaces, out DateOnly Parsed)
						&& Parsed == Date;
				});
			if (Day is not null)
			{
				Day.Click();
				Ui.Click("android:id/button1");
				Ui.Hidden("android:id/datePicker");
				return;
			}
			Ui.Click("android:id/prev");
		}
		Assert.Fail("The fixture date was not available in the native calendar.");
	}
}
