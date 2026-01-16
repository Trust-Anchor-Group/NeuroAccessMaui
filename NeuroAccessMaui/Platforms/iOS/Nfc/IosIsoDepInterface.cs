using System;
using System.Reflection;
using System.Threading.Tasks;
using CoreNFC;
using Foundation;
using NeuroAccess.Nfc;

namespace NeuroAccessMaui.IosPlatform.Nfc
{
	/// <summary>
	/// iOS implementation of <see cref="IIsoDepInterface"/> backed by <see cref="NFCISO7816Tag"/>.
	/// </summary>
	public sealed class IosIsoDepInterface : IIsoDepInterface
	{
		private readonly INFCIso7816Tag iso7816Tag;
		private readonly NFCTagReaderSession session;
		private bool isDisposed;

		/// <summary>
		/// Initializes a new instance of the <see cref="IosIsoDepInterface"/> class.
		/// </summary>
		/// <param name="Tag">Owning NFC tag wrapper.</param>
		/// <param name="Iso7816Tag">CoreNFC ISO7816 tag.</param>
		/// <param name="Session">CoreNFC tag reader session.</param>
		public IosIsoDepInterface(INfcTag Tag, INFCIso7816Tag Iso7816Tag, NFCTagReaderSession Session)
		{
			this.Tag = Tag;
			this.iso7816Tag = Iso7816Tag;
			this.session = Session;
		}

		/// <inheritdoc />
		public INfcTag? Tag { get; }

		/// <inheritdoc />
		public Task OpenIfClosed()
		{
			// The session connects before this interface is used.
			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public void CloseIfOpen()
		{
			// CoreNFC manages disconnection via session invalidation.
		}

		/// <inheritdoc />
		public void Dispose()
		{
			this.isDisposed = true;
		}

		/// <inheritdoc />
		public Task<byte[]> GetHighLayerResponse()
		{
			return Task.FromResult(GetNSDataProperty(this.iso7816Tag, "ApplicationData"));
		}

		/// <inheritdoc />
		public Task<byte[]> GetHistoricalBytes()
		{
			return Task.FromResult(GetNSDataProperty(this.iso7816Tag, "HistoricalBytes"));
		}

		/// <inheritdoc />
		public Task<byte[]> ExecuteCommand(byte[] Command)
		{
			if (this.isDisposed)
				throw new ObjectDisposedException(nameof(IosIsoDepInterface));

			TaskCompletionSource<byte[]> TaskSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

			try
			{
				NSData CommandData = NSData.FromArray(Command);
				NFCIso7816Apdu Apdu = new(CommandData);
				this.iso7816Tag.SendCommand(Apdu, (NSData ResponseData, byte Sw1, byte Sw2, NSError Error) =>
				{
					if (Error is not null)
					{
						TaskSource.TrySetException(new NSErrorException(Error));
						return;
					}

					byte[] DataBytes = ResponseData?.ToArray() ?? Array.Empty<byte>();
					byte[] Response = new byte[DataBytes.Length + 2];
					Array.Copy(DataBytes, 0, Response, 0, DataBytes.Length);
					Response[^2] = Sw1;
					Response[^1] = Sw2;
					TaskSource.TrySetResult(Response);
				});
			}
			catch (Exception Ex)
			{
				TaskSource.TrySetException(Ex);
			}

			return TaskSource.Task;
		}

		private static byte[] GetNSDataProperty(object Obj, string PropertyName)
		{
			try
			{
				PropertyInfo? Property = Obj.GetType().GetProperty(PropertyName, BindingFlags.Instance | BindingFlags.Public);
				if (Property?.GetValue(Obj) is NSData Data)
					return Data.ToArray();
			}
			catch
			{
			}

			return Array.Empty<byte>();
		}
	}
}
