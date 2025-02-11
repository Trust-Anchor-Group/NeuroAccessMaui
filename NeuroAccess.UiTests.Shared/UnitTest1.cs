using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Interactions;
using System;

namespace NeuroAccess.UiTests
{
	[TestClass]
	public class UnitTest1 : BaseTest
	{
		[TestMethod]
		[TestCategory("Android")]
		public void Test1()
		{
			Task.Delay(10000).Wait();
			App.GetScreenshot().SaveAsFile($"{nameof(Test1)}.png");



			Assert.IsTrue(true);
		}
	}
}
