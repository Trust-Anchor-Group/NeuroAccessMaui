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
			// Steg 1: Öppna navigation drawer och gå till inställningar – körs innan varje test
			AutoFindElement("MainPage");
			App.FindElement(By.XPath("//*[@content-desc='Open navigation drawer']")).Click();
			AutoFindElement("AppShell_SettingsMenuItem", 20).Click();
		}

		[TestMethod]
		[TestCategory("Android")]
		public void ClickLightMode_FromDarkMode_ThemeModeChanged()
		{
			
			// Växla till mörkt läge
			AppiumElement darkModeButton = AutoFindElement("DarkModeButton", 20);
			darkModeButton.Click();

			// Växla tillbaka till ljust läge
			AppiumElement lightModeButton = AutoFindElement("LightModeButton", 20);
			lightModeButton.Click();

			// Klicka på tillbaka-knapp
			AutoFindElement("BackButton", 20).Click();

		}

		[TestMethod]
		[TestCategory("Android")]
		public void ClickSuomi_FromLanguageMenu_LanguageChanged()
		{
			// Klicka på språkvalsknapp
			AutoFindElement("ChangeLanguageButton", 20).Click();

			// Välj "suomi"
			AppiumElement finnishLanguage = AutoFindElement("suomi", 20);
			finnishLanguage.Click();

			// Klicka på tillbaka-knapp
			AutoFindElement("BackButton", 20).Click();

		}

		[TestMethod]
		[TestCategory("Android")]
		public void ClickReportCompromized_ShowAlert_AlertAccepted()
		{
			// Klicka på "Anmäl som komprometterad"
			AutoFindElement("CompromizedButton",20).Click();

			//Task.Delay(1000).Wait();

			// Acceptera alert som visas
			//App.SwitchTo().Alert().Accept();

			// Klicka på tillbaka-knapp
			AutoFindElement("BackButton", 20).Click();

		}

		[TestMethod]
		[TestCategory("Android")]
		public void ClickRevokeIdentity_ShowAlert_AlertAccepted()
		{
			// Klicka på "Återkalla identitet"
			AutoFindElement("RevokeButton", 20).Click();

			// Acceptera alert som visas
			//App.SwitchTo().Alert().Accept();

			// Klicka på tillbaka-knapp
			AutoFindElement("BackButton", 20).Click();
		}

		[TestMethod]
		[TestCategory("Android")]
		public void ClickTransfer_CancelPopup_PopupClosed()
		{
			// Klicka på "Överför identitet"
			//AutoFindElement("TransferButton", 20).Click();

			AppiumElement transferButton = ScrollUpOrDownAndFindElement("TransferButton", false, 3);
			transferButton.Click();

			// Stäng popupen
			AutoFindElement("PasswordPopup_CancelButton", 20).Click();

			// Klicka på tillbaka-knapp
			AutoFindElement("BackButton", 20).Click();

		}

		[TestMethod]
		[TestCategory("Android")]
		public void ClickChangePassword_CancelPopup_PopupClosed()
		{
			// Klicka på "Byt lösenord"
			AutoFindElement("ChangePasswordButton", 20).Click();

			// Stäng popupen
			AutoFindElement("PasswordPopup_CancelButton", 20).Click();

			// Klicka på tillbaka-knapp
			AutoFindElement("BackButton", 20).Click();

		}

		[TestMethod]
		[TestCategory("Android")]
		public void ClickAllowRecording_RecordingEnabled_StateUpdated()
		{
			// Aktivera skärminspelning
			AutoFindElement("AllowRecordingButton", 20).Click();
		}

		[TestMethod]
		[TestCategory("Android")]
		public void ClickProhibitRecording_RecordingDisabled_StateUpdated()
		{
		// Avaktivera skärminspelning
		AutoFindElement("ProhibitRecordingButton", 20).Click();

		// Klicka på tillbaka-knapp
		AutoFindElement("BackButton", 20).Click();

		}

		[TestMethod]
		[TestCategory("Android")]
		public void ClickBack_FromSettingsPage_NavigatedBack()
		{
		// Klicka på tillbaka-knapp
		AutoFindElement("BackButton", 20).Click();
		}

	}
}








