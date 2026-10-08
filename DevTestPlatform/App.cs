using DevTestPlatform.Navigation;
using NeuroAccessMaui.Services;

namespace DevTestPlatform;

/// <summary>Hosts the developer catalog without activating production application services.</summary>
public sealed partial class App : Application
{
    /// <summary>Loads current production dictionaries in their production merge order.</summary>
    public App()
    {
        this.InitializeComponent();
    }

    /// <inheritdoc/>
    protected override Window CreateWindow(IActivationState? ActivationState)
    {
        DevShell Shell = ServiceRef.Provider.GetRequiredService<DevShell>();
        ServiceRef.Provider.GetRequiredService<DevNavigationService>().Attach(Shell);
        return new Window(Shell);
    }
}
