using System;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using NeuroAccessMaui.UI.Popups;

namespace NeuroAccessMaui.Test
{
    /// <summary>
    /// Presenter abstraction host for page, popup and toast transitions.
    /// </summary>
    public interface IShellPresenter
    {
        Task ShowScreen(ContentView screen, TransitionType transition = TransitionType.None);
        Task ShowPopup(ContentView popup, PopupTransition transition, PopupVisualState visualState);
        Task HidePopup(ContentView popup, PopupTransition transition, PopupVisualState? nextVisualState);
        Task ShowToast(View toast, ToastTransition transition = ToastTransition.SlideFromTop, ToastPlacement placement = ToastPlacement.Top);
        Task HideToast(ToastTransition transition = ToastTransition.SlideFromTop);
        void UpdateBars(BindableObject screen);
        event EventHandler? PopupBackgroundTapped;
        event EventHandler? PopupBackRequested;
    }

    /// <summary>
    /// Visual directives supplied by the popup service for presenter rendering.
    /// </summary>
    public sealed class PopupVisualState
    {
        public PopupVisualState(double overlayOpacity, bool isBlocking, bool allowBackgroundTap, PopupPlacement placement, Point? anchorPoint, Thickness margin, Thickness padding)
        {
            this.OverlayOpacity = overlayOpacity;
            this.IsBlocking = isBlocking;
            this.AllowBackgroundTap = allowBackgroundTap;
            this.Placement = placement;
            this.AnchorPoint = anchorPoint;
            this.Margin = margin;
            this.Padding = padding;
        }

        public double OverlayOpacity { get; }

        public bool IsBlocking { get; }

        public bool AllowBackgroundTap { get; }

        public PopupPlacement Placement { get; }

        public Point? AnchorPoint { get; }

        public Thickness Margin { get; }

        public Thickness Padding { get; }
    }
}
