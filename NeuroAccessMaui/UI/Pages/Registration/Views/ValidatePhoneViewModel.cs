using System.ComponentModel;
using System.Globalization;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mopups.Services;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Data;
using NeuroAccessMaui.Services.Localization;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.UI.Pages.Main.VerifyCode;
using NeuroAccessMaui.UI.Popups;
using Waher.Content;

namespace NeuroAccessMaui.UI.Pages.Registration.Views
{
	public partial class ValidatePhoneViewModel : BaseRegistrationViewModel, ICodeVerification
	{
		public ValidatePhoneViewModel()
			: base(RegistrationStep.ValidatePhone)
		{
		}
		bool MyCertValidationCallback(
	object sender,
	RemoteCertificateEventArgs args)
		{
			string script =
				"""
			s1:="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";
			s2:="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";

			printline(s1=s2);

			C:=Create(System.Security.Cryptography.X509Certificates.X509Certificate2,base64decode(s1));
			R:=C.Verify();
			printline(R);

			""";

			//object? res = Waher.Script.Expression.Eval(script);

			if (args.SslPolicyErrors == SslPolicyErrors.None)
			{
				args.IsValid = true; // Accept certificate
				return true; 
			}
			// Check for incomplete revocation check in the chain
			if ((args.SslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) != 0 && args.Chain is not null)
			{
				foreach (var status in args.Chain.ChainStatus)
				{
					// Apple-specific error code for incomplete revocation check
					if (status.Status == X509ChainStatusFlags.RevocationStatusUnknown ||
						status.Status == X509ChainStatusFlags.OfflineRevocation)
					{
						// Optionally log or handle as needed
						continue; // Ignore this error
					}
					if (status.Status != X509ChainStatusFlags.NoError)
						return false; // Fail on other errors
				}
				args.IsValid = true; // Accept certificate
				return true; // Only revocation check failed, allow
				
			}

			return false;


		//	ServiceRef.LogService.LogDebug("Result: " + res?.ToString() ?? "null");

			byte[] Cert = args.Certificate?.Export(X509ContentType.Cert);    // Avoids SafeHandle exception when accessing certificate later.


			StringBuilder Base64 = new StringBuilder();
			string s;
			int c = Cert?.Length ?? 0;
			int i = 0;
			int j;

			while (i < c)
			{
				j = Math.Min(57, c - i);
				s = Convert.ToBase64String(Cert, i, j);
				i += j;

				Base64.Append(s);

				if (i < c)
					Base64.AppendLine();
			}

			StringBuilder SniffMsg = new StringBuilder();

			SniffMsg.AppendLine("Invalid certificate received (and rejected).");

			SniffMsg.AppendLine();
			SniffMsg.Append("sslPolicyErrors: ");
			SniffMsg.AppendLine(args.SslPolicyErrors.ToString());
			SniffMsg.Append("Subject: ");
			SniffMsg.AppendLine(args.Certificate?.Subject);
			SniffMsg.Append("Issuer: ");
			SniffMsg.AppendLine(args.Certificate?.Issuer);
			SniffMsg.Append("BASE64(Cert): ");
			SniffMsg.Append(Base64);
			SniffMsg.AppendLine();

			SniffMsg.AppendLine("Nr of elements in chain: ");
			SniffMsg.Append(args.Chain.ChainElements.Count);
			SniffMsg.AppendLine();
			SniffMsg.AppendLine("Chain status: ");
			foreach (X509ChainStatus Status in args.Chain.ChainStatus)
			{
				SniffMsg.Append("Status: ");
				SniffMsg.AppendLine(Status.Status.ToString());
				SniffMsg.Append("StatusInformation: ");
				SniffMsg.AppendLine(Status.StatusInformation);
				SniffMsg.AppendLine();

			}

			SniffMsg.AppendLine("Chain elements:");
			foreach (X509ChainElement Element in args.Chain.ChainElements)
			{
				SniffMsg.Append("Subject: ");
				SniffMsg.AppendLine(Element.Certificate.Subject);
				SniffMsg.Append("Issuer: ");
				SniffMsg.AppendLine(Element.Certificate.Issuer);
				SniffMsg.AppendLine("Info: ");
				SniffMsg.Append(Element.Information);
				SniffMsg.AppendLine();
			}

			try
			{
				ServiceRef.LogService.LogWarning(SniffMsg.ToString());
			}
			catch (Exception ex2)
			{
				ServiceRef.LogService.LogException(ex2);
			}
			//	X509Certificate? Cert2 = args.Chain.ChainElements.FirstOrDefault()?.Certificate;
			//	if(Cert2 is not null)
			//		MyCertValidationCallback(null, new RemoteCertificateEventArgs(Cert2, null, args.SslPolicyErrors));

			return false;
		}
		// Inspect args.ServerCertificate, args.Chain, args.PolicyErrors etc.
		// Return true to accept the cert, false to reject.
		//return MyOwnLogic(args.ServerCertificate, args.Chain, args.PolicyErrors);

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			LocalizationManager.Current.PropertyChanged += this.LocalizationManagerEventHandler;

