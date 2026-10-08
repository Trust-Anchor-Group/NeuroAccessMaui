namespace DevTestPlatform.Catalog;

/// <summary>Describes a controlled state independently of screen construction and display mode.</summary>
/// <param name="Key">The immutable scenario key.</param>
/// <param name="ScreenKey">The owning production subject key.</param>
/// <param name="NameKey">The localized state-name resource key.</param>
/// <param name="CategoryKey">The scenario category resource key.</param>
/// <param name="Value">The synthetic input value, when applicable.</param>
/// <param name="IsBusy">Whether the visual state is busy.</param>
/// <param name="OutcomeKey">The simulated result resource key, when applicable.</param>
public sealed record ScenarioDefinition(string Key, string ScreenKey, string NameKey,
    string CategoryKey = "Default", string Value = "", bool IsBusy = false, string OutcomeKey = "");
