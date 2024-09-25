using Android.Nfc;
using Android.Nfc.Tech;
using NeuroAccess.Nfc;

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
		public async Task OpenIfClosed()
		{
			if (!this.technology.IsConnected)
				await this.technology.ConnectAsync();
		}

		/// <summary>
		/// Closes the interface
		/// </summary>
		public void Close()
		{
			if (this.technology.IsConnected)
				this.technology.Close();
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
				this.Close();
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
