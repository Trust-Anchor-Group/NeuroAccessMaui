using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Testing.Platform.Capabilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace NeuroAccess.UiTests
{
	[TestClass]
	public class SettingsTest : BaseTest
	{
		[TestInitialize]
		public void SetUp()
		{

			// Open navigation drawer and click settings before each test
			AutoFindElement("MainPage");
			App.FindElement(By.XPath("//*[@content-desc='Open navigation drawer']")).Click();
			AutoFindElement("AppShell_SettingsMenuItem", 20).Click();

		}

		[TestMethod]
		[TestCategory("Android")]
		public void ClickLightMode_FromDarkMode_ThemeModeChanged()
		{

			// Switch to dark mode
			AppiumElement darkModeButton = AutoFindElement("DarkModeButton", 20);
			darkModeButton.Click();

			// Switch to light mode
			AppiumElement lightModeButton = AutoFindElement("LightModeButton", 20);
			lightModeButton.Click();

			// Navigate back to main page
			AutoFindElement("BackButton", 20).Click();
			AutoFindElement("MainPage");

		}

		[TestMethod]
		[TestCategory("Android")]
		public void ClickEnglish_FromLanguageMenu_LanguageChanged()
		{
			// Open language menu
			AutoFindElement("ChangeLanguageButton", 20).Click();

			// Select English 
			AppiumElement englishLanguage = AutoFindElement("English", 20);
			englishLanguage.Click();

			// Navigate back to main page
			AutoFindElement("BackButton", 20).Click();
         AutoFindElement("MainPage");	

		}

		[TestMethod]
		[TestCategory("Android")]
		public void ClickReportCompromized_ShowAlert_AlertAccepted()
		{
			// Click "Report Compromized"	
			AutoFindElement("CompromizedButton",20).Click();
         Task.Delay(2000).Wait();

			// Accept alert that appears
			App.SwitchTo().Alert().Accept();

			// Close password popup
			AutoFindElement("CheckPasswordPopup_CancelTextButton", 20).Click();

			// Nevigate back to main page
			AutoFindElement("BackButton").Click();
         AutoFindElement("MainPage");

		}

		[TestMethod]
		[TestCategory("Android")]
		public void ClickRevokeIdentity_ShowAlert_AlertAccepted()
		{
			// Click "Revoke identity"
			AutoFindElement("RevokeButton", 20).Click();
	      Task.Delay(1000).Wait();

			// Accept alert that appears
			App.SwitchTo().Alert().Accept();

			// Close password popup
			AutoFindElement("CheckPasswordPopup_CancelTextButton", 20).Click();

			// Navigate back to main page
			AutoFindElement("BackButton", 20).Click();
         AutoFindElement("MainPage");
		}

		[TestMethod]
		[TestCategory("Android")]
		public void ClickTransfer_CancelPopup_PopupClosed()
		{

			// Scroll down to find the "Transfer" button and click it
			AppiumElement transferButton = ScrollUpOrDownAndFindElement("TransferButton", false, 3);
			transferButton.Click();
         Task.Delay(1000).Wait();

			// Close password popup
			AutoFindElement("CheckPasswordPopup_CancelTextButton", 20).Click();

			// Navigate back to main page
			AutoFindElement("BackButton", 20).Click();
         AutoFindElement("MainPage");

		}

		[TestMethod]
		[TestCategory("Android")]
		public void ClickChangePassword_CancelPopup_PopupClosed()
		{

			// Scroll down to find the "Change Password" button and click it
			AppiumElement changePasswordButton = ScrollUpOrDownAndFindElement("ChangePasswordButton", false, 3);
			changePasswordButton.Click();
         Task.Delay(1000).Wait();

			// Close password popup
			AutoFindElement("CheckPasswordPopup_CancelTextButton", 20).Click();

			// // Scroll up to find the "Back" button and click it to navigate to main page
			AppiumElement backButton = ScrollUpOrDownAndFindElement("BackButton", true, 3);
			backButton.Click();
         AutoFindElement("MainPage");

		}

		[TestMethod]
		[TestCategory("Android")]
		public void ClickAllowRecording_RecordingEnabled_StateUpdated()
		{
			// Scroll down to find the "AllowRecording" button and click it
			AppiumElement allowRecordingButton = ScrollUpOrDownAndFindElement("AllowRecordingButton", false, 3);
			allowRecordingButton.Click();
         Task.Delay(1000).Wait();

			// Close	password popup
			AutoFindElement("CheckPasswordPopup_CancelTextButton", 20).Click();

			// Scroll up to find the "Back" button and click it to navigate to main page
			AppiumElement backButton = ScrollUpOrDownAndFindElement("BackButton", true, 3);
			backButton.Click();
			AutoFindElement("MainPage");

		}

		[TestMethod]
		[TestCategory("Android")]
		public void ClickProhibitRecording_RecordingDisabled_StateUpdated()
		{
			// Scroll down to find the "ProhibitRecording" button and click it
			AppiumElement prohibitRecordingButton = ScrollUpOrDownAndFindElement("ProhibitRecordingButton", false, 3);
			prohibitRecordingButton.Click();
			Task.Delay(1000).Wait();

			// Scroll up to find the "Back" button and click it to navigate to main page
			AppiumElement backButton = ScrollUpOrDownAndFindElement("BackButton", true, 3);
			backButton.Click();
         AutoFindElement("MainPage");

		}

		[TestMethod]
		[TestCategory("Android")]
		public void ClickBack_FromSettingsPage_NavigatedBack()
		{
			// Click the "Back" button to navigate back to the main page 
			AutoFindElement("BackButton", 20).Click();
         AutoFindElement("MainPage");

		}

	}
}








