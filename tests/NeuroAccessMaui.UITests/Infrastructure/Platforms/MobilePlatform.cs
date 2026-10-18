using OpenQA.Selenium.Appium;

namespace NeuroAccessMaui.UITests.Infrastructure.Platforms;

/// <summary>Contains everything that differs between Android and iOS, so screens and <see cref="MobileUi"/> never branch on the platform.</summary>
internal abstract class MobilePlatform
{
	internal abstract void Configure(AppiumOptions Options, TestSettings Settings);
	internal abstract AppiumDriver Connect(Uri Server, AppiumOptions Options);
	internal abstract By Locator(string Id, string AppId);

	/// <summary>Finds descendants whose automation ID starts with <paramref name="Prefix"/>, such as keyed list items.</summary>
	internal abstract By LocatorStartingWith(string Prefix, string AppId);

	internal abstract Dictionary<string, object> AppArguments(string AppId);
	internal abstract void ClearApp(AppiumDriver Driver, TestSettings Settings);
	internal abstract void SetClipboard(AppiumDriver Driver, TestSettings Settings, string Text);
	internal abstract string GetClipboard(AppiumDriver Driver, TestSettings Settings);
	internal abstract void SelectPickerValue(UiSession Session, string Value);
	internal abstract void SelectDate(UiSession Session, DateOnly Date);

	/// <summary>Gets the element attribute that holds the native control type.</summary>
	internal abstract string TypeAttribute { get; }

	/// <summary>Gets the native types of text inputs.</summary>
	internal abstract string[] InputTypes { get; }

	/// <summary>Gets the native types whose text a user reads, such as labels and buttons.</summary>
	internal abstract string[] LabelTypes { get; }

	/// <summary>Gets the native type of images.</summary>
	internal abstract string ImageType { get; }

	/// <summary>Reads the text a user sees in a label or input.</summary>
	internal abstract string Value(IWebElement Element);

	/// <summary>Tells whether an input masks its content, such as a PIN field.</summary>
	internal abstract bool IsSecure(IWebElement Element);

	/// <summary>Closes the visible software keyboard.</summary>
	internal abstract void HideKeyboard(UiSession Session);

	/// <summary>Types digits into the focused verification code input.</summary>
	internal abstract void TypeDigits(UiSession Session, IWebElement Input, string Digits);

	/// <summary>Waits for the app's native alert and returns the scope that holds its texts, and its visible buttons.</summary>
	internal abstract (ISearchContext? Scope, IReadOnlyList<IWebElement> Buttons) Alert(UiSession Session, double Seconds);

	/// <summary>Returns the system notification permission prompt's message and Allow button, or null when no prompt is shown.</summary>
	internal abstract (IWebElement Message, IWebElement? Allow)? NotificationPrompt(UiSession Session);

	internal void Terminate(AppiumDriver Driver, string AppId) => Execute(Driver, "terminateApp", this.AppArguments(AppId));
	internal void Activate(AppiumDriver Driver, string AppId) => Execute(Driver, "activateApp", this.AppArguments(AppId));

	/// <summary>Builds an XPath that matches any of this platform's label types.</summary>
	internal By LabelLocator(bool Scoped) =>
		By.XPath((Scoped ? ".//*" : "//*") + "[" + string.Join(" or ", this.LabelTypes.Select(Type => "self::" + Type)) + "]");

	internal static object Execute(AppiumDriver Driver, string Command, Dictionary<string, object>? Arguments = null) =>
		Driver.ExecuteScript("mobile: " + Command, Arguments ?? new Dictionary<string, object>());
}
