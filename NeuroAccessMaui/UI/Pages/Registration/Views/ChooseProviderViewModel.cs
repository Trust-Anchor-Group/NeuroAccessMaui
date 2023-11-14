using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Xml;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Localization;
using NeuroAccessMaui.Services.Tag;
using Waher.Content;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Registration.Views;

public partial class ChooseProviderViewModel : BaseRegistrationViewModel
{
	public ChooseProviderViewModel() : base(RegistrationStep.ChooseProvider)
	{
	}

	/// <inheritdoc />
	protected override async Task OnInitialize()
	{
		await base.OnInitialize();
		await this.SetDomainName();

		ServiceRef.TagProfile.Changed += this.TagProfile_Changed;
	}

	/// <inheritdoc />
	protected override async Task OnDispose()
	{
		ServiceRef.TagProfile.Changed -= this.TagProfile_Changed;

		await base.OnDispose();
	}

	protected override void OnPropertyChanged(PropertyChangedEventArgs e)
	{
		base.OnPropertyChanged(e);

		if (e.PropertyName == nameof(this.SelectedButton))
		{
			if ((this.SelectedButton is not null) && (this.SelectedButton.Button == ButtonType.Change))
			{
				MainThread.BeginInvokeOnMainThread(() =>
				{
					if (this.ScanQrCodeCommand.CanExecute(null))
					{
						this.ScanQrCodeCommand.Execute(null);
					}
				});
			}
		}
	}

