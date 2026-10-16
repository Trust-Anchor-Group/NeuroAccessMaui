using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevTestPlatform.Catalog;
using DevTestPlatform.Navigation;
using DevTestPlatform.Resources;
using NeuroAccessMaui.UI.Pages;

namespace DevTestPlatform.UI.ViewModels;

/// <summary>Filters one catalog of production subjects and scenarios by area, kind and searchable metadata.</summary>
public partial class CatalogViewModel : BaseViewModel
{
    private readonly CatalogRegistry catalog;
    private readonly DevNavigationService navigation;
    /// <summary>The searchable catalog metadata.</summary>
    [ObservableProperty] private string searchText = string.Empty;
    /// <summary>The selected catalog section.</summary>
    [ObservableProperty] private string selectedSection = DevText.Get("Screens");
    /// <summary>The selected application area.</summary>
    [ObservableProperty] private string selectedArea = DevText.Get("All");
    /// <summary>The selected production subject.</summary>
    [ObservableProperty] private string selectedSubject = DevText.Get("All");
    /// <summary>The selected scenario category.</summary>
    [ObservableProperty] private string selectedCategory = DevText.Get("All");
    /// <summary>The filtered production scenarios.</summary>
    [ObservableProperty] private IReadOnlyList<CatalogItemViewModel> items = Array.Empty<CatalogItemViewModel>();

    /// <summary>Creates a searchable catalog over current registered production UI.</summary>
    /// <param name="Catalog">The subject/scenario registry.</param>
    /// <param name="Navigation">The developer navigation service.</param>
    public CatalogViewModel(CatalogRegistry Catalog, DevNavigationService Navigation)
    {
        this.catalog = Catalog;
        this.navigation = Navigation;
        this.Refresh();
    }

    /// <summary>Gets localized catalog sections.</summary>
    public IReadOnlyList<string> Sections { get; } = [DevText.Get("Screens"), DevText.Get("Components"), DevText.Get("Scenarios")];
    /// <summary>Gets the application areas represented in the registry.</summary>
    public IReadOnlyList<string> Areas => new[] { DevText.Get("All") }.Concat(this.catalog.Screens.Select(Screen => DevText.Get(Screen.AreaKey)).Distinct()).ToArray();
    /// <summary>Gets production subjects available in the selected catalog section and area.</summary>
    public IReadOnlyList<string> Subjects => new[] { DevText.Get("All") }.Concat(this.catalog.Screens.Where(this.MatchesSectionAndArea).Select(Screen => DevText.Get(Screen.NameKey))).ToArray();
    /// <summary>Gets localized scenario-category filters.</summary>
    public IReadOnlyList<string> Categories { get; } = [DevText.Get("All"), DevText.Get("Default"), DevText.Get("HappyPaths"), DevText.Get("Validation"), DevText.Get("Loading"), DevText.Get("Errors"), DevText.Get("EdgeCases")];
    /// <summary>Gets whether the current filter has no matching entries.</summary>
    public bool HasNoResults => this.Items.Count == 0;
    partial void OnSearchTextChanged(string Value) => this.Refresh();
    partial void OnSelectedSectionChanged(string Value) => this.ResetSubject();
    partial void OnSelectedAreaChanged(string Value) => this.ResetSubject();
    partial void OnSelectedSubjectChanged(string Value) => this.Refresh();

    private void ResetSubject()
    {
        this.SelectedSubject = DevText.Get("All");
        this.OnPropertyChanged(nameof(this.Subjects));
        this.Refresh();
    }

    private bool MatchesSectionAndArea(ScreenDefinition Screen)
    {
        bool MatchesSection = this.SelectedSection == DevText.Get("Scenarios") || Screen.IsComponent == (this.SelectedSection == DevText.Get("Components"));
        return MatchesSection && (this.SelectedArea == DevText.Get("All") || this.SelectedArea == DevText.Get(Screen.AreaKey));
    }
    partial void OnSelectedCategoryChanged(string Value) => this.Refresh();

    private void Refresh()
    {
        List<CatalogItemViewModel> Results = new List<CatalogItemViewModel>();
        foreach (ScreenDefinition Screen in this.catalog.Screens)
        {
            if (!this.MatchesSectionAndArea(Screen) || (this.SelectedSubject != DevText.Get("All") && this.SelectedSubject != DevText.Get(Screen.NameKey)))
                continue;
            IEnumerable<ScenarioDefinition> States = this.catalog.Scenarios.Where(Scenario => Scenario.ScreenKey == Screen.Key);
            foreach (ScenarioDefinition Scenario in States)
            {
                if (this.SelectedCategory != DevText.Get("All") && DevText.Get(Scenario.CategoryKey) != this.SelectedCategory)
                    continue;
                string Search = $"{Screen.Key} {Scenario.Key} {DevText.Get(Screen.NameKey)} {DevText.Get(Screen.AreaKey)} {Screen.Tags} {DevText.Get(Scenario.NameKey)} {DevText.Get(Scenario.CategoryKey)}";
                if (this.SearchText.Trim().Length > 0 && !Search.Contains(this.SearchText.Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;
                Results.Add(new CatalogItemViewModel(Screen, Scenario, this.navigation));
            }
        }
        this.Items = Results;
        this.OnPropertyChanged(nameof(this.HasNoResults));
    }
}
