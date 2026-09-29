using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using NeuroAccessMaui.Animations;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Controls;

namespace NeuroAccessMaui.UI.Pages.Kyc
{
	/// <summary>
	/// Page for guided travel-document MRZ and NFC evidence capture.
	/// </summary>
	/// <remarks>
	/// Code-behind only runs purely visual transitions; flow state lives in <see cref="KycTravelDocumentViewModel"/>.
	/// </remarks>
	public partial class KycTravelDocumentPage : BaseContentPage
	{
		private NfcScanVisualState? presentedState;

		/// <summary>
		/// Initializes a new instance of the <see cref="KycTravelDocumentPage"/> class.
		/// </summary>
		/// <param name="ViewModel">The page view model.</param>
		public KycTravelDocumentPage(KycTravelDocumentViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentPageModel = ViewModel;
		}

		/// <inheritdoc/>
		public override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();
			await this.Dispatcher.DispatchAsync(() =>
			{
				KycTravelDocumentViewModel ViewModel = this.ViewModel<KycTravelDocumentViewModel>();
				ViewModel.PropertyChanged -= this.OnViewModelPropertyChanged;
				ViewModel.PropertyChanged += this.OnViewModelPropertyChanged;
				this.ScanVisual.IsAnimationActive = true;

				// The chip path usually starts on the scanner page, so the state may have moved on while this page was hidden.
				this.PresentState(ViewModel.ScanVisualState);
			});
		}

		/// <inheritdoc/>
		public override async Task OnDisappearingAsync()
		{
			await this.Dispatcher.DispatchAsync(() =>
			{
				this.ViewModel<KycTravelDocumentViewModel>().PropertyChanged -= this.OnViewModelPropertyChanged;
				this.ScanVisual.IsAnimationActive = false;
				ResetTransition(this.MessageBlock);
				ResetTransition(this.Stepper);
			});
			await base.OnDisappearingAsync();
		}

		private void OnViewModelPropertyChanged(object? Sender, PropertyChangedEventArgs E)
		{
			if (E.PropertyName != nameof(KycTravelDocumentViewModel.ScanVisualState))
				return;

			this.Dispatcher.Dispatch(() => this.PresentState(this.ViewModel<KycTravelDocumentViewModel>().ScanVisualState));
		}

		/// <summary>
		/// Animates the message, and the step guide when it first appears, when the flow moves to a new state.
		/// The first state shown is presented without animation.
		/// </summary>
		/// <param name="State">The state now shown by the view model.</param>
		private void PresentState(NfcScanVisualState State)
		{
			NfcScanVisualState? Previous = this.presentedState;
			if (State == Previous)
				return;

			this.presentedState = State;
			if (Previous is null)
				return;

			_ = RevealAsync(this.MessageBlock, 10);

			if (Previous == NfcScanVisualState.Intro && State != NfcScanVisualState.Intro)
				_ = RevealAsync(this.Stepper, -8);
		}

		private static void ResetTransition(VisualElement Element)
		{
			Element.CancelAnimations();
			Element.Opacity = 1;
			Element.TranslationY = 0;
		}

		/// <summary>
		/// Fades an element in while it slides into place from a small vertical offset.
		/// </summary>
		/// <param name="Element">The element to reveal.</param>
		/// <param name="Offset">The starting vertical offset; positive values rise into place, negative values settle down.</param>
		/// <returns>A task that completes when the transition ends.</returns>
		private static async Task RevealAsync(VisualElement Element, double Offset)
		{
			Element.CancelAnimations();

			if (ServiceRef.Provider.GetService<IMotionSettings>()?.ReduceMotion == true)
			{
				Element.Opacity = 1;
				Element.TranslationY = 0;
				return;
			}

			try
			{
				Element.Opacity = 0;
				Element.TranslationY = Offset;
				await Task.WhenAll(
					Element.FadeToAsync(1, 260, Easing.CubicOut),
					Element.TranslateToAsync(0, 0, 260, Easing.CubicOut));
			}
			catch (Exception Ex)
			{
				Element.Opacity = 1;
				Element.TranslationY = 0;
				ServiceRef.LogService.LogException(Ex);
			}
		}
	}
}
