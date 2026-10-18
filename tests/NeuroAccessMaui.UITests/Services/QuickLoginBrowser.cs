using System.Globalization;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;

namespace NeuroAccessMaui.UITests.Services;

/// <summary>Runs the real Quick Login page and checks its rendered identity response.</summary>
internal sealed class QuickLoginBrowser : IDisposable
{
	private readonly IWebDriver driver;
	private readonly Uri page;

	internal QuickLoginBrowser(TestSettings Settings)
	{
		this.page = Settings.GetUri("QUICK_LOGIN_PAGE_URL", true);
		ChromeOptions Options = new ChromeOptions();
		Options.AddArgument("--headless=new");
		Options.AddArgument("--window-size=1280,1000");
		this.driver = Settings.Get("BROWSER_URL").Length == 0
			? new ChromeDriver(Options)
			: new RemoteWebDriver(Settings.GetUri("BROWSER_URL", false), Options);
	}

	internal void Open() => this.driver.Navigate().GoToUrl(this.page);

	internal string SignUrl() => this.Wait(() =>
	{
		string Link = this.Script("return document.getElementById('quickLoginA')?.getAttribute('href') || '';");
		return Link.StartsWith("tagsign:", StringComparison.OrdinalIgnoreCase)
			&& Link.Contains(',') && Link[(Link.IndexOf(',') + 1)..].Length > 0 ? Link : null;
	}, "Quick Login signing link");

	internal string Purpose()
	{
		string Purpose = this.Script("return document.getElementById('quickLoginCode')?.getAttribute('data-purpose') || '';");
		Assert.IsFalse(string.IsNullOrWhiteSpace(Purpose), "Quick Login purpose is empty.");
		return Purpose;
	}

	internal void AssertApprovedIdentity(string MobileId)
	{
		this.Wait(() => this.Script("return document.getElementById('quickLoginCode')?.getAttribute('data-done') || '';") == "1" ? "done" : null,
			"Quick Login completion");
		Assert.IsTrue(this.driver.FindElements(By.CssSelector("h2")).Any(Element => Element.Text.Trim() == "Successfully logged in."),
			"Quick Login success heading is missing.");
		IWebElement[] Tables = this.driver.FindElements(By.CssSelector("table"))
			.Where(Table => Table.FindElements(By.CssSelector("thead th")).Any(Header => Header.Text.Trim() == "Identity of user.")).ToArray();
		Assert.IsTrue(Tables.Length == 1, "Expected exactly one identity table.");
		Dictionary<string, string> Fields = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (IWebElement Row in Tables[0].FindElements(By.CssSelector("tbody tr")))
		{
			IReadOnlyList<IWebElement> Cells = Row.FindElements(By.CssSelector("td"));
			if (Cells.Count == 2)
				Assert.IsTrue(Fields.TryAdd(Cells[0].Text.Trim(), Cells[1].Text.Trim()), "Duplicate identity table field.");
		}
		Assert.AreEqual(MobileId, Fields.GetValueOrDefault("Id"), "The browser shows another identity than the phone.");
		Assert.AreEqual("Approved", Fields.GetValueOrDefault("State"), "The browser identity is not approved.");
		Assert.AreEqual(MobileId[(MobileId.IndexOf('@') + 1)..], Fields.GetValueOrDefault("Provider"), "The browser identity has another provider.");
		foreach (string Field in new[] { "Created", "From", "To", "Client Key Name", "Client Public Key", "Client Signature", "Server Signature" })
			Assert.IsFalse(string.IsNullOrWhiteSpace(Fields.GetValueOrDefault(Field)), "Missing browser identity field: " + Field);
	}

	private string Script(string Script) => Convert.ToString(((IJavaScriptExecutor)this.driver).ExecuteScript(Script), CultureInfo.InvariantCulture) ?? "";

	private string Wait(Func<string?> Read, string Description)
	{
		WebDriverWait Waiter = new WebDriverWait(this.driver, TimeSpan.FromSeconds(60));
		Waiter.PollingInterval = TimeSpan.FromMilliseconds(200);
		try { return Waiter.Until(_ => Read())!; }
		catch (WebDriverTimeoutException) { throw new AssertFailedException("Timed out waiting for " + Description + "."); }
	}

	/// <summary>Closes the browser session, including the live Quick Login connection.</summary>
	public void Dispose() => this.driver.Dispose();
}
