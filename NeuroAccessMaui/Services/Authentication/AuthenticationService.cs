using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.Settings;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.UI.Popups;
using NeuroAccessMaui.UI.Popups.Password;
using Waher.Events;
using Waher.Runtime.Inventory;
using Waher.Security;
using Waher.Security.LoginMonitor;
using IPopupService = NeuroAccessMaui.Services.UI.Popups.IPopupService;
using PopupOptions = NeuroAccessMaui.Services.UI.Popups.PopupOptions;

namespace NeuroAccessMaui.Services.Authentication
{
	[Singleton]
	public class AuthenticationService : IAuthenticationService, IDisposable, IAsyncDisposable
	{
		private readonly ITagProfile tagProfile;
		private readonly IUiService uiService;
		private readonly IPopupService popupService;
		private readonly IPlatformSpecific platformSpecific;
		private readonly ISettingsService settingsService;
		private readonly IStringLocalizer localizer;
		private readonly LoginAuditor loginAuditor;

		private DateTime lastAuthenticationTime = DateTime.MinValue;
		private DateTime savedStartTime = DateTime.MinValue;
		private bool displayedPasswordPopup = false;

		private static readonly LoginInterval[] loginIntervals =
				[
					new(Constants.Password.FirstMaxPasswordAttempts, TimeSpan.FromHours(Constants.Password.FirstBlockInHours)),
					new(Constants.Password.SecondMaxPasswordAttempts, TimeSpan.FromHours(Constants.Password.SecondBlockInHours)),
					new(Constants.Password.ThirdMaxPasswordAttempts, TimeSpan.FromHours(Constants.Password.ThirdBlockInHours))
				];

		public AuthenticationService()
		{
			this.tagProfile = ServiceRef.Provider.GetRequiredService<TagProfile>();

			this.uiService = ServiceRef.Provider.GetRequiredService<IUiService>();
			this.popupService = ServiceRef.Provider.GetRequiredService<IPopupService>();
			this.platformSpecific = ServiceRef.Provider.GetRequiredService<IPlatformSpecific>();
			this.loginAuditor = new LoginAuditor(Constants.Password.LogAuditorObjectID, loginIntervals);
			this.settingsService = ServiceRef.Provider.GetRequiredService<ISettingsService>();
			this.localizer = ServiceRef.Localizer;
		}

		/// <summary>
		/// Prompts for password if required by user profile.
		/// </summary>
		public async Task<string?> InputPasswordAsync(AuthenticationPurpose purpose)
		{
			if (!this.tagProfile.HasLocalPassword)
				return string.Empty;

			return await this.InputPasswordInternalAsync(purpose);
		}

		private async Task<string?> InputPasswordInternalAsync(AuthenticationPurpose purpose)
		{
			this.displayedPasswordPopup = true;
			try
			{
				if (!this.tagProfile.HasLocalPassword)
					return string.Empty;

				CheckPasswordViewModel ViewModel = new(purpose);
				string? Result = await this.popupService.PushAsync<CheckPasswordPopup, CheckPasswordViewModel, string>(ViewModel, new PopupOptions
				{
					CloseOnBackButton = true,
					CloseOnBackgroundTap = true
				});
				await this.CheckUserBlockingAsync();
				return Result;
			}
			catch
			{
				return null;
			}
			finally
			{
				this.displayedPasswordPopup = false;
			}
		}

		/// <summary>
		/// Main entry point for authentication UI flow (password or biometrics).
		/// </summary>
		public Task<bool> AuthenticateUserAsync(AuthenticationPurpose purpose, bool force = false)
		{
			// MainThread is needed for Maui UI operations
			TaskCompletionSource<bool> Tcs = new TaskCompletionSource<bool>();

			MainThread.BeginInvokeOnMainThread(async () =>
			{
				bool Result = await this.AuthenticateUserOnMainThreadAsync(purpose, force);
				if (Result)
					this.lastAuthenticationTime = DateTime.UtcNow;
				Tcs.TrySetResult(Result);
			});
			return Tcs.Task;
		}

		private async Task<bool> AuthenticateUserOnMainThreadAsync(AuthenticationPurpose purpose, bool force = false)
		{
			bool needToVerifyPassword = this.IsInactivitySafeIntervalPassed();
			if (!force && !needToVerifyPassword)
				return true;

			switch (this.tagProfile.AuthenticationMethod)
			{
				case AuthenticationMethod.Fingerprint:
					if (!this.platformSpecific.SupportsFingerprintAuthentication)
						this.tagProfile.AuthenticationMethod = AuthenticationMethod.Password;

					if (await this.platformSpecific.AuthenticateUserFingerprint(
						this.localizer[nameof(AppResources.FingerprintTitle)],
						null,
						this.localizer[nameof(AppResources.FingerprintDescription)],
						this.localizer[nameof(AppResources.Cancel)],
						null))
					{
						await this.UserAuthenticationSuccessfulAsync();
						return true;
					}
					goto case AuthenticationMethod.Password;

				case AuthenticationMethod.Password:
					if (!this.tagProfile.HasLocalPassword)
						return true;

					if (this.displayedPasswordPopup)
						return false;

					return await this.InputPasswordInternalAsync(purpose) is not null;

				default:
					return false;
			}
		}

