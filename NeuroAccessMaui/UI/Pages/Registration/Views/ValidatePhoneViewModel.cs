using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Mopups.Services;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Data;
using NeuroAccessMaui.Services.Localization;
using NeuroAccessMaui.Services.Navigation;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.UI.Pages.Main.VerifyCode;
using NeuroAccessMaui.UI.Popups;
using Waher.Content;

namespace NeuroAccessMaui.UI.Pages.Registration.Views;

public partial class ValidatePhoneViewModel : BaseRegistrationViewModel, ICodeVerification
{
	public ValidatePhoneViewModel() : base(RegistrationStep.ValidatePhone)
	{
	}

	/// <inheritdoc/>
	protected override async Task OnInitialize() //!!! this should be reworked to use the StateView.StateKey specific
	{
		await base.OnInitialize();

		LocalizationManager.Current.PropertyChanged += this.PropertyChangedEventHandler;

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
				object Result = await InternetContent.PostAsync(
					new Uri("https://" + Constants.Domains.IdDomain + "/ID/CountryCode.ws"), string.Empty,
					new KeyValuePair<string, string>("Accept", "application/json"));

				if ((Result is Dictionary<string, object> Response) &&
					Response.TryGetValue("CountryCode", out object? cc) &&
					(cc is string CountryCode))
				{
					if (ISO_3166_1.TryGetCountryByCode(CountryCode, out ISO3166Country? Country))
					{
						this.SelectedCountry = Country;
					}
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
		LocalizationManager.Current.PropertyChanged -= this.PropertyChangedEventHandler;

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

		if (string.IsNullOrEmpty(ServiceRef.TagProfile.PhoneNumber))
		{
			try
			{
				object Result = await InternetContent.PostAsync(
					new Uri("https://" + Constants.Domains.IdDomain + "/ID/CountryCode.ws"), string.Empty,
					new KeyValuePair<string, string>("Accept", "application/json"));

				if ((Result is Dictionary<string, object> Response) &&
					Response.TryGetValue("CountryCode", out object? cc) &&
					(cc is string CountryCode))
				{
					if (ISO_3166_1.TryGetCountryByCode(CountryCode, out ISO3166Country? Country))
					{
						this.SelectedCountry = Country;
					}
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}
		else
		{
			this.PhoneNumber = ServiceRef.TagProfile.PhoneNumber;

			if (ISO_3166_1.TryGetCountryByPhone(this.PhoneNumber, out ISO3166Country? Country))
			{
				this.SelectedCountry = Country;
			}
		}
	}

	protected override void OnPropertyChanged(PropertyChangedEventArgs e)
	{
		base.OnPropertyChanged(e);

		if (e.PropertyName == nameof(this.IsBusy))
		{
			this.SendCodeCommand.NotifyCanExecuteChanged();
		}
	}

	public void PropertyChangedEventHandler(object? sender, PropertyChangedEventArgs e)
	{
		this.OnPropertyChanged(nameof(this.LocalizedSendCodeText));
		this.OnPropertyChanged(nameof(this.LocalizedValidationError));
	}

	[ObservableProperty]
	ISO3166Country selectedCountry = ISO_3166_1.DefaultCountry;

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
			{
				return ServiceRef.Localizer[nameof(AppResources.SendCodeSeconds), this.CountDownSeconds];
			}

			return ServiceRef.Localizer[nameof(AppResources.SendCode)];
		}
	}

	public string LocalizedValidationError
	{
		get
		{
			if (!this.TypeIsValid)
			{
				return ServiceRef.Localizer[nameof(AppResources.PhoneValidationDigits)];
			}

			if (!this.LengthIsValid)
			{
				return ServiceRef.Localizer[nameof(AppResources.PhoneValidationLength)];
			}

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
		SelectPhoneCodePage Page = new();
		await MopupService.Instance.PushAsync(Page);

		ISO3166Country? Result = await Page.Result;

		if (Result is not null)
		{
			this.SelectedCountry = Result;
		}

		return;
	}

	[RelayCommand(CanExecute = nameof(CanSendCode))]
	private async Task SendCode()
	{
		//!!! for VerifyCodePage tests
		/*
		this.StartTimer();
		VerifyCodeNavigationArgs NavigationArgs1 = new(this, "+15551234567");
		await ServiceRef.NavigationService.GoToAsync(nameof(VerifyCodePage), NavigationArgs1, BackMethod.Pop);
		string? Code1 = await NavigationArgs1.VarifyCode!.Task;
		return;
		*/
		this.IsBusy = true;

		try
		{
			if (!ServiceRef.NetworkService.IsOnline)
			{
				await ServiceRef.UiSerializer.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.NetworkSeemsToBeMissing)]);
				return;
			}

			//!!! string FullPhoneNumber = $"+{this.SelectedCountry.DialCode}{this.PhoneNumber}";
			string FullPhoneNumber = "+15551234567";

			object SendResult = await InternetContent.PostAsync(
				new Uri("https://" + Constants.Domains.IdDomain + "/ID/SendVerificationMessage.ws"),
				new Dictionary<string, object>()
				{
						{ "Nr", FullPhoneNumber }
				}, new KeyValuePair<string, string>("Accept", "application/json"));

			if (SendResult is Dictionary<string, object> SendResponse &&
				SendResponse.TryGetValue("Status", out object? Obj) && Obj is bool SendStatus && SendStatus &&
				SendResponse.TryGetValue("IsTemporary", out Obj) && Obj is bool SendIsTemporary)
			{
				if (!string.IsNullOrEmpty(ServiceRef.TagProfile.PhoneNumber) && (ServiceRef.TagProfile.TestOtpTimestamp is null) && SendIsTemporary)
				{
					await ServiceRef.UiSerializer.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.SwitchingToTestPhoneNumberNotAllowed)]);
				}
				else
				{
					this.StartTimer();

					VerifyCodeNavigationArgs NavigationArgs = new(this, FullPhoneNumber);
					await ServiceRef.NavigationService.GoToAsync(nameof(VerifyCodePage), NavigationArgs, BackMethod.Pop);
					string? Code = await NavigationArgs.VarifyCode!.Task;

					if (!string.IsNullOrEmpty(Code))
					{
						//!!! bool IsTest = this.PurposeRequired ? this.IsEducationalPurpose || this.IsExperimentalPurpose : this.TagProfile.IsTest;
						//!!! PurposeUse Purpose = this.PurposeRequired ? (PurposeUse)this.PurposeNr : this.TagProfile.Purpose;

						PurposeUse Purpose = ServiceRef.TagProfile.Purpose;
						bool IsTest = (Purpose == PurposeUse.Educational) || (Purpose == PurposeUse.Experimental);

						object VerifyResult = await InternetContent.PostAsync(
							new Uri("https://" + Constants.Domains.IdDomain + "/ID/VerifyNumber.ws"),
							new Dictionary<string, object>()
							{
								{ "Nr", FullPhoneNumber },
								{ "Code", int.Parse(Code, NumberStyles.None, CultureInfo.InvariantCulture) },
								{ "Test", IsTest }
							}, new KeyValuePair<string, string>("Accept", "application/json"));


						if (VerifyResult is Dictionary<string, object> VerifyResponse &&
							VerifyResponse.TryGetValue("Status", out Obj) && Obj is bool VerifyStatus && VerifyStatus &&
							VerifyResponse.TryGetValue("Domain", out Obj) && Obj is string VerifyDomain &&
							VerifyResponse.TryGetValue("Key", out Obj) && Obj is string VerifyKey &&
							VerifyResponse.TryGetValue("Secret", out Obj) && Obj is string VerifySecret &&
							VerifyResponse.TryGetValue("Temporary", out Obj) && Obj is bool VerifyIsTemporary)
						{
							ServiceRef.TagProfile.SetPhone(FullPhoneNumber);
							ServiceRef.TagProfile.SetPurpose(IsTest, Purpose);
							ServiceRef.TagProfile.SetTestOtpTimestamp(VerifyIsTemporary ? DateTime.Now : null);

							//!!! if (this.IsRevalidating)
							if (false)
							{
							}
							else
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

							ServiceRef.TagProfile.GoToStep(RegistrationStep.ValidateEmail);
							WeakReferenceMessenger.Default.Send(new RegistrationPageMessage(ServiceRef.TagProfile.Step));
						}
						else
						{
							await ServiceRef.UiSerializer.DisplayAlert(
								ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
								ServiceRef.Localizer[nameof(AppResources.UnableToVerifyCode)],
								ServiceRef.Localizer[nameof(AppResources.Ok)]);
						}
					}
				}
			}
			else
			{
				await ServiceRef.UiSerializer.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.SomethingWentWrongWhenSendingPhoneCode)]);
			}
		}
		catch (Exception ex)
		{
			ServiceRef.LogService.LogException(ex);

			await ServiceRef.UiSerializer.DisplayAlert(
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
				await ServiceRef.UiSerializer.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.NetworkSeemsToBeMissing)]);
				return;
			}

			//!!! string FullPhoneNumber = $"+{this.SelectedCountry.DialCode}{this.PhoneNumber}";
			string FullPhoneNumber = "+15551234567";

			object SendResult = await InternetContent.PostAsync(
				new Uri("https://" + Constants.Domains.IdDomain + "/ID/SendVerificationMessage.ws"),
				new Dictionary<string, object>()
				{
						{ "Nr", FullPhoneNumber }
				}, new KeyValuePair<string, string>("Accept", "application/json"));

			if (SendResult is Dictionary<string, object> SendResponse &&
				SendResponse.TryGetValue("Status", out object? Obj) && Obj is bool SendStatus && SendStatus)
			{
				this.StartTimer();
			}
			else
			{
				await ServiceRef.UiSerializer.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.SomethingWentWrongWhenSendingPhoneCode)]);
			}
		}
		catch (Exception ex)
		{
			ServiceRef.LogService.LogException(ex);

			await ServiceRef.UiSerializer.DisplayAlert(
				ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], ex.Message,
				ServiceRef.Localizer[nameof(AppResources.Ok)]);
		}
	}

	private void StartTimer()
	{
		if (this.CountDownTimer is not null)
		{
			this.CountDownSeconds = 30;

			if (!this.CountDownTimer.IsRunning)
			{
				this.CountDownTimer.Start();
			}
		}
	}

	private void CountDownEventHandler(object? sender, EventArgs e)
	{
		if (this.CountDownTimer is not null)
		{
			if (this.CountDownSeconds > 0)
			{
				this.CountDownSeconds--;
			}
			else
			{
				this.CountDownTimer.Stop();
			}
		}
	}
}
