using OpenQA.Selenium.Appium;
using NeuroAccessMaui.UITests.Infrastructure.Platforms;

namespace NeuroAccessMaui.UITests.Infrastructure;

/// <summary>Owns one driver and delegates native operations to its selected platform.</summary>
internal sealed class UiSession : IDisposable
{
	private bool disposed;

	internal UiSession(TestSettings Settings)
	{
		this.Settings = Settings;
		this.Text = new AppTranslations(Settings.Language);
		this.Platform = Settings.IsAndroid ? new AndroidPlatform() : new IosPlatform();
		this.Driver = AppiumDriverFactory.Create(Settings, this.Platform);
		this.Ui = new MobileUi(this);
		try { this.Driver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero; }
		catch
		{
			this.Driver.Dispose();
			throw;
		}
	}

	internal TestSettings Settings { get; }
	internal AppiumDriver Driver { get; }
	internal MobilePlatform Platform { get; }
	internal MobileUi Ui { get; }
	internal AppTranslations Text { get; set; }

	internal object Execute(string Command, Dictionary<string, object>? Arguments = null) => MobilePlatform.Execute(this.Driver, Command, Arguments);

	internal void ColdStart()
	{
		this.Platform.Terminate(this.Driver, this.Settings.AppId);
		this.Platform.Activate(this.Driver, this.Settings.AppId);
	}

	internal void SetClipboard(string Text) => this.Platform.SetClipboard(this.Driver, this.Settings, Text);
	internal string GetClipboard() => this.Platform.GetClipboard(this.Driver, this.Settings);

	internal void ClearApp()
	{
		this.Settings.RequireOptIn("ALLOW_RESET");
		if (!this.Settings.IsAndroid)
			Assert.IsTrue(this.Settings.Flag("IOS_SIMULATOR"), "Clean-state flows require a dedicated iOS simulator; physical-device keychains persist across reinstalls.");
		this.Platform.Terminate(this.Driver, this.Settings.AppId);
		this.Platform.ClearApp(this.Driver, this.Settings);
		this.Platform.Activate(this.Driver, this.Settings.AppId);
	}

	/// <summary>Stops the configured application and releases the owned automation session.</summary>
	public void Dispose()
	{
		if (this.disposed)
			return;
		this.disposed = true;
		try { this.Platform.Terminate(this.Driver, this.Settings.AppId); }
		finally { this.Driver.Dispose(); }
	}
}
