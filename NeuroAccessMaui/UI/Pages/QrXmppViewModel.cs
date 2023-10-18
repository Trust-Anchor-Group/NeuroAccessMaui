using CommunityToolkit.Mvvm.ComponentModel;

namespace NeuroAccessMaui.UI.Pages;

/// <summary>
/// A view model that holds the XMPP state.
/// </summary>
public abstract partial class QrXmppViewModel : XmppViewModel, ILinkableView
{
	/// <summary>
	/// Creates an instance of a <see cref="XmppViewModel"/>.
	/// </summary>
	protected QrXmppViewModel()
	{
	}

	/// <summary>
	/// Generates a QR-code
	/// </summary>
	/// <param name="Uri">URI to encode in QR-code.</param>
	public void GenerateQrCode(string Uri)
	{
		byte[] Bin = Services.UI.QR.QrCode.GeneratePng(Uri, this.QrCodeWidth, this.QrCodeHeight);
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
