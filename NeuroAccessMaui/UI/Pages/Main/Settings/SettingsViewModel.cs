using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Authentication;
using NeuroAccessMaui.Services.Cache;
using NeuroAccessMaui.Services.Cache.Invalidation;
using NeuroAccessMaui.Services.Kyc;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.UI.Pages.Identity.TransferIdentity;
using NeuroAccessMaui.UI.Pages.Onboarding;
using NeuroAccessMaui.UI.Popups.Settings;
using Waher.Content.Xml;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.StanzaErrors;
using Waher.Persistence;
using Waher.Persistence.Filters;
using NeuroAccessMaui.Services.Cache.Invalidation;

namespace NeuroAccessMaui.UI.Pages.Main.Settings
{
	/// <summary>
	/// The view model to bind to for when displaying the settings page.
	/// </summary>
	public partial class SettingsViewModel : XmppViewModel
	{
		private readonly IAuthenticationService authenticationService = ServiceRef.Provider.GetRequiredService<IAuthenticationService>();

		private const string allowed = "Allowed";
		private const string prohibited = "Prohibited";

		private readonly bool initializing = false;

		/// <summary>
		/// Creates an instance of the <see cref="SettingsViewModel"/> class.
		/// </summary>
		public SettingsViewModel()
			: base()
		{
			this.initializing = true;
			try
			{
				this.CanProhibitScreenCapture = ServiceRef.PlatformSpecific.CanProhibitScreenCapture;
				this.ScreenCaptureMode = ServiceRef.PlatformSpecific.ProhibitScreenCapture ? prohibited : allowed;

				this.CanUseFingerprint = ServiceRef.PlatformSpecific.SupportsFingerprintAuthentication;
				this.CanUseAlternativeAuthenticationMethods = this.CanUseFingerprint;
				this.AuthenticationMethod = ServiceRef.TagProfile.AuthenticationMethod.ToString();
				this.ApprovedAuthenticationMethod = this.AuthenticationMethod;

				this.DisplayMode = CurrentDisplayMode.ToString();

				// App and Hardware information
				this.VersionNumber = AppInfo.VersionString;
				this.BuildNumber = AppInfo.BuildString;
				this.BuildTime = GetBuildTime();
				this.DeviceManufactorer = DeviceInfo.Manufacturer.ToString();
				this.DeviceModel = DeviceInfo.Model.ToString();
				this.DevicePlatform = DeviceInfo.Platform.ToString();
				this.DeviceVersion = DeviceInfo.Version.ToString();
			}
			finally
			{
				this.initializing = false;
			}
		}

		/// <summary>
		/// Reference to current page. Needed to propagate radio-button states, as the current version of Maui does not
		/// handle these properly (at the time of writing).
		///
		/// TODO: Check if this has been fixed after updating Maui and related components.
		/// </summary>
		internal SettingsPage? Page { get; set; }

		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();
			this.NotifyCommandsCanExecuteChanged();
		}

		/// <inheritdoc/>
		protected override Task XmppService_ConnectionStateChanged(object? Sender, XmppState NewState)
		{
			return MainThread.InvokeOnMainThreadAsync(async () =>
			{
				await base.XmppService_ConnectionStateChanged(Sender, NewState);

				this.NotifyCommandsCanExecuteChanged();
			});
		}

		/// <inheritdoc/>
		public override void SetIsBusy(bool IsBusy)
		{
			base.SetIsBusy(IsBusy);
			this.NotifyCommandsCanExecuteChanged();
		}

		private void NotifyCommandsCanExecuteChanged()
		{
			this.RevokeCommand.NotifyCanExecuteChanged();
			this.CompromiseCommand.NotifyCanExecuteChanged();
			this.TransferCommand.NotifyCanExecuteChanged();
			this.ChangePasswordCommand.NotifyCanExecuteChanged();
		}

		public bool IsBetaEnabled
		{
			get => ServiceRef.TagProfile.HasBetaFeatures;
			set
			{
				if (value != ServiceRef.TagProfile.HasBetaFeatures)
				{
					ServiceRef.TagProfile.HasBetaFeatures = value;
				}
			}
		}

