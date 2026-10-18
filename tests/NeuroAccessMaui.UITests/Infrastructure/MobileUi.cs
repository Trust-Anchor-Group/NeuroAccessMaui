using System.Drawing;
using System.Globalization;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using NeuroAccessMaui.UITests.Infrastructure.Platforms;

namespace NeuroAccessMaui.UITests.Infrastructure;

/// <summary>Finds, reads and operates controls by automation ID; platform differences live in <see cref="MobilePlatform"/>.</summary>
internal sealed class MobileUi
{
	private readonly UiSession session;

	internal MobileUi(UiSession Session) => this.session = Session;

	private MobilePlatform Platform => this.session.Platform;

	internal By Locator(string Id) => this.Platform.Locator(Id, this.session.Settings.AppId);

	internal IReadOnlyList<IWebElement> FindAll(string Id, string? Scope = null, bool Visible = true)
	{
		ISearchContext Root = Scope is null ? this.session.Driver : this.Find(Scope);
		return Root.FindElements(this.Locator(Id)).Where(Element => !Visible || this.IsVisible(Element)).ToArray();
	}

	internal bool Visible(string Id, string? Scope = null)
	{
		IReadOnlyList<IWebElement> Matches = this.FindAll(Id, Scope);
		Assert.IsTrue(Matches.Count <= 1, $"{Id} is visible {Matches.Count} times.");
		return Matches.Count == 1;
	}

	internal IWebElement Find(string Id, string? Scope = null, bool Ready = false)
	{
		return this.Wait(() =>
		{
			IReadOnlyList<IWebElement> Matches = this.FindAll(Id, Scope);
			Assert.IsTrue(Matches.Count <= 1, $"{Id} is visible {Matches.Count} times.");
			return Matches.Count == 1 && (!Ready || Matches[0].Enabled) ? Matches[0] : null;
		}, Ready ? Id + " to be enabled" : Id);
	}

