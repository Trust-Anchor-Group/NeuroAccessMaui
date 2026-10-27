using DevTestPlatform.UI.ViewModels;
using NeuroAccessMaui.UI.Pages;

namespace DevTestPlatform.UI.Pages;

/// <summary>Displays the independent developer catalog without production startup lifecycle.</summary>
public partial class CatalogPage : BaseContentPage
{
    /// <summary>Creates a catalog bound to its developer ViewModel.</summary>
    /// <param name="ViewModel">The searchable catalog ViewModel.</param>
    public CatalogPage(CatalogViewModel ViewModel)
    {
        this.InitializeComponent();
        this.ContentPageModel = ViewModel;
    }
    /// <inheritdoc/>
    public override Task OnInitializeAsync() => Task.CompletedTask;
    /// <inheritdoc/>
    public override Task OnAppearingAsync() => Task.CompletedTask;
    /// <inheritdoc/>
    public override Task OnDisappearingAsync() => Task.CompletedTask;
    /// <inheritdoc/>
    public override Task OnDisposeAsync() => Task.CompletedTask;
}
