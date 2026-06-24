using CoreNFC;
using Foundation;
using NeuroAccess.Nfc;
using NeuroAccessMaui.Services.Nfc;
using Waher.Networking;

namespace NeuroAccessMaui.Platforms.iOS.Nfc
{
	/// <summary>
	/// Wraps an iOS CoreNFC ISO 7816 tag as a shared ISO-DEP interface.
	/// </summary>
	internal sealed class IosIsoDepInterface : IIsoDepInterface
	{
		private const int defaultTimeoutMilliseconds = 60000;

		private readonly INFCIso7816Tag iso7816Tag;
		private readonly IosNfcTag tag;
		private readonly CancellationToken cancellationToken;
		private int timeoutMilliseconds = defaultTimeoutMilliseconds;
		private bool isDisposed;

		/// <summary>
		/// Initializes a new instance of the <see cref="IosIsoDepInterface"/> class.
		/// </summary>
		/// <param name="Tag">The CoreNFC tag wrapper.</param>
		/// <param name="Iso7816Tag">The ISO 7816 tag interface.</param>
		/// <param name="CancellationToken">The session cancellation token.</param>
		public IosIsoDepInterface(INFCTag Tag, INFCIso7816Tag Iso7816Tag, CancellationToken CancellationToken)
		{
			ArgumentNullException.ThrowIfNull(Tag);
			ArgumentNullException.ThrowIfNull(Iso7816Tag);

			this.iso7816Tag = Iso7816Tag;
			this.tag = new IosNfcTag(Tag);
			this.cancellationToken = CancellationToken;
		}

		/// <inheritdoc/>
		public INfcTag? Tag => this.tag;

