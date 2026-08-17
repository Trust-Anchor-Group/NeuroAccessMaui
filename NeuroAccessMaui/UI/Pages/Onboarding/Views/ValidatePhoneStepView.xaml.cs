using System;
using NeuroAccessMaui.UI.Pages.Onboarding.ViewModels;

namespace NeuroAccessMaui.UI.Pages.Onboarding.Views
{
	public partial class ValidatePhoneStepView : BaseOnboardingView
	{
		private const int requiredTaps = 7;
		private static readonly TimeSpan tapWindow = TimeSpan.FromSeconds(2);

		private int tapCount;
		private DateTime lastTapUtc = DateTime.MinValue;

		public static ValidatePhoneStepView Create()
		{
			return Create<ValidatePhoneStepView>();
		}

		public ValidatePhoneStepView(ValidatePhoneOnboardingStepViewModel viewModel)
		{
			this.InitializeComponent();
			this.ContentViewModel = viewModel;

			TapGestureRecognizer Recognizer = new();
			Recognizer.Tapped += this.HiddenTestModeTapped;
			this.TitleLabel.GestureRecognizers.Add(Recognizer);
		}

		private async void HiddenTestModeTapped(object? Sender, TappedEventArgs E)
		{
			DateTime Now = DateTime.UtcNow;
			if ((Now - this.lastTapUtc) > tapWindow)
				this.tapCount = 0;

			this.lastTapUtc = Now;
			this.tapCount++;

			if (this.tapCount < requiredTaps)
				return;

			this.tapCount = 0;
			if (this.ContentViewModel is ValidatePhoneOnboardingStepViewModel ViewModel)
				await ViewModel.ActivateHiddenTestModeCommand.ExecuteAsync(null);
		}
	}
}
