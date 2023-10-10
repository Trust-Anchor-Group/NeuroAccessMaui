using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mopups.Services;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Data;
using NeuroAccessMaui.UI.Popups;
using Waher.Content;

namespace NeuroAccessMaui.Pages.Registration.Views;

public partial class ValidatePhoneViewModel : BaseRegistrationViewModel
{
	public ValidatePhoneViewModel()
	{
	}


	/// <inheritdoc/>
	protected override async Task OnInitialize() //!!! this should be reworked to use the StateView.StateKey specific
	{
		await base.OnInitialize();

		if (string.IsNullOrEmpty(ServiceRef.TagProfile.PhoneNumber))
		{
			try
			{
				object Result = await InternetContent.PostAsync(
					new Uri("https://" + Constants.Domains.IdDomain + "/ID/CountryCode.ws"), string.Empty,
					new KeyValuePair<string, string>("Accept", "application/json"));

				if ((Result is Dictionary<string, object> Response) &&
					Response.TryGetValue("CountryCode", out object? cc) &&
					Response.TryGetValue("PhoneCode", out object? pc) &&
					(cc is string CountryCode) && (pc is string PhoneCode))
				{
					if (ISO_3166_1.TryGetCountryByPhone(CountryCode, PhoneCode, out ISO3166Country? Country))
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
		}
	}

	[ObservableProperty]
	ISO3166Country selectedCountry = ISO_3166_1.DefaultCountry;

	/// <summary>
	/// Phone number
	/// </summary>
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(SendCodeCommand))]
	private string phoneNumber = string.Empty;

	[RelayCommand]
	private async Task SelectPhoneCode()
	{
		SelectPhoneCodePage Page = new();
		await MopupService.Instance.PushAsync(Page);

		ISO3166Country? Result = await Page.Result;
		return;
	}

	public bool CanSendCode => false;

	[RelayCommand(CanExecute = nameof(CanSendCode))]
	private void SendCode()
	{
		/*
		if (!this.NetworkService.IsOnline)
		{
			await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["NetworkSeemsToBeMissing"]);
			return;
		}

		this.PhoneNrValidated = false;
		this.SetIsBusy(this.SendAndVerifyPhoneNrCodeCommand, this.SendPhoneNrCodeCommand, this.VerifyPhoneNrCodeCommand);

		try
		{
			string TrimmedNumber = this.TrimPhoneNumber(this.PhoneNumber);

			object SendResult = await InternetContent.PostAsync(
				new Uri("https://" + Constants.Domains.IdDomain + "/ID/SendVerificationMessage.ws"),
				new Dictionary<string, object>()
				{
						{ "Nr", TrimmedNumber }
				}, new KeyValuePair<string, string>("Accept", "application/json"));

			if (SendResult is Dictionary<string, object> SendResponse &&
				SendResponse.TryGetValue("Status", out object SendObj) && SendObj is bool SendStatus && SendStatus
				&& SendResponse.TryGetValue("IsTemporary", out SendObj) && SendObj is bool SentIsTemporary)
			{
				if (!string.IsNullOrEmpty(this.TagProfile.PhoneNumber) && !this.TagProfile.TestOtpTimestamp.HasValue && SentIsTemporary)
				{
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["SwitchingToTestPhoneNumberNotAllowed"]);
				}
				else
				{
					this.StartTimer("phone");

					Popups.VerifyCode.VerifyCodePage Page = new(LocalizationResourceManager.Current["SendPhoneNumberWarning"]);
					await PopupNavigation.Instance.PushAsync(Page);
					string Code = await Page.Result;

					if (!string.IsNullOrEmpty(Code))
					{
						bool IsTest = this.PurposeRequired ? this.IsEducationalPurpose || this.IsExperimentalPurpose : this.TagProfile.IsTest;
						PurposeUse Purpose = this.PurposeRequired ? (PurposeUse)this.PurposeNr : this.TagProfile.Purpose;

						object VerifyResult = await InternetContent.PostAsync(
							new Uri("https://" + Constants.Domains.IdDomain + "/ID/VerifyNumber.ws"),
							new Dictionary<string, object>()
							{
									{ "Nr", TrimmedNumber },
									{ "Code", int.Parse(Code) },
									{ "Test", IsTest }
							}, new KeyValuePair<string, string>("Accept", "application/json"));

						this.PhoneNrVerificationCode = string.Empty;

						if (VerifyResult is Dictionary<string, object> VerifyResponse &&
							VerifyResponse.TryGetValue("Status", out object VerifyObj) && VerifyObj is bool VerifyStatus && VerifyStatus &&
							VerifyResponse.TryGetValue("Domain", out VerifyObj) && VerifyObj is string VerifyDomain &&
							VerifyResponse.TryGetValue("Key", out VerifyObj) && VerifyObj is string VerifyKey &&
							VerifyResponse.TryGetValue("Secret", out VerifyObj) && VerifyObj is string VerifySecret &&
							VerifyResponse.TryGetValue("Temporary", out VerifyObj) && VerifyObj is bool VerifyIsTemporary)
						{
							this.PhoneNrValidated = true;

							this.TagProfile.SetPhone(TrimmedNumber);
							this.TagProfile.SetPurpose(IsTest, Purpose);
							this.TagProfile.SetTestOtpTimestamp(VerifyIsTemporary ? DateTime.Now : null);

							if (this.IsRevalidating)
								await this.TagProfile.RevalidateContactInfo();
							else
							{
								bool DefaultConnectivity;
								try
								{
									(string HostName, int PortNumber, bool IsIpAddress) = await this.NetworkService.LookupXmppHostnameAndPort(VerifyDomain);
									DefaultConnectivity = HostName == VerifyDomain && PortNumber == Waher.Networking.XMPP.XmppCredentials.DefaultPort;
								}
								catch (Exception)
								{
									DefaultConnectivity = false;
								}

								await this.TagProfile.SetDomain(VerifyDomain, DefaultConnectivity, VerifyKey, VerifySecret);
							}

							this.OnStepCompleted(EventArgs.Empty);
						}
						else
						{
							this.PhoneNrValidated = false;

							await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"],
								LocalizationResourceManager.Current["UnableToVerifyCode"], LocalizationResourceManager.Current["Ok"]);
						}
					}
				}
			}
			else
			{
				this.PhoneNrValidated = false;

				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["SomethingWentWrongWhenSendingPhoneCode"]);
			}
		}
		catch (Exception ex)
		{
			this.PhoneNrValidated = false;

			this.LogService.LogException(ex);
			await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], ex.Message, LocalizationResourceManager.Current["Ok"]);
		}
		finally
		{
			this.BeginInvokeSetIsDone(this.SendAndVerifyPhoneNrCodeCommand, this.SendPhoneNrCodeCommand, this.VerifyPhoneNrCodeCommand);
		}
		*/
	}
}
