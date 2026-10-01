using DevStorybook.Controls;
using DevStorybook.Services;
using DevStorybook.Stories;
using DevStorybook.ViewModels;
using NeuroAccessMaui.Services.Data;
using NeuroAccessMaui.UI.Pages.Main.VerifyCode;
using NeuroAccessMaui.UI.Pages.Onboarding.Views;

namespace DevStorybook.Pages
{
	/// <summary>
	/// Renders a registered production screen or component with controlled fake state.
	/// </summary>
	public partial class StoryViewerPage : ContentPage
	{
		private readonly StoryViewerViewModel viewModel;
		private readonly OnboardingPreviewViewModel onboardingPreviewViewModel;
		private bool isExitingFullscreen;

		/// <summary>
		/// Initializes a new instance of the <see cref="StoryViewerPage"/> class.
		/// </summary>
		/// <param name="ViewModel">The controlled story ViewModel.</param>
		public StoryViewerPage(StoryViewerViewModel ViewModel)
		{
			this.InitializeComponent();
			this.viewModel = ViewModel;
			this.onboardingPreviewViewModel = new OnboardingPreviewViewModel();
			this.FullscreenPreviewHost.ExitRequested += this.OnFullscreenExitRequested;
			this.Loaded += this.OnPageLoaded;
			this.BindingContext = ViewModel;
		}

		/// <summary>
		/// Loads a story into the viewer without requiring prior application state.
		/// </summary>
		/// <param name="Story">The story to render.</param>
		public void LoadStory(StoryDefinition Story)
		{
			bool IsFullscreen = Story.StoryType == StoryType.FullScreen;
			this.RenderErrorPanel.IsVisible = false;
			NavigationPage.SetHasNavigationBar(this, !IsFullscreen);
			this.StorybookChrome.IsVisible = !IsFullscreen;
			this.FullscreenPreviewHost.IsVisible = IsFullscreen;

			try
			{
				this.viewModel.LoadStory(Story);
				this.LoadProductionStory(Story, IsFullscreen);
			}
			catch (Exception RenderException)
			{
				this.ShowRenderFailure(Story, RenderException);
			}
		}

		private void LoadProductionStory(StoryDefinition Story, bool IsFullscreen)
		{
			this.ClearHosts();
			if (Story.StoryType == StoryType.Component)
				return;

			this.onboardingPreviewViewModel.LoadState(Story.State);
			View ProductionContent;
			if (IsVerificationCodeState(Story.State))
			{
				VerifyCodePage VerificationPage = new VerifyCodePage(this.onboardingPreviewViewModel);
				VerificationPage.BindingContext = this.onboardingPreviewViewModel;
				ProductionContent = VerificationPage;
				if (!IsFullscreen)
				{
					this.ApplyStandaloneBottomInset();
					this.StandaloneStoryHost.Content = ProductionContent;
					this.StandaloneStoryHost.IsVisible = true;
					return;
				}
			}
			else
			{
				View ProductionView = this.CreateOnboardingView(Story.State);
				ProductionView.BindingContext = Story.Screen == "Phone Verification"
					? this.viewModel
					: this.onboardingPreviewViewModel;

				if (!IsFullscreen)
				{
					this.OnboardingPreviewHost.PreviewContent = ProductionView;
					this.OnboardingPreviewHost.IsVisible = true;
					return;
				}

				OnboardingPreviewHost ProductionContext = new OnboardingPreviewHost
				{
					UseSystemBottomInset = false,
					PreviewContent = ProductionView
				};
				ProductionContent = ProductionContext;
			}

			this.FullscreenPreviewHost.PreviewContent = ProductionContent;
		}

		private void OnPageLoaded(object? Sender, EventArgs Args)
		{
			this.ApplyStandaloneBottomInset();
		}

		private void ApplyStandaloneBottomInset()
		{
			double BottomInset = StorybookSystemInsets.GetBottomInset();
			this.StandaloneStoryHost.Padding = new Thickness(0.0, 0.0, 0.0, BottomInset);
		}

		private void ClearHosts()
		{
			this.RenderErrorPanel.IsVisible = false;
			this.OnboardingPreviewHost.PreviewContent = null;
			this.OnboardingPreviewHost.IsVisible = false;
			this.StandaloneStoryHost.Content = null;
			this.StandaloneStoryHost.IsVisible = false;
			this.FullscreenPreviewHost.PreviewContent = null;
		}

		private void ShowRenderFailure(StoryDefinition Story, Exception RenderException)
		{
			this.ClearHosts();
			NavigationPage.SetHasNavigationBar(this, true);
			this.StorybookChrome.IsVisible = true;
			this.FullscreenPreviewHost.IsVisible = false;
			this.RenderErrorMessage.Text = $"{Story.DisplayName} could not be rendered. {RenderException.GetType().Name}: {RenderException.Message}";
			this.RenderErrorPanel.IsVisible = true;
			System.Diagnostics.Debug.WriteLine(RenderException);
		}

		private View CreateOnboardingView(PhoneVerificationStoryState State)
		{
			return State switch
			{
				PhoneVerificationStoryState.Default or
				PhoneVerificationStoryState.Empty or
				PhoneVerificationStoryState.ValidPhoneNumber or
				PhoneVerificationStoryState.InvalidPhoneNumber or
				PhoneVerificationStoryState.SendingCode or
				PhoneVerificationStoryState.CodeSent or
				PhoneVerificationStoryState.BackendError or
				PhoneVerificationStoryState.NetworkError => new PhoneVerificationView(),
				PhoneVerificationStoryState.IdentityProvider => new WelcomeStepView(),
				PhoneVerificationStoryState.EmailEmpty or
				PhoneVerificationStoryState.EmailValid or
				PhoneVerificationStoryState.EmailInvalid => new ValidateEmailStepView(),
				PhoneVerificationStoryState.UsernameEmpty or
				PhoneVerificationStoryState.UsernameValid or
				PhoneVerificationStoryState.UsernameInvalid => new NameEntryStepView(),
				PhoneVerificationStoryState.AccountCreation => new CreateAccountStepView(),
				PhoneVerificationStoryState.PinEmpty or
				PhoneVerificationStoryState.PinValid or
				PhoneVerificationStoryState.PinMismatch => new DefinePasswordStepView(),
				PhoneVerificationStoryState.Biometrics => new BiometricsStepView(),
				PhoneVerificationStoryState.Success => new FinalizeStepView(),
				_ => throw new InvalidOperationException($"No full-screen renderer exists for {State}.")
			};
		}

		private static bool IsVerificationCodeState(PhoneVerificationStoryState State)
		{
			return State is >= PhoneVerificationStoryState.PhoneCodeDefault
				and <= PhoneVerificationStoryState.EmailCodeBackendError;
		}

		private async void OnFullscreenExitRequested(object? Sender, EventArgs Args)
		{
			if (this.isExitingFullscreen)
				return;

			this.isExitingFullscreen = true;
			try
			{
				await this.Navigation.PopAsync();
			}
			finally
			{
				this.isExitingFullscreen = false;
			}
		}

		private void PhoneCountrySelectionView_CountrySelected(object? Sender, ISO_3166_Country SelectedCountry)
		{
			this.viewModel.SelectCountry(SelectedCountry);
		}
	}
}
