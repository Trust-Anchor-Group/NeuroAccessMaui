using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.Test
{
    /// <summary>
    /// Presenter abstraction host for page, popup and toast transitions.
    /// </summary>
    public interface IShellPresenter
    {
        Task ShowScreen(ContentView screen, TransitionType transition = TransitionType.None);
        Task ShowPopup(ContentView popup, PopupTransition transition = PopupTransition.Fade, double overlayOpacity = 0.7);
        Task HideTopPopup(PopupTransition transition = PopupTransition.Fade);
        Task ShowToast(View toast, ToastTransition transition = ToastTransition.SlideFromTop, ToastPlacement placement = ToastPlacement.Top);
        Task HideToast(ToastTransition transition = ToastTransition.SlideFromTop);
        void UpdateBars(BindableObject screen);
        event EventHandler? PopupBackgroundTapped;
        event EventHandler? PopupBackRequested;
    }
}
