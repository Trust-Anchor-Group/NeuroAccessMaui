using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using NeuroAccessMaui;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.UI.QR;

namespace NeuroAccessMaui.UI.Pages.Onboarding.ViewModels
{
	public partial class WelcomeOnboardingStepViewModel : BaseOnboardingStepViewModel
	{
		private CancellationTokenSource? inviteCodeCts;
		private const int InviteCodeDebounceMs = 500;

		public WelcomeOnboardingStepViewModel()
			: base(OnboardingStep.Welcome)
		{
		}

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanContinue))]
		private bool isLoading = true;

		[ObservableProperty]
		private string? inviteCode;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanContinue))]
		private bool inviteCodeIsValid = true;

		public override string Title => AppResources.ActivateYourDigitalIdentity;

		public override string NextButtonText => AppResources.Continue;

		public bool CanContinue => !this.IsLoading && this.InviteCodeIsValid;

		internal override async Task OnActivatedAsync()
		{
			await base.OnActivatedAsync();
			if (this.IsLoading)
			{
				await Task.Delay(800).ConfigureAwait(false);
				MainThread.BeginInvokeOnMainThread(() => this.IsLoading = false);
			}
		}

		internal override Task<bool> OnNextAsync()
		{
			this.inviteCodeCts?.Cancel();
			return Task.FromResult(true);
		}

		partial void OnIsLoadingChanged(bool value)
		{
			this.OnPropertyChanged(nameof(this.CanContinue));
		}

		partial void OnInviteCodeChanged(string? value)
		{
			this.inviteCodeCts?.Cancel();

			if (string.IsNullOrWhiteSpace(value))
			{
				this.InviteCodeIsValid = true;
				return;
			}

			bool basicValid = BasicValidateInviteCode(value, out string trimmed);
			this.InviteCodeIsValid = basicValid;
			if (!basicValid)
				return;

			CancellationTokenSource cts = new();
			this.inviteCodeCts = cts;
			_ = Task.Run(async () =>
			{
				try
				{
					await Task.Delay(InviteCodeDebounceMs, cts.Token).ConfigureAwait(false);
					if (cts.IsCancellationRequested)
						return;
					if (!string.Equals(this.InviteCode, value, StringComparison.Ordinal))
						return;

					await QrCode.OpenUrl(trimmed, false).ConfigureAwait(false);
				}
				catch (OperationCanceledException)
				{
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
				}
				finally
				{
					cts.Dispose();
				}
			}, cts.Token);
		}

		[RelayCommand]
		private async Task PasteInviteCode()
		{
			if (!Clipboard.HasText)
				return;

			string? clipboardText = await Clipboard.GetTextAsync();
			if (string.IsNullOrWhiteSpace(clipboardText))
				return;

			await Clipboard.SetTextAsync(null);
			this.InviteCode = clipboardText;
		}

		[RelayCommand]
		private async Task ScanQrCode()
		{
			string? url = await QrCode.ScanQrCode(
				AppResources.QrPageTitleScanInvitation,
				[Constants.UriSchemes.Onboarding]).ConfigureAwait(false);

			if (string.IsNullOrWhiteSpace(url))
				return;

			string? scheme = Constants.UriSchemes.GetScheme(url);
			if (!string.Equals(scheme, Constants.UriSchemes.Onboarding, StringComparison.OrdinalIgnoreCase))
				return;

			MainThread.BeginInvokeOnMainThread(() => this.InviteCode = url);
		}

		internal override Task OnBackAsync()
		{
			this.inviteCodeCts?.Cancel();
			return Task.CompletedTask;
		}

		private static bool BasicValidateInviteCode(string code, out string trimmed)
		{
			trimmed = code.Trim();
			if (string.IsNullOrEmpty(trimmed))
				return true;

			if (!trimmed.StartsWith(Constants.UriSchemes.Onboarding + ":", StringComparison.OrdinalIgnoreCase))
				return false;

			string[] parts = trimmed.Split(':');
			if (parts.Length != 5)
				return false;

			string domain = parts[1];
			string codePart = parts[2];
			string keyPart = parts[3];
			string ivPart = parts[4];

			if (string.IsNullOrWhiteSpace(domain) || domain.Contains(' '))
				return false;

			if (string.IsNullOrWhiteSpace(codePart))
				return false;

			try
			{
				_ = Convert.FromBase64String(keyPart);
				_ = Convert.FromBase64String(ivPart);
			}
			catch
			{
				return false;
			}

			return true;
		}
	}
}
