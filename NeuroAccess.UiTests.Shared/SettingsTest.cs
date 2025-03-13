using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace NeuroAccess.UiTests
{
	[TestClass]
	public class SettingsTest : BaseTest
	{

		[TestMethod]
		[TestCategory("Android")]
		public async Task NavigateTo_Settings_Test()

		{ 

		try
            {
		// Click "DarkModeButton" (Selects Dark Mode)
		string darkModeButtonId = "DarkModeButton";
		AppiumElement darkModeButton = this.AutoFindElement(darkModeButtonId);
		darkModeButton.Click();
      

		// Click "LightModeButton" (Selects Light Mode)
		string lightModeButtonId = "LightModeButton";
		AppiumElement lightModeButton = this.AutoFindElement(lightModeButtonId);
		lightModeButton.Click();
     

		// Click "ChangeLanguageButton" (Opens Language Settings)
		string changeLanguageButtonId = "ChangeLanguageButton";
		AppiumElement changeLanguageButton = this.AutoFindElement(changeLanguageButtonId);
		changeLanguageButton.Click();
      

		// Click "CompromizedButton" (Navigates to compromised identity report)
		string compromisedButtonId = "CompromizedButton";
		AppiumElement compromisedButton = this.AutoFindElement(compromisedButtonId);
		compromisedButton.Click();
      

		// Click "RevokeButton" (Navigates to revoke identity page)
		string revokeButtonId = "RevokeButton";
		AppiumElement revokeButton = this.AutoFindElement(revokeButtonId);
		revokeButton.Click();
      

		// Click "TransferButton" (Navigates to identity transfer page)
		string transferButtonId = "TransferButton";
		AppiumElement transferButton = this.AutoFindElement(transferButtonId);
		transferButton.Click();
     

		// Click "ChangePasswordButton" (Navigates to password change page)
		string changePasswordButtonId = "ChangePasswordButton";
		AppiumElement changePasswordButton = this.AutoFindElement(changePasswordButtonId);
		changePasswordButton.Click();
     

		// Click "AllowRecordingButton" (Enables screen recording)
		string allowRecordingButtonId = "AllowRecordingButton";
		AppiumElement allowRecordingButton = this.AutoFindElement(allowRecordingButtonId);
		allowRecordingButton.Click();
		

		// Click "ProhibitRecordingButton" (Disables screen recording)
		string prohibitRecordingButtonId = "ProhibitRecordingButton";
		AppiumElement prohibitRecordingButton = this.AutoFindElement(prohibitRecordingButtonId);
		prohibitRecordingButton.Click();
	   

		// Click "BackButton" (Navigates back)
		string backButtonId = "BackButton";
		AppiumElement backButton = this.AutoFindElement(backButtonId);
		backButton.Click();
      

		// If we reach here, all tests passed
		Console.WriteLine("All navigation buttons were tested successfully.");
            }
            catch (Exception ex)
            {
                Assert.Fail($"An error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Custom method to find an element by AutomationId.
        /// If the element is not found, it throws an exception.
        /// </summary>
        private AppiumElement AutoFindElement(string automationId)
{
	try
	{
		return App.FindElement(MobileBy.AccessibilityId(automationId));
	}
	catch (Exception)
	{
		throw new Exception($"Element with AutomationId '{automationId}' was not found.");
	}
}
    }
}








