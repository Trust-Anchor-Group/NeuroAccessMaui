using NUnit.Framework;

namespace NeuroAccess.UiTests
{
	public class UnitTest1 : BaseTest
	{
		[Test]
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
			Assert.Pass();
		}
	}
}
