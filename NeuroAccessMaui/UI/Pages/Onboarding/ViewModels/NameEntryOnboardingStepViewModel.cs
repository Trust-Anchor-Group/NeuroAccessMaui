using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Tag;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.StanzaErrors; // Added for InternalServerErrorException

namespace NeuroAccessMaui.UI.Pages.Onboarding.ViewModels
{
	public partial class NameEntryOnboardingStepViewModel : BaseOnboardingStepViewModel, IDisposable
	{
		private CancellationTokenSource? cooldownCts;
		private bool disposed;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(IsInCooldown))]
		[NotifyPropertyChangedFor(nameof(LocalizedContinueText))]
		private int cooldownSecondsLeft;

		[ObservableProperty]
		private string? username;

		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(CreateAccountCommand))]
		private bool usernameIsValid;

		[ObservableProperty]
		private string? alternativeName;

		[ObservableProperty]
		private string? localizedValidationMessage;

		public NameEntryOnboardingStepViewModel() : base(OnboardingStep.NameEntry)
		{
		}

		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			ServiceRef.XmppService.ConnectionStateChanged += this.XmppService_ConnectionStateChanged;

			if (!string.IsNullOrEmpty(ServiceRef.TagProfile.Account) && this.CoordinatorViewModel is not null)
			{
				await this.CoordinatorViewModel.GoToStepCommand.ExecuteAsync(OnboardingStep.CreateAccount);
			}
		}

		public override async Task OnDisposeAsync()
		{
			ServiceRef.XmppService.ConnectionStateChanged -= this.XmppService_ConnectionStateChanged;
			this.Dispose();

			await base.OnDisposeAsync();
		}

		private Task XmppService_ConnectionStateChanged(object? _, XmppState __)
		{
			this.CreateAccountCommand.NotifyCanExecuteChanged();
			return Task.CompletedTask;
		}

		public bool IsInCooldown => this.CooldownSecondsLeft > 0;

		public bool IsXmppConnected => ServiceRef.XmppService.State == XmppState.Connected;

		public string LocalizedContinueText
		{
			get
			{
				if (this.CooldownSecondsLeft > 0)
					return ServiceRef.Localizer[nameof(AppResources.ContinueSecondsFormat), this.CooldownSecondsLeft];

				return ServiceRef.Localizer[nameof(AppResources.Continue)];
			}
		}

		public bool CanCreateAccount => this.UsernameIsValid &&
			!string.IsNullOrEmpty(this.Username) &&
			string.IsNullOrEmpty(this.AlternativeName) &&
			!this.IsBusy &&
			!this.IsInCooldown;

		partial void OnUsernameChanged(string? value)
		{
			this.UsernameIsValid = IsValidUsernameString(value);
			if (this.UsernameIsValid)
			{
				this.AlternativeName = string.Empty;
				this.LocalizedValidationMessage = string.Empty;
			}
			else
			{
				this.LocalizedValidationMessage = ServiceRef.Localizer[nameof(AppResources.InvalidUsernameCharacters)];
				this.AlternativeName = this.GenerateUsername(value);
			}

			this.CreateAccountCommand.NotifyCanExecuteChanged();
		}

		partial void OnAlternativeNameChanged(string? value)
		{
			this.CreateAccountCommand.NotifyCanExecuteChanged();
		}

		[RelayCommand]
		private void SelectName(string? name)
		{
			if (string.IsNullOrEmpty(name))
				return;

			this.Username = name;
			this.AlternativeName = string.Empty;
		}

		[RelayCommand(CanExecute = nameof(CanCreateAccount))]
		private async Task CreateAccount()
		{
			this.SetIsBusy(true);
			try
			{
				string? Account = this.Username;
				if (string.IsNullOrEmpty(Account))
					return;

				string Password = ServiceRef.CryptoService.CreateRandomPassword();

				if (string.IsNullOrWhiteSpace(ServiceRef.TagProfile.Domain))
				{
					await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], ServiceRef.Localizer[nameof(AppResources.UnableToConnect)], ServiceRef.Localizer[nameof(AppResources.Ok)]);
					return;
				}

				(string HostName, int PortNumber, bool IsIpAddress) = await ServiceRef.NetworkService.LookupXmppHostnameAndPort(ServiceRef.TagProfile.Domain!);

				async Task OnConnected(XmppClient Client)
				{
					if (ServiceRef.TagProfile.NeedsUpdating())
						await ServiceRef.XmppService.DiscoverServices(Client);

					ServiceRef.TagProfile.SetAccount(Account, Client.PasswordHash, Client.PasswordHashMethod);

					if (this.CoordinatorViewModel is not null)
						await this.CoordinatorViewModel.GoToStepCommand.ExecuteAsync(OnboardingStep.CreateAccount);
				}

				int Attempt = 0;
				bool InternalServerErrorRetried = false;
				while (Attempt < 2)
				{
					Attempt++;
					try
					{
						(bool Succeeded, string? ErrorMessage, string[]? Alternatives) = await ServiceRef.XmppService.TryConnectAndCreateAccount(
							ServiceRef.TagProfile.Domain!,
							IsIpAddress,
							HostName,
							PortNumber,
							Account,
							Password,
							Constants.LanguageCodes.Default,
							ServiceRef.TagProfile.ApiKey ?? string.Empty,
							ServiceRef.TagProfile.ApiSecret ?? string.Empty,
							typeof(App).Assembly,
							OnConnected);

						if (Alternatives is not null && Alternatives.Length > 0)
						{
							Random Rnd = new Random();
							this.UsernameIsValid = false;
							this.AlternativeName = Alternatives[Rnd.Next(0, Alternatives.Length)];
							this.LocalizedValidationMessage = ServiceRef.Localizer[nameof(AppResources.UsernameNameAlreadyTaken)];
						}
						else if (!Succeeded && !string.IsNullOrEmpty(ErrorMessage))
						{
							await ServiceRef.UiService.DisplayAlert(
								ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], ErrorMessage,
								ServiceRef.Localizer[nameof(AppResources.Ok)]);
						}

						// Exit loop after first successful attempt or handled failure.
						break;
					}
					catch (InternalServerErrorException Ex)
					{
						if (!InternalServerErrorRetried)
						{
							InternalServerErrorRetried = true;
							ServiceRef.LogService.LogWarning("Internal server error during account creation. Retrying once.");
							continue; // Immediate retry
						}
						ServiceRef.LogService.LogException(Ex);
						await ServiceRef.UiService.DisplayAlert(
							ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], Ex.Message,
							ServiceRef.Localizer[nameof(AppResources.Ok)]);
						break;
					}
					catch (Exception Ex)
					{
						ServiceRef.LogService.LogException(Ex);
						await ServiceRef.UiService.DisplayAlert(
							ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], Ex.Message,
							ServiceRef.Localizer[nameof(AppResources.Ok)]);
						break;
					}
				}
			}
			finally
			{
				await this.StartCooldownAsync();
				this.SetIsBusy(false);
			}
		}

		public override void SetIsBusy(bool isBusy)
		{
			bool changed = this.IsBusy != isBusy;
			base.SetIsBusy(isBusy);
			if (changed)
				this.CreateAccountCommand.NotifyCanExecuteChanged();
		}

		private async Task StartCooldownAsync(int seconds = 3)
		{
			this.cooldownCts?.Cancel();
			this.cooldownCts?.Dispose();
			this.cooldownCts = new CancellationTokenSource();

			this.CooldownSecondsLeft = seconds;
			this.CreateAccountCommand.NotifyCanExecuteChanged();

			try
			{
				while (this.CooldownSecondsLeft > 0)
				{
					await Task.Delay(1000, this.cooldownCts.Token);
					this.CooldownSecondsLeft--;
				}
			}
			catch (TaskCanceledException)
			{
				// Ignore
			}
			finally
			{
				if (this.CooldownSecondsLeft != 0)
				{
					this.CooldownSecondsLeft = 0;
				}
				this.CreateAccountCommand.NotifyCanExecuteChanged();
			}
		}

		private static bool IsValidUsernameString(string? input)
		{
			if (string.IsNullOrEmpty(input))
				return true;

			foreach (char c in input)
			{
				switch (c)
				{
					case '"':
					case '&':
					case '\'':
					case '/':
					case ':':
					case '<':
					case '>':
					case '@':
					case ' ':
					case '|':
					case '\0':
					case '\u0001':
					case '\u0002':
					case '\u0003':
					case '\u0004':
					case '\u0005':
					case '\u0006':
					case '\a':
					case '\b':
					case '\t':
					case '\n':
					case '\v':
					case '\f':
					case '\r':
					case '\u000e':
					case '\u000f':
					case '\u0010':
					case '\u0011':
					case '\u0012':
					case '\u0013':
					case '\u0014':
					case '\u0015':
					case '\u0016':
					case '\u0017':
					case '\u0018':
					case '\u0019':
					case '\u001a':
					case '\u001b':
					case '\u001c':
					case '\u001d':
					case '\u001e':
					case '\u001f':
					case '*':
					case '?':
					case '\\':
						return false;
				}
			}

			return true;
		}

		private string? GenerateUsername(string? source)
		{
			if (string.IsNullOrWhiteSpace(source))
				return string.Empty;

			string normalizedSource = source.Normalize(NormalizationForm.FormC).ToLowerInvariant();

			IEnumerable<string?> processedParts = normalizedSource
				.Split(' ', StringSplitOptions.RemoveEmptyEntries)
				.Select(ReplaceInvalidUsernameCharacters)
				.Where(processedPart => !string.IsNullOrEmpty(processedPart));

			if (!processedParts.Any())
				return string.Empty;

			string result = string.Join(".", processedParts);

			string randomDigits = new Random().Next(0, 10000).ToString("D4", CultureInfo.InvariantCulture);
			result += randomDigits;

			result = System.Text.RegularExpressions.Regex.Replace(result, @"\.+", ".");

			return result;
		}

		private static string ReplaceInvalidUsernameCharacters(string input)
		{
			StringBuilder sb = new(input.Length);
			foreach (char c in input)
			{
				switch (c)
				{
					case '"':
					case '&':
					case '\'':
					case '/':
					case ':':
					case '<':
					case '>':
					case '@':
					case ' ':
					case '|':
					case '\0':
					case '\u0001':
					case '\u0002':
					case '\u0003':
					case '\u0004':
					case '\u0005':
					case '\u0006':
					case '\a':
					case '\b':
					case '\t':
					case '\n':
					case '\v':
					case '\f':
					case '\r':
					case '\u000e':
					case '\u000f':
					case '\u0010':
					case '\u0011':
					case '\u0012':
					case '\u0013':
					case '\u0014':
					case '\u0015':
					case '\u0016':
					case '\u0017':
					case '\u0018':
					case '\u0019':
					case '\u001a':
					case '\u001b':
					case '\u001c':
					case '\u001d':
					case '\u001e':
					case '\u001f':
					case '*':
					case '?':
					case '\\':
						break;
					default:
						sb.Append(c);
						break;
				}
			}

			return sb.ToString();
		}

		/// <summary>
		/// Disposes resources used by the NameEntryOnboardingStepViewModel.
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Disposes resources used by the NameEntryOnboardingStepViewModel.
		/// </summary>
		/// <param name="disposing">True if called from Dispose; false if called from finalizer.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (this.disposed)
				return;
			this.disposed = true;

			if (disposing)
			{
				this.cooldownCts?.Cancel();
				this.cooldownCts?.Dispose();
				this.cooldownCts = null;
			}
		}
	}
}
