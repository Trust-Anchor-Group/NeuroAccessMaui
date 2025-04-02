using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using System;
using System.Threading;

namespace NeuroAccess.UiTests
{
	[TestClass]

	public class MainPageNavigationTest : BaseTest
	{
		[TestMethod]
		[Priority(1)]
		public void TestScanQRButtonNavigation()
		{
			AutoFindElement("MainPage");

			var ScanQrButton = AutoFindElement("MainPage_ScanQRTemplatedButton");
			ScanQrButton.Click();

			Task.Delay(5000).Wait();

			var goBackButton = AutoFindElement("ScanQrCodePage_GoBackImageButton");
			goBackButton.Click();

			Task.Delay(2000).Wait();

			AutoFindElement("MainPage");
		}


		// Navigate and click the Show ID Button, and enter PIN code
		[TestMethod]
		[Priority(2)]
		public void TestViewIdentyButton()
		{
			var showIdButton = AutoFindElement("MainPage_ShowIDTemplatedButton");
			showIdButton.Click();

			Task.Delay(2000).Wait();


			var cancelButton = AutoFindElement("CheckPasswordPopup_CancelTextButton");
			cancelButton.Click();

			Task.Delay(2000).Wait();
			AutoFindElement("MainPage");

			showIdButton = AutoFindElement("MainPage_ShowIDTemplatedButton");
			showIdButton.Click();

			Task.Delay(2000).Wait();

			var passwordEntry = AutoFindElement("CheckPasswordPopup_PasswordCompositeEntry");
			Actions actions = new Actions(App);
			actions.SendKeys("135135135135135").Perform();

			var enterButton = AutoFindElement("CheckPasswordPopup_EnterPasswordTextButton");
			enterButton.Click();

			Task.Delay(2000).Wait();

			var goBackButton = AutoFindElement("ViewIdentyGoBackButton");
			goBackButton.Click();

			AutoFindElement("MainPage");
		
		}


		// Navigate and click the Show ID Settings Button, and enter PIN code
		[TestMethod]
		[Priority(3)]
		public void TestViewSettingsIdMenu()
		{
			AutoFindElement("MainPage");
			AppiumElement flyoutIcon = App.FindElement(By.XPath("//*[@content-desc='Open navigation drawer']"));
			flyoutIcon.Click();

			var showIdButton = AutoFindElement("AppShell_ViewIdMenuItem");
			showIdButton.Click();

			Task.Delay(2000).Wait();


			var cancelButton = AutoFindElement("CheckPasswordPopup_CancelTextButton");
			cancelButton.Click();

			Task.Delay(2000).Wait();

			showIdButton = AutoFindElement("AppShell_ViewIdMenuItem");
			showIdButton.Click();

			Task.Delay(2000).Wait();

			var passwordEntry = AutoFindElement("CheckPasswordPopup_PasswordCompositeEntry");
			Actions actions = new Actions(App);
			actions.SendKeys("135135135135135").Perform();

			var enterButton = AutoFindElement("CheckPasswordPopup_EnterPasswordTextButton");
			enterButton.Click();

			Task.Delay(2000).Wait();

			var goBackButton = AutoFindElement("ViewIdentyGoBackButton");
			goBackButton.Click();

			AutoFindElement("MainPage");
		}
	
	}
}