			if (App.Current is not null)
			{
				this.CountDownTimer = App.Current.Dispatcher.CreateTimer();
				this.CountDownTimer.Interval = TimeSpan.FromMilliseconds(1000);
				this.CountDownTimer.Tick += this.CountDownEventHandler;
			}

			if (string.IsNullOrEmpty(ServiceRef.TagProfile.PhoneNumber))
			{
				try
				{
					ContentResponse Result = await InternetContent.PostAsync(
						new Uri("https://" + Constants.Domains.IdDomain + "/ID/CountryCode.ws"),
						string.Empty,                       // Data
		//				null,                               // Certificate
		//				(object? sender, RemoteCertificateEventArgs e) => MyCertValidationCallback(sender, e),          // RemoteCertificateValidator
						new KeyValuePair<string, string>("Accept", "application/json")  // Headers
					);
					Result.AssertOk();

					if ((Result.Decoded is Dictionary<string, object> Response) &&
						Response.TryGetValue("CountryCode", out object? cc) &&
						cc is string CountryCode &&
						ISO_3166_1.TryGetCountryByCode(CountryCode, out ISO_3166_Country? Country))
					{
						this.SelectedCountry = Country;
					}
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
				}
			}
		}


		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			LocalizationManager.Current.PropertyChanged -= this.LocalizationManagerEventHandler;

			if (this.CountDownTimer is not null)
			{
				this.CountDownTimer.Stop();
				this.CountDownTimer.Tick -= this.CountDownEventHandler;
				this.CountDownTimer = null;
			}

