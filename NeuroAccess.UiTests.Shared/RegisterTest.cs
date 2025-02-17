using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Appium;

namespace NeuroAccess.UiTests
{
	[TestClass]
    public class RegisterTest: BaseTest
    {
		[TestMethod]
		[TestCategory("Android")]
		public void NavigateByCreateAccountButton_Test()
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
				numberEntry.SendKeys("555123123");
				Task.Delay(1000).Wait();
				////// Then put in the test number in the number entry
				string sendCodeButtonID = "ValidatePhoneView_SendCodeTextButton";
				problemViewID = sendCodeButtonID;

				AppiumElement sendCodeButton = FindUIElement(sendCodeButtonID);
				sendCodeButton.Click();
				Task.Delay(5000).Wait();

			}
			catch (Exception ex)
			{
				Assert.Fail($"The view with the Id: [{problemViewID}] not found, ex:{ex}");
			}
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