		/// <inheritdoc/>
		public Task OpenIfClosed()
		{
			ObjectDisposedException.ThrowIf(this.isDisposed, this);
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public void CloseIfOpen()
		{
		}

		/// <inheritdoc/>
		public Task<byte[]> GetHighLayerResponse()
		{
			ObjectDisposedException.ThrowIf(this.isDisposed, this);
			return Task.FromResult(this.iso7816Tag.ApplicationData?.ToArray() ?? Array.Empty<byte>());
		}

		/// <inheritdoc/>
		public Task<byte[]> GetHistoricalBytes()
		{
			ObjectDisposedException.ThrowIf(this.isDisposed, this);
			return Task.FromResult(this.iso7816Tag.HistoricalBytes?.ToArray() ?? Array.Empty<byte>());
		}

		/// <inheritdoc/>
		public void SetTimeout(int Timeout)
		{
			this.timeoutMilliseconds = Timeout > 0 ? Timeout : defaultTimeoutMilliseconds;
		}

		/// <inheritdoc/>
		public Task<byte[]> ExecuteCommand(byte[] Command)
		{
			ArgumentNullException.ThrowIfNull(Command);
			ObjectDisposedException.ThrowIf(this.isDisposed, this);

			TaskCompletionSource<byte[]> ResultSource = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
			this.ExecuteCommandCore(Command, null, ResultSource);
			return ResultSource.Task;
		}

		/// <inheritdoc/>
		public Task<byte[]> ExecuteCommand(byte[] Command, ICommunicationLayer CommunicationLayer)
		{
			ArgumentNullException.ThrowIfNull(Command);
			ArgumentNullException.ThrowIfNull(CommunicationLayer);
			ObjectDisposedException.ThrowIf(this.isDisposed, this);

			TaskCompletionSource<byte[]> ResultSource = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
			this.ExecuteCommandCore(Command, CommunicationLayer, ResultSource);
			return ResultSource.Task;
		}

		private void ExecuteCommandCore(byte[] Command, ICommunicationLayer? CommunicationLayer, TaskCompletionSource<byte[]> ResultSource)
		{
			CommunicationLayer?.TransmitBinary(false, Command);
			NFCIso7816Apdu CommandApdu = CreateCommandApdu(Command);

			CancellationTokenSource TimeoutSource = CancellationTokenSource.CreateLinkedTokenSource(this.cancellationToken);
			TimeoutSource.CancelAfter(this.timeoutMilliseconds);
			CancellationTokenRegistration TimeoutRegistration = default;
			int CleanupCompleted = 0;

			void Cleanup()
			{
				if (Interlocked.Exchange(ref CleanupCompleted, 1) != 0)
					return;

				TimeoutRegistration.Dispose();
				TimeoutSource.Dispose();
			}

			TimeoutRegistration = TimeoutSource.Token.Register(() =>
			{
				if (this.cancellationToken.IsCancellationRequested)
				{
					CommunicationLayer?.Error("iOS NFC command canceled.");
					ResultSource.TrySetCanceled(this.cancellationToken);
				}
				else
				{
					CommunicationLayer?.Error("Timed out waiting for iOS NFC response.");
					ResultSource.TrySetException(new TimeoutException("Timed out waiting for the iOS NFC command response."));
				}

				Cleanup();
			});
			try
			{
				this.iso7816Tag.SendCommand(
					CommandApdu,
					(NSData ResponseData, byte StatusWord1, byte StatusWord2, NSError? Error) =>
					{
						Cleanup();

						if (Error is not null)
						{
							NFCReaderError ReaderError = (NFCReaderError)(long)Error.Code;
							if (ReaderError is NFCReaderError.ReaderTransceiveErrorTagConnectionLost or
								NFCReaderError.ReaderTransceiveErrorTagNotConnected or
								NFCReaderError.ReaderTransceiveErrorRetryExceeded)
							{
								ResultSource.TrySetException(new NfcConnectionLostException(Error.LocalizedDescription));
								return;
							}

							ResultSource.TrySetException(new InvalidOperationException(Error.LocalizedDescription));
							return;
						}

						byte[] ResponsePayload = ResponseData?.ToArray() ?? Array.Empty<byte>();
						byte[] Response = new byte[ResponsePayload.Length + 2];
						Buffer.BlockCopy(ResponsePayload, 0, Response, 0, ResponsePayload.Length);
						Response[^2] = StatusWord1;
						Response[^1] = StatusWord2;
						CommunicationLayer?.ReceiveBinary(false, Response);
						ResultSource.TrySetResult(Response);
					});
			}
			catch
			{
				Cleanup();
				throw;
			}
		}

		private static NFCIso7816Apdu CreateCommandApdu(byte[] Command)
		{
			if (Command.Length < 4)
				throw new ArgumentException("ISO 7816 APDU commands must contain at least CLA, INS, P1, and P2.", nameof(Command));

			byte InstructionClass = Command[0];
			byte InstructionCode = Command[1];
			byte P1Parameter = Command[2];
			byte P2Parameter = Command[3];
			byte[] CommandData = Array.Empty<byte>();
			int ExpectedResponseLength = -1;

			if (Command.Length == 5)
			{
				ExpectedResponseLength = DecodeShortExpectedResponseLength(Command[4]);
			}
			else if (Command.Length > 5 && Command[4] != 0)
			{
				int DataLength = Command[4];
				int DataOffset = 5;
				int RemainingLength = Command.Length - DataOffset - DataLength;
				if (RemainingLength is not 0 and not 1)
				{
					DataLength = Command.Length - DataOffset;
					RemainingLength = 0;
				}

				CommandData = CopyCommandData(Command, DataOffset, DataLength);
				if (RemainingLength == 1)
					ExpectedResponseLength = DecodeShortExpectedResponseLength(Command[DataOffset + DataLength]);
			}
			else if (Command.Length > 5)
			{
				if (Command.Length < 7)
					throw new ArgumentException("Extended ISO 7816 APDU commands must include a two-byte Lc or Le field.", nameof(Command));

				if (Command.Length == 7)
				{
					ExpectedResponseLength = DecodeExtendedExpectedResponseLength(Command[5], Command[6]);
				}
				else
				{
					int DataLength = (Command[5] << 8) | Command[6];
					int DataOffset = 7;
					int RemainingLength = Command.Length - DataOffset - DataLength;
					if (RemainingLength is not 0 and not 2)
						throw new ArgumentException("Extended ISO 7816 APDU command length does not match Lc/Le fields.", nameof(Command));

					CommandData = CopyCommandData(Command, DataOffset, DataLength);
					if (RemainingLength == 2)
						ExpectedResponseLength = DecodeExtendedExpectedResponseLength(Command[DataOffset + DataLength], Command[DataOffset + DataLength + 1]);
				}
			}

			return new NFCIso7816Apdu(
				InstructionClass,
				InstructionCode,
				P1Parameter,
				P2Parameter,
				NSData.FromArray(CommandData),
				(nint)ExpectedResponseLength);
		}

		private static byte[] CopyCommandData(byte[] Command, int Offset, int Length)
		{
			if (Length == 0)
				return Array.Empty<byte>();

			if (Offset < 0 || Length < 0 || Offset + Length > Command.Length)
				throw new ArgumentException("ISO 7816 APDU command data length is outside the command buffer.", nameof(Command));

			byte[] CommandData = new byte[Length];
			Buffer.BlockCopy(Command, Offset, CommandData, 0, Length);
			return CommandData;
		}

		private static int DecodeShortExpectedResponseLength(byte Length)
		{
			return Length == 0 ? 256 : Length;
		}

		private static int DecodeExtendedExpectedResponseLength(byte HighByte, byte LowByte)
		{
			int Length = (HighByte << 8) | LowByte;
			return Length == 0 ? 65536 : Length;
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			if (this.isDisposed)
				return;

			this.isDisposed = true;
			this.tag.Dispose();
			GC.SuppressFinalize(this);
		}
	}
}
