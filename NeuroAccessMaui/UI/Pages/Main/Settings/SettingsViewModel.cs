using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using Waher.Content.Xml;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using NeuroAccessMaui.UI.Pages.Identity;
using Mopups.Services;
using NeuroAccessMaui.UI.Popups;
using NeuroAccessMaui.Services.Tag;

namespace NeuroAccessMaui.UI.Pages.Main.Settings
{
	/// <summary>
	/// The view model to bind to for when displaying security options.
	/// </summary>
	public partial class SettingsViewModel : XmppViewModel
	{
		private bool initializing = false;

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
				this.ScreenCaptureProhibited = ServiceRef.PlatformSpecific.ProhibitScreenCapture;
				this.ScreenCaptureAllowed = !this.ScreenCaptureProhibited;
				this.CanUseFingerprint = ServiceRef.PlatformSpecific.SupportsFingerprintAuthentication;
				this.CanUseAlternativeAuthenticationMethods = this.CanUseFingerprint;

				switch (DisplayMode)
				{
					case AppTheme.Light:
						this.IsLightMode = true;
						break;

					case AppTheme.Dark:
						this.IsDarkMode = true;
						break;
				}

				this.ResetAuthenticationMode();
			}
			finally
			{
				this.initializing = false;
			}
		}

		protected override async Task OnInitialize()
		{
			await base.OnInitialize();
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
			this.ChangePinCommand.NotifyCanExecuteChanged();
		}

		#region Properties

		/// <summary>
		/// If screen capture prohibition can be controlled
		/// </summary>
		[ObservableProperty]
		private bool canProhibitScreenCapture;

		/// <summary>
		/// If screen capture is allowed.
		/// </summary>
		[ObservableProperty]
		private bool screenCaptureAllowed;

		/// <summary>
		/// If screen capture is prohibited.
		/// </summary>
		[ObservableProperty]
		private bool screenCaptureProhibited;

		/// <summary>
		/// Gets or sets whether the current display mode is Light Mode.
		/// </summary>
		[ObservableProperty]
		private bool isLightMode;

		/// <summary>
		/// Gets or sets whether the current display mode is Dark Mode.
		/// </summary>
		[ObservableProperty]
		private bool isDarkMode;

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
		/// If PIN code should be used to authenticate user.
		/// </summary>
		[ObservableProperty]
		private bool usePinCode;

		/// <summary>
		/// If fingerprint should be used to authenticate user.
		/// </summary>
		[ObservableProperty]
		private bool useFingerprint;

		/// <summary>
		/// Current display mode
		/// </summary>
		public static AppTheme DisplayMode
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
		/// Used to find out if an ICommand can execute
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
					case nameof(this.IsLightMode):
						if (!this.initializing)
							this.SetTheme(this.IsLightMode ? AppTheme.Light : AppTheme.Dark);
						break;

					case nameof(this.IsDarkMode):
						if (!this.initializing)
							this.SetTheme(this.IsDarkMode ? AppTheme.Dark : AppTheme.Light);
						break;

					case nameof(this.ScreenCaptureAllowed):
						if (!this.initializing && this.ScreenCaptureAllowed)
							await PermitScreenCapture();
						break;

					case nameof(this.ScreenCaptureProhibited):
						if (!this.initializing && this.ScreenCaptureProhibited)
							await ProhibitScreenCapture();
						break;

					case nameof(this.UsePinCode):
						if (!this.initializing && this.UsePinCode)
						{
							if (await App.AuthenticateUser(true))
								ServiceRef.TagProfile.SetAuthenticationMethod(AuthenticationMethod.Pin);
							else
								await Task.Delay(100).ContinueWith((_) => MainThread.InvokeOnMainThreadAsync(this.ResetAuthenticationMode));
						}
						break;

					case nameof(this.UseFingerprint):
						if (!this.initializing && this.UseFingerprint)
						{
							if (await App.AuthenticateUser(true))
								ServiceRef.TagProfile.SetAuthenticationMethod(AuthenticationMethod.Fingerprint);
							else
								await Task.Delay(100).ContinueWith((_) => MainThread.InvokeOnMainThreadAsync(this.ResetAuthenticationMode));
						}
						break;
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		private void ResetAuthenticationMode()
		{
			bool Bak = this.initializing;

			this.initializing = true;
			try
			{
				this.UsePinCode = ServiceRef.TagProfile.AuthenticationMethod == AuthenticationMethod.Pin;
				this.UseFingerprint = ServiceRef.TagProfile.AuthenticationMethod == AuthenticationMethod.Fingerprint;
			}
			finally
			{
				this.initializing = Bak;
			}
		}

		private void SetTheme(AppTheme Theme)
		{
			ServiceRef.TagProfile.SetTheme(Theme);

			if (!this.RestartNeeded)
			{
				// TODO: When changing Theme, menu items in the flyout menu are not styled correctly. The foreground color
				// is not updated. According to info currently available, a future version of Maui allows you to fix this
				// with a stylable FlyoutForegroundColor, which does not exist in the currently used version.

				this.RestartNeeded = true;
				ServiceRef.UiSerializer.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.Message)],
					ServiceRef.Localizer[nameof(AppResources.RestartNeededDueToThemeChange)],
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
			}
		}

		#endregion

		#region Commands

		[RelayCommand(CanExecute = nameof(CanExecuteCommands))]
		internal static async Task ChangePin()
		{
			try
			{
				await App.CheckUserBlocking();
				await ServiceRef.NavigationService.GoToAsync(nameof(ChangePinPage));
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiSerializer.DisplayException(ex);
			}
		}

		private static async Task PermitScreenCapture()
		{
			if (!ServiceRef.PlatformSpecific.CanProhibitScreenCapture)
				return;

			if (!await App.AuthenticateUser())
				return;

			ServiceRef.PlatformSpecific.ProhibitScreenCapture = false;
		}

		private static async Task ProhibitScreenCapture()
		{
			if (!ServiceRef.PlatformSpecific.CanProhibitScreenCapture)
				return;

			if (!await App.AuthenticateUser())
				return;

			ServiceRef.PlatformSpecific.ProhibitScreenCapture = true;
		}

		[RelayCommand]
		private async Task GoBack()
		{
			if (this.RestartNeeded)
				await App.Stop();
			else
				await ServiceRef.NavigationService.GoBackAsync();
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

				if (!await App.AuthenticateUser(true))
					return;

				(bool succeeded, LegalIdentity? RevokedIdentity) = await ServiceRef.NetworkService.TryRequest(
					() => ServiceRef.XmppService.ObsoleteLegalIdentity(ServiceRef.TagProfile.LegalIdentity.Id));

				if (succeeded && RevokedIdentity is not null)
				{
					ServiceRef.TagProfile.RevokeLegalIdentity(RevokedIdentity);
					await App.SetRegistrationPageAsync();
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiSerializer.DisplayException(ex);
			}
		}

		private static async Task<bool> AreYouSure(string Message)
		{
			if (!await App.AuthenticateUser())
				return false;

			return await ServiceRef.UiSerializer.DisplayAlert(
				ServiceRef.Localizer[nameof(AppResources.Confirm)], Message,
				ServiceRef.Localizer[nameof(AppResources.Yes)],
				ServiceRef.Localizer[nameof(AppResources.No)]);
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

				if (!await App.AuthenticateUser(true))
					return;

				(bool succeeded, LegalIdentity? CompromisedIdentity) = await ServiceRef.NetworkService.TryRequest(
					() => ServiceRef.XmppService.CompromiseLegalIdentity(ServiceRef.TagProfile.LegalIdentity.Id));

				if (succeeded && CompromisedIdentity is not null)
				{
					ServiceRef.TagProfile.CompromiseLegalIdentity(CompromisedIdentity);
					await App.SetRegistrationPageAsync();
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiSerializer.DisplayException(ex);
			}
		}

		[RelayCommand(CanExecute = nameof(CanExecuteCommands))]
		private async Task Transfer()
		{
			if (ServiceRef.TagProfile.LegalIdentity is null)
				return;

			try
			{
				string? Pin = await App.InputPin();
				if (Pin is null)
					return;

				if (!await ServiceRef.UiSerializer.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.Confirm)],
					ServiceRef.Localizer[nameof(AppResources.AreYouSureYouWantToTransferYourLegalIdentity)],
					ServiceRef.Localizer[nameof(AppResources.Yes)],
					ServiceRef.Localizer[nameof(AppResources.No)]))
				{
					return;
				}

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
						Output.WriteAttributeString("pin", Pin);
						Output.WriteEndElement();

						Output.WriteStartElement("Account", ContractsClient.NamespaceOnboarding);
						Output.WriteAttributeString("domain", ServiceRef.TagProfile.Domain);
						Output.WriteAttributeString("userName", ServiceRef.TagProfile.Account);
						Output.WriteAttributeString("password", ServiceRef.TagProfile.PasswordHash);

						if (!string.IsNullOrEmpty(ServiceRef.TagProfile.PasswordHashMethod))
						{
							Output.WriteAttributeString("passwordMethod", ServiceRef.TagProfile.PasswordHashMethod);
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

					await ServiceRef.UiSerializer.DisplayAlert(
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
				await ServiceRef.UiSerializer.DisplayException(ex);
			}
		}

		[RelayCommand]
		private static async Task ChangeLanguage()
		{
			SelectLanguagePage Page = new();
			await MopupService.Instance.PushAsync(Page);
		}

		#endregion
	}
}
