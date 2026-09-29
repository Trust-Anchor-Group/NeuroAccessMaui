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
				this.presentedState = ViewModel.ScanVisualState;
				ViewModel.PropertyChanged -= this.OnViewModelPropertyChanged;
				ViewModel.PropertyChanged += this.OnViewModelPropertyChanged;
				this.ScanVisual.IsAnimationActive = true;
			});
		}

		/// <inheritdoc/>
		public override async Task OnDisappearingAsync()
		{
			await this.Dispatcher.DispatchAsync(() =>
			{
				this.ViewModel<KycTravelDocumentViewModel>().PropertyChanged -= this.OnViewModelPropertyChanged;
				this.ScanVisual.IsAnimationActive = false;
				this.MessageBlock.CancelAnimations();
				this.MessageBlock.Opacity = 1;
				this.MessageBlock.TranslationY = 0;
			});
			await base.OnDisappearingAsync();
		}

		private void OnViewModelPropertyChanged(object? Sender, PropertyChangedEventArgs E)
		{
			if (E.PropertyName != nameof(KycTravelDocumentViewModel.ScanVisualState))
				return;

			this.Dispatcher.Dispatch(() =>
			{
				NfcScanVisualState State = this.ViewModel<KycTravelDocumentViewModel>().ScanVisualState;
				if (State == this.presentedState)
					return;

				this.presentedState = State;
				_ = this.RevealMessageAsync();
			});
		}

		/// <summary>
		/// Fades and lifts the headline and status into place when the flow moves to a new state.
		/// </summary>
		/// <returns>A task that completes when the transition ends.</returns>
		private async Task RevealMessageAsync()
		{
			VisualElement Block = this.MessageBlock;
			Block.CancelAnimations();

			if (ServiceRef.Provider.GetService<IMotionSettings>()?.ReduceMotion == true)
			{
				Block.Opacity = 1;
				Block.TranslationY = 0;
				return;
			}

			try
			{
				Block.Opacity = 0;
				Block.TranslationY = 10;
				await Task.WhenAll(
					Block.FadeToAsync(1, 260, Easing.CubicOut),
					Block.TranslateToAsync(0, 0, 260, Easing.CubicOut));
			}
			catch (Exception Ex)
			{
				Block.Opacity = 1;
				Block.TranslationY = 0;
				ServiceRef.LogService.LogException(Ex);
			}
		}
	}
}
