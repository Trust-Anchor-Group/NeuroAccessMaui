using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;

namespace NeuroAccess.UiTests
{
	[TestClass]
    public class _1MainPageTest: BaseTest
    {


		private static bool reset = false; 
		public _1MainPageTest(): base(reset, nameof(_1MainPageTest)) {

		}
		[TestMethod]
		[TestCategory("Android")]
		public void c1() {
			AutoFindElement("MainPage");
			AppiumElement flyoutIcon = App.FindElement(By.XPath("//*[@content-desc='Open navigation drawer']"));
			flyoutIcon.Click();
		}
		[TestMethod]
		[TestCategory("Android")]
		public void FlyoutIcon_Test() {
			AutoFindElement("MainPage");
			var screenSize = App.Manage().Window.Size;

			int xPosition = screenSize.Width / 20;  // almost completly to the left
			int yPosition = screenSize.Height / 20; // almost exactly on the top edge

			Actions action = new Actions(App);

			action.MoveToLocation(xPosition, yPosition) // First move to the flyout icon
					.Click()// Now press the flyout icon
					.Perform();
			AppiumElement applications = AutoFindElement("AppShell_ApplicationsMenuItem");
		}
		[TestMethod]
		[TestCategory("Android")]
		public void HideMenuByPressingOnBackground_Test() {
			var screenSize = App.Manage().Window.Size;
			//The position where it will be pressed
			int xPosition = screenSize.Width / 20 * 19;  // almost completly to the right
			int yPosition = screenSize.Height / 2; // in the middle vertically

			Actions action = new Actions(App);

			action.MoveToLocation(xPosition, yPosition) // First move to the flyout icon
					.Click()// Now press the flyout icon
					.Perform();

			AppiumElement applications = AutoFindElement("AppShell_ApplicationsMenuItem");
		}


	}
}
