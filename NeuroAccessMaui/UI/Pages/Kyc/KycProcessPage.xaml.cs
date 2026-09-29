using System.ComponentModel;
using NeuroAccessMaui.Animations;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Kyc.ViewModels;

namespace NeuroAccessMaui.UI.Pages.Kyc
{
	/// <summary>
	/// Page for filling in, reviewing, and sending a KYC identity application.
	/// </summary>
	/// <remarks>
	/// Code-behind only runs purely visual behavior: page transitions, scrolling, and the celebration after sending.
	/// Flow state lives in <see cref="KycProcessViewModel"/>.
	/// </remarks>
	public partial class KycProcessPage : BaseContentPage
	{
		private const double slideDistance = 28;
		private const uint transitionLength = 280;
		private static readonly TimeSpan fieldRevealDelay = TimeSpan.FromMilliseconds(180);

		private bool hasPresentedStep;
		private bool presentedSummary;
		private int presentedPageIndex;

		/// <summary>
		/// Initializes a new instance of the <see cref="KycProcessPage"/> class.
		/// </summary>
		/// <param name="ViewModel">The page view model.</param>
		public KycProcessPage(KycProcessViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentPageModel = ViewModel;

			ViewModel.ScrollToTop += this.OnScrollToTop;
			ViewModel.ValidationFailed += this.OnValidationFailed;
			ViewModel.FieldRevealRequested += this.OnFieldRevealRequested;
			ViewModel.ApplicationSubmitted += this.OnApplicationSubmitted;
			ViewModel.PropertyChanged += this.OnViewModelPropertyChanged;
		}

		private static bool IsReducedMotion
		{
			get
			{
				try
				{
					return ServiceRef.Provider.GetService<IMotionSettings>()?.ReduceMotion == true;
				}
				catch (Exception)
				{
					return false;
				}
			}
		}

		private void OnViewModelPropertyChanged(object? Sender, PropertyChangedEventArgs E)
		{
			if (E.PropertyName != nameof(KycProcessViewModel.CurrentPagePosition) &&
				E.PropertyName != nameof(KycProcessViewModel.IsInSummary) &&
				E.PropertyName != nameof(KycProcessViewModel.IsLoading))
			{
				return;
			}

			this.Dispatcher.Dispatch(this.PresentCurrentStep);
		}

		/// <summary>
		/// Slides the new page or the summary in from the direction of travel when the visible step changes.
		/// </summary>
		private void PresentCurrentStep()
		{
			KycProcessViewModel ViewModel = this.ViewModel<KycProcessViewModel>();
			bool InSummary = ViewModel.IsInSummary;
			int PageIndex = ViewModel.CurrentPagePosition;

			if (!this.hasPresentedStep || ViewModel.IsLoading)
			{
				this.hasPresentedStep = !ViewModel.IsLoading;
				this.presentedSummary = InSummary;
				this.presentedPageIndex = PageIndex;
				return;
			}

			if (InSummary == this.presentedSummary && (InSummary || PageIndex == this.presentedPageIndex))
				return;

			int Direction;
			if (InSummary != this.presentedSummary)
				Direction = InSummary ? 1 : -1;
			else
				Direction = PageIndex >= this.presentedPageIndex ? 1 : -1;

			this.presentedSummary = InSummary;
			this.presentedPageIndex = PageIndex;

			_ = SlideInAsync(InSummary ? this.SummaryContent : this.FormContent, Direction);
		}

		private static async Task SlideInAsync(VisualElement Content, int Direction)
		{
			Content.CancelAnimations();

			if (IsReducedMotion)
			{
				Content.Opacity = 1;
				Content.TranslationX = 0;
				return;
			}

			try
			{
				Content.Opacity = 0;
				Content.TranslationX = slideDistance * Direction;
				await Task.WhenAll(
					Content.FadeToAsync(1, transitionLength, Easing.CubicOut),
					Content.TranslateToAsync(0, 0, transitionLength, Easing.CubicOut));
			}
			catch (Exception)
			{
				Content.Opacity = 1;
				Content.TranslationX = 0;
			}
		}

