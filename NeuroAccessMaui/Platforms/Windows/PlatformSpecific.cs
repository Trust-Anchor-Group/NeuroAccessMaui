using NeuroAccessMaui.Services.Push;
using System.Diagnostics;
using Waher.Networking.XMPP.Push;

namespace NeuroAccessMaui.Services
{
	/// <summary>
	/// Windows implementation of platform-specific features.
	/// </summary>
	public class PlatformSpecific : IPlatformSpecific
	{
		private bool isDisposed;

		/// <summary>
		/// Windows implementation of platform-specific features.
		/// </summary>
		public PlatformSpecific()
		{
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		/// <param name="Disposing">If disposing.</param>
		protected virtual void Dispose(bool Disposing)
		{
			if (this.isDisposed)
				return;

			this.isDisposed = true;
		}

		/// <inheritdoc/>
		public bool CanProhibitScreenCapture => false;

		/// <inheritdoc/>
		public bool ProhibitScreenCapture
		{
			get => false;
			set => _ = value; // Not supported on Windows currently.
		}

		/// <inheritdoc/>
		public string? GetDeviceId()
		{
			try
			{
				// Use SecureStorage if available (works on Windows). Persist generated GUID.
				string? DeviceId = SecureStorage.GetAsync("DeviceIdentifier").Result;
				if (!string.IsNullOrEmpty(DeviceId))
					return DeviceId;

				string NewId = Guid.NewGuid().ToString();
				SecureStorage.SetAsync("DeviceIdentifier", NewId).Wait();
				return NewId;
			}
			catch (Exception ex)
			{
				try
				{
					App.SendAlertAsync("Unable to get or store device ID: " + ex.Message, "text/plain").Wait();
					this.CloseApplication().Wait();
				}
				catch (Exception)
				{
					Environment.Exit(0);
				}
			}

			return null;
		}

		/// <inheritdoc/>
		public Task CloseApplication()
		{
			try
			{
#if WINDOWS
				Environment.Exit(0);
#else
				Environment.Exit(0);
#endif
			}
			catch (Exception)
			{
				// Ignore.
			}

			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public void ShareImage(byte[] PngFile, string Message, string Title, string FileName)
		{
			try
			{
				string CachePath = FileSystem.CacheDirectory;
				if (!Directory.Exists(CachePath))
					Directory.CreateDirectory(CachePath);

				string FullPath = Path.Combine(CachePath, FileName);
				File.WriteAllBytes(FullPath, PngFile);

				ShareFileRequest Request = new()
				{
					Title = Title,
					File = new ShareFile(FullPath, "image/png")
				};

				MainThread.BeginInvokeOnMainThread(async () => await Share.Default.RequestAsync(Request));
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		/// <inheritdoc/>
		public bool SupportsFingerprintAuthentication => false; // Windows Hello not integrated yet.

		/// <inheritdoc/>
		public BiometricMethod GetBiometricMethod() => BiometricMethod.None;

		/// <inheritdoc/>
		public Task<bool> AuthenticateUserFingerprint(string Title, string? Subtitle, string Description, string Cancel, CancellationToken? CancellationToken)
		{
			return Task.FromResult(false); // Not implemented.
		}

		/// <inheritdoc/>
		public Task<TokenInformation> GetPushNotificationToken()
		{
			// No push implementation for Windows at this time. Return empty token.
			TokenInformation TokenInformation = new()
			{
				Token = string.Empty,
				ClientType = ClientType.Other,
				Service = PushMessagingService.Firebase // Placeholder; value ignored if token empty.
			};

			return Task.FromResult(TokenInformation);
		}

		#region Keyboard (No-op on Windows desktop)
		public event EventHandler<KeyboardSizeMessage>? KeyboardShown;
		public event EventHandler<KeyboardSizeMessage>? KeyboardHidden;
		public event EventHandler<KeyboardSizeMessage>? KeyboardSizeChanged;

		/// <inheritdoc/>
		public void HideKeyboard()
		{
			// No-op on Windows.
		}
		#endregion

		#region Notifications (No-op placeholders)
		public void ShowMessageNotification(string Title, string MessageBody, IDictionary<string, string> Data) { }
		public void ShowIdentitiesNotification(string Title, string MessageBody, IDictionary<string, string> Data) { }
		public void ShowPetitionNotification(string Title, string MessageBody, IDictionary<string, string> Data) { }
		public void ShowContractsNotification(string Title, string MessageBody, IDictionary<string, string> Data) { }
		public void ShowEDalerNotification(string Title, string MessageBody, IDictionary<string, string> Data) { }
		public void ShowTokenNotification(string Title, string MessageBody, IDictionary<string, string> Data) { }
		public void ShowProvisioningNotification(string Title, string MessageBody, IDictionary<string, string> Data) { }
		#endregion

		/// <inheritdoc/>
		public Thickness GetInsets()
		{
			return new Thickness(0); // No safe area concept needed for desktop window.
		}
	}
}
