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

namespace NeuroAccess.UiTests
{
	[TestClass]
    public class RegisterTest: BaseTest
    {
		[TestMethod]
		[TestCategory("Android")]
		public async Task AllRegisteringProcess_Test()
		{
			string problemViewID = "";//Before every FindUIElement, this will become the id that will be tried to be located. This way in the fail message, it can be put there to know which view wasn't located, or after which view the eror happened, because other errors can happen. This is especially good since i don't have to code a try-catch for every find FindUIElement method call to know which view wasn't found.
			try
			{
				////// First press create account button
				string createAccountButtonID = "GetStarted_CreateAccountButton";
				problemViewID = createAccountButtonID;

				AppiumElement createAccountButton = FindUIElement(createAccountButtonID);
				createAccountButton.Click();
				Task.Delay(1000).Wait();//There are delays like this in the code so that there is enough time for the navigation to the other view. If you do it directly then it's likely an error will happen if you try to find something in the next page because we aren't there yet
				////// Then press select phone code button
				string phoneCodeButtonID = "ValidatePhoneView_SelectPhoneCodeHorizontalStackLayout";
				problemViewID = phoneCodeButtonID;

				AppiumElement phoneCodeButton = FindUIElement(phoneCodeButtonID);
				phoneCodeButton.Click();
				Task.Delay(2000).Wait();
				////// Then search for USA country code 
				string phoneCodeSearchBarID = "SelectPhoneCodePopup_SearchBar";
				problemViewID = phoneCodeSearchBarID;

				AppiumElement phoneCodeSearchBar = FindUIElement(phoneCodeSearchBarID);
				phoneCodeSearchBar.Click();
				Task.Delay(500).Wait();
				phoneCodeSearchBar.SendKeys("United States");
				Task.Delay(2000).Wait();
				////// Then press code 1 USA country code button
				string code1USAID = "US";//US stands for USA(Alpha 2 country code). This country's phone code is 1 which is what we need for the testing number. And only the USA country code works.
				problemViewID = code1USAID;

				AppiumElement code1USA = FindUIElement(code1USAID);
				code1USA.Click();
				Task.Delay(1000).Wait();
				////// Then put in the test number in the number entry
				string numberEntryID = "ValidatePhoneView_NumberEntry";
				problemViewID = numberEntryID;

				AppiumElement numberEntry = FindUIElement(numberEntryID);
				numberEntry.Click();
				string testNumber = "555123123";
				Actions action = new Actions(App);//I'am using action.SendKeys because the usual numberEntry.sendKeys doesn't behave like a real user. And it looks like the send code button is pressable only when the input is like of that of a real user
				foreach (char number in testNumber) {
					action.SendKeys(number.ToString()).Perform();//this is in a for loop because, just like a real user would press one key at a time, the code needs to do the same or the send code button doesn't become pressable
					Task.Delay(100).Wait();
				}
				Task.Delay(1000).Wait();
				////// Then press the sendCodeButton
				string sendCodeButtonID = "ValidatePhoneView_SendCodeTextButton";
				problemViewID = sendCodeButtonID;

				AppiumElement sendCodeButton = FindUIElement(sendCodeButtonID);
				sendCodeButton.Click();
				Task.Delay(4000).Wait();
				////// Then put in the verification code
				string verificationCode = await this.GetVerificationCodeAsync();
				Task.Delay(1000).Wait();
				action.SendKeys(verificationCode).Perform();
				Task.Delay(500).Wait();
				////// Then click the verify code button
				string verifyButtonID = "VerifyCodePage_VerifyTextButton";
				problemViewID = verifyButtonID;

				AppiumElement verifyButton = FindUIElement(verifyButtonID);
				verifyButton.Click();
				Task.Delay(1000).Wait();
				////// Then put in a name in the NameEntryView
				string nickNameEntryID = "NameEntryView_NickNameEntry";
				problemViewID = nickNameEntryID;

				AppiumElement nickNameEntry = FindUIElement(nickNameEntryID);
				nickNameEntry.Click();
				Task.Delay(1000).Wait();
				DateTime now = DateTime.Now;
				string testRoundUserName = $"TestUser{verificationCode}y{now.Year}m{now.Month}d{now.Day}h{now.Hour}m{now.Minute}s{now.Second}";
				action.SendKeys(testRoundUserName).Perform();// Maybe an error happens if I try to put the same username every time, so for a unique id I will use the verification code and date for now.
				Task.Delay(1000).Wait();
				////// Then press the continue button
				string continueButtonID = "NameEntryView_ContinueTextButton";
				problemViewID = continueButtonID;

				AppiumElement continueButton = FindUIElement(continueButtonID);
				continueButton.Click();
				Task.Delay(20000).Wait();//The delay here is longer because after this step the "Creating account" loading screen appears
				////// Then press the toggle password type button to write alphapetic password
				string togglePasswordTypeButtonID = "DefinePasswordView_TogglePasswordTypeTemplatedButton";
				problemViewID = togglePasswordTypeButtonID;

				AppiumElement togglePasswordTypeButton = FindUIElement(togglePasswordTypeButtonID);
				togglePasswordTypeButton.Click();
				Task.Delay(2000).Wait();
				////// then put the password in first entry
				string passwordEntryID = "DefinePasswordView_PasswordCompositeEntry";
				problemViewID = passwordEntryID;

				AppiumElement passwordEntry = FindUIElement(passwordEntryID);
				passwordEntry.Click();
				Task.Delay(500).Wait();
				foreach (char letter in testRoundUserName)
				{
					action.SendKeys(letter.ToString()).Perform();
					Task.Delay(100).Wait();
				}//The username and password are the same because it's simpler this way, and if you try to log in later you will know the password by just knowing the user name
				Task.Delay(1000).Wait();
				////// then confirm the password in the second entry
				string confirmPasswordEntryID = "DefinePasswordView_ConfirmPasswordCompositeEntry";
				problemViewID = confirmPasswordEntryID;

				AppiumElement confirmPasswordEntry = FindUIElement(confirmPasswordEntryID);
				confirmPasswordEntry.Click();
				Task.Delay(500).Wait();
				foreach (char letter in testRoundUserName)
				{
					action.SendKeys(letter.ToString()).Perform();
					Task.Delay(100).Wait();
				}
				Task.Delay(3000).Wait();
				////// Then press the create password button
				string createPasswordButtonID = "DefinePasswordView_CreatePasswordTextButton";
				problemViewID = createPasswordButtonID;

				AppiumElement createPasswordButton = FindUIElement(createPasswordButtonID);
				createPasswordButton.Click();
				Task.Delay(200000).Wait();

			}
			catch (Exception ex)
			{
				Assert.Fail($"Error happened after the view: [{problemViewID}],\n ex:{ex}");
			}
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
