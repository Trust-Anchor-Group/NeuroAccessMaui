using System.Globalization;
using System.Text;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.iOS;

namespace NeuroAccessMaui.UITests.Infrastructure.Platforms;

/// <summary>Implements the platform operations with XCUITest on iOS simulators and devices.</summary>
internal sealed class IosPlatform : MobilePlatform
{
	internal override void Configure(AppiumOptions Options, TestSettings Settings)
	{
		Options.AutomationName = "XCUITest";
		Options.AddAdditionalAppiumOption("bundleId", Settings.AppId);
		Options.AddAdditionalAppiumOption("wdaLocalPort", Settings.Number("WDA_LOCAL_PORT", 8100, 1024, 65535));
		Options.AddAdditionalAppiumOption("waitForIdleTimeout", 0);
	}

	internal override AppiumDriver Connect(Uri Server, AppiumOptions Options) => new IOSDriver(Server, Options, TimeSpan.FromMinutes(3));
	internal override By Locator(string Id, string AppId) => MobileBy.AccessibilityId(Id);
	internal override By LocatorStartingWith(string Prefix, string AppId) => By.XPath($".//*[starts-with(@name,'{Prefix}')]");
	internal override Dictionary<string, object> AppArguments(string AppId) => new Dictionary<string, object> { ["bundleId"] = AppId };

	internal override string TypeAttribute => "type";
	internal override string[] InputTypes { get; } = ["XCUIElementTypeTextField", "XCUIElementTypeSecureTextField", "XCUIElementTypeTextView"];
	internal override string[] LabelTypes { get; } = ["XCUIElementTypeStaticText", "XCUIElementTypeButton"];
	internal override string ImageType => "XCUIElementTypeImage";

	internal override string Value(IWebElement Element) =>
		Element.GetAttribute(this.InputTypes.Contains(Element.GetAttribute(this.TypeAttribute)) ? "value" : "label") ?? Element.Text;

	internal override bool IsSecure(IWebElement Element) => Element.GetAttribute(this.TypeAttribute) == "XCUIElementTypeSecureTextField";

	internal override void HideKeyboard(UiSession Session)
	{
		IReadOnlyList<IWebElement> Buttons = Session.Driver.FindElements(MobileBy.AccessibilityId("Done"));
		if (Buttons.Count == 1 && Session.Ui.IsVisible(Buttons[0]))
			Buttons[0].Click();
		else
			Session.Execute("hideKeyboard");
	}

	internal override void TypeDigits(UiSession Session, IWebElement Input, string Digits) => Input.SendKeys(Digits);

	internal override (ISearchContext? Scope, IReadOnlyList<IWebElement> Buttons) Alert(UiSession Session, double Seconds)
	{
		IWebElement Root = Session.Ui.Wait(() => Session.Driver.FindElements(By.ClassName("XCUIElementTypeAlert"))
			.SingleOrDefault(Session.Ui.IsVisible), "native alert", Seconds);
		return (Root, Root.FindElements(By.ClassName("XCUIElementTypeButton")).Where(Session.Ui.IsVisible).ToArray());
	}

	internal override (IWebElement Message, IWebElement? Allow)? NotificationPrompt(UiSession Session)
	{
		IWebElement? Root = Session.Driver.FindElements(By.ClassName("XCUIElementTypeAlert")).SingleOrDefault(Session.Ui.IsVisible);
		if (Root is null)
			return null;
		IWebElement? Message = Session.Ui.TextElements(Value => Value.Contains("notifications", StringComparison.OrdinalIgnoreCase), Root).FirstOrDefault();
		Assert.IsNotNull(Message, "Expected an iOS notification permission alert.");
		return (Message, Root.FindElements(By.ClassName("XCUIElementTypeButton"))
			.SingleOrDefault(Element => Session.Ui.IsVisible(Element) && this.Value(Element) == "Allow"));
	}

	internal override void ClearApp(AppiumDriver Driver, TestSettings Settings)
	{
		Execute(Driver, "clearApp", this.AppArguments(Settings.AppId));
		Execute(Driver, "clearKeychains");
	}