		#region Properties

			/// <summary>
			/// If screen capture prohibition can be controlled
			/// </summary>
			[ObservableProperty]
			private bool canProhibitScreenCapture;

		/// <summary>
		/// Screen capture mode.
		/// </summary>
		[ObservableProperty]
		private string screenCaptureMode;

		/// <summary>
		/// Selected display mode.
		/// </summary>
		[ObservableProperty]
		private string displayMode;

		/// <summary>
		/// If a restart is needed.
		/// </summary>
		[ObservableProperty]
		private bool restartNeeded;

		/// <summary>
		/// If fingerprint authentication is permitted on device.
		/// </summary>
		[ObservableProperty]
		private bool canUseFingerprint;

		/// <summary>
		/// If alternative authentication methods are availasble on device.
		/// </summary>
		[ObservableProperty]
		private bool canUseAlternativeAuthenticationMethods;

		/// <summary>
		/// User Authentication method to use.
		/// </summary>
		[ObservableProperty]
		private string authenticationMethod;

		/// <summary>
		/// Approved User Authentication method to use.
		/// </summary>
		[ObservableProperty]
		private string approvedAuthenticationMethod;

		/// <summary>
		/// Version number of the app.
		/// </summary>
		[ObservableProperty]
		private string versionNumber;

		/// <summary>
		/// Build number of the app.
		/// </summary>
		[ObservableProperty]
		private string buildNumber;

		/// <summary>
		/// Manufactor or brand of used device.
		/// </summary>
		[ObservableProperty]
		private string deviceManufactorer;

		/// <summary>
		/// The model of used device.
		/// </summary>
		[ObservableProperty]
		private string deviceModel;

		/// <summary>
		/// Platform or operating system of used device.
		/// </summary>
		[ObservableProperty]
		private string devicePlatform;

		/// <summary>
		/// Version of device operating system.
		/// </summary>
		[ObservableProperty]
		private string deviceVersion;

		/// <summary>
		/// Build time of the app.
		/// </summary>
		[ObservableProperty]
		private string buildTime;

		/// <summary>
		/// Current display mode
		/// </summary>
		public static AppTheme CurrentDisplayMode
		{
			get
			{
				AppTheme? Result = Application.Current?.UserAppTheme;

				if (!Result.HasValue || Result.Value == AppTheme.Unspecified)
					Result = Application.Current?.PlatformAppTheme;

				return Result ?? AppTheme.Unspecified;
			}
		}

		/// <summary>
		/// Used to find out if a command can execute
		/// </summary>
		public bool CanExecuteCommands => !this.IsBusy && this.IsConnected;

