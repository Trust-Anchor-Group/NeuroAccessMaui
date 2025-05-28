using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Appium.Interfaces;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium;

namespace NeuroAccess.UiTests
{
	[TestClass]
    public class RegisterTest: BaseTest
    {

		[TestMethod]
		[TestCategory("Android")]
		public void tempTest() {
			Actions action = new Actions(App);

			//DefinePasswordView_CreatePasswordTextButton
			AppiumElement NotEnabledCreatePasswordButton = AutoCustomFindElement(() => App.FindElement(By.XPath("//*[contains(@resource-id, 'DefinePasswordView_CreatePasswordTextButton') and @enabled='false']")), 5);//The create password button shouldn't be enabled by default, but when a valid password is in place.
			AppiumElement passwordEntry = AutoFindElement("DefinePasswordView_PasswordCompositeEntry", 30);
			bool didAnErrorHappen = false;
			try {
				WriteInAndTestComplexEntry("DefinePasswordView_PasswordCompositeEntry", "123456", passwordEntry, action);
			} catch {
				didAnErrorHappen = true;//Because password entries text should be hidden by default an error should naturally happen and we should come here
			}
			if (didAnErrorHappen == false) {
				throw new Exception("Tha passwordEtnry's text should have been hidden by default");
			}
			CustomTestComplexEntry("DefinePasswordView_PasswordCompositeEntry", "123456", () => {//Now it should work and it should be detected that "123456" was written, because password visibility was toggled.
				AppiumElement togglePasswordVisibilityButton = AutoFindElement("DefinePasswordView_TogglePasswordVisibilityTemplatedButton");
				togglePasswordVisibilityButton.Click();
				Task.Delay(1000).Wait();
			});
			AppiumElement confirmPasswordEntry = AutoFindElement("DefinePasswordView_ConfirmPasswordCompositeEntry");
			WriteInEmulator("123456", confirmPasswordEntry);
			//The button shouldn't be enabled now because numbers that follow each others in value is not allowed as a password. By Example "1234"
			NotEnabledCreatePasswordButton = AutoCustomFindElement(() => App.FindElement(By.XPath("//*[contains(@resource-id, 'DefinePasswordView_CreatePasswordTextButton') and @enabled='false']")), 5);
			DeleteText("123456".Length, passwordEntry, action);
			DeleteText("123456".Length, confirmPasswordEntry, action);

			WriteInEmulator("135135", passwordEntry);
			WriteInEmulator("135135", confirmPasswordEntry);
			//The button should be enabled now because it's not numbers that follow each others in value.
			AppiumElement enabledCreatePasswordButton = AutoCustomFindElement(() => App.FindElement(By.XPath("//*[contains(@resource-id, 'DefinePasswordView_CreatePasswordTextButton') and @enabled='true']")), 5);//The button shouldn't be enabled now because numbers that follow each others in value is not allowed as a password. By Example "1234"
			DeleteText("135135".Length, passwordEntry, action);
			DeleteText("135135".Length, confirmPasswordEntry, action);

			AppiumElement togglePasswordTypeButton = AutoFindElement("DefinePasswordView_TogglePasswordTypeTemplatedButton", 30);//The max time the view will be tried to be located is 30s here not the usual 10s because this step usually takes longer and it varies in time.
			togglePasswordTypeButton.Click();

			WriteInEmulator("testtest", passwordEntry);
			WriteInEmulator("testtest", confirmPasswordEntry);
			//The button shouldn't be enabled now because there isn't a big letter nor a symbol like by example "1"
			NotEnabledCreatePasswordButton = AutoCustomFindElement(() => App.FindElement(By.XPath("//*[contains(@resource-id, 'DefinePasswordView_CreatePasswordTextButton') and @enabled='false']")), 5);//The button shouldn't be enabled now because numbers that follow each others in value is not allowed as a password. By Example "1234"
			DeleteText("testtest".Length, passwordEntry, action);
			DeleteText("testtest".Length, confirmPasswordEntry, action);

			WriteInEmulator("Test1", passwordEntry);
			WriteInEmulator("Test1", confirmPasswordEntry);
			//The button shouldn't be enabled now because there is 5 letters and minimum is 6
			NotEnabledCreatePasswordButton = AutoCustomFindElement(() => App.FindElement(By.XPath("//*[contains(@resource-id, 'DefinePasswordView_CreatePasswordTextButton') and @enabled='false']")), 5);//The button shouldn't be enabled now because numbers that follow each others in value is not allowed as a password. By Example "1234"
			DeleteText("Test1".Length, passwordEntry, action);
			DeleteText("Test1".Length, confirmPasswordEntry, action);

			WriteInEmulator("Test11", passwordEntry);
			WriteInEmulator("Test11", confirmPasswordEntry);
			enabledCreatePasswordButton = AutoCustomFindElement(() => App.FindElement(By.XPath("//*[contains(@resource-id, 'DefinePasswordView_CreatePasswordTextButton') and @enabled='true']")), 5);
			enabledCreatePasswordButton.Click();

		}
		private static bool reset = false;
		public RegisterTest() : base(reset, nameof(RegisterTest)) { }
		[TestMethod]
		[TestCategory("Android")]
		public async Task AllRegisteringProcess_Test()
		{
			Actions action = new Actions(App);
			var screenSize = App.Manage().Window.Size;
			////// First press create account button
			AppiumElement createAccountButton = AutoFindElement("GetStarted_CreateAccountButton");
			createAccountButton.Click();

			////// Now test go back function
			AppiumElement goBackButton = AutoFindElement("RegistrationPage_GoToPrevImageButton");
			goBackButton.Click();
			createAccountButton = AutoFindElement("GetStarted_CreateAccountButton");
			createAccountButton.Click();

			////// Then press select phone code button
			AppiumElement phoneCodeButton = AutoFindElement("ValidatePhoneView_SelectPhoneCodeLabel");
			if(phoneCodeButton.GetAttribute("text") != "46") {
				throw new Exception("phone code wasn't +46 by default");
			}
			phoneCodeButton.Click();

			////// Then test the scrolling function in the phone code popup
			AppiumElement belgiumCode = ScrollUpOrDownAndFindElement("BE", false, 30, new SpecialActions(App), screenSize.Width / 2);

			////// Then search for USA country code 
			AppiumElement phoneCodeSearchBar = AutoFindElement("SelectPhoneCodePopup_SearchBar");
			WriteInAndTestComplexEntry("SelectPhoneCodePopup_SearchBar", "United States", phoneCodeSearchBar, action);

			////// Then test delete search bar button 
			AppiumElement deleteSearchBarButton =  AutoFindElement("search_close_btn");
			deleteSearchBarButton.Click();
			if (!CustomCheckIfElementDisappeared(() => AutoFindElement("search_close_btn"), 5)) {
				throw new Exception("deleteSearchBarButton didn't work");
			}
			WriteInAndTestComplexEntry("SelectPhoneCodePopup_SearchBar", "United States", phoneCodeSearchBar, action);

			////// Then press code 1 USA country code button
			AppiumElement code1USA = AutoFindElement("US");//US stands for USA(Alpha 2 country code). This country's phone code is 1 which is what we need for the testing number. And only the USA country code works.
			code1USA.Click();
			phoneCodeButton = AutoFindElement("ValidatePhoneView_SelectPhoneCodeLabel");
			if(phoneCodeButton.GetAttribute("text") != "1") {
				throw new Exception("Phone code wasn't changed to +1 that belongs to USA");
			}

			////// Then put in the test number in the number entry
			AppiumElement numberEntry = AutoFindElement("ValidatePhoneView_NumberEntry");
			WriteInAndTestComplexEntry("ValidatePhoneView_NumberEntry", "555123123", numberEntry, action);

			////// Then press the sendCodeButton
			AppiumElement sendCodeButton = AutoFindElement("ValidatePhoneView_SendCodeTextButton");
			sendCodeButton.Click();

			////// Then get the verification code
			AutoFindElement("VerifyCodePage");//Make sure we navigated to VerifyCodePage
			string verificationCode = await this.GetVerificationCodeAsync();
			if(verificationCode.Length != 6) {
				throw new Exception("Getting verification code wasn't successfull");
			}

			////// Then test resend code button
			AppiumElement enabledResendCodeButton = AutoCustomFindElement(() => App.FindElement(By.XPath("//*[contains(@resource-id, 'VerifyCodePage_ResendCodeTextButton') and @enabled='true']")), 310);
			enabledResendCodeButton.Click();
			if (!CustomCheckIfElementDisappeared(() => App.FindElement(By.XPath("//*[contains(@resource-id, 'VerifyCodePage_ResendCodeTextButton') and @enabled='true']")), 5)) {
				throw new Exception("Resend code button worked, but it didn't get disabled again and the timer for it to work again didn't start");
			}

			////// Then get the verification code again
			verificationCode = await this.GetVerificationCodeAsync();
			if (verificationCode.Length != 6) {
				throw new Exception("Getting verification code wasn't successfull");
			}
			action.SendKeys(verificationCode).Perform();
			Task.Delay(500).Wait();

			////// Then click the verify code button
			AppiumElement verifyButton = AutoFindElement("VerifyCodePage_VerifyTextButton");
			verifyButton.Click();

			////// Then test if invalid charachters warning message and alternative name works in NameEntryView
			AppiumElement nickNameEntry = AutoFindElement("NameEntryView_NickNameEntry");
			bool didAnErrorHappen = false;
			try {
				AutoFindElement("NameEntryView_NameInvalidWarningLabel", 5);
				AutoFindElement("NameEntryView_AlternativeNameLabel", 5);
			} catch {
				didAnErrorHappen = true;
				// Reaching here means the warning message doesn't show by default which is what's intented
			}
			if (!didAnErrorHappen) {
				throw new Exception("Invalid name warning message was showing by default");
			}
			WriteInAndTestComplexEntry("NameEntryView_NickNameEntry", "G<", nickNameEntry);
			AutoFindElement("NameEntryView_NameInvalidWarningLabel", 5);
			AppiumElement alternativeNameLabel = AutoFindElement("NameEntryView_AlternativeNameLabel", 5);
			AppiumElement NotEnabledContinruButton = AutoCustomFindElement(() => App.FindElement(By.XPath("//*[contains(@resource-id, 'NameEntryView_ContinueTextButton') and @enabled='false']")), 310);//By searching for the not enabled continue button, the feature where one cannot contineu unless one chooses a valid name is tested.
			string alternativeName = alternativeNameLabel.Text;
			CustomTestComplexEntry("NameEntryView_NickNameEntry", alternativeName, () => {//Here it's tested if clicking on alternative name works and nicknameentry text becomes it.
				alternativeNameLabel.Click();
			}, nickNameEntry, action);
			DeleteText(alternativeName.Length, nickNameEntry, action);


			////// Then put in a name in the NameEntryView
			DateTime now = DateTime.Now;
			string testRoundUserName = $"TestUser{verificationCode}y{now.Year}m{now.Month}d{now.Day}h{now.Hour}m{now.Minute}s{now.Second}";
			WriteInAndTestComplexEntry("NameEntryView_NickNameEntry", testRoundUserName, nickNameEntry, action);// Maybe an error happens if I try to put the same username every time, so for a unique id I will use the verification code and date for now.
			////// Then press the continue button
			AppiumElement continueButton = AutoFindElement("NameEntryView_ContinueTextButton");
			continueButton.Click();
			////// then put the password in first entry
			//DefinePasswordView_CreatePasswordTextButton
			AppiumElement NotEnabledCreatePasswordButton = AutoCustomFindElement(() => App.FindElement(By.XPath("//*[contains(@resource-id, 'DefinePasswordView_CreatePasswordTextButton') and @enabled='false']")), 20);//The create password button shouldn't be enabled by default, but when a valid password is in place.
			AppiumElement passwordEntry = AutoFindElement("DefinePasswordView_PasswordCompositeEntry", 30);
			didAnErrorHappen = false;
			try {
				WriteInAndTestComplexEntry("DefinePasswordView_PasswordCompositeEntry", "123456", passwordEntry, action);
			} catch {
				didAnErrorHappen = true;//Because password entries text should be hidden by default an error should naturally happen and we should come here
			}
			if (didAnErrorHappen == false) {
				throw new Exception("Tha passwordEtnry's text should have been hidden by default");
			}
			CustomTestComplexEntry("DefinePasswordView_PasswordCompositeEntry", "123456", () => {//Now it should work and it should be detected that "123456" was written, because password visibility was toggled.
				AppiumElement togglePasswordVisibilityButton = AutoFindElement("DefinePasswordView_TogglePasswordVisibilityTemplatedButton");
				togglePasswordVisibilityButton.Click();
				Task.Delay(1000).Wait();
			});
			AppiumElement confirmPasswordEntry = AutoFindElement("DefinePasswordView_ConfirmPasswordCompositeEntry");
			WriteInEmulator("123456", confirmPasswordEntry);
			//The button shouldn't be enabled now because numbers that follow each others in value is not allowed as a password. By Example "1234"
			NotEnabledCreatePasswordButton = AutoCustomFindElement(() => App.FindElement(By.XPath("//*[contains(@resource-id, 'DefinePasswordView_CreatePasswordTextButton') and @enabled='false']")), 5);
			DeleteText("123456".Length, passwordEntry, action);
			DeleteText("123456".Length, confirmPasswordEntry, action);

			WriteInEmulator("135135", passwordEntry);
			WriteInEmulator("135135", confirmPasswordEntry);
			//The button should be enabled now because it's not numbers that follow each others in value.
			AppiumElement enabledCreatePasswordButton = AutoCustomFindElement(() => App.FindElement(By.XPath("//*[contains(@resource-id, 'DefinePasswordView_CreatePasswordTextButton') and @enabled='true']")), 5);//The button shouldn't be enabled now because numbers that follow each others in value is not allowed as a password. By Example "1234"
			DeleteText("135135".Length, passwordEntry, action);
			DeleteText("135135".Length, confirmPasswordEntry, action);

			AppiumElement togglePasswordTypeButton = AutoFindElement("DefinePasswordView_TogglePasswordTypeTemplatedButton", 30);//The max time the view will be tried to be located is 30s here not the usual 10s because this step usually takes longer and it varies in time.
			togglePasswordTypeButton.Click();

			WriteInEmulator("testtest", passwordEntry);
			WriteInEmulator("testtest", confirmPasswordEntry);
			//The button shouldn't be enabled now because there isn't a big letter nor a symbol like by example "1"
			NotEnabledCreatePasswordButton = AutoCustomFindElement(() => App.FindElement(By.XPath("//*[contains(@resource-id, 'DefinePasswordView_CreatePasswordTextButton') and @enabled='false']")), 5);//The button shouldn't be enabled now because numbers that follow each others in value is not allowed as a password. By Example "1234"
			DeleteText("testtest".Length, passwordEntry, action);
			DeleteText("testtest".Length, confirmPasswordEntry, action);

			WriteInEmulator("Test1", passwordEntry);
			WriteInEmulator("Test1", confirmPasswordEntry);
			//The button shouldn't be enabled now because there is 5 letters and minimum is 6
			NotEnabledCreatePasswordButton = AutoCustomFindElement(() => App.FindElement(By.XPath("//*[contains(@resource-id, 'DefinePasswordView_CreatePasswordTextButton') and @enabled='false']")), 5);//The button shouldn't be enabled now because numbers that follow each others in value is not allowed as a password. By Example "1234"
			DeleteText("Test1".Length, passwordEntry, action);
			DeleteText("Test1".Length, confirmPasswordEntry, action);

			WriteInEmulator("Test11", passwordEntry);
			WriteInEmulator("Test11", confirmPasswordEntry);
			//The button should work because it's 6 charachters and it has a big letter and a symbol.
			enabledCreatePasswordButton = AutoCustomFindElement(() => App.FindElement(By.XPath("//*[contains(@resource-id, 'DefinePasswordView_CreatePasswordTextButton') and @enabled='true']")), 5);
			enabledCreatePasswordButton.Click();

			Task.Delay(1000).Wait();// Wait a second to make sure it registered.
		}

