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
		public async Task NavigateByCreateAccountButton_Test()
		{
			string problemViewID = "";//Before every FindUIElement, this will become the id that will be tried to be located. This way in the fail message, it can be put there to know which view wasn't located. This is especially good sin i don't have to code a try-catch for every sind FindUIElement method call to know which view wasn't found.
			try
			{
				////// First press create account button
				string createAccountButtonID = "GetStarted_CreateAccountButton";
				problemViewID = createAccountButtonID;

				AppiumElement createAccountButton = FindUIElement(createAccountButtonID);
				createAccountButton.Click();
				Task.Delay(1000).Wait();
				////// Then press select phone code button
				string phoneCodeButtonID = "ValidatePhoneView_SelectPhoneCodeHorizontalStackLayout";
				problemViewID = phoneCodeButtonID;

				AppiumElement phoneCodeButton = FindUIElement(phoneCodeButtonID);
				phoneCodeButton.Click();
				Task.Delay(1000).Wait();
				////// Then press code 1 American Samoa button
				string code1AmericanSamoaButtonID = "AS";//AS stands for American Samoa(Alpha 2 country code). This countries phone code is 1 which is what we need for the testing number
				problemViewID = code1AmericanSamoaButtonID;

				AppiumElement code1AmericanSamoaButton = FindUIElement(code1AmericanSamoaButtonID);
				phoneCodeButton.Click();
				Task.Delay(1000).Wait();
				////// Then put in the test number in the number entry
				string numberEntryID = "ValidatePhoneView_NumberEntry";//AS stands for American Samoa(Alpha 2 country code). This countries phone code is 1 which is what we need for the testing number
				problemViewID = numberEntryID;

				AppiumElement numberEntry = FindUIElement(numberEntryID);
				numberEntry.Click();
				string testNumber = "1555123123";
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
				Task.Delay(2000).Wait();
				////// Then put in the verification code
				string verificationCode = await this.GetVerificationCodeAsync();
				Task.Delay(1000).Wait();
				action.SendKeys(verificationCode + "43").Perform();
				Task.Delay(3000).Wait();
			}
			catch (Exception ex)
			{
				Assert.Fail($"The view with the Id: [{problemViewID}] not found, ex:{ex}");
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
