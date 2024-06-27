﻿using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages.Registration.Views;
using Waher.Content;
using Waher.Script.Constants;

namespace NeuroAccessMaui.UI.Popups.Password
{
	/// <summary>
	/// View model for page letting the user enter a password to be verified with the password defined by the user earlier.
	/// </summary>
	public partial class CheckPasswordViewModel(AuthenticationPurpose purpose) : ReturningPopupViewModel<string>
	{

		/// <summary>
		/// Password text entry
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(EnterPasswordCommand))]
		private string passwordText = string.Empty;

		/// <summary>
		/// If password can be entered
		/// </summary>
		public bool CanEnterPassword => !string.IsNullOrEmpty(this.PasswordText);

		/// <summary>
		/// The purpose of the authentication request.
		/// </summary>
		public AuthenticationPurpose PurposeInfo { get; } = purpose;

		/// <summary>
		/// Enters the password provided by the user.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanEnterPassword))]
		private async Task EnterPassword()
		{
			if (!string.IsNullOrEmpty(this.PasswordText))
			{
				string Password = this.PasswordText;

				if (await App.CheckPasswordAndUnblockUser(Password))
				{
					await ServiceRef.UiService.PopAsync();
					this.result.TrySetResult(Password);
				}
				else
				{
					this.PasswordText = string.Empty;

					long PasswordAttemptCounter = await App.GetCurrentPasswordCounter();
					long RemainingAttempts = Math.Max(0, Constants.Password.FirstMaxPasswordAttempts - PasswordAttemptCounter);

					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.PasswordIsInvalid), RemainingAttempts]);

					await App.CheckUserBlocking();
				}
			}
		}

		/// <summary>
		/// Cancels password-entry
		/// </summary>
		[RelayCommand]
		private async Task Cancel()
		{
			await ServiceRef.UiService.PopAsync();
			this.result.TrySetResult(null);
		}
	}
	///TODO: remove or implement this
	/* Recover password
	 	try
	   {
	   	string AcceptLanguage = App.SelectedLanguage.TwoLetterISOLanguageName;

	   	if (AcceptLanguage != "en")
	   		AcceptLanguage += ";q=1,en;q=0.9";

	   	byte[] nonceData = new byte[32];

	   	using (RandomNumberGenerator Rnd = RandomNumberGenerator.Create())
	   	{
	   		Rnd.GetBytes(nonceData);
	   	}
	   	string nonce = Guid.NewGuid().ToString("n");
	   	string host = ServiceRef.TagProfile.Domain;
	   	string s = $"{ServiceRef.TagProfile.Account}:{host}:{nonce}";
	   	string NewNetworkPassword = ServiceRef.CryptoService.CreateRandomPassword();
	   	if (!await ServiceRef.XmppService.ChangePassword(NewNetworkPassword))
	   		throw new Exception("password failed");
	   	ServiceRef.TagProfile.SetAccount(ServiceRef.TagProfile.Account!, NewNetworkPassword, string.Empty);

	   	byte[] keyBytes = Encoding.UTF8.GetBytes(NewNetworkPassword);
	   	byte[] dataBytes = Encoding.UTF8.GetBytes(s);

	   	string signature;
	   	using (System.Security.Cryptography.HMACSHA256 hmac = new System.Security.Cryptography.HMACSHA256(keyBytes))
	   	{
	   		byte[] signatureBytes = hmac.ComputeHash(dataBytes);
	   		signature = Convert.ToBase64String(signatureBytes);
	   	}

	   	object result2 = await InternetContent.PostAsync(
	   		new Uri("https://" + host + "/Agent/Account/Login"),
	    new Dictionary<string, object>()
	   		{
	   			{ "userName", ServiceRef.TagProfile.Account },
	   			{ "nonce", nonce.ToString() },
	   			{ "signature", signature },
	   			{ "seconds", 3600 }
	   		},
	   		new KeyValuePair<string, string>("Accept", "application/json"),
	   		new KeyValuePair<string, string>("Accept-Language", AcceptLanguage),
	   		new KeyValuePair<string, string>("Accept-Encoding", "0")
	   		);
	   	string jwt = string.Empty;
	   	if (result2 is Dictionary<string, object> Response)
	   	{
	   		if (Response.TryGetValue("jwt", out object? Obj) && Obj is string JWT)
	   			jwt = JWT;
	   	}
	   	Console.WriteLine("JWT: " + jwt);
	   	object result = await InternetContent.PostAsync(new Uri("https://" + host + "/Agent/Account/Recover"),
	    new Dictionary<string, object>
	   		{
	   			{ "country", ""},
	   			{ "eMail", ServiceRef.TagProfile.EMail },
	   			{ "personalNr", ""},
	   			{ "phoneNr", ""},
	   			{ "userName", ServiceRef.TagProfile.Account }
	   		},
	   			new KeyValuePair<string, string>("Accept", "application/json"),
	   			new KeyValuePair<string, string>("Accept-Language", AcceptLanguage),
	   			new KeyValuePair<string, string>("Accept-Encoding", "0"),
	   			new KeyValuePair<string, string>("Authorization", "Bearer " + jwt)
	   	);

	   }
	   catch (Exception ex)
	   {
	   	Console.WriteLine(ex.Message);
	   }
	 */
}