		/// <summary>
		/// Checks if user is blocked from authentication and shows UI as needed.
		/// </summary>
		public async Task CheckUserBlockingAsync()
		{
			DateTime? nextLoginTime = await this.loginAuditor.GetEarliestLoginOpportunity(Constants.Password.RemoteEndpoint, Constants.Password.Protocol);
			if (nextLoginTime.HasValue)
			{
				string messageAlert;
				if (nextLoginTime == DateTime.MaxValue)
				{
					messageAlert = this.localizer[nameof(AppResources.PasswordIsInvalidAplicationBlockedForever)];
				}
				else
				{
					DateTime localDateTime = nextLoginTime.Value.ToLocalTime();
					if (nextLoginTime.Value.ToShortDateString() == DateTime.Today.ToShortDateString())
					{
						string dateString = localDateTime.ToShortTimeString();
						messageAlert = this.localizer[nameof(AppResources.PasswordIsInvalidAplicationBlocked), dateString];
					}
					else if (nextLoginTime.Value.ToShortDateString() == DateTime.Today.AddDays(1).ToShortDateString())
					{
						string dateString = localDateTime.ToShortTimeString();
						messageAlert = this.localizer[nameof(AppResources.PasswordIsInvalidAplicationBlockedTillTomorrow), dateString];
					}
					else
					{
						string dateString = localDateTime.ToString("yyyy-MM-dd, 'at' HH:mm", CultureInfo.InvariantCulture);
						messageAlert = this.localizer[nameof(AppResources.PasswordIsInvalidAplicationBlocked), dateString];
					}
				}

				await this.uiService.DisplayAlert(this.localizer[nameof(AppResources.ErrorTitle)], messageAlert);
				await this.CloseApplicationAsync();
			}
		}

		/// <summary>
		/// Validates password and unblocks user if correct.
		/// </summary>
		public async Task<bool> CheckPasswordAndUnblockUserAsync(string password)
		{
			if (password is null)
				return false;

			if (this.tagProfile.ComputePasswordHash(password) == this.tagProfile.LocalPasswordHash)
			{
				await this.UserAuthenticationSuccessfulAsync();
				return true;
			}
			else
			{
				await this.UserAuthenticationFailedAsync();
				return false;
			}
		}

		private async Task UserAuthenticationSuccessfulAsync()
		{
			this.SetStartInactivityTime();
			await this.SetCurrentPasswordCounterAsync(0);
			await this.loginAuditor.UnblockAndReset(Constants.Password.RemoteEndpoint);
		}

		private async Task UserAuthenticationFailedAsync()
		{
			await this.loginAuditor.ProcessLoginFailure(Constants.Password.RemoteEndpoint, Constants.Password.Protocol, DateTime.UtcNow, Constants.Password.Reason);
			long CurrentCounter = await this.GetCurrentPasswordCounterAsync();
			CurrentCounter++;
			await this.SetCurrentPasswordCounterAsync(CurrentCounter);
		}

		private void SetStartInactivityTime()
		{
			this.savedStartTime = DateTime.UtcNow;
		}

		private bool IsInactivitySafeIntervalPassed()
		{
			// Uses last successful authentication to determine if re-prompt is needed
			if (DateTime.UtcNow < this.lastAuthenticationTime)
				return true; // Require authentication.
			return DateTime.Compare(DateTime.UtcNow,
				this.lastAuthenticationTime.AddMinutes(Constants.Password.PossibleInactivityInMinutes)) > 0;
		}

		public async Task<long> GetCurrentPasswordCounterAsync()
		{
			return await this.settingsService.RestoreLongState(Constants.Password.CurrentPasswordAttemptCounter);
		}

		private async Task SetCurrentPasswordCounterAsync(long counter)
		{
			await this.settingsService.SaveState(Constants.Password.CurrentPasswordAttemptCounter, counter);
		}

		private async Task CloseApplicationAsync()
		{
			try
			{
				await this.platformSpecific.CloseApplication();
			}
			catch
			{
				Environment.Exit(0);
			}
		}


		#region IDisposable Implementation
		private bool isDisposed = false;

		public void Dispose()
		{
			this.Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// <see cref="IDisposableAsync.Dispose"/>
		/// </summary>
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
				this.DisposeAsync().AsTask().Wait();
		}


		/// <summary>
		/// <see cref="IDisposableAsync.Dispose"/>
		/// </summary>
		public virtual async ValueTask DisposeAsync()
		{
			if (this.isDisposed)
				return;

			await this.loginAuditor.DisposeAsync();

			this.isDisposed = true;
		}

		#endregion
	}
}
