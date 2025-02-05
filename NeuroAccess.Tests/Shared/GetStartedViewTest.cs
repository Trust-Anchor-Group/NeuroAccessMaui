using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using OpenQA.Selenium;

namespace NeuroAccess.UiTests.Shared
{
	[TestClass]
	public class GetStartedViewTest : BaseTest
	{

		[TestMethod]
		[TestCategory("Android")]
		 public void Test_NavigateTo_GetStartedButtons()
		{

			var createAccountButton = App.FindElement(By.Id("GetStarted_CreateAccountButton"));
			Assert.IsNotNull(createAccountButton, "Create Account-button not found!");
			createAccountButton.Click();

			{
				var haveAccountButton = App.FindElement(By.Id("GetStarted_HaveAccountButton"));
				Assert.IsNotNull(haveAccountButton, "Have Account-Button not found");
				haveAccountButton.Click();
			}

			



		}
	}
}
