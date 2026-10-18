using System.Globalization;
using System.Text.RegularExpressions;

namespace NeuroAccessMaui.Testing.Configuration;

/// <summary>Reads run settings from NEUROACCESS_TEST_* environment variables or the runsettings file.</summary>
internal sealed class TestSettings
{
	private readonly TestContext context;
	private readonly string role;

	internal TestSettings(TestContext Context, string Role = "")
	{
		this.context = Context;
		this.role = Role;
	}

	internal string Get(string Name, string Default = "")
	{
		string Key = string.IsNullOrEmpty(this.role) ? Name : Name + "_" + this.role;
		string? Value = Environment.GetEnvironmentVariable("NEUROACCESS_TEST_" + Key);
		if (string.IsNullOrWhiteSpace(Value))
			Value = null;
		Value ??= this.context.Properties.TryGetValue(Key, out object? Parameter) ? Parameter?.ToString() : null;
		if (string.IsNullOrWhiteSpace(Value) && !string.IsNullOrEmpty(this.role))
		{
			Value = Environment.GetEnvironmentVariable("NEUROACCESS_TEST_" + Name);
			if (string.IsNullOrWhiteSpace(Value))
				Value = null;
			Value ??= this.context.Properties.TryGetValue(Name, out Parameter) ? Parameter?.ToString() : null;
		}
		return string.IsNullOrWhiteSpace(Value) ? Default : Value.Trim();
	}

	internal string Required(string Name)
	{
		string Value = this.Get(Name);
		Assert.IsFalse(string.IsNullOrWhiteSpace(Value), $"Set NEUROACCESS_TEST_{Name} or the {Name} run setting.");
		return Value;
	}

	internal bool Flag(string Name, bool Default = false)
	{
		string Value = this.Get(Name, Default.ToString());
		Assert.IsTrue(bool.TryParse(Value, out bool Result), $"{Name} must be true or false.");
		return Result;
	}

	internal int Number(string Name, int Default, int Minimum, int Maximum)
	{
		Assert.IsTrue(int.TryParse(this.Get(Name, Default.ToString(CultureInfo.InvariantCulture)), out int Value)
			&& Value >= Minimum && Value <= Maximum, $"{Name} must be between {Minimum} and {Maximum}.");
		return Value;
	}

	internal void RequireOptIn(string Name)
	{
		if (!this.Flag(Name))
			Assert.Inconclusive($"Set NEUROACCESS_TEST_{Name}=true to enable this scenario.");
	}

	internal string Platform
	{
		get
		{
			string Value = this.Required("PLATFORM");
			Assert.IsTrue(Value is "Android" or "iOS", "PLATFORM must be Android or iOS.");
			return Value;
		}
	}

	internal bool IsAndroid => this.Platform == "Android";
	internal string AppId => this.Get("APP_ID", this.IsAndroid ? "com.tag.NeuroAccess" : "com.trustanchorgroup.NeuroAccess");
	internal string DeviceId => this.Required("DEVICE_ID");
	internal Uri Server => this.GetUri("APPIUM_URL", false, "http://127.0.0.1:4723");
	internal string Language => this.Get("LANGUAGE_CODE", "en");
	internal TimeSpan ApprovalTimeout => TimeSpan.FromMinutes(this.Number("MANUAL_APPROVAL_TIMEOUT_MINUTES", 10, 1, 60));

	internal Uri GetUri(string Name, bool HttpsOnly, string Default = "")
	{
		string Value = this.Get(Name, Default);
		Assert.IsTrue(Uri.TryCreate(Value, UriKind.Absolute, out Uri? Result)
			&& Result.Host.Length > 0 && Result.UserInfo.Length == 0
			&& (Result.Scheme == "https" || (!HttpsOnly && Result.Scheme == "http")),
			$"{Name} must be an absolute {(HttpsOnly ? "HTTPS" : "HTTP(S)")} URL without embedded credentials.");
		return Result!;
	}

	internal string Pin(bool New = false, bool Creating = false)
	{
		string Value = this.Required(New ? "NEW_PIN" : "PIN");
		Assert.IsTrue(Regex.IsMatch(Value, "^[0-9]{6}$"), "PIN must contain exactly six digits.");
		if (New || Creating)
		{
			Assert.IsFalse(Regex.IsMatch(Value, @"^(\d)\1{5}$")
				|| new[] { "0123", "1234", "2345", "3456", "4567", "5678", "6789" }.Any(Value.Contains),
				"Configured PIN violates the repetition/sequence policy.");
		}
		if (New)
			Assert.IsTrue(Value != this.Pin(), "New and current PIN must differ.");
		return Value;
	}

	internal string Phone()
	{
		string Value = this.Required("PHONE_NUMBER");
		Assert.IsTrue(Regex.IsMatch(Value, "^[0-9]+$"), "Phone number must contain digits without country code.");
		return Value;
	}

	internal string Username()
	{
		string Value = this.Get("REGISTRATION_USERNAME_TIMESTAMP", DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture));
		Assert.IsTrue(Regex.IsMatch(Value, "^[0-9]{14}$"), "Username timestamp must use yyyyMMddHHmmss.");
		return Value;
	}
}
