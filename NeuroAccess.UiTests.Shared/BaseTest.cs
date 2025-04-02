using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Internal;
using OpenCvSharp;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Appium.Service;


namespace NeuroAccess.UiTests
{
	[TestClass]
	public class BaseTest
	{
		public static bool Reset = true;//Must be true here so that the app resets before the first test. Can be manually made to false after a test so that the app will only close and reopen instead of completly resetting
		private static AppiumDriver? driver;

		public static AppiumDriver App => driver ?? throw new InvalidOperationException("AppiumDriver is not initialized.");

		// This will run once before each test class
		[ClassInitialize(InheritanceBehavior.BeforeEachDerivedClass)]
		public static void RunBeforeAnyTests(TestContext context)
		{
			try
			{
				if (Reset)
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
						PlatformVersion = "14",
						// The full path to the .apk file to test or the package name if the app is already installed on the device
						App = "com.tag.NeuroAccess.apk",
					};

					AndroidOptions.AddAdditionalAppiumOption("forceReset", true);
					//AndroidOptions.AddAdditionalAppiumOption("noReset", true);//This is prioritized over forceReset. So if this is true, even if forceReset is true the app won't reset. If this is true and the app is open, it won't reopen.
					Console.WriteLine("Initializing Android Driver...");
					driver = new AndroidDriver(AndroidOptions);
					Console.WriteLine("Android Driver initialized successfully.");
				}
				else
				{
					var AndroidOptions = new AppiumOptions { AutomationName = "UIAutomator2", PlatformName = "Android", PlatformVersion = "14", App = "com.tag.NeuroAccess.apk" };
					AndroidOptions.AddAdditionalAppiumOption("noReset", true);// The app doesnt reset because this is true

					driver.ExecuteScript("mobile: pressKey", new Dictionary<string, string> { { "keycode", "187" } });//187 is the code for recent apps/overview button
					Task.Delay(500).Wait();
					//Get the screen size to use it for getting the coordinates of the middle of the screen, so the swipe up happens from there.
					var screenSize = driver.Manage().Window.Size;

					int startX = (screenSize.Width / 20) * 13;  // Middle of the screen but slightly to the right
					int startY = screenSize.Height / 2; // Half screen size down

					Actions action = new Actions(driver);

					action.MoveToLocation(startX, startY) // Start point
							.ClickAndHold()
							.MoveByOffset(0, 0 - startY - 1000) // Swipe up (fast swipe up to close the app). 0 - startY makes it swipe to the top edge and -1000 is for good measure so that the app doesn't have a chance to come back without getting closed
						   .Release()
							.Release()
							.Perform();				 
					

					Task.Delay(3000).Wait();//Wait some time to make sure the app is closed before launching it again
					driver = new AndroidDriver(AndroidOptions);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Exception in RunBeforeAnyTests: {ex.Message}");
				throw;
			}
		}