		/// <summary>
		/// <inheritdoc/>
		/// </summary>
		protected override async void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			try
			{
				switch (e.PropertyName)
				{
					case nameof(this.DisplayMode):
						if (!this.initializing && Enum.TryParse(this.DisplayMode, out AppTheme Theme) && Theme != CurrentDisplayMode)
						{
							ServiceRef.ThemeService.SetTheme(Theme);
						}
						break;

					case nameof(this.ScreenCaptureMode):
						if (!this.initializing)
						{
							switch (this.ScreenCaptureMode)
							{
								case allowed:
									await PermitScreenCapture();
									break;

								case prohibited:
									await ProhibitScreenCapture();
									break;
							}
						}
						break;

					case nameof(this.AuthenticationMethod):
						if (!this.initializing &&
							this.AuthenticationMethod != this.ApprovedAuthenticationMethod &&
							Enum.TryParse(this.AuthenticationMethod, out AuthenticationMethod AuthenticationMethod))
						{
							if (await this.authenticationService.AuthenticateUserAsync(AuthenticationPurpose.ChangeAuthenticationMethod, true))
							{
								ServiceRef.TagProfile.AuthenticationMethod = AuthenticationMethod;
								this.ApprovedAuthenticationMethod = this.AuthenticationMethod;
							}
							else
							{
								this.AuthenticationMethod = this.ApprovedAuthenticationMethod;

								if (this.Page is not null)
								{
									// Needed to propagate radio-button states, as the current version of Maui does not
									// handle these properly (at the time of writing).
									//
									// TODO: Check if this has been fixed after updating Maui and related components.

									switch (Enum.Parse<AuthenticationMethod>(this.AuthenticationMethod))
									{
										case AuthenticationMethod.Password:
											this.Page.Fingerprint.IsChecked = false;
											this.Page.UsePassword.IsChecked = true;
											break;

										case AuthenticationMethod.Fingerprint:
											this.Page.UsePassword.IsChecked = false;
											this.Page.Fingerprint.IsChecked = true;
											break;
									}
								}
							}
						}
						break;
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Get the build time of the app as a formated string from build information.
		/// </summary>
		/// <returns> Build time excrated from assembly data </returns>
		private static string GetBuildTime()
		{
			Assembly Assembly = Assembly.GetExecutingAssembly();

			const string BuildVersionMetadataPrefix = "+build";

			AssemblyInformationalVersionAttribute? Attribute = Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
			if (Attribute?.InformationalVersion is not null)
			{
				string Value = Attribute.InformationalVersion;

				int DatePosition = Value.IndexOf(BuildVersionMetadataPrefix, System.StringComparison.OrdinalIgnoreCase);
				if (DatePosition > 0)
				{
					Value = Value[(DatePosition + BuildVersionMetadataPrefix.Length)..];

					return Value;
				}
			}

			return string.Empty;
		}

		#endregion

		#region Commands

		[RelayCommand(CanExecute = nameof(CanExecuteCommands))]
		internal async Task ChangePassword()
		{
			try
			{
				//Authenticate user
				await this.authenticationService.CheckUserBlockingAsync();
				if (await this.authenticationService.AuthenticateUserAsync(AuthenticationPurpose.ChangePassword, true) == false)
					return;

				//Update the network password
				await ServiceRef.XmppService.TryGenerateAndChangePassword();

				//Update the local password
				await ServiceRef.NavigationService.GoToAsync(nameof(OnboardingPage), new OnboardingNavigationArgs() { Scenario = OnboardingScenario.ChangePin });

				//Listen for completed event
				WeakReferenceMessenger.Default.Register<RegistrationPageMessage>(this, this.HandleRegistrationPageMessage);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		private async void HandleRegistrationPageMessage(object recipient, RegistrationPageMessage msg)
		{
			if (msg.Step != RegistrationStep.Complete)
				return;
			await ServiceRef.UiService.DisplayAlert(
				ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
				ServiceRef.Localizer[nameof(AppResources.PasswordChanged)]);
			WeakReferenceMessenger.Default.Unregister<RegistrationPageMessage>(this);
		}

		private static async Task PermitScreenCapture()
		{
			if (!ServiceRef.PlatformSpecific.CanProhibitScreenCapture)
				return;

			if (!await ServiceRef.Provider.GetRequiredService<IAuthenticationService>().AuthenticateUserAsync(AuthenticationPurpose.PermitScreenCapture))
				return;

			ServiceRef.PlatformSpecific.ProhibitScreenCapture = false;
		}

		private static async Task ProhibitScreenCapture()
		{
			if (!ServiceRef.PlatformSpecific.CanProhibitScreenCapture)
				return;

			if (!await ServiceRef.Provider.GetRequiredService<IAuthenticationService>().AuthenticateUserAsync(AuthenticationPurpose.ProhibitScreenCapture))
				return;

			ServiceRef.PlatformSpecific.ProhibitScreenCapture = true;
		}

		/// <inheritdoc/>
		public override async Task GoBack()
		{
			if (this.RestartNeeded)
				await App.StopAsync();
			else
				await base.GoBack();
		}

		[RelayCommand(CanExecute = nameof(CanExecuteCommands))]
		private async Task Revoke()
		{
			if (ServiceRef.TagProfile.LegalIdentity is null)
				return;

			try
			{
				if (!await AreYouSure(ServiceRef.Localizer[nameof(AppResources.AreYouSureYouWantToRevokeYourLegalIdentity)]))
					return;

				if (!await this.authenticationService.AuthenticateUserAsync(AuthenticationPurpose.RevokeIdentity, true))
					return;

				(bool succeeded, LegalIdentity? RevokedIdentity) = await ServiceRef.NetworkService.TryRequest(async () =>
				{
					try
					{
						return await ServiceRef.XmppService.ObsoleteLegalIdentity(ServiceRef.TagProfile.LegalIdentity.Id);
					}
					catch (ForbiddenException)
					{
						return null;
					}
				});

				if (succeeded)
				{
					if (RevokedIdentity is not null)
						await ServiceRef.TagProfile.RevokeLegalIdentity(RevokedIdentity);
					else
						await ServiceRef.TagProfile.ClearLegalIdentity();
					await ServiceRef.NavigationService.GoToAsync(nameof(OnboardingPage), new OnboardingNavigationArgs() { Scenario = OnboardingScenario.ReverifyIdentity });
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		[RelayCommand(CanExecute = nameof(CanExecuteCommands))]
		private async Task Compromise()
		{
			if (ServiceRef.TagProfile.LegalIdentity is null)
				return;

			try
			{
				if (!await AreYouSure(ServiceRef.Localizer[nameof(AppResources.AreYouSureYouWantToReportYourLegalIdentityAsCompromized)]))
					return;

				if (!await this.authenticationService.AuthenticateUserAsync(AuthenticationPurpose.ReportAsCompromized, true))
					return;

				(bool succeeded, LegalIdentity? CompromisedIdentity) = await ServiceRef.NetworkService.TryRequest(
					() => ServiceRef.XmppService.CompromiseLegalIdentity(ServiceRef.TagProfile.LegalIdentity.Id));

				if (succeeded && CompromisedIdentity is not null)
				{
					await ServiceRef.TagProfile.CompromiseLegalIdentity(CompromisedIdentity);
					await ServiceRef.NavigationService.GoToAsync(nameof(OnboardingPage), new OnboardingNavigationArgs() { Scenario = OnboardingScenario.ReverifyIdentity });
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		[RelayCommand(CanExecute = nameof(CanExecuteCommands))]
		private async Task Transfer()
		{
			if (ServiceRef.TagProfile.LegalIdentity is null)
				return;

			try
			{


				if (!await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.Confirm)],
					ServiceRef.Localizer[nameof(AppResources.AreYouSureYouWantToTransferYourLegalIdentity)],
					ServiceRef.Localizer[nameof(AppResources.Yes)],
					ServiceRef.Localizer[nameof(AppResources.No)]))
				{
					return;
				}

				string? Password = await this.authenticationService.InputPasswordAsync(AuthenticationPurpose.TransferIdentity);
				if (Password is null)
					return;

				this.SetIsBusy(true);

				try
				{
					StringBuilder Xml = new();
					XmlWriterSettings Settings = XML.WriterSettings(false, true);

					using (XmlWriter Output = XmlWriter.Create(Xml, Settings))
					{
						Output.WriteStartElement("Transfer", ContractsClient.NamespaceOnboarding);

						await ServiceRef.XmppService.ExportSigningKeys(Output);

						Output.WriteStartElement("Pin");
						Output.WriteAttributeString("pin", Password);
						Output.WriteEndElement();

						Output.WriteStartElement("Account", ContractsClient.NamespaceOnboarding);
						Output.WriteAttributeString("domain", ServiceRef.TagProfile.Domain);
						Output.WriteAttributeString("userName", ServiceRef.TagProfile.Account);
						Output.WriteAttributeString("password", ServiceRef.TagProfile.XmppPasswordHash);

						if (!string.IsNullOrEmpty(ServiceRef.TagProfile.XmppPasswordHashMethod))
						{
							Output.WriteAttributeString("passwordMethod", ServiceRef.TagProfile.XmppPasswordHashMethod);
						}

						Output.WriteEndElement();
						Output.WriteEndElement();
					}

					using RandomNumberGenerator Rnd = RandomNumberGenerator.Create();
					byte[] Data = Encoding.UTF8.GetBytes(Xml.ToString());
					byte[] Key = new byte[16];
					byte[] IV = new byte[16];

					Rnd.GetBytes(Key);
					Rnd.GetBytes(IV);

					using Aes Aes = Aes.Create();
					Aes.BlockSize = 128;
					Aes.KeySize = 256;
					Aes.Mode = CipherMode.CBC;
					Aes.Padding = PaddingMode.PKCS7;

					using ICryptoTransform Transform = Aes.CreateEncryptor(Key, IV);
					byte[] Encrypted = Transform.TransformFinalBlock(Data, 0, Data.Length);

					Xml.Clear();

					using (XmlWriter Output = XmlWriter.Create(Xml, Settings))
					{
						Output.WriteStartElement("Info", ContractsClient.NamespaceOnboarding);
						Output.WriteAttributeString("base64", Convert.ToBase64String(Encrypted));
						Output.WriteAttributeString("once", "true");
						Output.WriteAttributeString("expires", XML.Encode(DateTime.UtcNow.AddMinutes(1)));
						Output.WriteEndElement();
					}

					XmlElement Response = await ServiceRef.XmppService.IqSetAsync(Constants.Domains.OnboardingDomain, Xml.ToString());

					foreach (XmlNode N in Response.ChildNodes)
					{
						if (N is XmlElement Info && Info.LocalName == "Code" && Info.NamespaceURI == ContractsClient.NamespaceOnboarding)
						{
							string Code = XML.Attribute(Info, "code");
							string Url = "obinfo:" + Constants.Domains.IdDomain + ":" + Code + ":" +
								Convert.ToBase64String(Key) + ":" + Convert.ToBase64String(IV);

							await ServiceRef.XmppService.AddTransferCode(Code);
							await ServiceRef.NavigationService.GoToAsync(nameof(TransferIdentityPage), new TransferIdentityNavigationArgs(Url));
							return;
						}
					}

					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.UnexpectedResponse)]);
				}
				finally
				{
					this.SetIsBusy(false);
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		[RelayCommand]
		private static async Task ChangeLanguage()
		{
			await ServiceRef.PopupService.PushAsync<SelectLanguagePopup>();
		}

		[RelayCommand]
		private void ToggleDarkMode()
		{
			this.DisplayMode = "Dark";
		}

		[RelayCommand]
		private async Task ClearCacheAsync()
		{
			try
			{
				// Internet cache
				await Database.FindDelete<CacheEntry>(
					new FilterFieldGreaterOrEqualTo("Url", string.Empty));
				await Database.Provider.Flush();

				// Branding and KYC invalidations
				string? domain = ServiceRef.TagProfile.Domain;
				string? pubSub = ServiceRef.TagProfile.PubSubJid;
				ICacheInvalidationService invalidation = Waher.Runtime.Inventory.Types.InstantiateDefault<ICacheInvalidationService>(false);
				if (!string.IsNullOrWhiteSpace(domain))
					await invalidation.InvalidateByParentId($"KycProcess:{domain}", scope: "Kyc");
				if (!string.IsNullOrWhiteSpace(pubSub))
					await invalidation.InvalidateByParentId(pubSub, scope: "Branding");

				// ThemeService local cache
				await ServiceRef.ThemeService.ClearBrandingCacheForCurrentDomain();


				// Remove KYC drafts/current application (delete all, robustly)
				IEnumerable<KycReference> drafts = Array.Empty<KycReference>();
				try { drafts = await Database.Find<KycReference>(); } catch { /* ignore */ }
				foreach (KycReference draft in drafts)
				{
					try { await Database.Delete(draft); } catch { /* ignore individual failures */ }
				}
				await Database.Provider.Flush();

				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
					ServiceRef.Localizer[nameof(AppResources.CacheCleared)],
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}


		#endregion

		public void SetBetaFeaturesEnabled(bool Enabled)
		{
			this.IsBetaEnabled = Enabled;
		}
	}
}