		public async Task<string> GetVerificationCodeAsync()
		{
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
						return verificationCode;
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
			return "";
		}

		////// A method to test the FindImagePosition method, which takes an image then scans it around a bigger image to find where it's most similar
		//[TestMethod]
		//[TestCategory("Android")]
		//public void ValidatePhone_Test()
		//{
		//	//First click the phoneCodeButton, then let FindImagePosition find the 1 code
		//AppiumElement phoneCodeButton = FindUIElement("ValidatePhoneView_SelectPhoneCodeHorizontalStackLayout");
		//phoneCodeButton.Click();
		//Task.Delay(1000).Wait();

		//	string afterPhoneCodeClickImagePath = "C:\\My Projects\\NeuroAccessMaui\\NeuroAccess.UiTests.Shared\\ImagesForTests\\afterPhoneCodeClick.png";
		//	string code1AmericanSamoaButtonImagePath = "C:\\My Projects\\NeuroAccessMaui\\NeuroAccess.UiTests.Shared\\ImagesForTests\\code1AmericanSamoaButton.png"; // Reference imag
		//	// Find the button's position
		//	(int x, int y) = FindImagePosition(afterPhoneCodeClickImagePath, code1AmericanSamoaButtonImagePath);
		//	Console.WriteLine($"Button found at: {x}, {y}");
		//	Task.Delay(1000).Wait();

		//	// Tap on the detected position
		//	base.TapAtPosition(x, y);
		//	Task.Delay(1000).Wait();

		//	//type in the number for testing
		//	AppiumElement numberEntry = FindUIElement("ValidatePhoneView_NumberEntry");
		//	numberEntry.SendKeys("555123123");
		//	Task.Delay(1000).Wait();

		//	//Check if everything went according to plan, by comparing image to screenshot
		//	string afterEverythingDoneScreenshotPath = "finishedScreenShot.png";
		//	string afterNumberEnteredTemplateImagePath = "C:\\My Projects\\NeuroAccessMaui\\NeuroAccess.UiTests.Shared\\ImagesForTests\\afterNumberEnteredTemplate.png";
		//	base.CaptureScreenshot(afterEverythingDoneScreenshotPath);
		//	Task.Delay(1000).Wait();
		//	double maxSimilarityFound = CheckIfImageExistsInImage(afterEverythingDoneScreenshotPath, afterNumberEnteredTemplateImagePath);
		//	Console.WriteLine("max sim: " + maxSimilarityFound);
		//	if (maxSimilarityFound < 0.8)
		//	{
		//		Assert.Fail("Succesfull template image did not match with after tests screenshots");
		//	}

		//}

	}
}
