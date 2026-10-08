using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevTestPlatform.Catalog;
using DevTestPlatform.Fakes;
using DevTestPlatform.Navigation;
using DevTestPlatform.ProductionAdapters;
using DevTestPlatform.Rendering;
using DevTestPlatform.Resources;
using NeuroAccessMaui.UI.Controls;
using NeuroAccessMaui.UI.Pages;

namespace DevTestPlatform.UI.ViewModels;

/// <summary>Owns developer chrome and a current-production render session without substituting its BindingContext.</summary>
public partial class PreviewViewModel : BaseViewModel
{
    private readonly ProductionScreenAdapter adapter;
    private readonly DevNavigationService navigation;
    private readonly ScenarioSimulationService simulations;
    private RenderSession? session;
    private CancellationTokenSource? simulationCancellation;
    private ScenarioDefinition? scenario;
    /// <summary>The localized subject and scenario title.</summary>
    [ObservableProperty] private string title = string.Empty;
    /// <summary>The interaction scope of the preview.</summary>
    [ObservableProperty] private string details = string.Empty;
    /// <summary>The current render or simulation diagnostic.</summary>
    [ObservableProperty] private string diagnostics = string.Empty;
    /// <summary>The production view with its developer preview host.</summary>
    [ObservableProperty] private View? preview;
    /// <summary>The current framed or fullscreen presentation mode.</summary>
    [ObservableProperty] private PreviewMode mode;

    /// <summary>Creates a production preview coordinator.</summary>
    /// <param name="Adapter">The current-production subject factory.</param>
    /// <param name="Navigation">The developer catalog navigation.</param>
    /// <param name="Simulations">The local simulated outcome service.</param>
    public PreviewViewModel(ProductionScreenAdapter Adapter, DevNavigationService Navigation, ScenarioSimulationService Simulations)
    {
        this.adapter = Adapter;
        this.navigation = Navigation;
        this.simulations = Simulations;
    }
    /// <summary>Gets whether developer scenario chrome is visible.</summary>
    public bool ShowChrome => this.Mode == PreviewMode.Framed;
    /// <summary>Gets whether developer diagnostics contain a message.</summary>
    public bool HasDiagnostics => this.Diagnostics.Length > 0;
    partial void OnModeChanged(PreviewMode Value) => this.OnPropertyChanged(nameof(this.ShowChrome));
    partial void OnDiagnosticsChanged(string Value) => this.OnPropertyChanged(nameof(this.HasDiagnostics));

    /// <summary>Creates a fresh controlled production view and catches construction failures.</summary>
    /// <param name="Screen">The production render subject.</param>
    /// <param name="Scenario">Its controlled visual state.</param>
    /// <returns>The preview preparation operation.</returns>
    public Task LoadAsync(ScreenDefinition Screen, ScenarioDefinition Scenario)
        => MainThread.InvokeOnMainThreadAsync(() => this.LoadOnMainThreadAsync(Screen, Scenario));

    private async Task LoadOnMainThreadAsync(ScreenDefinition Screen, ScenarioDefinition Scenario)
    {
        await this.ReleaseAsync();
        this.scenario = Scenario;
        this.Title = $"{DevText.Get(Screen.NameKey)} — {DevText.Get(Scenario.NameKey)}";
        this.Details = Screen.IsComponent ? DevText.Get("ComponentNotice") : DevText.Get("VisualNotice");
        this.Diagnostics = Scenario.OutcomeKey.Length > 0 ? DevText.Get(Scenario.OutcomeKey) : string.Empty;
        try
        {
            this.session = this.adapter.Create(Screen, Scenario);
            this.session.Failed += this.OnRenderFailed;
            this.session.Content.InputTransparent = !Screen.IsComponent;
            Grid Host = new Grid();
            if (Screen.Key.StartsWith("onboarding.", StringComparison.Ordinal))
            {
                Host.Add(new Background());
                ContentView Content = new ContentView { Content = this.session.Content };
                Content.SetDynamicResource(ContentView.PaddingProperty, "LargeMargins");
                Host.Add(new ScrollView { Content = Content });
            }
            else if (Screen.IsComponent)
            {
                Host.Padding = new Thickness(24);
                Host.Add(new VerticalStackLayout { Children = { this.session.Content }, VerticalOptions = LayoutOptions.Center });
            }
            else
            {
                Host.Add(this.session.Content);
            }
            this.Preview = Host;
        }
        catch (Exception Exception)
        {
            this.Diagnostics = $"{DevText.Get("RenderFailure")} {Exception.GetType().Name}: {Exception.Message}";
            System.Diagnostics.Debug.WriteLine(Exception);
            await this.ReleaseAsync();
        }
    }

    private void OnRenderFailed(Exception Exception) => MainThread.BeginInvokeOnMainThread(() =>
    {
        this.Diagnostics = $"{DevText.Get("RenderFailure")} {Exception.GetType().Name}: {Exception.Message}";
        System.Diagnostics.Debug.WriteLine(Exception);
    });

    [RelayCommand]
    private void ToggleFullscreen() => this.Mode = this.ShowChrome ? PreviewMode.Fullscreen : PreviewMode.Framed;

    [RelayCommand]
    private async Task BackAsync()
    {
        if (!this.ShowChrome)
            this.Mode = PreviewMode.Framed;
        else
            await this.navigation.GoBackAsync();
    }

    [RelayCommand]
    private async Task SimulateAsync()
    {
        if (this.scenario is null)
            return;
        this.simulationCancellation?.Cancel();
        this.simulationCancellation?.Dispose();
        CancellationTokenSource Cancellation = new CancellationTokenSource();
        this.simulationCancellation = Cancellation;
        this.Diagnostics = DevText.Get("Pending");
        try
        {
            this.Diagnostics = await this.simulations.RunAsync(this.scenario, Cancellation.Token);
        }
        catch (OperationCanceledException) { }
    }

    /// <summary>Stops pending simulations and releases the current production view and ViewModel.</summary>
    /// <returns>The cleanup operation.</returns>
    public Task ReleaseAsync() => MainThread.InvokeOnMainThreadAsync(this.ReleaseOnMainThreadAsync);

    private async Task ReleaseOnMainThreadAsync()
    {
        this.simulationCancellation?.Cancel();
        this.simulationCancellation?.Dispose();
        this.simulationCancellation = null;
        this.Preview = null;
        RenderSession? Previous = this.session;
        this.session = null;
        this.scenario = null;
        if (Previous is not null)
        {
            Previous.Failed -= this.OnRenderFailed;
            await Previous.DisposeAsync();
        }
    }
}
