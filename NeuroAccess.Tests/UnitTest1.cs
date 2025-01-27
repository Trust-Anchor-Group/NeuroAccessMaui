using NUnit.Framework;

namespace NeuroAccess.UiTests
{
	public class Tests
	{
		[SetUp]
		public void Setup()
		{

		}

		[Test]
		public void Test1()
		{
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
