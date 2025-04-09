using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages;

namespace NeuroAccessMaui.UI.Popups.QR
{
	public partial class ShowQRViewModel : BasePopupViewModel
	{
		#region Private Properties

		private byte[] qrCodeBin = [];

		/// <summary>
		/// Legal ID
		/// </summary>
		[ObservableProperty]
		private string? legalId;

		/// <summary>
		/// Generated QR code image for the identity
		/// </summary>
		[ObservableProperty]
		private ImageSource? qrCode;

		/// <summary>
		/// Determines whether there's a generated <see cref="QrCode"/> image for this identity.
		/// </summary>
		[ObservableProperty]
		private string? qrCodeUri;

		/// <summary>
		/// Gets or sets the width, in pixels, of the QR Code image to generate.
		/// </summary>
		[ObservableProperty]
		private int qrCodeWidth;

		/// <summary>
		/// Gets or sets the height, in pixels, of the QR Code image to generate.
		/// </summary>
		[ObservableProperty]
		private int qrCodeHeight;

		/// <summary>
		/// The scale factor to apply to the QR Code image resolution.
		/// A value of 1 generates images at the default width and height.
		/// For example, a value of 2 generates QR Code images at twice the default width and height (480x480).
		/// </summary>
		[ObservableProperty]
		private int qrResolutionScale;

		/// <summary>
		/// Content-Type of QR Code
		/// </summary>
		[ObservableProperty]
		private string? qrCodeContentType;

		#endregion
		#region Public Properties

		public byte[] QrCodeBin
		{
			get => this.qrCodeBin;
			set
			{
				if (value == this.qrCodeBin) return;
				this.qrCodeBin = value;
				this.OnPropertyChanged();
			}
		}

		/// <summary>
		/// Gets the size of the background for Camera Icon 
		/// </summary>
		public static double CameraIconBackgroundSize => 120.0;

		/// <summary>
		/// Gets the Readius of the background for Camera Icon
		/// </summary>
		public static double CameraIconBackgroundCornerRadius => CameraIconBackgroundSize / 2;

		/// <summary>
		/// Gets the size of the Camera Icon
		/// </summary>
		public static double CameraIconSize => 60.0;

		#endregion

		public ShowQRViewModel(byte[] QrCodeBin)
		{
			this.QrCodeBin = QrCodeBin;
			this.LegalId = ServiceRef.TagProfile.LegalIdentity?.Id;

			if (this.QrCodeWidth == 0 || this.QrCodeHeight == 0)
			{
				this.QrCodeWidth = Constants.QrCode.DefaultImageWidth;
				this.QrCodeHeight = Constants.QrCode.DefaultImageHeight;
			}

			if (this.QrResolutionScale == 0)
				this.QrResolutionScale = Constants.QrCode.DefaultResolutionScale;

			this.QrCode = ImageSource.FromStream(() => new MemoryStream(QrCodeBin));
		}

		#region Commands

		[RelayCommand]
		private static async Task Close()
		{
			await ServiceRef.UiService.PopAsync();
		}

		[RelayCommand]
		private async Task ShareQR()
		{
			if (this.QrCodeBin is null)
				return;

			try
			{
				// Generate a random filename with a suitable extension (e.g., ".tmp", ".dat")
				string FileName = $"{Guid.NewGuid()}.png";

				// Define the path to save the file in the cache directory
				string FilePath = Path.Combine(FileSystem.CacheDirectory, FileName);

				// Save the byte array as a file
				await File.WriteAllBytesAsync(FilePath, this.QrCodeBin);

				// Share the file
				await Share.Default.RequestAsync(new ShareFileRequest
				{
					Title = "QR Code",
					File = new ShareFile(FilePath, "image/png")
				});

			}
			catch (Exception ex)
			{
				// Handle exceptions
				ServiceRef.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Copies Item to clipboard
		/// </summary>
		[RelayCommand]
		private async Task Copy(object Item)
		{
			try
			{
				this.SetIsBusy(true);

				if (Item is string Label)
				{
					if (Label == this.LegalId)
					{
						await Clipboard.SetTextAsync(Constants.UriSchemes.IotId + ":" + this.LegalId);
						await ServiceRef.UiService.DisplayAlert(
							ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
							ServiceRef.Localizer[nameof(AppResources.IdCopiedSuccessfully)]);
					}
					else
					{
						await Clipboard.SetTextAsync(Label);
						await ServiceRef.UiService.DisplayAlert(
							ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
							ServiceRef.Localizer[nameof(AppResources.TagValueCopiedToClipboard)]);
					}
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
			finally
			{
				this.SetIsBusy(false);
			}
		}

		#endregion
	}
}
