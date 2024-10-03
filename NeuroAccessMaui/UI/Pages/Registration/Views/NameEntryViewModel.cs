using System.Text;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Tag;
using Waher.Networking.XMPP;

namespace NeuroAccessMaui.UI.Pages.Registration.Views
{
	public partial class NameEntryViewModel : BaseRegistrationViewModel
	{
		public NameEntryViewModel()
			  : base(RegistrationStep.NameEntry)
		{
		}

		/// <inheritdoc />
		public override async Task DoAssignProperties()
		{
			await base.DoAssignProperties();

			if (!string.IsNullOrEmpty(ServiceRef.TagProfile.Account))
				GoToRegistrationStep(RegistrationStep.CreateAccount);
		}

		/// <inheritdoc />
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			ServiceRef.XmppService.ConnectionStateChanged += this.XmppService_ConnectionStateChanged;
		}

		/// <inheritdoc />
		protected override async Task OnDispose()
		{
			ServiceRef.XmppService.ConnectionStateChanged -= this.XmppService_ConnectionStateChanged;

			await base.OnDispose();
		}

		private Task XmppService_ConnectionStateChanged(object _, XmppState NewState)
		{
			this.OnPropertyChanged(nameof(this.IsXmppConnected));
			this.CreateAccountCommand.NotifyCanExecuteChanged();
			return Task.CompletedTask;
		}

		[ObservableProperty]
		private string? username;

		partial void OnUsernameChanged(string? value)
		{
			this.UsernameIsValid = IsValidUsernameString(value);
			this.OnPropertyChanged(nameof(this.CanCreateAccount));

			if (this.UsernameIsValid)
			{
				this.AlternativeName = string.Empty;
				this.LocalizedValidationMessage = string.Empty;
			}
			else
			{
				this.LocalizedValidationMessage = ServiceRef.Localizer[nameof(AppResources.InvalidUsernameCharacters)];
				this.AlternativeName = this.GenerateUsername(this.Username);
			}
			this.CreateAccountCommand.NotifyCanExecuteChanged();
		}

		[ObservableProperty]
		private string? alternativeName;

		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(CreateAccountCommand))]
		private bool usernameIsValid;

		[ObservableProperty]
		private string? localizedValidationMessage;

		/// <summary>
		/// If App is connected to the XMPP network.
		/// </summary>
		public bool IsXmppConnected => ServiceRef.XmppService.State == XmppState.Connected;

		public bool CanCreateAccount => this.UsernameIsValid && !string.IsNullOrEmpty(this.Username) && string.IsNullOrEmpty(this.AlternativeName) && !this.IsBusy;

		[RelayCommand(CanExecute = nameof(CanCreateAccount))]
		private async Task CreateAccount()
		{
			this.IsBusy = true;
			try
			{
				string? account = this.Username;

				if (string.IsNullOrEmpty(account))
					return;
				string PasswordToUse = ServiceRef.CryptoService.CreateRandomPassword();

				(string HostName, int PortNumber, bool IsIpAddress) = await ServiceRef.NetworkService.LookupXmppHostnameAndPort(ServiceRef.TagProfile.Domain!);

				async Task OnConnected(XmppClient Client)
				{
					if (ServiceRef.TagProfile.NeedsUpdating())
						await ServiceRef.XmppService.DiscoverServices(Client);

					ServiceRef.TagProfile.SetAccount(account, Client.PasswordHash, Client.PasswordHashMethod);

					GoToRegistrationStep(RegistrationStep.CreateAccount);
				}

				(bool Succeeded, string? ErrorMessage, string[]? Alternatives) = await ServiceRef.XmppService.TryConnectAndCreateAccount(ServiceRef.TagProfile.Domain!,
					IsIpAddress, HostName, PortNumber, account, PasswordToUse, Constants.LanguageCodes.Default,
					ServiceRef.TagProfile.ApiKey ?? string.Empty, ServiceRef.TagProfile.ApiSecret ?? string.Empty,
					typeof(App).Assembly, OnConnected);

				if (Succeeded)
				{
					this.IsBusy = false;
				}
				if (Alternatives is not null && Alternatives.Length > 0)
				{
					Random rnd = new Random();
					//Set alternative name to random alternative
					this.UsernameIsValid = false;
					this.AlternativeName = Alternatives[rnd.Next(0, Alternatives.Length)];
					this.LocalizedValidationMessage = ServiceRef.Localizer[nameof(AppResources.UsernameNameAlreadyTaken)];
				}
				else if (ErrorMessage is not null)
				{
					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], ErrorMessage,
						ServiceRef.Localizer[nameof(AppResources.Ok)]);
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);

				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], ex.Message,
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
			}

			this.IsBusy = false;
		}


		[RelayCommand]
		private void SelectName(string? name)
		{
			if (string.IsNullOrEmpty(name))
				return;
			this.Username = name;
			this.AlternativeName = string.Empty;
		}

		/// <summary>
		/// Generates a username based on a source string.
		/// </summary>
		/// <param name="source">The source string to generate the username from.</param>
		/// <returns>Generated username or empty string if failed</returns>
		public string GenerateUsername(string? source)
		{
			if (string.IsNullOrWhiteSpace(source))
				return string.Empty;

			// Normalize and convert to lowercase
			string normalizedSource = source.Normalize(NormalizationForm.FormC).ToLowerInvariant();

			// Split the source by spaces and process each part
			IEnumerable<string?> processedParts = normalizedSource
				.Split(' ', StringSplitOptions.RemoveEmptyEntries)
				.Select(ReplaceInvalidUsernameCharacters)
				.Where(processedPart => !string.IsNullOrEmpty(processedPart));

			if (!processedParts.Any())
				return string.Empty;

			// Join parts with dots
			string result = string.Join(".", processedParts);

			// Generate 4 random digits
			string randomDigits = new Random().Next(0, 10000).ToString("D4");
			result += randomDigits;

			// Replace multiple dots with a single dot
			result = Regex.Replace(result, @"\.+", ".");

			return result;
		}


		internal static bool IsValidUsernameString(string? input)
		{
			if (string.IsNullOrEmpty(input))
				return true;
			foreach (char c in input)
			{
				switch (c)
				{
					// From XMPP spec (RFC 6122):
					case '"':
					case '&':
					case '\'':
					case '/':
					case ':':
					case '<':
					case '>':
					case '@':
					// Disallow space
					case ' ':
					// Invalid as file name characters (for file sniffers, etc.)
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

		private static string ReplaceInvalidUsernameCharacters(string input)
		{
			StringBuilder sb = new(input.Length);
			foreach (char c in input)
			{
				switch (c)
				{
					// From XMPP spec (RFC 6122):
					case '"':
					case '&':
					case '\'':
					case '/':
					case ':':
					case '<':
					case '>':
					case '@':
					// Disallow space
					case ' ':
					// Invalid as file name characters (for file sniffers, etc.)
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
	}
}
