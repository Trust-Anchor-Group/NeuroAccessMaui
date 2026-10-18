namespace NeuroAccessMaui.UITests.Infrastructure;

/// <summary>Opens device sessions for a test and closes them afterwards, saving a screenshot and page source on failure when enabled.</summary>
public abstract class BaseUiTest
{
	private readonly List<UiSession> sessions = new List<UiSession>();

	/// <summary>Gets or sets the current MSTest execution context and run parameters.</summary>
	public TestContext TestContext { get; set; } = null!;

	/// <summary>Skips device tests unless they are explicitly enabled for this run.</summary>
	[TestInitialize]
	public void Initialize()
	{
		new TestSettings(this.TestContext).RequireOptIn("ENABLED");
	}

	internal UiSession Open(string Role = "")
	{
		TestSettings Settings = new TestSettings(this.TestContext, Role);
		this.TestContext.WriteLine($"Opening {Settings.Platform} session ({(Role.Length == 0 ? "primary" : Role)}).");
		UiSession Session = new UiSession(Settings);
		this.sessions.Add(Session);
		if (Settings.Flag("RESET_BEFORE_TEST"))
			Session.ClearApp();
		else
			Session.ColdStart();
		return Session;
	}

	/// <summary>Saves failure artifacts when enabled and closes every session the test opened.</summary>
	[TestCleanup]
	public void Cleanup()
	{
		bool Failed = this.TestContext.CurrentTestOutcome != UnitTestOutcome.Passed;
		List<Exception> Errors = new List<Exception>();
		foreach (UiSession Session in this.sessions)
		{
			try
			{
				// Screenshots and page source can show personal data, so they are only saved when asked for.
				if (Failed && Session.Settings.Flag("CAPTURE_SENSITIVE_ARTIFACTS"))
					this.SaveArtifacts(Session);
			}
			catch (WebDriverException Error)
			{
				this.TestContext.WriteLine("Could not save failure artifacts: " + Error.Message);
			}
			try { Session.Dispose(); }
			catch (Exception Error) { Errors.Add(Error); }
		}
		this.sessions.Clear();
		if (Errors.Count > 0)
			throw new AggregateException("Could not close every device session.", Errors);
	}

	private void SaveArtifacts(UiSession Session)
	{
		string DirectoryName = this.TestContext.TestResultsDirectory ?? Path.GetTempPath();
		Directory.CreateDirectory(DirectoryName);
		string Prefix = Path.Combine(DirectoryName, Guid.NewGuid().ToString("N"));
		Session.Driver.GetScreenshot().SaveAsFile(Prefix + ".png");
		File.WriteAllText(Prefix + ".xml", Session.Driver.PageSource);
		this.TestContext.AddResultFile(Prefix + ".png");
		this.TestContext.AddResultFile(Prefix + ".xml");
	}
}