	[RelayCommand]
	private async Task ScanQrCode()
	{
		string? Url = await Services.UI.QR.QrCode.ScanQrCode(nameof(AppResources.QrPageTitleScanInvitation), Constants.UriSchemes.Onboarding);

		if (string.IsNullOrEmpty(Url))
		{
			return;
		}

		string Scheme = Constants.UriSchemes.GetScheme(Url) ?? string.Empty;

		if (!string.Equals(Scheme, Constants.UriSchemes.Onboarding, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		string[] Parts = Url.Split(':');

		if (Parts.Length != 5)
		{
			await ServiceRef.UiSerializer.DisplayAlert(
				ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
				ServiceRef.Localizer[nameof(AppResources.InvalidInvitationCode)],
				ServiceRef.Localizer[nameof(AppResources.Ok)]);

			return;
		}

		string Domain = Parts[1];
		string Code = Parts[2];
		string KeyStr = Parts[3];
		string IVStr = Parts[4];
		string EncryptedStr;
		Uri Uri;

		try
		{
			Uri = new Uri("https://" + Domain + "/Onboarding/GetInfo");
		}
		catch (Exception ex)
		{
			ServiceRef.LogService.LogException(ex);

			await ServiceRef.UiSerializer.DisplayAlert(
				ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
				ServiceRef.Localizer[nameof(AppResources.InvalidInvitationCode)],
				ServiceRef.Localizer[nameof(AppResources.Ok)]);

			return;
		}
	}

	/*
	private async Task<bool> ScanQrCode()
	{



		this.SetIsBusy(this.CreateNewCommand, this.ScanQrCodeCommand);
		try
		{
			try
			{
				KeyValuePair<byte[], string> P = await InternetContent.PostAsync(Uri, Encoding.ASCII.GetBytes(Code), "text/plain",
					new KeyValuePair<string, string>("Accept", "text/plain"));

				object Decoded = await InternetContent.DecodeAsync(P.Value, P.Key, Uri);

				EncryptedStr = (string)Decoded;
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["UnableToAccessInvitation"], LocalizationResourceManager.Current["Ok"]);
				return false;
			}

			try
			{
				byte[] Key = Convert.FromBase64String(KeyStr);
				byte[] IV = Convert.FromBase64String(IVStr);
				byte[] Encrypted = Convert.FromBase64String(EncryptedStr);
				byte[] Decrypted;

				using (Aes Aes = Aes.Create())
				{
					Aes.BlockSize = 128;
					Aes.KeySize = 256;
					Aes.Mode = CipherMode.CBC;
					Aes.Padding = PaddingMode.PKCS7;

					using ICryptoTransform Decryptor = Aes.CreateDecryptor(Key, IV);
					Decrypted = Decryptor.TransformFinalBlock(Encrypted, 0, Encrypted.Length);
				}

				string Xml = Encoding.UTF8.GetString(Decrypted);

				XmlDocument Doc = new()
				{
					PreserveWhitespace = true
				};
				Doc.LoadXml(Xml);

				if (Doc.DocumentElement is null || Doc.DocumentElement.NamespaceURI != ContractsClient.NamespaceOnboarding)
					throw new Exception("Invalid Invitation XML");

				LinkedList<XmlElement> ToProcess = new();
				ToProcess.AddLast(Doc.DocumentElement);

				bool AccountDone = false;
				XmlElement LegalIdDefinition = null;
				string Pin = null;

				while (ToProcess.First is not null)
				{
					XmlElement E = ToProcess.First.Value;
					ToProcess.RemoveFirst();

					switch (E.LocalName)
					{
						case "ApiKey":
							KeyStr = XML.Attribute(E, "key");
							string Secret = XML.Attribute(E, "secret");
							Domain = XML.Attribute(E, "domain");

							await this.SelectDomain(Domain, KeyStr, Secret);

							await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["InvitationAccepted"],
								string.Format(LocalizationResourceManager.Current["InvitedToCreateAccountOnDomain"], Domain), LocalizationResourceManager.Current["Ok"]);
							break;

						case "Account":
							string UserName = XML.Attribute(E, "userName");
							string Password = XML.Attribute(E, "password");
							string PasswordMethod = XML.Attribute(E, "passwordMethod");
							Domain = XML.Attribute(E, "domain");

							string DomainBak = this.TagProfile.Domain;
							bool DefaultConnectivityBak = this.TagProfile.DefaultXmppConnectivity;
							string ApiKeyBak = this.TagProfile.ApiKey;
							string ApiSecretBak = this.TagProfile.ApiSecret;

							await this.SelectDomain(Domain, string.Empty, string.Empty);

							if (!await this.ConnectToAccount(UserName, Password, PasswordMethod, string.Empty, LegalIdDefinition, Pin))
							{
								await this.TagProfile.SetDomain(DomainBak, DefaultConnectivityBak, ApiKeyBak, ApiSecretBak);
								throw new Exception("Invalid account.");
							}

							LegalIdDefinition = null;
							this.AccountName = UserName;
							AccountDone = true;
							break;

						case "LegalId":
							LegalIdDefinition = E;
							break;

						case "Pin":
							Pin = XML.Attribute(E, "pin");
							break;

						case "Transfer":
							foreach (XmlNode N in E.ChildNodes)
							{
								if (N is XmlElement E2)
									ToProcess.AddLast(E2);
							}
							break;

						default:
							throw new Exception("Invalid Invitation XML");
					}
				}

				if (LegalIdDefinition is not null)
					await this.XmppService.ImportSigningKeys(LegalIdDefinition);

				if (AccountDone)
				{
					this.UiSerializer.BeginInvokeOnMainThread(() =>
					{
						this.SetIsDone(this.CreateNewCommand, this.ScanQrCodeCommand);

						this.OnStepCompleted(EventArgs.Empty);
					});
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["InvalidInvitationCode"], LocalizationResourceManager.Current["Ok"]);
				return false;
			}
		}
		finally
		{
			this.BeginInvokeSetIsDone(this.CreateNewCommand, this.ScanQrCodeCommand);
		}

		return true;
	}
	*/


	/// <summary>
	/// The localized intro text to display to the user for explaining what 'choose account' is for.
	/// </summary>
	[ObservableProperty]
	private string domainName = string.Empty;

	/// <summary>
	/// The localized intro text to display to the user for explaining what 'choose account' is for.
	/// </summary>
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasLocalizedName))]
	private string localizedName = string.Empty;

	/// <summary>
	/// The localized intro text to display to the user for explaining what 'choose account' is for.
	/// </summary>
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasLocalizedDescription))]
	private string localizedDescription = string.Empty;

	/// <summary>
	/// The localized intro text to display to the user for explaining what 'choose account' is for.
	/// </summary>
	public bool HasLocalizedName => this.LocalizedName.Length > 0;

	/// <summary>
	/// The localized intro text to display to the user for explaining what 'choose account' is for.
	/// </summary>
	public bool HasLocalizedDescription => this.LocalizedDescription.Length > 0;


	/// <summary>
	/// Holds the list of buttons to display.
	/// </summary>
	public Collection<ButtonInfo> Buttons { get; } =
		[
			new(ButtonType.Approve),
			new(ButtonType.Change),
		];


	/// <summary>
	/// The selected Button
	/// </summary>
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(ContinueCommand))]
	private ButtonInfo? selectedButton;

	private async void TagProfile_Changed(object? Sender, PropertyChangedEventArgs e)
	{
		if (this.DomainName != ServiceRef.TagProfile.Domain)
		{
			await this.SetDomainName();
		}
	}

	private async Task SetDomainName()
	{
		if (string.IsNullOrEmpty(ServiceRef.TagProfile.Domain))
		{
			this.DomainName = string.Empty;
			this.LocalizedName = string.Empty;
			this.LocalizedDescription = string.Empty;
			return;
		}

		this.DomainName = ServiceRef.TagProfile.Domain;

		try
		{
			Uri DomainInfo = new("https://" + this.DomainName + "/Agent/Account/DomainInfo");
			string AcceptLanguage = App.SelectedLanguage.TwoLetterISOLanguageName;

			if (AcceptLanguage != "en")
			{
				AcceptLanguage += ";q=1,en;q=0.9";
			}

			object Result = await InternetContent.GetAsync(DomainInfo,
				new KeyValuePair<string, string>("Accept", "application/json"),
				new KeyValuePair<string, string>("Accept-Language", AcceptLanguage));

			if (Result is Dictionary<string, object> Response)
			{
				if (Response.TryGetValue("humanReadableName", out object? Obj) && Obj is string LocalizedName)
				{
					this.LocalizedName = LocalizedName;
				}

				if (Response.TryGetValue("humanReadableDescription", out Obj) && Obj is string LocalizedDescription)
				{
					this.LocalizedDescription = LocalizedDescription;
				}
			}
		}
		catch (Exception ex)
		{
			ServiceRef.LogService.LogException(ex);
		}
	}

	public bool CanContinue => (this.SelectedButton is not null) && (this.SelectedButton.Button == ButtonType.Approve);

	[RelayCommand(CanExecute = nameof(CanContinue))]
	private void Continue()
	{

	}
}

