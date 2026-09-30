namespace DevStorybook.Stories
{
	/// <summary>
	/// Provides the lightweight registry used to discover, group, sort, and search stories.
	/// </summary>
	public static class StoryRegistry
	{
		private static readonly IReadOnlyList<StoryDefinition> stories = CreateStories();

		/// <summary>Gets every registered story in deterministic catalog order.</summary>
		public static IReadOnlyList<StoryDefinition> Stories => stories;

		/// <summary>
		/// Finds all stories associated with a screen key.
		/// </summary>
		/// <param name="ScreenKey">The stable screen key.</param>
		/// <returns>The screen's stories in section and explicit order.</returns>
		public static IReadOnlyList<StoryDefinition> GetScreenStories(string ScreenKey)
		{
			return stories
				.Where(Story => Story.ScreenKey == ScreenKey)
				.OrderBy(Story => Story.StoryType)
				.ThenBy(Story => Story.Order)
				.ToList();
		}

		private static IReadOnlyList<StoryDefinition> CreateStories()
		{
			List<StoryDefinition> Stories = new List<StoryDefinition>();

			AddScreen(Stories, "ID Provider", "ID Provider", PhoneVerificationStoryState.IdentityProvider,
				[("Default", PhoneVerificationStoryState.IdentityProvider)], ["provider", "invite", "qr"], 10);
			AddScreen(Stories, "Phone", "Phone Verification", PhoneVerificationStoryState.Default,
				[
					("Default", PhoneVerificationStoryState.Default),
					("Empty", PhoneVerificationStoryState.Empty),
					("Valid Number", PhoneVerificationStoryState.ValidPhoneNumber),
					("Invalid Number", PhoneVerificationStoryState.InvalidPhoneNumber),
					("Loading", PhoneVerificationStoryState.SendingCode),
					("Code Sent", PhoneVerificationStoryState.CodeSent),
					("Backend Error", PhoneVerificationStoryState.BackendError),
					("Network Error", PhoneVerificationStoryState.NetworkError)
				], ["phone", "number", "send code"], 20);
			AddScreen(Stories, "Phone", "Phone Code Verification", PhoneVerificationStoryState.PhoneCodeDefault,
				[
					("Default", PhoneVerificationStoryState.PhoneCodeDefault),
					("Empty Code", PhoneVerificationStoryState.PhoneCodeEmpty),
					("Invalid Code", PhoneVerificationStoryState.PhoneCodeInvalid),
					("Verifying", PhoneVerificationStoryState.PhoneCodeVerifying),
					("Verified", PhoneVerificationStoryState.PhoneCodeVerified),
					("Expired Code", PhoneVerificationStoryState.PhoneCodeExpired),
					("Backend Error", PhoneVerificationStoryState.PhoneCodeBackendError)
				], ["phone", "code", "verification", "otp"], 30);
			AddScreen(Stories, "Email", "Email Verification", PhoneVerificationStoryState.EmailEmpty,
				[
					("Default", PhoneVerificationStoryState.EmailEmpty),
					("Empty", PhoneVerificationStoryState.EmailEmpty),
					("Valid Email", PhoneVerificationStoryState.EmailValid),
					("Invalid Email", PhoneVerificationStoryState.EmailInvalid)
				], ["email", "address", "send code"], 40);
			AddScreen(Stories, "Email", "Email Code Verification", PhoneVerificationStoryState.EmailCodeDefault,
				[
					("Default", PhoneVerificationStoryState.EmailCodeDefault),
					("Empty Code", PhoneVerificationStoryState.EmailCodeEmpty),
					("Invalid Code", PhoneVerificationStoryState.EmailCodeInvalid),
					("Verifying", PhoneVerificationStoryState.EmailCodeVerifying),
					("Verified", PhoneVerificationStoryState.EmailCodeVerified),
					("Expired Code", PhoneVerificationStoryState.EmailCodeExpired),
					("Backend Error", PhoneVerificationStoryState.EmailCodeBackendError)
				], ["email", "code", "verification", "otp"], 50);
			AddScreen(Stories, "Account", "Username", PhoneVerificationStoryState.UsernameEmpty,
				[
					("Empty", PhoneVerificationStoryState.UsernameEmpty),
					("Valid Username", PhoneVerificationStoryState.UsernameValid),
					("Invalid With Suggestion", PhoneVerificationStoryState.UsernameInvalid)
				], ["account", "username", "name"], 60);
			AddScreen(Stories, "Account", "Create Account", PhoneVerificationStoryState.AccountCreation,
				[("Creating Identity", PhoneVerificationStoryState.AccountCreation)], ["account", "identity", "loading"], 70);
			AddScreen(Stories, "Account", "Create PIN", PhoneVerificationStoryState.PinEmpty,
				[
					("Empty", PhoneVerificationStoryState.PinEmpty),
					("Matching PIN", PhoneVerificationStoryState.PinValid),
					("Confirmation Mismatch", PhoneVerificationStoryState.PinMismatch)
				], ["pin", "password", "security"], 80);
			AddScreen(Stories, "Biometrics", "Biometrics", PhoneVerificationStoryState.Biometrics,
				[("Default", PhoneVerificationStoryState.Biometrics)], ["fingerprint", "face", "authentication"], 90);
			AddScreen(Stories, "Success", "Success", PhoneVerificationStoryState.Success,
				[("Default", PhoneVerificationStoryState.Success)], ["complete", "finished", "checkmark"], 100);

			AddComponent(Stories, "Phone", "Phone Verification", "Country Code Picker", "United States",
				PhoneVerificationStoryState.CountryUnitedStates, "Pickers", ["phone", "country", "dial code", "picker"], 10);
			AddComponent(Stories, "Phone", "Phone Verification", "Country Code Picker", "Sweden",
				PhoneVerificationStoryState.CountrySweden, "Pickers", ["phone", "country", "dial code", "picker"], 20);
			AddComponent(Stories, "Phone", "Phone Verification", "Country Code Picker", "Disabled",
				PhoneVerificationStoryState.CountryDisabled, "Pickers", ["phone", "country", "dial code", "picker"], 30);
			AddComponent(Stories, "Phone", "Phone Verification", "Phone Number Input", "Empty",
				PhoneVerificationStoryState.PhoneInputEmpty, "Inputs", ["phone", "number", "input", "entry"], 10);
			AddComponent(Stories, "Phone", "Phone Verification", "Phone Number Input", "Valid",
				PhoneVerificationStoryState.PhoneInputValid, "Inputs", ["phone", "number", "input", "entry"], 20);
			AddComponent(Stories, "Phone", "Phone Verification", "Phone Number Input", "Invalid",
				PhoneVerificationStoryState.PhoneInputInvalid, "Inputs", ["phone", "number", "input", "validation"], 30);
			AddComponent(Stories, "Phone", "Phone Verification", "Phone Number Input", "Disabled",
				PhoneVerificationStoryState.PhoneInputDisabled, "Inputs", ["phone", "number", "input", "entry"], 40);
			AddComponent(Stories, "Phone", "Phone Verification", "Phone Number Input", "Focused",
				PhoneVerificationStoryState.PhoneInputFocused, "Inputs", ["phone", "number", "input", "focus"], 50);

			return Stories;
		}

		private static void AddScreen(
			ICollection<StoryDefinition> Stories,
			string Subcategory,
			string Screen,
			PhoneVerificationStoryState FullScreenState,
			IReadOnlyList<(string Name, PhoneVerificationStoryState State)> States,
			IReadOnlyList<string> Tags,
			int ScreenOrder)
		{
			Stories.Add(new StoryDefinition("Onboarding", Subcategory, Screen, Screen, StoryType.FullScreen,
				"Full Screen", FullScreenState, Tags, ScreenOrder));

			for (int i = 0; i < States.Count; i++)
			{
				(string Name, PhoneVerificationStoryState State) StateDefinition = States[i];
				Stories.Add(new StoryDefinition("Onboarding", Subcategory, Screen, Screen, StoryType.State,
					StateDefinition.Name, StateDefinition.State, Tags, (i + 1) * 10));
			}
		}

		private static void AddComponent(
			ICollection<StoryDefinition> Stories,
			string Subcategory,
			string Screen,
			string Feature,
			string Name,
			PhoneVerificationStoryState State,
			string ComponentGroup,
			IReadOnlyList<string> Tags,
			int Order)
		{
			Stories.Add(new StoryDefinition("Onboarding", Subcategory, Feature, Screen, StoryType.Component,
				$"{Feature} — {Name}", State, Tags, Order, ComponentGroup));
		}
	}
}