			await base.OnDispose();
		}

		/// <inheritdoc />
		public override async Task DoAssignProperties()
		{
			await base.DoAssignProperties();

			string? PhoneNumber = ServiceRef.TagProfile.PhoneNumber;

			if (string.IsNullOrEmpty(PhoneNumber))
			{
				try
				{
					ContentResponse Result = await InternetContent.PostAsync(
						new Uri("https://" + Constants.Domains.IdDomain + "/ID/CountryCode.ws"), string.Empty,
						new KeyValuePair<string, string>("Accept", "application/json"));

					Result.AssertOk();

					if ((Result.Decoded is Dictionary<string, object> Response) &&
						Response.TryGetValue("CountryCode", out object? cc) &&
						cc is string CountryCode &&
						ISO_3166_1.TryGetCountryByCode(CountryCode, out ISO_3166_Country? Country))
					{
						this.SelectedCountry = Country;
					}
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
				}
			}
			else
			{
				string SelectedCountry = ServiceRef.TagProfile.SelectedCountry!;

				if (ISO_3166_1.TryGetCountryByCode(SelectedCountry, out ISO_3166_Country? Country))
				{
					this.SelectedCountry = Country;
					this.PhoneNumber = PhoneNumber[(Country.DialCode.Length + 1)..];
					this.SendCodeCommand.NotifyCanExecuteChanged();
				}
			}
		}

		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			if (e.PropertyName == nameof(this.IsBusy))
				this.SendCodeCommand.NotifyCanExecuteChanged();
		}

		public void LocalizationManagerEventHandler(object? sender, PropertyChangedEventArgs e)
		{
			this.OnPropertyChanged(nameof(this.LocalizedSendCodeText));
			this.OnPropertyChanged(nameof(this.LocalizedValidationError));
		}

		[ObservableProperty]
		ISO_3166_Country selectedCountry = ISO_3166_1.DefaultCountry;

		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(SendCodeCommand))]
		bool numberIsValid;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(LocalizedValidationError))]
		bool typeIsValid;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(LocalizedValidationError))]
		bool lengthIsValid;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(LocalizedSendCodeText))]
		[NotifyCanExecuteChangedFor(nameof(SendCodeCommand))]
		[NotifyCanExecuteChangedFor(nameof(ResendCodeCommand))]
		private int countDownSeconds;

		[ObservableProperty]
		private IDispatcherTimer? countDownTimer;

		public string LocalizedSendCodeText
		{
			get
			{
				if (this.CountDownSeconds > 0)
					return ServiceRef.Localizer[nameof(AppResources.SendCodeSeconds), this.CountDownSeconds];

				return ServiceRef.Localizer[nameof(AppResources.SendCode)];
			}
		}

		public string LocalizedValidationError
		{
			get
			{
				if (!this.TypeIsValid)
					return ServiceRef.Localizer[nameof(AppResources.PhoneValidationDigits)];

				if (!this.LengthIsValid)
					return ServiceRef.Localizer[nameof(AppResources.PhoneValidationLength)];

				return string.Empty;
			}
		}

		/// <summary>
		/// Phone number
		/// </summary>
		[ObservableProperty]
		private string phoneNumber = string.Empty;

		public bool CanSendCode => this.NumberIsValid && !this.IsBusy &&
			(this.PhoneNumber.Length > 0) && (this.CountDownSeconds <= 0);

		public bool CanResendCode => this.CountDownSeconds <= 0;

		[RelayCommand]
		private async Task SelectPhoneCode()
		{
			SelectPhoneCodePopup Page = new();
			await MopupService.Instance.PushAsync(Page);

			ISO_3166_Country? Result = await Page.Result;

			if (Result is not null)
				this.SelectedCountry = Result;

			return;
		}

		[RelayCommand(CanExecute = nameof(CanSendCode))]
		private async Task SendCode()
		{
			this.IsBusy = true;

			try
			{
				if (!ServiceRef.NetworkService.IsOnline)
				{
					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.NetworkSeemsToBeMissing)]);
					return;
				}
				string FullPhoneNumber = $"+{this.SelectedCountry.DialCode}{this.PhoneNumber}";

				if (this.SelectedCountry.DialCode == "46") ///TODO: Make this more generic for other countries
					FullPhoneNumber = $"+{this.SelectedCountry.DialCode}{this.PhoneNumber.TrimStart('0')}";

				ContentResponse SendResult = await InternetContent.PostAsync(
					new Uri("https://" + Constants.Domains.IdDomain + "/ID/SendVerificationMessage.ws"),
					new Dictionary<string, object>()
					{
						{ "Nr", FullPhoneNumber },
						{ "AppName", Constants.Application.Name },
						{ "Language", CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
					}, new KeyValuePair<string, string>("Accept", "application/json"));

				SendResult.AssertOk();

				if (SendResult.Decoded is Dictionary<string, object> SendResponse &&
					SendResponse.TryGetValue("Status", out object? Obj) && Obj is bool SendStatus && SendStatus &&
					SendResponse.TryGetValue("IsTemporary", out Obj) && Obj is bool SendIsTemporary)
				{
					if (!string.IsNullOrEmpty(ServiceRef.TagProfile.PhoneNumber) && (ServiceRef.TagProfile.TestOtpTimestamp is null) && SendIsTemporary)
					{
						await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.SwitchingToTestPhoneNumberNotAllowed)]);
					}
					else
					{
						this.StartTimer();

						VerifyCodeNavigationArgs NavigationArgs = new(this, FullPhoneNumber);
						await ServiceRef.UiService.GoToAsync(nameof(VerifyCodePage), NavigationArgs, BackMethod.Pop);
						string? Code = await NavigationArgs.VarifyCode!.Task;

						if (!string.IsNullOrEmpty(Code))
						{
							//!!! The old code for existing accounts. Should be implemented somehow else
							// bool IsTest = this.PurposeRequired ? this.IsEducationalPurpose || this.IsExperimentalPurpose : this.TagProfile.IsTest;
							// PurposeUse Purpose = this.PurposeRequired ? (PurposeUse)this.PurposeNr : this.TagProfile.Purpose;

							PurposeUse Purpose = ServiceRef.TagProfile.Purpose;
							bool IsTest = (Purpose == PurposeUse.Educational) || (Purpose == PurposeUse.Experimental);

							ContentResponse VerifyResult = await InternetContent.PostAsync(
								new Uri("https://" + Constants.Domains.IdDomain + "/ID/VerifyNumber.ws"),
								new Dictionary<string, object>()
								{
									{ "Nr", FullPhoneNumber },
									{ "Code", int.Parse(Code, NumberStyles.None, CultureInfo.InvariantCulture) },
									{ "Test", IsTest }
								}, new KeyValuePair<string, string>("Accept", "application/json"));

							VerifyResult.AssertOk();

							if (VerifyResult.Decoded is Dictionary<string, object> VerifyResponse &&
								VerifyResponse.TryGetValue("Status", out Obj) && Obj is bool VerifyStatus && VerifyStatus &&
								VerifyResponse.TryGetValue("Domain", out Obj) && Obj is string VerifyDomain &&
								VerifyResponse.TryGetValue("Key", out Obj) && Obj is string VerifyKey &&
								VerifyResponse.TryGetValue("Secret", out Obj) && Obj is string VerifySecret &&
								VerifyResponse.TryGetValue("Temporary", out Obj) && Obj is bool VerifyIsTemporary)
							{
								ServiceRef.TagProfile.SetPhone(this.SelectedCountry.Alpha2, FullPhoneNumber);
								ServiceRef.TagProfile.SetPurpose(IsTest, Purpose);
								ServiceRef.TagProfile.TestOtpTimestamp = VerifyIsTemporary ? DateTime.Now : null;

								//!!! The old code for existing accounts. Should be implemented somehow else
								//!!! if (this.IsRevalidating)


								if (string.IsNullOrEmpty(ServiceRef.TagProfile.Domain))
								{
									bool DefaultConnectivity;

									try
									{
										(string HostName, int PortNumber, bool IsIpAddress) = await ServiceRef.NetworkService.LookupXmppHostnameAndPort(VerifyDomain);
										DefaultConnectivity = HostName == VerifyDomain && PortNumber == Waher.Networking.XMPP.XmppCredentials.DefaultPort;
									}
									catch (Exception)
									{
										DefaultConnectivity = false;
									}

									ServiceRef.TagProfile.SetDomain(VerifyDomain, DefaultConnectivity, VerifyKey, VerifySecret);
								}

								if (VerifyIsTemporary)
									GoToRegistrationStep(RegistrationStep.NameEntry);
								else
									GoToRegistrationStep(RegistrationStep.ValidateEmail);
							}
							else
							{
								await ServiceRef.UiService.DisplayAlert(
									ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
									ServiceRef.Localizer[nameof(AppResources.UnableToVerifyCode)],
									ServiceRef.Localizer[nameof(AppResources.Ok)]);
							}
						}
					}
				}
				else
				{
					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.SomethingWentWrongWhenSendingPhoneCode)]);
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);

				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], ex.Message,
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
			}
			finally
			{
				this.IsBusy = false;
			}
		}

		[RelayCommand(CanExecute = nameof(CanResendCode))]
		private async Task ResendCode()
		{
			try
			{
				if (!ServiceRef.NetworkService.IsOnline)
				{
					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.NetworkSeemsToBeMissing)]);
					return;
				}

				string FullPhoneNumber = $"+{this.SelectedCountry.DialCode}{this.PhoneNumber}";

				if (this.SelectedCountry.DialCode == "46") //If swedish phone nr
					FullPhoneNumber = $"+{this.SelectedCountry.DialCode}{this.PhoneNumber.TrimStart('0')}";

				ContentResponse SendResult = await InternetContent.PostAsync(
					new Uri("https://" + Constants.Domains.IdDomain + "/ID/SendVerificationMessage.ws"),
					new Dictionary<string, object>()
					{
						{ "Nr", FullPhoneNumber },
						{ "AppName", Constants.Application.Name },
						{ "Language", CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
					}, new KeyValuePair<string, string>("Accept", "application/json"));

				SendResult.AssertOk();

				if (SendResult.Decoded is Dictionary<string, object> SendResponse &&
					SendResponse.TryGetValue("Status", out object? Obj) &&
					Obj is bool SendStatus &&
					SendStatus)
				{
					this.StartTimer();
				}
				else
				{
					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.SomethingWentWrongWhenSendingPhoneCode)]);
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);

				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], ex.Message,
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
			}
		}

		private void StartTimer()
		{
			if (this.CountDownTimer is not null)
			{
				this.CountDownSeconds = 300;

				if (!this.CountDownTimer.IsRunning)
					this.CountDownTimer.Start();
			}
		}

		private void CountDownEventHandler(object? sender, EventArgs e)
		{
			if (this.CountDownTimer is not null)
			{
				if (this.CountDownSeconds > 0)
					this.CountDownSeconds--;
				else
					this.CountDownTimer.Stop();
			}
		}
	}
}
