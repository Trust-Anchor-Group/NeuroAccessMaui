using DevTestPlatform.UI.Pages;
using NeuroAccessMaui.Animations;
using NeuroAccessMaui.Services.UI;

namespace DevTestPlatform.Navigation;

/// <summary>Uses current production shell presentation with developer catalog back navigation.</summary>
public sealed class DevShell : CustomShell
{
    private readonly DevNavigationService navigation;
    /// <summary>Creates a shell initially displaying the developer catalog.</summary>
    /// <param name="Catalog">The initial catalog view.</param>
    /// <param name="KeyboardInsets">The current keyboard inset service.</param>
    /// <param name="AnimationCoordinator">The current production animation coordinator.</param>
    /// <param name="Navigation">The developer navigation stack.</param>
    public DevShell(CatalogPage Catalog, IKeyboardInsetsService KeyboardInsets, IAnimationCoordinator AnimationCoordinator, DevNavigationService Navigation)
        : base(Catalog, KeyboardInsets, AnimationCoordinator) => this.navigation = Navigation;

    /// <inheritdoc/>
    protected override bool OnBackButtonPressed()
    {
        if (this.navigation.CurrentPage is PreviewPage Page)
        {
            Page.ViewModel<DevTestPlatform.UI.ViewModels.PreviewViewModel>().BackCommand.Execute(null);
            return true;
        }
        return base.OnBackButtonPressed();
    }
}