		// This will run after all the tests have happened
		[AssemblyCleanup]
		public static void RunAfterAllTests()
		{
			if (driver != null)
			{
				driver.ExecuteScript("mobile: pressKey", new Dictionary<string, string> { { "keycode", "187" } });//187 is the code for recent apps/overview button
				Task.Delay(500).Wait();
				//Get the screen size to use it for getting the coordinates of the middle of the screen, so the swipe up happens from there.
				var screenSize = driver.Manage().Window.Size;

				int startX = (screenSize.Width / 20) * 13;  // Middle of the screen but slightly to the right
				int startY = screenSize.Height / 2; // Half screen size down 

				Actions action = new Actions(driver);

				action.MoveToLocation(startX, startY) // Start point
						.ClickAndHold()
						.MoveByOffset(0, 0 - startY - 1000) // Swipe up (fast swipe up to close the app). 0 - startY makes it swipe to the top edge and -1000 is for good measure so that the app doesn't have a chance to come back without getting closed
						.Release()
						.Perform();

			}

		}
		public static AppiumElement ScrollUpOrDownAndFindElement(string automationIDToFind,bool scrollUpNotDown,int howManyScreenSizesToScroll, int xPosistionToStartFrom = 10, int? yPositionToStartFrom = null) {
			if (driver != null) {
				if (howManyScreenSizesToScroll <= 0) {
					throw new Exception("You have to scroll by at least one screenSize");
				}
				SpecialActions specialActions = new SpecialActions(driver);
				int upOrDownMultiplier;
				if (scrollUpNotDown) {
					upOrDownMultiplier = 1;
				} else {
					upOrDownMultiplier = -1;
				}
				var screenSize = driver.Manage().Window.Size;
				if (yPositionToStartFrom == null) {
					yPositionToStartFrom = screenSize.Height / 2;// starts by default from half the screen down.
				}
				while (howManyScreenSizesToScroll != 0) {
					specialActions.MoveByOffset(xPosistionToStartFrom, (int)yPositionToStartFrom, 0, CoordinateOrigin.Viewport)
						.ClickAndHold().MoveByOffset(0, screenSize.Height * upOrDownMultiplier, 100).Release().Perform();
					try {
						AppiumElement element = AutoFindElement(automationIDToFind, 0.5);
						return element;
					} catch {
						howManyScreenSizesToScroll --;
						if(howManyScreenSizesToScroll == 0) {
							throw new Exception("Appium element wasn't found after scrolling");
						}
					}
				}
			} else {
				throw new Exception("Driver is null");
			}
			return null;
		}
		public static AppiumElement FindUIElement(string id)
		{
			if (App is WindowsDriver)
			{
				return App.FindElement(MobileBy.AccessibilityId(id));
			}

			return App.FindElement(MobileBy.Id(id));
		}
		public static AppiumElement AutoFindElement(string aViewID, double maxTryTimeInS = 20)//Default max trying to the find the element time is 20 seconds. Because slower computers take more time loading things. And the time is not infinite because if it takes inifnity to find an element it means something went wrong, so instead if something takes more time than usual to load the devoloper can by himself change the max trying time when calling the method.
		{
			double timeTaken = 0;//In seconds not milli seconds
			while (true)
			{
				try
				{
					Task.Delay(50).Wait();
					timeTaken += 0.05;
					AppiumElement viewToReturn = FindUIElement(aViewID);
					return viewToReturn;
				}
				catch (Exception ex)
				{
					if (timeTaken > maxTryTimeInS)
					{
						throw new Exception($"{aViewID} wasn't found after {timeTaken}s\nex: {ex}");
					} else
					{
						if(driver != null) {
							if (driver.IsKeyboardShown()) {
								driver.HideKeyboard();
							}
						}
					}
				}
			}
		}
		public static (int X, int Y) FindImagePosition(string mainImagePath, string templateImagePath)
		{
			using Mat mainImage = Cv2.ImRead(mainImagePath, ImreadModes.Grayscale);
			using Mat template = Cv2.ImRead(templateImagePath, ImreadModes.Grayscale);
			using Mat result = new Mat();
			if (mainImage.Empty() || template.Empty())
			{
				throw new Exception("Error: One or both images could not be loaded. Check file paths.");
			}

			// See how much the template matches all the places in the main image
			Cv2.MatchTemplate(mainImage, template, result, TemplateMatchModes.CCoeffNormed);//The comparing method ccoeffNormed is best overall, but bad if the thing one is trying to find changes scale every time.

			// Find the best matching position, maxLoc is the location where it's most similar, maxValue is the max similarity value found by comparing the template(The thing one is searching for) to all places in the mainImage
			Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

			// if max similarity is less that 0.8 it throws an exeption, since most likely it did not find it then.
			if (maxVal < 0.8)
			{
				throw new Exception("Image not found with high confidence.");
			}

			// Return center of detected image
			return (maxLoc.X + template.Width / 2, maxLoc.Y + template.Height / 2);
		}
		public void CaptureScreenshot(string savePath)
		{
			Screenshot screenshot = App.GetScreenshot();
			screenshot.SaveAsFile(savePath);
		}

