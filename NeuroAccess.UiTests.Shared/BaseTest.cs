﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Internal;

namespace NeuroAccess.UiTests
{
	[TestClass]
	public class BaseTest
	{
		private static AppiumDriver? driver;

		public static AppiumDriver App => driver ?? throw new InvalidOperationException("AppiumDriver is not initialized.");

		// This will run once before all tests in the derived classes
		[ClassInitialize(InheritanceBehavior.BeforeEachDerivedClass)]
		public static void RunBeforeAnyTests(TestContext context)
		{



			try
			{
				Console.WriteLine("Starting Appium Server...");
				AppiumServerHelper.StartAppiumLocalServer();

				Console.WriteLine("Configuring Appium Options...");
				var AndroidOptions = new AppiumOptions
				{
					// Specify UIAutomator2 as the driver, typically don't need to change this
					AutomationName = "UIAutomator2",
					// Always Android for Android
					PlatformName = "Android",
					// This is the Android version, not API level
					// This is ignored if you use the avd option below
					PlatformVersion = "14",
					// The full path to the .apk file to test or the package name if the app is already installed on the device
					//App = "com.tag.NeuroAccess-Signed.apk",
				};


				AndroidOptions.AddAdditionalAppiumOption("forceReset", true);
				//AndroidOptions.AddAdditionalAppiumOption("noReset", true);
				Console.WriteLine("Initializing Android Driver...");
				driver = new AndroidDriver(AndroidOptions);

				Console.WriteLine("Android Driver initialized successfully.");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Exception in RunBeforeAnyTests: {ex.Message}");
				throw;
			}
		}

		// This will run once after all tests in the derived classes
		[ClassCleanup(InheritanceBehavior.BeforeEachDerivedClass)]
		public static void RunAfterAnyTests()
		{
			driver?.Quit();
			driver?.Dispose();

			// If an Appium server was started locally above, make sure we clean it up here
			AppiumServerHelper.DisposeAppiumLocalServer();
		}
	}
}

/*
THE CODE FOR GETTING VERIFICATION FOR TEST PHONE NUMBER

			// Create a CookieContainer to manage cookies automatically
			CookieContainer cookieContainer = new();

			// Configure HttpClientHandler to use the CookieContainer and to follow redirects automatically
			HttpClientHandler handler = new()
			{
				CookieContainer = cookieContainer,
				AllowAutoRedirect = true // Automatically follows HTTP 3xx redirects
			};

			using (HttpClient client = new(handler))
			{
				// Set the base address (optional)
				client.BaseAddress = new Uri("https://id.tagroot.io");

				// Mimic some common headers that a browser might send
				client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
				client.DefaultRequestHeaders.UserAgent.ParseAdd("NeuroAccess/1.0 (.NET MAUI UITest)");

				client.DefaultRequestHeaders.Referrer = new Uri("https://id.tagroot.io/TestOTP.md");
				client.DefaultRequestHeaders.Add("Origin", "https://id.tagroot.io");

				// Prepare the form data. Adjust the field name and value as required.
				Dictionary<string, string> formData = new()
				{
					{ "PhoneNr", "+1555123123" }  
            };
				FormUrlEncodedContent content = new(formData);

				try
				{
					// Send a POST request. Since AllowAutoRedirect is true, the client will follow the 303 redirect.
					HttpResponseMessage response = await client.PostAsync("/TestOTP.md", content);
					response.EnsureSuccessStatusCode();

					// Read the final response content after redirects
					string htmlContent = await response.Content.ReadAsStringAsync();

					Console.WriteLine("Response content: " + htmlContent);

					// For demonstration, suppose the verification code appears in the page like "Verification Code: 123456".
					// You can use a regex (or an HTML parser like HtmlAgilityPack) to extract the code.
					Match codeMatch = Regex.Match(htmlContent, @"\<strong\>(?'Code'\d{6})\<\/strong\>", RegexOptions.IgnoreCase);
					if (codeMatch.Success)
					{
						string verificationCode = codeMatch.Groups[1].Value;
						Console.WriteLine("Verification Code: " + verificationCode);
					}
					else
					{
						Console.WriteLine("Verification code not found in the response.");
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine("An error occurred: " + ex.Message);
				}
			}
*/
