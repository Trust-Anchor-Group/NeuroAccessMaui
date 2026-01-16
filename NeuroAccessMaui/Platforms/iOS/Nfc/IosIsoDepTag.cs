using System;
using System.Threading.Tasks;
using CoreNFC;
using Foundation;
using NeuroAccess.Nfc;

namespace NeuroAccessMaui.IosPlatform.Nfc
{
	/// <summary>
	/// iOS NFC tag wrapper that exposes an <see cref="IIsoDepInterface"/> backed by CoreNFC ISO7816.
	/// </summary>
	public sealed class IosIsoDepTag : INfcTag
	{
		private readonly IIsoDepInterface[] interfaces;
		private bool isDisposed;

		/// <summary>
		/// Initializes a new instance of the <see cref="IosIsoDepTag"/> class.
		/// </summary>
		/// <param name="Tag">CoreNFC ISO7816 tag.</param>
		/// <param name="Session">Owning tag reader session.</param>
		public IosIsoDepTag(INFCIso7816Tag Tag, NFCTagReaderSession Session)
		{
			this.ID = Tag?.Identifier?.ToArray() ?? Array.Empty<byte>();
			this.interfaces = [new IosIsoDepInterface(this, Tag, Session)];
		}

		/// <inheritdoc />
		public byte[] ID { get; }

		/// <inheritdoc />
		public INfcInterface[] Interfaces => this.interfaces;

		/// <inheritdoc />
		public void Dispose()
		{
			if (this.isDisposed)
				return;

			this.isDisposed = true;
			foreach (INfcInterface Interface in this.interfaces)
				Interface.Dispose();
		}
	}
}
