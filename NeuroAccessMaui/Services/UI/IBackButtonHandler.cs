using System.Threading.Tasks;

namespace NeuroAccessMaui.Services.UI
{
    /// <summary>
    /// Implement on a Page or its ViewModel to intercept back button presses.
    /// Return true to consume (prevent default navigation), false to allow it.
    /// </summary>
    public interface IBackButtonHandler
    {
        Task<bool> OnBackButtonPressedAsync();
    }
}
