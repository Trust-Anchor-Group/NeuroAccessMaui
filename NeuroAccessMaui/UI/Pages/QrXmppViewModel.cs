using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages
{
	/// <summary>
	/// A view model that holds the XMPP state.
	/// </summary>
	public abstract partial class QrXmppViewModel : XmppViewModel, ILinkableView
	{
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
		public void GenerateQrCode(string Uri)
		{
			if (this.QrCodeWidth == 0 || this.QrCodeHeight == 0)
			{
				this.QrCodeWidth = Constants.QrCode.DefaultImageWidth;
				this.QrCodeHeight = Constants.QrCode.DefaultImageHeight;
			}

			if (this.QrResolutionScale == 0)
				this.QrResolutionScale = Constants.QrCode.DefaultResolutionScale;

			byte[] Bin = Services.UI.QR.QrCode.GeneratePng(Uri, this.QrCodeWidth * this.QrResolutionScale, this.QrCodeHeight * this.QrResolutionScale);
			this.QrCode = ImageSource.FromStream(() => new MemoryStream(Bin));
			this.QrCodeBin = Bin;
			this.QrCodeContentType = Constants.MimeTypes.Png;
			this.QrCodeUri = Uri;
			this.HasQrCode = true;
		}

		/// <summary>
		/// Removes the QR-code
		/// </summary>
		public void RemoveQrCode()
		{
			this.QrCode = null;
			this.QrCodeBin = null;
			this.QrCodeContentType = null;
			this.QrCodeUri = null;
			this.HasQrCode = false;
		}


		[RelayCommand]
		public async Task ShareQr()
		{

			if (this.QrCodeBin is null)
				return;

			try
			{
				// Generate a random filename with a suitable extension (e.g., ".tmp", ".dat")
				string fileName = $"{Guid.NewGuid()}.png";

				// Define the path to save the file in the cache directory
				string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

				// Save the byte array as a file
				await File.WriteAllBytesAsync(filePath, this.QrCodeBin);

				// Share the file
				await Share.Default.RequestAsync(new ShareFileRequest
				{
					Title = "QR Code",
					File = new ShareFile(filePath, "image/png")
				});

			}
			catch (Exception ex)
			{
				// Handle exceptions
				ServiceRef.LogService.LogException(ex);
			}
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