	internal T Wait<T>(Func<T?> Read, string Description, double Seconds = 30) where T : class
	{
		WebDriverWait Waiter = new WebDriverWait(this.session.Driver, TimeSpan.FromSeconds(Seconds));
		Waiter.PollingInterval = TimeSpan.FromMilliseconds(150);
		Waiter.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));
		try { return Waiter.Until(_ => Read())!; }
		catch (WebDriverTimeoutException) { throw new AssertFailedException($"Timed out after {Seconds} s waiting for {Description}."); }
	}

	internal void Until(Func<bool> Condition, string Description, double Seconds = 30) =>
		this.Wait(() => Condition() ? "ready" : null, Description, Seconds);

	internal string Any(params string[] Ids) => this.Wait(() =>
	{
		string[] Visible = Ids.Where(Id => this.Visible(Id)).ToArray();
		Assert.IsTrue(Visible.Length <= 1, "Several screens are visible at once: " + string.Join(", ", Visible));
		return Visible.SingleOrDefault();
	}, "one of " + string.Join(", ", Ids), 120);

	internal string AnyReady(params string[] Ids) => this.Wait(() =>
	{
		string[] Ready = Ids.Where(Id => this.FindAll(Id).Any(Element => Element.Enabled)).ToArray();
		Assert.IsTrue(Ready.Length <= 1, "Several actions are enabled at once: " + string.Join(", ", Ready));
		return Ready.SingleOrDefault();
	}, "one of " + string.Join(", ", Ids) + " to be enabled", 120);

	internal void Hidden(string Id) => this.Until(() => !this.Visible(Id), Id + " to close");

	/// <summary>Waits until the control is enabled, then taps it.</summary>
	internal void Click(string Id, string? Scope = null) => this.Find(Id, Scope, true).Click();

	internal IReadOnlyList<IWebElement> Descendants(IWebElement Root) =>
		// UiAutomator2 can include the context node in scoped XPath results.
		new[] { Root }.Concat(Root.FindElements(By.XPath(".//*"))).Distinct().ToArray();

	internal string NativeType(IWebElement Element) => Element.GetAttribute(this.Platform.TypeAttribute) ?? "";
	internal bool IsInput(IWebElement Element) => this.Platform.InputTypes.Contains(this.NativeType(Element));
	internal bool IsLabel(IWebElement Element) => this.Platform.LabelTypes.Contains(this.NativeType(Element));
	internal string Value(IWebElement Element) => this.Platform.Value(Element);

	internal string Read(string Id, string? Scope = null)
	{
		IReadOnlyList<IWebElement> Labels = this.Labels(this.Find(Id, Scope));
		Assert.IsTrue(Labels.Count == 1, $"Expected one visible label in {Id}, found {Labels.Count}.");
		return this.Value(Labels[0]);
	}

	internal IReadOnlyList<IWebElement> Labels(IWebElement Root)
	{
		IWebElement[] Children = Root.FindElements(this.Platform.LabelLocator(true))
			.Where(this.IsVisible).ToArray();
		return Children.Length > 0 ? Children : this.IsLabel(Root) ? new[] { Root } : Array.Empty<IWebElement>();
	}

	internal void Label(string Id, string Expected, string? Scope = null)
	{
		string[] Actual = this.Labels(this.Find(Id, Scope)).Select(this.Value).ToArray();
		Assert.IsTrue(Actual.Count(Value => Value == Expected) == 1, $"{Id} should show '{Expected}' but shows: {Quote(Actual)}.");
	}

	internal void LabelMatches(string Id, Func<string, bool> Matches, string Description, string? Scope = null)
	{
		string[] Actual = this.Labels(this.Find(Id, Scope)).Select(this.Value).ToArray();
		Assert.IsTrue(Actual.Count(Matches) == 1, $"{Id} should show {Description} but shows: {Quote(Actual)}.");
	}

	internal void Action(string Id, string Expected, bool Enabled, string? Scope = null)
	{
		this.Label(Id, Expected, Scope);
		this.Enabled(Id, Enabled, Scope);
	}

	internal void Enabled(string Id, bool Expected, string? Scope = null) =>
		Assert.AreEqual(Expected, this.Find(Id, Scope).Enabled, $"{Id} should be {(Expected ? "enabled" : "disabled")}.");

	internal IWebElement Input(string Id, string? Scope = null, bool Enabled = true, string? Expected = null)
	{
		IWebElement Root = this.Find(Id, Scope);
		Assert.AreEqual(Enabled, Root.Enabled, $"{Id} should be {(Enabled ? "enabled" : "disabled")}.");
		IWebElement[] Inputs = this.Descendants(Root).Where(Element => this.IsInput(Element) && this.IsVisible(Element)).ToArray();
		Assert.IsTrue(Inputs.Length == 1, $"Expected one native input in {Id}, found {Inputs.Length}.");
		if (Expected is not null)
			Assert.AreEqual(Expected, this.Value(Inputs[0]), $"{Id} has an unexpected value.");
		return Inputs[0];
	}

	internal bool InputVisible(string Id, string Container)
	{
		IReadOnlyList<IWebElement> Roots = this.FindAll(Id, Container);
		Assert.IsTrue(Roots.Count <= 1, $"{Id} is visible {Roots.Count} times.");
		if (Roots.Count == 0)
			return false;
		IWebElement Viewport = this.Find(Container);
		Rectangle Bounds = new Rectangle(Viewport.Location, Viewport.Size);
		Bounds.Intersect(new Rectangle(Point.Empty, this.session.Driver.Manage().Window.Size));
		IWebElement[] Inputs = this.Descendants(Roots[0]).Where(Element => this.IsInput(Element) && this.IsVisible(Element)).ToArray();
		Assert.IsTrue(Inputs.Length <= 1, $"Expected at most one native input in {Id}, found {Inputs.Length}.");
		if (Inputs.Length == 0)
			return false;
		Rectangle InputBounds = new Rectangle(Inputs[0].Location, Inputs[0].Size);
		Rectangle VisibleBounds = Rectangle.Intersect(Bounds, InputBounds);
		return VisibleBounds.Height >= InputBounds.Height * 0.9 && VisibleBounds.Width >= InputBounds.Width * 0.9;
	}

	internal void Enter(string Id, string Text, string? Scope = null)
	{
		IWebElement Input = this.Input(Id, Scope);
		Input.Clear();
		Input.SendKeys(Text);
		this.HideKeyboard();
		if (this.Platform.IsSecure(Input))
		{
			// Secure fields hold PINs, so the failure message leaves the value out.
			string Actual = this.Value(Input);
			Assert.IsTrue(Actual == Text || (Actual.Length == Text.Length && Actual.All(Character => Character is '•' or '●' or '*')),
				$"{Id} does not hold the {Text.Length} typed characters.");
		}
		else
			this.Input(Id, Scope, Expected: Text);
	}

	internal void HideKeyboard()
	{
		if (Convert.ToBoolean(this.session.Execute("isKeyboardShown"), CultureInfo.InvariantCulture))
			this.Platform.HideKeyboard(this.session);
	}

	internal void Image(string Id, string? Scope = null)
	{
		int Count = this.Descendants(this.Find(Id, Scope)).Count(Element => this.IsVisible(Element)
			&& this.NativeType(Element) == this.Platform.ImageType);
		Assert.IsTrue(Count == 1, $"Expected one visible image in {Id}, found {Count}.");
	}

	internal IReadOnlyList<IWebElement> TextElements(Func<string, bool> Matches, ISearchContext? Scope = null) =>
		(Scope ?? this.session.Driver).FindElements(this.Platform.LabelLocator(Scope is not null))
			.Where(Element => this.IsVisible(Element) && Matches(this.Value(Element))).ToArray();

	internal IWebElement Text(string Expected, bool Contains = false, double Seconds = 30) => this.Wait(() =>
	{
		IReadOnlyList<IWebElement> Matches = this.TextElements(Value => Contains ? Value.Contains(Expected, StringComparison.Ordinal) : Value == Expected);
		Assert.IsTrue(Matches.Count <= 1, $"'{Expected}' is visible {Matches.Count} times; use an automation ID instead.");
		return Matches.SingleOrDefault();
	}, $"the text '{Expected}'", Seconds);

	/// <summary>Checks a native alert's title and message, then taps <paramref name="Button"/>.</summary>
	internal void Dialog(string Title, Func<string, bool>? Message, string Button, double Seconds = 15)
	{
		(ISearchContext? Scope, IReadOnlyList<IWebElement> Buttons) = this.Platform.Alert(this.session, Seconds);
		this.AlertText(Scope, Value => Value == Title, $"the title '{Title}'");
		if (Message is not null)
			this.AlertText(Scope, Message, "the expected message");
		this.Press(Buttons, Button);
	}

	/// <summary>Dismisses a native alert that has a single button, whatever its text.</summary>
	internal void DismissInformationDialog()
	{
		IReadOnlyList<IWebElement> Buttons = this.Platform.Alert(this.session, 15).Buttons;
		Assert.HasCount(1, Buttons, "Expected an information dialog with one button.");
		this.Press(Buttons, this.Value(Buttons[0]));
	}

	/// <summary>Checks a native confirmation's texts and its accept and cancel buttons, then accepts it.</summary>
	internal void Confirm(string Title, string Message, string Accept, string Cancel)
	{
		(ISearchContext? Scope, IReadOnlyList<IWebElement> Buttons) = this.Platform.Alert(this.session, 30);
		this.AlertText(Scope, Value => Value == Title, $"the title '{Title}'");
		this.AlertText(Scope, Value => Value == Message, $"the message '{Message}'");
		string[] Labels = Buttons.Select(this.Value).ToArray();
		CollectionAssert.AreEquivalent(new[] { Accept, Cancel }, Labels, $"Unexpected confirmation buttons: {Quote(Labels)}.");
		this.Press(Buttons, Accept);
	}

	/// <summary>Allows the system notification prompt when it is shown and returns whether it was.</summary>
	internal bool AllowNativeNotifications(bool AlreadyHandled)
	{
		if (this.Platform.NotificationPrompt(this.session) is not { } Prompt)
			return false;
		Assert.IsFalse(AlreadyHandled, "The notification permission prompt appeared twice.");
		Assert.IsTrue(this.session.Settings.Get("SYSTEM_LANGUAGE_CODE", "en") == "en", "Native notification assertions require English system UI.");
		IWebElement Message = Prompt.Message;
		IWebElement? Allow = Prompt.Allow;
		string Text = this.Value(Message);
		Assert.IsTrue(Text.Contains("notifications", StringComparison.OrdinalIgnoreCase), $"Unexpected permission prompt: '{Text}'.");
		Assert.IsNotNull(Allow, "The notification permission prompt has no Allow button.");
		Assert.IsTrue(Allow.Enabled && this.Value(Allow) == "Allow", $"Unexpected permission button: '{this.Value(Allow)}'.");
		Allow.Click();
		this.Until(() => !this.IsVisible(Message), "the notification permission prompt to close", 10);
		return true;
	}

	internal void Scroll(string? Container = null, bool Down = true, double Fraction = 0.45)
	{
		Rectangle Bounds = Container is null
			? new Rectangle(Point.Empty, this.session.Driver.Manage().Window.Size)
			: new Rectangle(this.Find(Container).Location, this.Find(Container).Size);
		this.ScrollBounds(Bounds, Down, Fraction);
	}

	internal void Scroll(IWebElement Container, bool Down = true, double Fraction = 0.45) =>
		this.ScrollBounds(new Rectangle(Container.Location, Container.Size), Down, Fraction);

	private void ScrollBounds(Rectangle Bounds, bool Down, double Fraction)
	{
		Bounds.Intersect(new Rectangle(Point.Empty, this.session.Driver.Manage().Window.Size));
		Assert.IsTrue(Bounds.Width > 0 && Bounds.Height > 0, "Scroll viewport is outside the screen.");
		int x = Bounds.Left + Bounds.Width / 2;
		int Start = Bounds.Top + (int)(Bounds.Height * (Down ? 0.75 : 0.25));
		int End = Start + (int)(Bounds.Height * Fraction * (Down ? -1 : 1));
		this.Drag(x, Start, x, End);
	}

	internal void Drag(int StartX, int StartY, int EndX, int EndY)
	{
		PointerInputDevice Finger = new PointerInputDevice(PointerKind.Touch);
		ActionSequence Sequence = new ActionSequence(Finger, 0);
		Sequence.AddAction(Finger.CreatePointerMove(CoordinateOrigin.Viewport, StartX, StartY, TimeSpan.Zero));
		Sequence.AddAction(Finger.CreatePointerDown(MouseButton.Left));
		Sequence.AddAction(Finger.CreatePointerMove(CoordinateOrigin.Viewport, EndX, EndY, TimeSpan.FromMilliseconds(550)));
		Sequence.AddAction(Finger.CreatePointerUp(MouseButton.Left));
		this.session.Driver.PerformActions(new[] { Sequence });
		this.session.Driver.ResetInputState();
	}

	internal void Reveal(string Id, string? Container = null, int Maximum = 8)
	{
		for (int i = 0; i <= Maximum; i++)
		{
			if (this.Visible(Id))
				return;
			if (i < Maximum)
				this.Scroll(Container);
		}
		Assert.Fail($"{Id} was not found after scrolling {Maximum} times.");
	}

	internal bool IsVisible(IWebElement Element)
	{
		try
		{
			if (!Element.Displayed)
				return false;
			Size ElementSize = Element.Size;
			if (ElementSize.Width <= 0 || ElementSize.Height <= 0)
				return false;
			Size Screen = this.session.Driver.Manage().Window.Size;
			return new Rectangle(Point.Empty, Screen).IntersectsWith(new Rectangle(Element.Location, ElementSize));
		}
		catch (StaleElementReferenceException) { return false; }
	}

	private void AlertText(ISearchContext? Scope, Func<string, bool> Matches, string Description)
	{
		string[] Texts = this.TextElements(_ => true, Scope).Select(this.Value).ToArray();
		Assert.IsTrue(Texts.Count(Matches) == 1, $"The dialog should show {Description} but shows: {Quote(Texts)}.");
	}

	private void Press(IReadOnlyList<IWebElement> Buttons, string Label)
	{
		IWebElement? Button = Buttons.SingleOrDefault(Element => this.Value(Element) == Label);
		Assert.IsNotNull(Button, $"The dialog has no '{Label}' button; it has: {Quote(Buttons.Select(this.Value))}.");
		Assert.IsTrue(Button.Enabled, $"The dialog's '{Label}' button is disabled.");
		Button.Click();
		this.Until(() => !this.IsVisible(Button), "the dialog to close");
	}

	private static string Quote(IEnumerable<string> Values) => string.Join(", ", Values.Select(Value => "'" + Value + "'"));
}
