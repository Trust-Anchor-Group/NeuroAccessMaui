using DevTestPlatform.Fixtures;

namespace DevTestPlatform.Catalog;

/// <summary>Discovers current production subjects and their explicitly ordered visual scenarios.</summary>
public sealed class CatalogRegistry
{
    /// <summary>Gets the production screens and independently reusable controls.</summary>
    public IReadOnlyList<ScreenDefinition> Screens { get; } =
    [
        new("onboarding.provider", "Provider", "Onboarding", false, "invite qr provider"),
        new("onboarding.phone", "Phone", "Onboarding", false, "phone number country send code"),
        new("verification.phone-code", "PhoneCode", "Onboarding", false, "phone otp code verify"),
        new("onboarding.email", "Email", "Onboarding", false, "email address send code"),
        new("verification.email-code", "EmailCode", "Onboarding", false, "email otp code verify"),
        new("onboarding.username", "Username", "Onboarding", false, "account username suggestion"),
        new("onboarding.account", "Account", "Onboarding", false, "identity account loading"),
        new("onboarding.pin", "Pin", "Onboarding", false, "password security validation"),
        new("onboarding.biometrics", "Biometrics", "Onboarding", false, "fingerprint face authentication"),
        new("onboarding.finalize", "Success", "Onboarding", false, "complete finished"),
        new("components.text-button", "TextButton", "Buttons", true, "send code button enabled disabled"),
        new("components.composite-entry", "CompositeEntry", "Inputs", true, "input text valid invalid disabled focus")
    ];

    /// <summary>Gets scenarios in deterministic registration order.</summary>
    public IReadOnlyList<ScenarioDefinition> Scenarios { get; } = CreateScenarios();

    /// <summary>Finds one production render subject by its immutable key.</summary>
    /// <param name="Key">The subject key.</param>
    /// <returns>The registered subject.</returns>
    public ScreenDefinition GetScreen(string Key) => this.Screens.First(Screen => Screen.Key == Key);

    private static IReadOnlyList<ScenarioDefinition> CreateScenarios()
    {
        List<ScenarioDefinition> Scenarios = new List<ScenarioDefinition>();
        void Add(string Screen, string Key, string Name, string Category = "Default", string Value = "", bool Busy = false, string Outcome = "")
            => Scenarios.Add(new ScenarioDefinition(Key, Screen, Name, Category, Value, Busy, Outcome));
        Add("onboarding.provider", "default", "Default");
        Add("onboarding.phone", "default", "Default");
        Add("onboarding.phone", "empty", "Empty");
        Add("onboarding.phone", "valid", "Valid", "HappyPaths", OnboardingFixtures.phone);
        Add("onboarding.phone", "invalid", "Invalid", "Validation", "12ab");
        Add("onboarding.phone", "loading", "Loading", "Loading", OnboardingFixtures.phone, true, "Pending");
        Add("onboarding.phone", "code-sent", "CodeSent", "HappyPaths", OnboardingFixtures.phone, false, "Sent");
        Add("onboarding.phone", "backend-error", "BackendError", "Errors", OnboardingFixtures.phone, false, "BackendFailure");
        Add("onboarding.phone", "network-error", "NetworkError", "Errors", OnboardingFixtures.phone, false, "NetworkFailure");
        Add("onboarding.phone", "country-us", "UnitedStates", "EdgeCases", OnboardingFixtures.phone);
        Add("onboarding.phone", "country-se", "Sweden", "EdgeCases", OnboardingFixtures.phone);
        Add("onboarding.phone", "read-only", "ReadOnly", "EdgeCases", OnboardingFixtures.phone);
        foreach (string Screen in new[] { "verification.phone-code", "verification.email-code" })
        {
            Add(Screen, "default", "Default");
            Add(Screen, "empty", "Empty");
            Add(Screen, "invalid", "Invalid", "Validation", "123");
            Add(Screen, "verifying", "Verifying", "Loading", OnboardingFixtures.code, true, "Pending");
            Add(Screen, "verified", "Verified", "HappyPaths", OnboardingFixtures.code, false, "Accepted");
            Add(Screen, "expired", "Expired", "EdgeCases", OnboardingFixtures.code, false, "ExpiredFailure");
            Add(Screen, "backend-error", "BackendError", "Errors", OnboardingFixtures.code, false, "BackendFailure");
        }
        Add("onboarding.email", "default", "Default");
        Add("onboarding.email", "empty", "Empty");
        Add("onboarding.email", "valid", "Valid", "HappyPaths", OnboardingFixtures.email);
        Add("onboarding.email", "invalid", "Invalid", "Validation", "not-an-email");
        Add("onboarding.username", "empty", "Empty");
        Add("onboarding.username", "valid", "Valid", "HappyPaths", OnboardingFixtures.username);
        Add("onboarding.username", "invalid", "Suggestion", "Validation", "alex!");
        Add("onboarding.account", "creating", "CreatingIdentity", "Loading", "", true);
        Add("onboarding.pin", "empty", "Empty");
        Add("onboarding.pin", "matching", "Matching", "HappyPaths", OnboardingFixtures.pin);
        Add("onboarding.pin", "mismatch", "Mismatch", "Validation", OnboardingFixtures.pin);
        Add("onboarding.biometrics", "default", "Default");
        Add("onboarding.finalize", "default", "Default");
        Add("components.text-button", "enabled", "Enabled");
        Add("components.text-button", "disabled", "Disabled", "EdgeCases");
        Add("components.text-button", "loading", "Loading", "Loading", "", true);
        Add("components.composite-entry", "empty", "Empty");
        Add("components.composite-entry", "valid", "Valid", "HappyPaths", OnboardingFixtures.phone);
        Add("components.composite-entry", "invalid", "Invalid", "Validation", "12ab");
        Add("components.composite-entry", "disabled", "Disabled", "EdgeCases", OnboardingFixtures.phone);
        Add("components.composite-entry", "focused", "Focused", "EdgeCases");
        if (Scenarios.GroupBy(Scenario => $"{Scenario.ScreenKey}/{Scenario.Key}").Any(Group => Group.Count() > 1))
            throw new InvalidOperationException("Duplicate scenario keys.");
        return Scenarios;
    }
}