		public void TapAtPosition(int x, int y)
		{
			PointerInputDevice action = new PointerInputDevice(PointerKind.Touch);
			ActionSequence actions = new ActionSequence(action);

			actions.AddAction(action.CreatePointerMove(CoordinateOrigin.Viewport, x, y, TimeSpan.Zero));
			actions.AddAction(action.CreatePointerDown(MouseButton.Left));
			actions.AddAction(action.CreatePointerUp(MouseButton.Left));

			App.PerformActions(new List<ActionSequence> { actions });

		}

		public static double CheckIfImageExistsInImage(string mainImagePath, string templateImagePath)
		{
			using Mat mainImage = Cv2.ImRead(mainImagePath, ImreadModes.Grayscale);
			using Mat template = Cv2.ImRead(templateImagePath, ImreadModes.Grayscale);
			using Mat result = new Mat();
			if (mainImage.Empty() || template.Empty())
			{
				throw new Exception("Error: One or both images could not be loaded. Check file paths.");
			}
			// See how much the template matches all the places in the main image
			Cv2.MatchTemplate(mainImage, template, result, TemplateMatchModes.CCoeffNormed);

			// Find the best match position, maxLoc is the location where it's most similar, maxValue is the max similarity value found by comparing the template(The thing one is searching for) to all places in the mainImage
			Cv2.MinMaxLoc(result, out _, out double maxSimilarityFound, out _, out _);

			return maxSimilarityFound;
		}

	}
}

/*
THE CODE FOR GETTING VERIFICATION FOR TEST PHONE NUMBER

			// Create a CookieContainer to manage cookies automatically
			CookieContainer cookieContainer = new();

			// Configure HttpClientHandler to use the CookieContainer and to follow redirects automatically
			HttpClientHandler handler = new()
			{
				CookieContainer = cookieContainer,
				AllowAutoRedirect = true // Automatically follows HTTP 3xx redirects
			};

			using (HttpClient client = new(handler))
			{
				// Set the base address (optional)
				client.BaseAddress = new Uri("https://id.tagroot.io");

				// Mimic some common headers that a browser might send
				client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
				client.DefaultRequestHeaders.UserAgent.ParseAdd("NeuroAccess/1.0 (.NET MAUI UITest)");

				client.DefaultRequestHeaders.Referrer = new Uri("https://id.tagroot.io/TestOTP.md");
				client.DefaultRequestHeaders.Add("Origin", "https://id.tagroot.io");

				// Prepare the form data. Adjust the field name and value as required.
				Dictionary<string, string> formData = new()
				{
					{ "PhoneNr", "+1555123123" }  
            };
				FormUrlEncodedContent content = new(formData);

				try
				{
					// Send a POST request. Since AllowAutoRedirect is true, the client will follow the 303 redirect.
					HttpResponseMessage response = await client.PostAsync("/TestOTP.md", content);
					response.EnsureSuccessStatusCode();

					// Read the final response content after redirects
					string htmlContent = await response.Content.ReadAsStringAsync();

					Console.WriteLine("Response content: " + htmlContent);

					// For demonstration, suppose the verification code appears in the page like "Verification Code: 123456".
					// You can use a regex (or an HTML parser like HtmlAgilityPack) to extract the code.
					Match codeMatch = Regex.Match(htmlContent, @"\<strong\>(?'Code'\d{6})\<\/strong\>", RegexOptions.IgnoreCase);
					if (codeMatch.Success)
					{
						string verificationCode = codeMatch.Groups[1].Value;
						Console.WriteLine("Verification Code: " + verificationCode);
					}
					else
					{
						Console.WriteLine("Verification code not found in the response.");
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine("An error occurred: " + ex.Message);
				}
			}
*/