	internal override void SetClipboard(AppiumDriver Driver, TestSettings Settings, string Text)
	{
		if (Settings.Flag("IOS_SIMULATOR"))
			Execute(Driver, "setPasteboard", new Dictionary<string, object> { ["content"] = Text });
		else
			this.WithClipboardAccess(Driver, Settings, () => Execute(Driver, "setClipboard", new Dictionary<string, object>
			{
				["content"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(Text)), ["contentType"] = "plaintext"
			}));
	}

	internal override string GetClipboard(AppiumDriver Driver, TestSettings Settings)
	{
		if (Settings.Flag("IOS_SIMULATOR"))
			return Convert.ToString(Execute(Driver, "getPasteboard"), CultureInfo.InvariantCulture) ?? "";
		string Content = Convert.ToString(this.WithClipboardAccess(Driver, Settings, () => Execute(Driver, "getClipboard",
			new Dictionary<string, object> { ["contentType"] = "plaintext" })), CultureInfo.InvariantCulture) ?? "";
		return Encoding.UTF8.GetString(Convert.FromBase64String(Content));
	}

	private object WithClipboardAccess(AppiumDriver Driver, TestSettings Settings, Func<object> Action)
	{
		string Wda = Settings.Required("WDA_BUNDLE_ID");
		try
		{
			this.Activate(Driver, Wda);
			return Action();
		}
		finally { this.Activate(Driver, Settings.AppId); }
	}

	internal override void SelectPickerValue(UiSession Session, string Value)
	{
		IWebElement Wheel = Session.Ui.Wait(() => Session.Driver.FindElements(By.ClassName("XCUIElementTypePickerWheel"))
			.SingleOrDefault(Element => Element.Displayed), "native picker wheel");
		// Country/gender options include flags or symbols. Find the expected text in an actual option,
		// instead of guessing its decorated label or treating an unaccepted SendKeys call as success.
		foreach (string Direction in new[] { "previous", "next" })
		{
			for (int i = 0; i < 300; i++)
			{
				string Before = Wheel.GetAttribute("value") ?? "";
				if (Before.Contains(Value, StringComparison.Ordinal))
					break;
				try
				{
					Session.Execute("selectPickerWheelValue", new Dictionary<string, object>
					{
						["elementId"] = ((AppiumElement)Wheel).Id, ["order"] = Direction, ["offset"] = 0.2
					});
				}
				catch (WebDriverException)
				{
					// XCTest reports a failure at a wheel boundary; an unchanged value permits reversing direction.
					if (Wheel.GetAttribute("value") != Before)
						throw;
					break;
				}
				if (Wheel.GetAttribute("value") == Before)
					break;
			}
			if ((Wheel.GetAttribute("value") ?? "").Contains(Value, StringComparison.Ordinal))
				break;
		}
		string Selected = Wheel.GetAttribute("value") ?? "";
		Assert.IsTrue(Selected.Contains(Value, StringComparison.Ordinal), $"Native picker shows '{Selected}' instead of '{Value}'.");
		Session.Ui.Text("Done").Click();
		Session.Ui.Until(() => !Session.Driver.FindElements(By.ClassName("XCUIElementTypePickerWheel")).Any(Element => Element.Displayed), "picker dismissal");
	}

	internal override void SelectDate(UiSession Session, DateOnly Date)
	{
		IWebElement[] Wheels = Session.Ui.Wait(() =>
		{
			IWebElement[] Visible = Session.Driver.FindElements(By.ClassName("XCUIElementTypePickerWheel"))
				.Where(Element => Element.Displayed).OrderBy(Element => Element.Location.X).ToArray();
			return Visible.Length == 3 ? Visible : null;
		}, "three native date wheels (English US system locale)");
		// MAUI uses the native wheels; the suite explicitly requires English US month/day/year order.
		string[] Values = [Date.ToString("MMMM", CultureInfo.GetCultureInfo("en-US")), Date.Day.ToString(CultureInfo.InvariantCulture), Date.Year.ToString(CultureInfo.InvariantCulture)];
		foreach (int Index in new[] { 2, 0, 1 })
		{
			Wheels[Index].SendKeys(Values[Index]);
			Session.Ui.Until(() => Wheels[Index].GetAttribute("value") == Values[Index],
				"fixture date wheel value; out-of-range dates must not be clamped");
		}
		Session.Ui.Text("Done").Click();
		Session.Ui.Until(() => !Session.Driver.FindElements(By.ClassName("XCUIElementTypePickerWheel")).Any(Element => Element.Displayed), "date picker dismissal");
	}
}
