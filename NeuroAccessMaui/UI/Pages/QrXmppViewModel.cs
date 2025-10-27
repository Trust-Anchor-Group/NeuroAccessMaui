using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Popups.QR;
using Waher.Content.Html.Elements;
using System; // For StringComparison
using System.IO; // For MemoryStream
using Microsoft.Maui.Storage; // For FileSystem paths

namespace NeuroAccessMaui.UI.Pages
{
	/// <summary>
	/// A view model that holds the XMPP state.
	/// </summary>
	public abstract partial class QrXmppViewModel : XmppViewModel, ILinkableView
	{
		private readonly object qrSync = new();

		/// <summary>
		/// Creates an instance of a <see cref="XmppViewModel"/>.
		/// </summary>
		protected QrXmppViewModel()
			: base()
		{
		}

		/// <summary>
		/// Generates a QR-code
		/// </summary>
		/// <param name="Uri">URI to encode in QR-code.</param>
		private string? qrTempFilePath;

		public void GenerateQrCode(string Uri)
		{
			lock (this.qrSync)
			{
				// Idempotency: Avoid regenerating the same QR multiple times (mitigates race conditions during animations)
				if (this.QrCode is not null && string.Equals(this.QrCodeUri, Uri, StringComparison.Ordinal))
				{
					ServiceRef.LogService.LogDebug($"GenerateQrCode skipped (same URI): {Uri}");
					return;
				}

				if (this.QrCodeWidth == 0 || this.QrCodeHeight == 0)
				{
					this.QrCodeWidth = Constants.QrCode.DefaultImageWidth;
					this.QrCodeHeight = Constants.QrCode.DefaultImageHeight;
				}

				if (this.QrResolutionScale == 0)
					this.QrResolutionScale = Constants.QrCode.DefaultResolutionScale;

				ServiceRef.LogService.LogDebug($"GenerateQrCode start: {Uri} ({this.QrCodeWidth}x{this.QrCodeHeight} scale={this.QrResolutionScale})");
				byte[] Bin = Services.UI.QR.QrCode.GeneratePng(Uri, this.QrCodeWidth * this.QrResolutionScale, this.QrCodeHeight * this.QrResolutionScale);
				this.QrCodeBin = Bin;

				try
				{
					// Persist to a temporary file to avoid premature bitmap recycling when view hierarchies animate.
					string CacheDir = FileSystem.CacheDirectory;
					string FileName = $"qr_{Guid.NewGuid():N}.png";
					string FullPath = Path.Combine(CacheDir, FileName);
					File.WriteAllBytes(FullPath, Bin);
					this.qrTempFilePath = FullPath;
					this.QrCode = ImageSource.FromFile(FullPath);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
					// Fallback to in-memory stream method if file write fails
					this.QrCode = ImageSource.FromStream(() => new MemoryStream(Bin));
				}

				this.QrCodeContentType = Constants.MimeTypes.Png;
				this.QrCodeUri = Uri;
				this.HasQrCode = true;
				ServiceRef.LogService.LogDebug($"GenerateQrCode done: {Uri} (bytes={Bin.Length})");
			}
		}

		/// <summary>
		/// Removes the QR-code
		/// </summary>
		public void RemoveQrCode()
		{
			lock (this.qrSync)
			{
				this.QrCode = null;
				this.QrCodeBin = null;
				this.QrCodeContentType = null;
				this.QrCodeUri = null;
				this.HasQrCode = false;
				if (!string.IsNullOrEmpty(this.qrTempFilePath))
				{
					try
					{
						if (File.Exists(this.qrTempFilePath))
							File.Delete(this.qrTempFilePath);
					}
					catch (Exception Ex)
					{
						ServiceRef.LogService.LogException(Ex);
					}
					finally
					{
						this.qrTempFilePath = null;
					}
				}
			}
		}

		/// <summary>
		/// Open QR Popup
		/// </summary>
		/// <returns></returns>
		[RelayCommand]
		public async Task OpenQrPopup()
		{
			if (this.QrCodeBin is null) return;

			ShowQRPopup QrPopup = new(this.QrCodeBin, this.QrCodeUri);
			await ServiceRef.UiService.PushAsync(QrPopup);
		}

		#region Properties

		/// <summary>
		/// Generated QR code image for the identity
		/// </summary>
		[ObservableProperty]
		private ImageSource? qrCode;

		/// <summary>
		/// Determines whether there's a generated <see cref="QrCode"/> image for this identity.
		/// </summary>
		[ObservableProperty]
		private bool hasQrCode;

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
		/// Binary encoding of QR Code
		/// </summary>
		[ObservableProperty]
		private byte[]? qrCodeBin;

		/// <summary>
		/// Content-Type of QR Code
		/// </summary>
		[ObservableProperty]
		private string? qrCodeContentType;



		#endregion

		#region ILinkableView

		/// <summary>
		/// If the current view is linkable.
		/// </summary>
		public virtual bool IsLinkable => this.HasQrCode;

		/// <summary>
		/// If App links should be encoded with the link.
		/// </summary>
		public virtual bool EncodeAppLinks => true;

		/// <summary>
		/// Link to the current view
		/// </summary>
		public virtual string? Link => this.QrCodeUri;

		/// <summary>
		/// Title of the current view
		/// </summary>
		public abstract Task<string> Title { get; }

		/// <summary>
		/// If linkable view has media associated with link.
		/// </summary>
		public virtual bool HasMedia => this.HasQrCode;

		/// <summary>
		/// Encoded media, if available.
		/// </summary>
		public virtual byte[]? Media => this.QrCodeBin;

		/// <summary>
		/// Content-Type of associated media.
		/// </summary>
		public virtual string? MediaContentType => this.QrCodeContentType;

		#endregion
	}
}
