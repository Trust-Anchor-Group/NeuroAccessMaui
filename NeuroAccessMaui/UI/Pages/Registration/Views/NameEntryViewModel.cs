using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.UI.Popups.Info;

namespace NeuroAccessMaui.UI.Pages.Registration.Views
{
	public partial class NameEntryViewModel : BaseRegistrationViewModel
	{
		public NameEntryViewModel()
			  : base(RegistrationStep.NameEntry)
		{
		}

		[ObservableProperty]
		private string? username;

		[ObservableProperty]
		private string? alternativeName;
		

		partial void OnUsernameChanged(string? value)
		{
			this.UsernameIsValid = IsValidUsernameString(value);
			this.OnPropertyChanged(nameof(CanCreateAccount));
		}

		[ObservableProperty]
		private bool usernameIsValid = false;

		public bool CanCreateAccount => this.UsernameIsValid && !string.IsNullOrEmpty(this.Username);

		[RelayCommand(CanExecute = nameof(CanCreateAccount))]
		private void CreateAccount()
		{


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
