using CoreNFC;
using NeuroAccess.Nfc;

namespace NeuroAccessMaui.Platforms.iOS.Nfc
{
	/// <summary>
	/// Wraps an iOS CoreNFC tag for shared NFC abstractions.
	/// </summary>
	internal sealed class IosNfcTag : INfcTag
	{
		private readonly INFCTag tag;
		private bool isDisposed;

		/// <summary>
		/// Initializes a new instance of the <see cref="IosNfcTag"/> class.
		/// </summary>
		/// <param name="Tag">The CoreNFC tag.</param>
		public IosNfcTag(INFCTag Tag)
		{
			ArgumentNullException.ThrowIfNull(Tag);
			this.tag = Tag;
		}

		/// <inheritdoc/>
		public byte[] ID
		{
			get
			{
				ObjectDisposedException.ThrowIf(this.isDisposed, this);
				return this.tag.Available ? (this.tag.AsNFCIso7816Tag?.Identifier?.ToArray() ?? Array.Empty<byte>()) : Array.Empty<byte>();
			}
		}

		/// <inheritdoc/>
		public INfcInterface[] Interfaces => Array.Empty<INfcInterface>();

		/// <inheritdoc/>
		public void Dispose()
		{
			if (this.isDisposed)
				return;

			this.isDisposed = true;
			GC.SuppressFinalize(this);
		}
	}
}
