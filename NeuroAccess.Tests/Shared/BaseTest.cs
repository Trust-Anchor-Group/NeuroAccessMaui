using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;

namespace NeuroAccess.UiTests
{
	[TestClass]
	public class BaseTest
	{
		private static AppiumDriver? driver;

		public static AppiumDriver App => driver ?? throw new InvalidOperationException("AppiumDriver is not initialized.");

		// This will run once before all tests in the derived classes
		[AssemblyInitialize]
		public static void RunBeforeAnyTests(TestContext context)
		{
			// If you started an Appium server manually, make sure to comment out the next line
			// This line starts a local Appium server for you as part of the test run
			AppiumServerHelper.StartAppiumLocalServer();

			var androidOptions = new AppiumOptions
			{
				// Specify UIAutomator2 as the driver, typically don't need to change this
				AutomationName = "UIAutomator2",
				// Always Android for Android
				PlatformName = "Android",
				// This is the Android version, not API level
				// This is ignored if you use the avd option below
				PlatformVersion = "14",
				// The full path to the .apk file to test or the package name if the app is already installed on the device
				App = "com.tag.NeuroAccess.apk",
			};
			driver = new AndroidDriver(androidOptions);
		
		}

		// This will run before each test in the derived classes
		[TestInitialize]
		public void SetUp()
		{
			Console.WriteLine("Setting up for test...");
			// Initialize any necessary objects, mock services, or test-specific setup here
		}

		// This will run after each test in the derived classes
		[TestCleanup]
		public void TearDown()
		{
			Console.WriteLine("Tearing down after test...");
			// Perform any cleanup for each individual test here
		}

		// This will run once after all tests in the derived classes
		[AssemblyCleanup]
		public static void RunAfterAnyTests()
		{
			driver?.Quit();
			driver?.Dispose();

			// If an Appium server was started locally above, make sure we clean it up here
			AppiumServerHelper.DisposeAppiumLocalServer();
		}
	}
}
