using DevTestPlatform.Catalog;
using DevTestPlatform.UI.ViewModels;
using NeuroAccessMaui.UI.Pages;

namespace DevTestPlatform.UI.Pages;

/// <summary>Hosts current production UI as a controlled preview without invoking production initialization.</summary>
public partial class PreviewPage : BaseContentPage
{
    private readonly PreviewViewModel viewModel;
    /// <summary>Creates a preview bound to its developer session coordinator.</summary>
    /// <param name="ViewModel">The preview coordinator.</param>
    public PreviewPage(PreviewViewModel ViewModel)
    {
        this.InitializeComponent();
        this.viewModel = ViewModel;
        this.ContentPageModel = ViewModel;
    }
    /// <summary>Loads one current production subject and visual state.</summary>
    /// <param name="Screen">The production subject.</param>
    /// <param name="Scenario">The controlled state.</param>
    /// <returns>The preview preparation operation.</returns>
    public Task LoadAsync(ScreenDefinition Screen, ScenarioDefinition Scenario) => this.viewModel.LoadAsync(Screen, Scenario);
    /// <inheritdoc/>
    public override Task OnInitializeAsync() => Task.CompletedTask;
    /// <inheritdoc/>
    public override Task OnAppearingAsync() => Task.CompletedTask;
    /// <inheritdoc/>
    public override Task OnDisappearingAsync() => Task.CompletedTask;
    /// <inheritdoc/>
    public override Task OnDisposeAsync() => this.viewModel.ReleaseAsync();
}
