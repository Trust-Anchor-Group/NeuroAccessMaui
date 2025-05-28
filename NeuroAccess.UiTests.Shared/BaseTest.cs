using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Internal;
using OpenCvSharp;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Appium.Service;
using System.Diagnostics;
using OpenQA.Selenium.DevTools.V131.IndexedDB;
using System;


namespace NeuroAccess.UiTests
{
	[TestClass]
	public class BaseTest
	{
		private static bool firsTest = true;
		private static string lasestTestClassName = "";
		private static AndroidDriver? driver;
		public static AndroidDriver App => driver ?? throw new InvalidOperationException("AppiumDriver is not initialized.");

		public  BaseTest(bool reset, string className = "", bool skipFirstTestForceReset = false) {//If one wants the app to reopen/reset only once in a test class then one should put the classname of the test class that inherit this. If one wants otherwise then one just ignores the classname.
			
			if(className != lasestTestClassName || className == "") {
				if (firsTest && !skipFirstTestForceReset) {//If one wants to skip first test force reset then one should make the "skipFirstTestForceReset" to true
					ResetAndOpenApp();
					firsTest = false;
				} else if (reset) {
					ResetAndOpenApp();
				} else {
					CloseAndOpenApp();
				}
				//Because in MSTest the constructor of a test class gets called before each method, and not only once before all methods in the class, logic to reset the app or to just close and reopen the app just once in a test class had to be put in here.
				lasestTestClassName = className;
			}
		}
		// This will run after all the tests have happened
		[AssemblyCleanup]
		public static void RunAfterAllTests()
		{
			App.ExecuteScript("mobile: pressKey", new Dictionary<string, string> { { "keycode", "187" } });//187 is the code for recent apps/overview button
			Task.Delay(500).Wait();
			//Get the screen size to use it for getting the coordinates of the middle of the screen, so the swipe up happens from there.
			var screenSize = App.Manage().Window.Size;

			int startX = (screenSize.Width / 20) * 13;  // Middle of the screen but slightly to the right
			int startY = screenSize.Height / 2; // Half screen size down 

			Actions action = new Actions(App);

			action.MoveToLocation(startX, startY) // Start point
					.ClickAndHold()
					.MoveByOffset(0, 0 - startY - 1000) // Swipe up (fast swipe up to close the app). 0 - startY makes it swipe to the top edge and -1000 is for good measure so that the app doesn't have a chance to come back without getting closed
					.Release()
					.Perform();

			AppiumServerHelper.DisposeAppiumLocalServer();


		}
		public static void ResetAndOpenApp() {
			Console.WriteLine("Starting Appium Server...");
			AppiumServerHelper.StartAppiumLocalServer();

			Console.WriteLine("Configuring Appium Options...");
			var AndroidOptions = new AppiumOptions {
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

			//AndroidOptions.AddAdditionalAppiumOption("forceReset", true);
			AndroidOptions.AddAdditionalAppiumOption("noReset", true);//This is prioritized over forceReset. So if this is true, even if forceReset is true the app won't reset. If this is true and the app is open, it won't reopen.

			// Specifying the avd option will boot the emulator for you
			// make sure there is an emulator with the name below
			// If not specified, make sure you have an emulator booted
			//androidOptions.AddAdditionalAppiumOption("avd", "pixel_5_-_api_33");

			Console.WriteLine("Initializing Android Driver...");
			driver = new AndroidDriver(AndroidOptions);
			Console.WriteLine("Android Driver initialized successfully.");
		}
		public static void CloseAndOpenApp() {
			App.ExecuteScript("mobile: pressKey", new Dictionary<string, string> { { "keycode", "187" } });//187 is the code for recent apps/overview button
			Task.Delay(500).Wait();
			//Get the screen size to use it for getting the coordinates of the middle of the screen, so the swipe up happens from there.
			var screenSize = App.Manage().Window.Size;

			int startX = (screenSize.Width / 20) * 13;  // Middle of the screen but slightly to the right
			int startY = screenSize.Height / 2; // Half screen size down

			Actions action = new Actions(App);

			action.MoveToLocation(startX, startY) // Start point
					.ClickAndHold()
					.MoveByOffset(0, 0 - startY - 1000) // Swipe up (fast swipe up to close the app). 0 - startY makes it swipe to the top edge and -1000 is for good measure so that the app doesn't have a chance to come back without getting closed
					.Release()
					.Perform();

			Task.Delay(3000).Wait();//Wait some time to make sure the app is closed before launching it again
			App.ActivateApp("com.tag.NeuroAccess");
		}
		public static AppiumElement ScrollUpOrDownAndFindElement(string automationIDToFind,bool scrollUpNotDown,int howManyScreenSizesToScroll, SpecialActions? specialActions = null, int xPosistionToStartFrom = 10, int? yPositionToStartFrom = null) {
			if (howManyScreenSizesToScroll <= 0) {
				throw new Exception("You have to scroll by at least one screenSize");
			}
			if (App.IsKeyboardShown()) {
				App.HideKeyboard();
			}
			try {//Here it will be tried once for it to be fast if the view that's being searched for is already here.
				AppiumElement element = AutoFindElement(automationIDToFind, 0.5);
				return element;
			} catch {}
			int upOrDownMultiplier;
			if (scrollUpNotDown) {
				upOrDownMultiplier = 1;
			} else {
				upOrDownMultiplier = -1;
			}
			var screenSize = App.Manage().Window.Size;
			if (yPositionToStartFrom == null) {
				yPositionToStartFrom = screenSize.Height / 2;// starts by default from half the screen down.
			}
			if(specialActions == null) {
				specialActions = new SpecialActions(App);
			}
			while (howManyScreenSizesToScroll != 0) {
				specialActions.MoveByOffset(xPosistionToStartFrom, (int)yPositionToStartFrom, 0, CoordinateOrigin.Viewport)
					.ClickAndHold().MoveByOffset(0, (screenSize.Height / 10 * 9) * upOrDownMultiplier, 1500).Release().Perform();//Yes the method name says it's gonna scroll by full screen sizes but it's actally 9/10 of the screen size every round. And it's so that if it somehow happens that the scroll happens a bit too fast we can still find small elements that could have been skipped.
				try {
					AppiumElement element = AutoFindElement(automationIDToFind, 0.5);
					return element;
				} catch {
					howManyScreenSizesToScroll--;
				}
			}
			throw new Exception($"({automationIDToFind}) wasn't found after scrolling");
		}
		public static void TestComplexEntryByRandomCode(AppiumElement entryTotest, Actions? actions = null) {
			while (App.IsKeyboardShown()) {
				App.HideKeyboard();
			}
			if (actions == null) {
				actions = new Actions(App);
			}
			entryTotest.Click();
			string randomString = GetRandomString(10);//To know if the inputfield works and the user text input goes through, this random string is sent in the emulator and then searched for to see if it exists, essientially to if the inputfield works
			actions.SendKeys(randomString).Perform();
			AppiumElement changedElement = AutoCustomFindElement(() => FindElementByText(randomString), 1);
			for(int i = changedElement.Text.Length; i > 0; i--) {
				actions.SendKeys(Keys.Backspace).Perform();
			}
		}
		public static void DeleteText(int textLength, AppiumElement? anElement = null, Actions? actions = null) {
			while (App.IsKeyboardShown()) {
				App.HideKeyboard();
			}
			if(anElement != null) {
				anElement.Click();
			}

			if (actions == null) {
				actions = new Actions(App);
			}

			for (int i = textLength; i > 0; i--) {
				actions.SendKeys(Keys.Backspace).Perform();
			}
		}
		public static AppiumElement WriteInAndTestComplexEntry(string entryAutomationId, string whatToWrite, AppiumElement? entry = null, Actions? actions = null) {
			while (App.IsKeyboardShown()) {
				App.HideKeyboard();
			}
			if (actions == null) {
				actions = new Actions(App);
			}
			if (entry  == null) {
				entry = AutoFindElement(entryAutomationId);
			}
			entry.Click();

			var elementsWithTextThatShouldBeWrittenCount = App.FindElements(By.XPath($"//*[contains(@resource-id, '{entryAutomationId}')]//*[@text = '{whatToWrite}']")).Count;//The count of the elements that have the same text that will be written in the entry later.
			if(entry.Text == whatToWrite) {
				elementsWithTextThatShouldBeWrittenCount++;
			}
			//Now write in the entry
			actions.SendKeys(whatToWrite).Perform();
			//Now test if the entry worked and the text was wrritten in it
			int elementsWithTextAfterCount = App.FindElements(By.XPath($"//*[contains(@resource-id, '{entryAutomationId}')]//*[@text = '{whatToWrite}']")).Count;
			if (entry.Text == whatToWrite) {
				elementsWithTextAfterCount++;
			}
			if (elementsWithTextAfterCount > elementsWithTextThatShouldBeWrittenCount) {
				//If it's more it means the test succeeded
			} else if(elementsWithTextAfterCount == elementsWithTextThatShouldBeWrittenCount) {
				//If it's the same it means it didn't succeeded and most likely nothing was written
				throw new Exception($"Writing in the entry ({entryAutomationId}) didn't succeed.");
			} else if (elementsWithTextAfterCount < elementsWithTextThatShouldBeWrittenCount) {
				//If the count became less it means the entry had text in it from before that matched what was written now.
				throw new Exception($"the entry ({entryAutomationId}) had text from before that matched what was written now");
			}
			return entry;

		}
		public static AppiumElement CustomTestComplexEntry(string entryAutomationId, string whatTheTextShouldBeAfter, Action customWriteInEntry, AppiumElement? entry = null, Actions? actions = null) {
			while (App.IsKeyboardShown()) {
				App.HideKeyboard();
			}
			if (actions == null) {
				actions = new Actions(App);
			}
			if (entry == null) {
				entry = AutoFindElement(entryAutomationId);
			}
			var elementsWithTextThatShouldBeWrittenCount = App.FindElements(By.XPath($"//*[contains(@resource-id, '{entryAutomationId}')]//*[@text = '{whatTheTextShouldBeAfter}']")).Count;//The count of the elements that have the same text that will be written in the entry later.
			Console.WriteLine($"custom test: wtsba: {whatTheTextShouldBeAfter}. count before: {elementsWithTextThatShouldBeWrittenCount}");
			if (entry.Text == whatTheTextShouldBeAfter) {
				elementsWithTextThatShouldBeWrittenCount++;
				Console.WriteLine($"custom test: wtsba: {whatTheTextShouldBeAfter}. +1");
			}
			//Now the custom  in the entry
			customWriteInEntry();
			//Now test if the entry worked and the text was wrritten in it
			int elementsWithTextAfterCount = App.FindElements(By.XPath($"//*[contains(@resource-id, '{entryAutomationId}')]//*[@text = '{whatTheTextShouldBeAfter}']")).Count;
			Console.WriteLine($"custom test: wtsba: {whatTheTextShouldBeAfter}. count after: {elementsWithTextThatShouldBeWrittenCount}");
			if (entry.Text == whatTheTextShouldBeAfter) {
				elementsWithTextAfterCount++;
				Console.WriteLine($"custom test: wtsba: {whatTheTextShouldBeAfter}. +1");
			}
			if (elementsWithTextAfterCount > elementsWithTextThatShouldBeWrittenCount) {
				//If it's more it means the test succeeded
			} else if (elementsWithTextAfterCount == elementsWithTextThatShouldBeWrittenCount) {
				//If it's the same it means it didn't succeeded and most likely nothing was written
				throw new Exception($"Writing in the entry ({entryAutomationId}) didn't succeed.");
			} else if (elementsWithTextAfterCount < elementsWithTextThatShouldBeWrittenCount) {
				//If the count became less it means the entry had text in it from before that matched what was written now.
				throw new Exception($"the entry ({entryAutomationId}) had text from before that matched what was written now");
			}
			return entry;

		}

		public static string GetRandomString(int stringLength) {
			string allowedCharachters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()¤#";
			Random randomNumberGenerator = new Random();
			string result = "";

			for(int i = stringLength; i > 0; i--) {
				int randomAllowedCharachtersIndex = randomNumberGenerator.Next(allowedCharachters.Length);
				result += allowedCharachters[randomAllowedCharachtersIndex];
			}
			return result;
		}
		public static bool CustomCheckIfElementDisappeared(Func<AppiumElement> customFindElement, double maxTryTimeInS = 5) {
			while (App.IsKeyboardShown()) {
				App.HideKeyboard();
			}
			var stopWatch = new Stopwatch();
			while(maxTryTimeInS > 0) {//Checking happens untlil max try time is 0 or less, and it constantly gets decreased as time goes.
				try {
					stopWatch.Start();
					customFindElement();
					stopWatch.Stop();
					Console.WriteLine(stopWatch.ElapsedMilliseconds);
					maxTryTimeInS -= stopWatch.ElapsedMilliseconds / 1000;
					stopWatch.Reset();
				} catch {
					return true;
				}
			}
			try {//Here the element gets searched for one extra time because sometimes the customfindelement method takes too much time. So one thinks sufficient searching has been done but in reality maybe only once the element was searched for because the method takes a lot of time.
				customFindElement();
			} catch {
				return true;
			}
			return false;
		}
		public static AppiumElement ScrollUpOrDownAndCustomFindElement(Func<AppiumElement> customFindElement, bool scrollUpNotDown, int howManyScreenSizesToScroll, SpecialActions? specialActions = null, int xPosistionToStartFrom = 10, int? yPositionToStartFrom = null) {
			if (howManyScreenSizesToScroll <= 0) {
				throw new Exception("You have to scroll by at least one screenSize");
			}
			if (App.IsKeyboardShown()) {
				App.HideKeyboard();
			}
			try {//Here it will be tried once for it to be fast if the view that's being searched for is already here.
				AppiumElement element = AutoCustomFindElement(customFindElement, 0.5);
				return element;
			} catch { }
			int upOrDownMultiplier;
			if (scrollUpNotDown) {
				upOrDownMultiplier = 1;
			} else {
				upOrDownMultiplier = -1;
			}
			var screenSize = App.Manage().Window.Size;
			if (yPositionToStartFrom == null) {
				yPositionToStartFrom = screenSize.Height / 2;// starts by default from half the screen down.
			}
			if (specialActions == null) {
				specialActions = new SpecialActions(App);
			}
			while (howManyScreenSizesToScroll != 0) {
				specialActions.MoveByOffset(xPosistionToStartFrom, (int)yPositionToStartFrom, 0, CoordinateOrigin.Viewport)
					.ClickAndHold().MoveByOffset(0, (screenSize.Height / 10 * 9) * upOrDownMultiplier, 1500).Release().Perform();//Yes the method name says it's gonna scroll by full screen sizes but it's actally 9/10 of the screen size every round. And it's so that if it somehow happens that the scroll happens a bit too fast we can still find small elements that could have been skipped.
				try {
					AppiumElement element = AutoCustomFindElement(customFindElement, 0.5);
					return element;
				} catch {
					howManyScreenSizesToScroll--;
				}
			}
			throw new Exception($"The view which is being searched for by a custom method wasn't found after scrolling");
		}
		public static AppiumElement FindElementByTextContaintedInText(string textThatShouldBeFoundInAppiumElementTextAttribute) {
			return App.FindElement(By.XPath($"//*[contains(@text, '{textThatShouldBeFoundInAppiumElementTextAttribute}')]"));
		}
		public static AppiumElement FindElementByText(string whatTextToLookForInElements) {
			return App.FindElement(By.XPath($"//*[@text='{whatTextToLookForInElements}']"));
		}
		public static AppiumElement FindElementByAttribute(string theAttributeName, string whatValueToLookForInElements) {
			return App.FindElement(By.XPath($"//*[@{theAttributeName}='{whatValueToLookForInElements}']"));
		}
		public static AppiumElement FindElementByTextContainedInAttribute(string theAttributeName, string textThatShouldBeFoundInAppiumElementAttribute) {
			return App.FindElement(By.XPath($"//*[contains(@{theAttributeName}, '{textThatShouldBeFoundInAppiumElementAttribute}')]"));
		}
		public static void WriteInEmulator(string whatToWrite, AppiumElement? inputFieldToPress = null) {
			Actions actions = new Actions(App);
			if (inputFieldToPress == null) {
				actions.SendKeys(whatToWrite).Perform();
			} else {
				while (App.IsKeyboardShown()) {
					App.HideKeyboard();
				}
				inputFieldToPress.Click();
				actions.SendKeys(whatToWrite).Perform();
			}
			if (App.IsKeyboardShown()) {//The reason it's here also is because the keyboard causes a lot problems. By example one wants to find an element, and one finds it, but the keyboard is covering most of it, so if one tries to click on the element for a test one will instead click the keyboard and the test won't work.
				App.HideKeyboard();
			}

		}

		public static AppiumElement FindUIElement(string id)
		{
			if (App is WindowsDriver)
			{
				return App.FindElement(MobileBy.AccessibilityId(id));
			}

			return App.FindElement(MobileBy.Id(id));
		}
		public static AppiumElement AutoFindElement(string automationId, double maxTryTimeInS = 20)//Default max trying to the find the element time is 20 seconds. Because slower computers take more time loading things. And the time is not infinite because if it takes inifnity to find an element it means something went wrong, so instead if something takes more time than usual to load the devoloper can by himself change the max trying time when calling the method.
		{
			if (App.IsKeyboardShown()) {//The reason it's here in the beginning also is because the keyboard causes a lot problems. By example one wants to find an element, and one finds it, but the keyboard is covering most of it, so if one tries to click on the element for a test one will instead click the keyboard and the test won't work.
				App.HideKeyboard();
			}
			float timeTaken = 0;
			Stopwatch stopwatch = new Stopwatch();
			while (true) {
				stopwatch.Start();
				try {
					AppiumElement viewToReturn = FindUIElement(automationId);
					stopwatch.Stop();
					timeTaken += (float) stopwatch.ElapsedMilliseconds /1000;
					Console.WriteLine($"{timeTaken}s were taken to find the view with the Id: {automationId}");
					return viewToReturn;
				}
				catch (Exception ex) {
					stopwatch.Stop();
					timeTaken += (float) stopwatch.ElapsedMilliseconds / 1000;
					stopwatch.Reset();
					if (timeTaken >= maxTryTimeInS) {
						if (App.IsKeyboardShown()) {
							App.HideKeyboard();
						} else {
							stopwatch.Start();
							try {
								AppiumElement viewToReturn = FindUIElement(automationId);
								stopwatch.Stop();
								timeTaken += (float) stopwatch.ElapsedMilliseconds / 1000;
								Console.WriteLine($"{timeTaken}s were taken to find the view with the Id: {automationId}");
								return viewToReturn;
							} catch {
								stopwatch.Stop();
								throw new Exception($"the view ({automationId}) wasn't found after {timeTaken}s\nex: {ex}");
							}
						}
					} else
					{
						if (App.IsKeyboardShown()) {
							App.HideKeyboard();
						}
					}
				}
			}
		}
		public static AppiumElement AutoCustomFindElement(Func<AppiumElement> customFindElement, double maxTryTimeInS = 20)//Default max trying to the find the element time is 20 seconds. Because slower computers take more time loading things. And the time is not infinite because if it takes inifnity to find an element it means something went wrong, so instead if something takes more time than usual to load the devoloper can by himself change the max trying time when calling the method.
		{
			if (App.IsKeyboardShown()) {//The reason it's here in the beginning also is because the keyboard causes a lot problems. By example one wants to find an element, and one finds it, but the keyboard is covering most of it, so if one tries to click on the element for a test one will instead click the keyboard and the test won't work.
				App.HideKeyboard();
			}
			float timeTaken = 0;
			Stopwatch stopwatch = new Stopwatch();
			while (true) {
				stopwatch.Start();
				try {
					AppiumElement viewToReturn = customFindElement();
					stopwatch.Stop();
					timeTaken += (float) stopwatch.ElapsedMilliseconds / 1000;
					Console.WriteLine($"{timeTaken}s were taken to find a view with a custom method");
					return viewToReturn;
				} catch (Exception ex) {
					stopwatch.Stop();
					timeTaken += (float) stopwatch.ElapsedMilliseconds / 1000;
					stopwatch.Reset();
					if (timeTaken >= maxTryTimeInS) {
						if (App.IsKeyboardShown()) {
							App.HideKeyboard();
						} else {
							stopwatch.Start();
							try {
								AppiumElement viewToReturn = customFindElement();
								stopwatch.Stop();
								timeTaken += (float) stopwatch.ElapsedMilliseconds / 1000;
								Console.WriteLine($"{timeTaken}s were taken to find the view with a custom method");
								return viewToReturn;
							} catch {
								stopwatch.Stop();
								throw new Exception($"The view being searched for by a custom method wasn't found after {timeTaken}s\nex: {ex}");
							}
						}
					} else {
						if (App.IsKeyboardShown()) {
							App.HideKeyboard();
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


