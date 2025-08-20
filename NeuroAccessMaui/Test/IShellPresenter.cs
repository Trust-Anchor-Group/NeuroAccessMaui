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
        void UpdateBars(BindableObject screen);
        event EventHandler? ModalBackgroundTapped;
    }
}
