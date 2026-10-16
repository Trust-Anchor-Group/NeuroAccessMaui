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
	/// Code-behind only runs purely visual behavior: page transitions, scrolling, and drawing attention to a field. Flow
	/// state lives in <see cref="KycProcessViewModel"/>. With reduced motion, pages switch without sliding and fields are
	/// scrolled into view without being shaken or faded.
	/// </remarks>
	public partial class KycProcessPage : BaseContentPage
	{
		private const double slideDistance = 28;
		private const uint transitionLength = 280;
		private static readonly TimeSpan fieldRevealDelay = TimeSpan.FromMilliseconds(180);

		private readonly KycProcessViewModel viewModel;
		private bool hasPresentedStep;
		private int presentedStepPosition;

		/// <summary>
		/// Initializes a new instance of the <see cref="KycProcessPage"/> class.
		/// </summary>
		/// <param name="Vm">The page view model.</param>
		public KycProcessPage(KycProcessViewModel Vm)
		{
			this.InitializeComponent();
			this.ContentPageModel = Vm;
			this.viewModel = Vm;

			Vm.ScrollToTop += this.OnScrollToTop;
			Vm.ValidationFailed += this.OnValidationFailed;
			Vm.FieldRevealRequested += this.OnFieldRevealRequested;
			Vm.PropertyChanged += this.OnViewModelPropertyChanged;
		}

		/// <inheritdoc/>
		public override async Task OnDisposeAsync()
		{
			this.viewModel.ScrollToTop -= this.OnScrollToTop;
			this.viewModel.ValidationFailed -= this.OnValidationFailed;
			this.viewModel.FieldRevealRequested -= this.OnFieldRevealRequested;
			this.viewModel.PropertyChanged -= this.OnViewModelPropertyChanged;
			await base.OnDisposeAsync();
		}

		private void OnViewModelPropertyChanged(object? Sender, PropertyChangedEventArgs E)
		{
			if (E.PropertyName != nameof(KycProcessViewModel.CurrentPagePosition) &&
				E.PropertyName != nameof(KycProcessViewModel.IsInSummary) &&
				E.PropertyName != nameof(KycProcessViewModel.IsInIdContents) &&
				E.PropertyName != nameof(KycProcessViewModel.IsLoading))
			{
				return;
			}

			this.Dispatcher.Dispatch(this.PresentCurrentStep);
		}

		/// <summary>
		/// Slides the new page, the ID contents step, or the summary in from the direction of travel when the visible step changes.
		/// </summary>
		private void PresentCurrentStep()
		{
			int StepPosition = this.GetStepPosition();

			if (!this.hasPresentedStep || this.viewModel.IsLoading)
			{
				this.hasPresentedStep = !this.viewModel.IsLoading;
				this.presentedStepPosition = StepPosition;
				return;
			}

			if (StepPosition == this.presentedStepPosition)
				return;

			int Direction = StepPosition > this.presentedStepPosition ? 1 : -1;
			this.presentedStepPosition = StepPosition;

			_ = SlideInAsync(this.GetStepContent(), Direction);
		}

		/// <summary>
		/// Gets the position of the visible step in the flow: form pages by index, then the ID contents step, then the summary.
		/// </summary>
		/// <returns>A value that grows in the direction of travel.</returns>
		private int GetStepPosition()
		{
			if (this.viewModel.IsInSummary)
				return int.MaxValue;

			if (this.viewModel.IsInIdContents)
				return int.MaxValue - 1;

			return this.viewModel.CurrentPagePosition;
		}

		/// <summary>
		/// Gets the content of the visible step.
		/// </summary>
		/// <returns>The layout to animate in.</returns>
		private VisualElement GetStepContent()
		{
			if (this.viewModel.IsInSummary)
				return this.SummaryContent;

			if (this.viewModel.IsInIdContents)
				return this.IdContentsContent;

			return this.FormContent;
		}

		private static async Task SlideInAsync(VisualElement Content, int Direction)
		{
			Content.CancelAnimations();

			if (MotionPreferences.ReduceMotion)
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
			this.Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(16), () =>
			{
				ScrollView Target = this.viewModel.IsInSummary
					? this.SummaryScroll
					: this.viewModel.IsInIdContents ? this.IdContentsScroll : this.FormScroll;
				_ = ScrollToTopAsync(Target);
			});
		}

		private static async Task ScrollToTopAsync(ScrollView Scroll)
		{
			try
			{
				await Scroll.ScrollToAsync(0, 0, false);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private void OnValidationFailed(object? Sender, ObservableKycField Field)
		{
			this.Dispatcher.Dispatch(() => this.RevealWhenLaidOut(Field, this.RevealInvalidFieldAsync));
		}

		private void OnFieldRevealRequested(object? Sender, ObservableKycField Field)
		{
			// Wait for the new page to be laid out so the field has a position to scroll to.
			this.Dispatcher.DispatchDelayed(fieldRevealDelay, () => this.RevealWhenLaidOut(Field, this.RevealFieldAsync));
		}

		/// <summary>
		/// Runs a reveal for the view of a field, waiting once for layout when the field is on a page that was just opened.
		/// </summary>
		/// <param name="Field">The field to reveal.</param>
		/// <param name="Reveal">The reveal to run on the field's view.</param>
		private void RevealWhenLaidOut(ObservableKycField Field, Func<VisualElement, Task> Reveal)
		{
			VisualElement? Target = this.FindFieldView(Field);
			if (Target is not null && Target.Height > 0)
			{
				_ = Reveal(Target);
				return;
			}

			this.Dispatcher.DispatchDelayed(fieldRevealDelay, () =>
			{
				VisualElement? LaidOutTarget = this.FindFieldView(Field);
				if (LaidOutTarget is not null)
					_ = Reveal(LaidOutTarget);
			});
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
		/// Scrolls a field opened from the summary into view and briefly highlights it.
		/// </summary>
		/// <param name="Target">The view of the field to reveal.</param>
		/// <returns>A task that completes when the field has been revealed.</returns>
		private async Task RevealFieldAsync(VisualElement Target)
		{
			bool ReduceMotion = MotionPreferences.ReduceMotion;

			try
			{
				await this.FormScroll.ScrollToAsync(Target, ScrollToPosition.Center, !ReduceMotion);
				SetSemanticFocus(Target);

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
		/// Scrolls the first invalid field into view and nudges it so the user sees what is missing. With reduced motion
		/// the field is only scrolled into view, where its validation message highlights it.
		/// </summary>
		/// <param name="Target">The view of the first invalid field on the page.</param>
		/// <returns>A task that completes when the field has been revealed.</returns>
		private async Task RevealInvalidFieldAsync(VisualElement Target)
		{
			bool ReduceMotion = MotionPreferences.ReduceMotion;

			try
			{
				await this.FormScroll.ScrollToAsync(Target, ScrollToPosition.Center, !ReduceMotion);
				SetSemanticFocus(Target);

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

		/// <summary>
		/// Moves screen reader focus to a field, so users of assistive technology land on the field that needs attention.
		/// </summary>
		/// <param name="Target">The view to focus.</param>
		private static void SetSemanticFocus(VisualElement Target)
		{
			try
			{
				Target.SetSemanticFocus();
			}
			catch (Exception)
			{
				// Semantic focus is best effort.
			}
		}
	}
}
