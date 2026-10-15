using Android.Nfc;
using Android.Nfc.Tech;
using NeuroAccess.Nfc;
using NeuroAccess.Nfc.Exceptions;

namespace NeuroAccessMaui.AndroidPlatform.Nfc
{
	/// <summary>
	/// Abstract base class for NFC Interfaces.
	/// </summary>
	/// <remarks>
	/// Abstract base class for NFC Interfaces.
	/// </remarks>
	/// <param name="Tag">Underlying Android Tag object.</param>
	/// <param name="Technology">Underlying Android Technology object.</param>
	public abstract class NfcInterface(Tag Tag, BasicTagTechnology Technology) : INfcInterface
	{
		/// <summary>
		/// Underlying Android Tag object.
		/// </summary>
		protected readonly Tag tag = Tag;

		/// <summary>
		/// Underlying Android Technology object.
		/// </summary>
		protected readonly BasicTagTechnology technology = Technology;

		private bool isDisposed;

		/// <summary>
		/// NFC Tag
		/// </summary>
		public INfcTag? Tag
		{
			get;
			internal set;
		}

		/// <summary>
		/// Connects the interface, if not connected.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="NfcConnectionLostException">The tag left the field before it could be connected.</exception>
		public async Task OpenIfClosed()
		{
			try
			{
				if (!this.technology.IsConnected)
					await this.technology.ConnectAsync();
			}
			catch (Java.IO.IOException Ex)
			{
				// Covers TagLostException, which Android throws when the tag is moved away before connecting.
				throw new NfcConnectionLostException("NFC tag connection lost: " + Ex.Message, Ex);
			}
		}

		/// <summary>
		/// Closes the interface, if connected.
		/// </summary>
		public void CloseIfOpen()
		{
			try
			{
				if (this.technology.IsConnected)
					this.technology.Close();
			}
			catch (Java.Lang.SecurityException)
			{
				// Android invalidates the tag handle once another tag is discovered, for example when the document is
				// presented again. Cleanup must not replace the original readout result with this failure.
			}
			catch (Java.IO.IOException)
			{
				// A tag that left the field can also fail while its connection is being closed.
			}
		}

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		protected virtual void Dispose(bool Disposing)
		{
			if (this.isDisposed)
				return;

			if (Disposing)
			{
				this.CloseIfOpen();
				this.technology.Dispose();
				this.tag.Dispose();
			}

			this.isDisposed = true;
		}

		/// <summary>
		/// Returns an <see cref="IOException"/> object that can be thrown when data was unexpectedly not read.
		/// </summary>
		/// <returns>Exception object.</returns>
		internal static IOException UnableToReadDataFromDevice()
		{
			return new IOException("Unable to read data from device.");
		}
	}
}
