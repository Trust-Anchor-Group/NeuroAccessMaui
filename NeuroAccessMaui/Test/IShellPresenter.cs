using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.Test
{
    /// <summary>
    /// Thin presenter abstraction so navigation logic can live outside the visual shell.
    /// </summary>
    public interface IShellPresenter
    {
        Task ShowScreen(ContentView screen, TransitionType transition = TransitionType.None);
        Task ShowModal(ContentView screen, TransitionType transition = TransitionType.Fade);
        Task HideTopModal(TransitionType transition = TransitionType.Fade);
        Task ShowPopup(ContentView popup, PopupTransition transition = PopupTransition.Fade, double overlayOpacity = 0.7);
        Task HideTopPopup(PopupTransition transition = PopupTransition.Fade);
        Task ShowToast(View toast, ToastTransition transition = ToastTransition.SlideFromTop, ToastPlacement placement = ToastPlacement.Top);
        Task HideToast(ToastTransition transition = ToastTransition.SlideFromTop);
        void UpdateBars(BindableObject screen);
        event EventHandler? ModalBackgroundTapped;
        event EventHandler? PopupBackgroundTapped;
        event EventHandler? PopupBackRequested;
    }
}
