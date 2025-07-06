using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Localization;

namespace NeuroAccessMaui.UI.Pages.Main.VerifyCode
{
	/// <summary>
	/// The view model to bind to when verifying a code.
	/// </summary>
	public partial class VerifyCodeViewModel : BaseViewModel
	{
		private readonly VerifyCodeNavigationArgs? navigationArgs;

		/// <summary>
		/// Creates a new instance of the <see cref="VerifyCodeViewModel"/> class.
		/// </summary>
		/// <param name="Args">Navigation arguments.</param>
		public VerifyCodeViewModel(VerifyCodeNavigationArgs? Args)
		{
			this.navigationArgs = Args;

			if (Args is not null)
				this.CodeVerification = Args.CodeVerification;
		}

		/// <inheritdoc />
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			LocalizationManager.Current.PropertyChanged += this.LocalizationManagerEventHandler;

			if (this.CodeVerification is not null)
				this.CodeVerification.CountDownTimer.Tick += this.CountDownEventHandler;

			this.LocalizationManagerEventHandler(null, new(null));
		}

		/// <inheritdoc/>
		public override async Task OnDisposeAsync()
		{
			LocalizationManager.Current.PropertyChanged -= this.LocalizationManagerEventHandler;

			if (this.CodeVerification is not null)
				this.CodeVerification.CountDownTimer.Tick -= this.CountDownEventHandler;

			if (this.navigationArgs?.VarifyCode is TaskCompletionSource<string> TaskSource)
				TaskSource.TrySetResult(string.Empty);

			await base.OnDisposeAsync();
		}

		public void LocalizationManagerEventHandler(object? sender, PropertyChangedEventArgs e)
		{
			this.OnPropertyChanged(nameof(this.LocalizedVerifyCodePageDetails));
			this.OnPropertyChanged(nameof(this.LocalizedResendCodeText));
		}

		private void CountDownEventHandler(object? sender, EventArgs e)
		{
			this.OnPropertyChanged(nameof(this.LocalizedResendCodeText));
		}

		/// <summary>
		/// Tries to set the Scan QR Code result and close the scan page.
		/// </summary>
		/// <param name="Url">The URL to set.</param>
		private async Task TrySetResultAndClosePage(string? Url)
		{
			if (this.navigationArgs?.VarifyCode is not null)
			{
				TaskCompletionSource<string?> TaskSource = this.navigationArgs.VarifyCode;
				this.navigationArgs.VarifyCode = null;

				await MainThread.InvokeOnMainThreadAsync(async () =>
				{
					try
					{
						await base.GoBack();
						TaskSource.TrySetResult(Url);
					}
					catch (Exception ex)
					{
						ServiceRef.LogService.LogException(ex);
					}
				});
			}
		}

		#region Properties

		[ObservableProperty]
		private ICodeVerification? codeVerification;

		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(VerifyCommand))]
		private string? verifyCodeText;

		public string LocalizedVerifyCodePageDetails
		{
			get
			{
				return ServiceRef.Localizer[nameof(AppResources.OnboardingVerifyCodePageDetails), this.navigationArgs?.PhoneOrEmail ?? string.Empty];
			}
		}

		public string LocalizedResendCodeText
		{
			get
			{
				if ((this.CodeVerification is not null) && (this.CodeVerification.CountDownSeconds > 0))
					return ServiceRef.Localizer[nameof(AppResources.ResendCodeSeconds), this.CodeVerification.CountDownSeconds];

				return ServiceRef.Localizer[nameof(AppResources.ResendCode)];
			}
		}

		#endregion

		public bool CanVerify => !string.IsNullOrEmpty(this.VerifyCodeText) && (this.VerifyCodeText.Length == 6);

		[RelayCommand(CanExecute = nameof(CanVerify))]
		public Task Verify()
		{
			return this.TrySetResultAndClosePage(this.VerifyCodeText);
		}

		/// <inheritdoc/>
		public override Task GoBack()
		{
			return this.TrySetResultAndClosePage(string.Empty);
		}
	}
}