		private void OnScrollToTop(object? Sender, EventArgs E)
		{
			// Dispatch so the scroll happens after the view model has finished switching between form and summary.
			this.Dispatcher.Dispatch(() =>
			{
				ScrollView Target = this.ViewModel<KycProcessViewModel>().IsInSummary ? this.SummaryScroll : this.FormScroll;
				_ = ScrollToTopAsync(Target, false);
			});
		}

		private static async Task ScrollToTopAsync(ScrollView Scroll, bool Animated)
		{
			try
			{
				await Scroll.ScrollToAsync(0, 0, Animated);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private void OnValidationFailed(object? Sender, ObservableKycField Field)
		{
			this.Dispatcher.Dispatch(() => _ = this.RevealInvalidFieldAsync(Field));
		}

		private void OnFieldRevealRequested(object? Sender, ObservableKycField Field)
		{
			// Wait for the new page to be laid out so the field has a position to scroll to.
			this.Dispatcher.DispatchDelayed(fieldRevealDelay, () => _ = this.RevealFieldAsync(Field));
		}

		/// <summary>
		/// Scrolls a field opened from the summary into view and briefly highlights it.
		/// </summary>
		/// <param name="Field">The field to reveal.</param>
		/// <returns>A task that completes when the field has been revealed.</returns>
		private async Task RevealFieldAsync(ObservableKycField Field)
		{
			VisualElement? Target = this.FindFieldView(Field);
			if (Target is null)
				return;

			bool ReduceMotion = IsReducedMotion;

			try
			{
				await this.FormScroll.ScrollToAsync(Target, ScrollToPosition.Center, !ReduceMotion);

				if (ReduceMotion)
					return;

				Target.CancelAnimations();
				Target.Opacity = 0.35;
				await Target.FadeToAsync(1, 600, Easing.CubicOut);
			}
			catch (Exception Ex)
			{
				Target.Opacity = 1;
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Finds the root view of a field template in the form.
		/// </summary>
		/// <param name="Field">The field whose view to find.</param>
		/// <returns>The outermost view bound to the field, or <c>null</c> if it is not on the current page.</returns>
		private VisualElement? FindFieldView(ObservableKycField Field)
		{
			return this.FormContent
				.GetVisualTreeDescendants()
				.OfType<VisualElement>()
				.FirstOrDefault(Element => ReferenceEquals(Element.BindingContext, Field));
		}

		/// <summary>
		/// Scrolls the first invalid field into view and nudges it so the user sees what is missing.
		/// </summary>
		/// <param name="Field">The first invalid field on the page.</param>
		/// <returns>A task that completes when the field has been revealed.</returns>
		private async Task RevealInvalidFieldAsync(ObservableKycField Field)
		{
			VisualElement? Target = this.FindFieldView(Field);
			if (Target is null)
				return;

			bool ReduceMotion = IsReducedMotion;

			try
			{
				await this.FormScroll.ScrollToAsync(Target, ScrollToPosition.Center, !ReduceMotion);

				if (ReduceMotion)
					return;

				Target.CancelAnimations();
				await Target.TranslateToAsync(-8, 0, 60, Easing.CubicOut);
				await Target.TranslateToAsync(7, 0, 80, Easing.CubicInOut);
				await Target.TranslateToAsync(-4, 0, 80, Easing.CubicInOut);
				await Target.TranslateToAsync(0, 0, 70, Easing.CubicOut);
			}
			catch (Exception Ex)
			{
				Target.TranslationX = 0;
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private void OnApplicationSubmitted(object? Sender, EventArgs E)
		{
			this.Dispatcher.Dispatch(() =>
			{
				_ = ScrollToTopAsync(this.SummaryScroll, !IsReducedMotion);
				this.SentVisual.Celebrate();
				_ = SlideUpAsync(this.SentPanel);
			});
		}

		private static async Task SlideUpAsync(VisualElement Content)
		{
			Content.CancelAnimations();

			if (IsReducedMotion)
			{
				Content.Opacity = 1;
				Content.TranslationY = 0;
				return;
			}

			try
			{
				Content.Opacity = 0;
				Content.TranslationY = 16;
				await Task.WhenAll(
					Content.FadeToAsync(1, 320, Easing.CubicOut),
					Content.TranslateToAsync(0, 0, 320, Easing.CubicOut));
			}
			catch (Exception)
			{
				Content.Opacity = 1;
				Content.TranslationY = 0;
			}
		}
	}
}
