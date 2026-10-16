using CommunityToolkit.Mvvm.Input;
using DevTestPlatform.Catalog;
using DevTestPlatform.Navigation;
using DevTestPlatform.Resources;

namespace DevTestPlatform.UI.ViewModels;

/// <summary>Displays one discoverable scenario with immutable automation identity.</summary>
public sealed class CatalogItemViewModel
{
    /// <summary>Creates a catalog item for one screen and scenario.</summary>
    /// <param name="Screen">The current production subject.</param>
    /// <param name="Scenario">The selected state.</param>
    /// <param name="Navigation">The developer navigation service.</param>
    public CatalogItemViewModel(ScreenDefinition Screen, ScenarioDefinition Scenario, DevNavigationService Navigation)
    {
        this.Title = $"{DevText.Get(Screen.NameKey)} — {DevText.Get(Scenario.NameKey)}";
        this.Breadcrumb = $"{DevText.Get(Screen.AreaKey)} / {DevText.Get(Scenario.CategoryKey)}";
        this.AutomationId = NeuroAccessMaui.UI.Automation.AutomationIds.ForKey(
            NeuroAccessMaui.UI.Automation.AutomationIds.DevItemPlatformCatalogScenario, Screen.Key, Scenario.Key);
        this.OpenCommand = new AsyncRelayCommand(() => Navigation.OpenPreviewAsync(Screen, Scenario));
    }
    /// <summary>Gets the localized subject and scenario names.</summary>
    public string Title { get; }
    /// <summary>Gets the subject area and scenario category.</summary>
    public string Breadcrumb { get; }
    /// <summary>Gets the stable developer automation identifier.</summary>
    public string AutomationId { get; }
    /// <summary>Gets the command opening the current production preview.</summary>
    public IAsyncRelayCommand OpenCommand { get; }
}
