using Microsoft.VisualStudio.TestTools.UnitTesting;
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
			App.GetScreenshot().SaveAsFile($"{nameof(Test1)}.png");

#if ANDROID
			Console.WriteLine("Running tests for Android.");
#elif IOS
            Console.WriteLine("Running tests for iOS.");
#else
			throw new PlatformNotSupportedException();
#endif
			Assert.IsTrue(true);
		}
	}
}
