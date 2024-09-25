using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.UI.Popups.Info;
using Waher.Networking.XMPP;
using Waher.Script.Operators.Membership;

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

			if(string.IsNullOrEmpty(ServiceRef.TagProfile.Account))
				GoToRegistrationStep(RegistrationStep.ValidatePhone);
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
			return Task.CompletedTask;
		}

		[ObservableProperty]
		private string? username;

		partial void OnUsernameChanged(string? value)
		{
			this.UsernameIsValid = IsValidUsernameString(value);
			this.OnPropertyChanged(nameof(CanCreateAccount));
		}

		[ObservableProperty]
		private string? alternativeName;

		[ObservableProperty]
		private bool usernameIsValid;

		/// <summary>
		/// If App is connected to the XMPP network.
		/// </summary>
		public bool IsXmppConnected => ServiceRef.XmppService.State == XmppState.Connected;

		public bool CanCreateAccount => this.UsernameIsValid && !string.IsNullOrEmpty(this.Username) && !string.IsNullOrEmpty(this.AlternativeName) && this.IsXmppConnected;

		[RelayCommand(CanExecute = nameof(CanCreateAccount))]
		private async Task CreateAccount()
		{
			try
			{
				string? account = this.Username;

				if(string.IsNullOrEmpty(account))
					return;
				string PasswordToUse = ServiceRef.CryptoService.CreateRandomPassword();

				(string HostName, int PortNumber, bool IsIpAddress) = await ServiceRef.NetworkService.LookupXmppHostnameAndPort(ServiceRef.TagProfile.Domain!);

				async Task OnConnected(XmppClient Client)
				{
					if (ServiceRef.TagProfile.NeedsUpdating())
						await ServiceRef.XmppService.DiscoverServices(Client);

					ServiceRef.TagProfile.SetAccount(account, Client.PasswordHash, Client.PasswordHashMethod);

					GoToRegistrationStep(RegistrationStep.ValidatePhone);
				}

				(bool Succeeded, string? ErrorMessage, string[]? Alternatives) = await ServiceRef.XmppService.TryConnectAndCreateAccount(ServiceRef.TagProfile.Domain!,
					IsIpAddress, HostName, PortNumber, account, PasswordToUse, Constants.LanguageCodes.Default,
					ServiceRef.TagProfile.ApiKey ?? string.Empty, ServiceRef.TagProfile.ApiSecret ?? string.Empty,
					typeof(App).Assembly, OnConnected);

				if (Succeeded)
					return;
				if (Alternatives is not null && Alternatives.Length > 0)
				{
					Random rnd = new Random();
					//Set alternative name to random alternative
					this.AlternativeName = Alternatives[rnd.Next(0, Alternatives.Length)];
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

			GoToRegistrationStep(RegistrationStep.ValidatePhone);
		}

		[RelayCommand]
		private async Task ShowDataInfo()
		{
			string title = ServiceRef.Localizer[nameof(AppResources.WhyIsThisDataCollected)];
			string message = ServiceRef.Localizer[nameof(AppResources.WhyIsThisDataCollectedInfo)];
			ShowInfoPopup infoPage = new(title, message);
			await ServiceRef.UiService.PushAsync(infoPage);
		}

		[RelayCommand]
		private void SelectName(string? name)
		{
			if(string.IsNullOrEmpty(name))
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
			if(string.IsNullOrEmpty(source))
				return string.Empty;
			// Normalize and convert to lowercase
			string username = source.Normalize(NormalizationForm.FormC).ToLowerInvariant();

			// Split first names by spaces
			string[] nameParts = username.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

			List<string> processedParts = [];

			foreach (string part in nameParts)
			{
				// Replace invalid characters
				string processedPart = ReplaceInvalidUsernameCharacters(part);

				if (!string.IsNullOrEmpty(processedPart))
					processedParts.Add(processedPart);
			}

			if (processedParts.Count == 0)
				return string.Empty;

			// Join parts with dots
			string result = string.Join(".", processedParts);
			return Regex.Replace(result, @".+", ".");
		}

		internal static bool IsValidUsernameString(string? input)
		{
			if (string.IsNullOrWhiteSpace(input))
				return false;

			foreach (Rune rune in input.EnumerateRunes())
			{
				switch (rune.Value)
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
					default:
						return true;
				}
			}
			return false;
		}

		private static string ReplaceInvalidUsernameCharacters(string input)
		{
			StringBuilder sb = new(input.Length);
			foreach (Rune rune in input.EnumerateRunes())
			{
				switch (rune.Value)
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
						sb.Append(rune.ToString());
						break;
				}
			}

			return sb.ToString();
		}
	}
}
