using OpenQA.Selenium.Appium.Service;

namespace NeuroAccess.UiTests
{

	public static class AppiumServerHelper
	{
		private static AppiumLocalService? appiumLocalService;

		public const string DefaultHostAddress = "127.0.0.1";
		public const int DefaultHostPort = 4724;

		public static void StartAppiumLocalServer(string host = DefaultHostAddress,
			int port = DefaultHostPort)
		{
			if (appiumLocalService is not null)
			{
				return;
			}

			var builder = new AppiumServiceBuilder()
				.WithIPAddress(host)
				.UsingPort(port);

			// Start the server with the builder
			appiumLocalService = builder.Build();
			appiumLocalService.Start();
		}

		public static void DisposeAppiumLocalServer()
		{
		
			appiumLocalService?.Dispose();
		}
	}
}
