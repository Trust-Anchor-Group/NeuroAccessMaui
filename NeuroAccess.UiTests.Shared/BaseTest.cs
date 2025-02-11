using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Internal;

namespace NeuroAccess.UiTests
{
	[TestClass]
	public class BaseTest
	{
		private static AppiumDriver? driver;

		public static AppiumDriver App => driver ?? throw new InvalidOperationException("AppiumDriver is not initialized.");

		// This will run once before all tests in the derived classes
		[ClassInitialize(InheritanceBehavior.BeforeEachDerivedClass)]
		public static void RunBeforeAnyTests(TestContext context)
		{
			try
			{
				Console.WriteLine("Starting Appium Server...");
				AppiumServerHelper.StartAppiumLocalServer();

				Console.WriteLine("Configuring Appium Options...");
				var AndroidOptions = new AppiumOptions
				{
					// Specify UIAutomator2 as the driver, typically don't need to change this
					AutomationName = "UIAutomator2",
					// Always Android for Android
					PlatformName = "Android",
					// This is the Android version, not API level
					// This is ignored if you use the avd option below
				//	PlatformVersion = "14",
					// The full path to the .apk file to test or the package name if the app is already installed on the device
					//App = "com.tag.NeuroAccess-Signed.apk",
				};


				AndroidOptions.AddAdditionalAppiumOption("forceReset", true);
				//AndroidOptions.AddAdditionalAppiumOption("noReset", true);
				Console.WriteLine("Initializing Android Driver...");
				driver = new AndroidDriver(AndroidOptions);

				Console.WriteLine("Android Driver initialized successfully.");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Exception in RunBeforeAnyTests: {ex.Message}");
				throw;
			}
		}

		// This will run once after all tests in the derived classes
		[ClassCleanup(InheritanceBehavior.BeforeEachDerivedClass)]
		public static void RunAfterAnyTests()
		{
			driver?.Quit();
			driver?.Dispose();

			// If an Appium server was started locally above, make sure we clean it up here
			AppiumServerHelper.DisposeAppiumLocalServer();
		}
	}
}
