using System.Text.Json;
using OpenQA.Selenium.Appium;
using NeuroAccessMaui.UITests.Infrastructure.Platforms;

namespace NeuroAccessMaui.UITests.Infrastructure;

/// <summary>Builds the Appium capabilities from the run settings and opens the platform's driver.</summary>
internal static class AppiumDriverFactory
{
	internal static AppiumDriver Create(TestSettings Settings, MobilePlatform Platform)
	{
		AppiumOptions Options = new AppiumOptions();
		Options.PlatformName = Settings.Platform;
		Options.DeviceName = Settings.DeviceId;
		Options.AddAdditionalAppiumOption("udid", Settings.DeviceId);
		Options.AddAdditionalAppiumOption("noReset", true);
		Options.AddAdditionalAppiumOption("newCommandTimeout", 3900);
		Options.AddAdditionalAppiumOption("printPageSourceOnFindFailure", false);
		if (Settings.Get("APP_PATH") is string AppPath && AppPath.Length > 0)
			Options.App = AppPath;
		if (Settings.Get("PLATFORM_VERSION") is string Version && Version.Length > 0)
			Options.PlatformVersion = Version;
		Platform.Configure(Options, Settings);
		foreach (KeyValuePair<string, JsonElement> Capability in ReadCapabilities(Settings))
		{
			object Value = Capability.Value.ValueKind switch
			{
				JsonValueKind.String => Capability.Value.GetString()!,
				JsonValueKind.True => true,
				JsonValueKind.False => false,
				JsonValueKind.Number when Capability.Value.TryGetInt32(out int Number) => Number,
				_ => throw new AssertFailedException($"CAPABILITIES value for {Capability.Key} must be a string, boolean or integer.")
			};
			Options.AddAdditionalAppiumOption(Capability.Key, Value);
		}
		return Platform.Connect(Settings.Server, Options);
	}

	private static Dictionary<string, JsonElement> ReadCapabilities(TestSettings Settings)
	{
		try
		{
			return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(Settings.Get("CAPABILITIES", "{}"))
				?? throw new AssertFailedException("CAPABILITIES must be a JSON object.");
		}
		catch (JsonException Error) { throw new AssertFailedException("CAPABILITIES must be a JSON object: " + Error.Message); }
	}
}