public enum ButtonType
{
	Approve = 0,
	Change = 1,
}

public partial class ButtonInfo : ObservableObject
{
	public ButtonInfo(ButtonType Button)
	{
		this.Button = Button;

		LocalizationManager.CurrentCultureChanged += this.OnCurrentCultureChanged;
	}

	~ButtonInfo()
	{
		LocalizationManager.CurrentCultureChanged -= this.OnCurrentCultureChanged;
	}

	private void OnCurrentCultureChanged(object? Sender, CultureInfo Culture)
	{
		this.OnPropertyChanged(nameof(this.LocalizedName));
		//!!! this.OnPropertyChanged(nameof(this.LocalizedDescription));
	}

	public ButtonType Button { get; set; }

	public string LocalizedName
	{
		get
		{
			return this.Button switch
			{
				ButtonType.Approve => ServiceRef.Localizer[nameof(AppResources.ProviderSectionApproveOption)],
				ButtonType.Change => ServiceRef.Localizer[nameof(AppResources.ProviderSectionChangeOption)],
				_ => throw new NotImplementedException(),
			};
		}
	}

	public Geometry ImageData
	{
		get
		{
			return this.Button switch
			{
				ButtonType.Approve => Geometries.ApproveProviderIconPath,
				ButtonType.Change => Geometries.ChangeProviderIconPath,
				_ => throw new NotImplementedException(),
			};
		}
	}
}
