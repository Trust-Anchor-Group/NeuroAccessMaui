using NeuroAccess.Nfc.TravelDocuments;
using Waher.Networking;

namespace NeuroAccess.Nfc.Test.IcaoPart3
{
	/// <summary>
	/// Test-only virtual eMRTD card backed by committed elementary file fixtures.
	/// </summary>
	public sealed class VirtualEmrtdCard : IIsoDepInterface
	{
		private readonly Dictionary<ushort, byte[]> files;
		private readonly Dictionary<byte, ushort> sfiToFileId;
		private readonly bool requireSecureMessagingForRead;
		private bool applicationSelected;
		private ushort? selectedFileId;

		/// <summary>
		/// Initializes a new instance of the <see cref="VirtualEmrtdCard"/> class.
		/// </summary>
		/// <param name="Files">Elementary files keyed by file identifier.</param>
		/// <param name="RequireSecureMessagingForRead">If unprotected reads should be denied.</param>
		public VirtualEmrtdCard(Dictionary<ushort, byte[]> Files, bool RequireSecureMessagingForRead = false)
		{
			this.files = Files;
			this.requireSecureMessagingForRead = RequireSecureMessagingForRead;
			this.sfiToFileId = CreateSfiMap();
		}

		/// <summary>
		/// Gets if an extended READ BINARY command was seen.
		/// </summary>
		public bool SawExtendedReadBinary { get; private set; }

		/// <summary>
		/// Gets the associated NFC tag.
		/// </summary>
		public INfcTag? Tag => null;

		/// <summary>
		/// Opens the virtual card.
		/// </summary>
		/// <returns>A completed task.</returns>
		public Task OpenIfClosed()
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Closes the virtual card.
		/// </summary>
		public void CloseIfOpen()
		{
		}

		/// <summary>
		/// Gets high-layer response bytes.
		/// </summary>
		/// <returns>Empty bytes.</returns>
		public Task<byte[]> GetHighLayerResponse()
		{
			return Task.FromResult<byte[]>([]);
		}

		/// <summary>
		/// Gets historical bytes.
		/// </summary>
		/// <returns>Empty bytes.</returns>
		public Task<byte[]> GetHistoricalBytes()
		{
			return Task.FromResult<byte[]>([]);
		}

		/// <summary>
		/// Sets the communication timeout.
		/// </summary>
		/// <param name="Timeout">Timeout in milliseconds.</param>
		public void SetTimeout(int Timeout)
		{
		}

		/// <summary>
		/// Executes an ISO 14443-4 APDU command.
		/// </summary>
		/// <param name="Command">Command APDU.</param>
		/// <param name="CommunicationLayer">Communication layer.</param>
		/// <returns>Response APDU.</returns>
		public Task<byte[]> ExecuteCommand(byte[] Command, ICommunicationLayer CommunicationLayer)
		{
			return Task.FromResult(this.Transmit(Command));
		}

		/// <summary>
		/// Transmits a command APDU to the virtual card.
		/// </summary>
		/// <param name="Command">Command APDU.</param>
		/// <returns>Response APDU.</returns>
		public byte[] Transmit(byte[] Command)
		{
			if (Command.Length < 4 || Command[0] != 0x00)
				return Status(0x6e, 0x00);

			return Command[1] switch
			{
				0xa4 => this.Select(Command),
				0xb0 => this.ReadBinary(Command),
				0xb1 => this.ReadBinaryExtended(Command),
				_ => Status(0x6d, 0x00)
			};
		}

		/// <summary>
		/// Disposes the virtual card.
		/// </summary>
		public void Dispose()
		{
		}

		private byte[] Select(byte[] Command)
		{
			if (Command.Length < 5)
				return Status(0x67, 0x00);

			if (Command[2] == 0x04)
			{
				int Length = Command[4];
				if (Command.Length != 5 + Length)
					return Status(0x67, 0x00);

				byte[] Application = new byte[Length];
				Buffer.BlockCopy(Command, 5, Application, 0, Length);
				if (!Application.SequenceEqual(Applications.DF1))
					return Status(0x6a, 0x82);

				this.applicationSelected = true;
				this.selectedFileId = null;
				return Status(0x90, 0x00);
			}

			if (Command[2] == 0x02)
			{
				if (!this.applicationSelected)
					return Status(0x69, 0x86);

				if (Command.Length != 7 || Command[4] != 0x02)
					return Status(0x67, 0x00);

				ushort FileId = (ushort)((Command[5] << 8) | Command[6]);
				if (!this.files.ContainsKey(FileId))
					return Status(0x6a, 0x82);

				if (this.requireSecureMessagingForRead &&
					FileId is not (EF.COM or EF.SOD))
				{
					return Status(0x69, 0x82);
				}

				this.selectedFileId = FileId;
				return Status(0x90, 0x00);
			}

			return Status(0x6a, 0x86);
		}

		private byte[] ReadBinary(byte[] Command)
		{
			if (Command.Length != 5)
				return Status(0x67, 0x00);

			if (!this.applicationSelected)
				return Status(0x69, 0x86);

			if (this.requireSecureMessagingForRead)
				return Status(0x69, 0x82);

			int Length = Command[4] == 0 ? 256 : Command[4];
			if ((Command[2] & 0x80) != 0)
			{
				byte Sfi = (byte)(Command[2] & 0x1f);
				if (!this.sfiToFileId.TryGetValue(Sfi, out ushort FileId))
					return Status(0x6a, 0x82);

				this.selectedFileId = FileId;
				return this.ReadFile(FileId, Command[3], Length);
			}

			uint Offset = (uint)((Command[2] << 8) | Command[3]);
			return this.ReadSelected(Offset, Length);
		}

		private byte[] ReadBinaryExtended(byte[] Command)
		{
			this.SawExtendedReadBinary = true;

			if (!this.applicationSelected)
				return Status(0x69, 0x86);

			if (this.requireSecureMessagingForRead)
				return Status(0x69, 0x82);

			if (Command.Length < 9 || Command[4] + 6 != Command.Length || Command[5] != 0x54)
				return Status(0x67, 0x00);

			int OffsetLength = Command[6];
			if (OffsetLength is < 1 or > 4 || Command.Length != 8 + OffsetLength)
				return Status(0x67, 0x00);

			uint Offset = 0;
			for (int i = 0; i < OffsetLength; i++)
				Offset = (Offset << 8) | Command[7 + i];

			int Length = Command[^1] == 0 ? 256 : Command[^1];
			return this.ReadSelected(Offset, Length);
		}

		private byte[] ReadSelected(uint Offset, int Length)
		{
			if (!this.selectedFileId.HasValue)
				return Status(0x69, 0x86);

			return this.ReadFile(this.selectedFileId.Value, Offset, Length);
		}

		private byte[] ReadFile(ushort FileId, uint Offset, int Length)
		{
			if (!this.files.TryGetValue(FileId, out byte[]? FileData))
				return Status(0x6a, 0x82);

			if (Offset > FileData.Length)
				return Status(0x6b, 0x00);

			long Remaining = FileData.Length - (long)Offset;
			int Count = (int)Math.Min(Length, Remaining);
			byte[] Response = new byte[Count + 2];
			Buffer.BlockCopy(FileData, (int)Offset, Response, 0, Count);
			Response[^2] = 0x90;
			Response[^1] = 0x00;
			return Response;
		}

		private static Dictionary<byte, ushort> CreateSfiMap()
		{
			return new Dictionary<byte, ushort>()
			{
				[0x1e] = EF.COM,
				[0x1d] = EF.SOD,
				[0x01] = EF.DG1,
				[0x02] = EF.DG2,
				[0x03] = EF.DG3,
				[0x04] = EF.DG4,
				[0x05] = EF.DG5,
				[0x06] = 0x0106,
				[0x07] = EF.DG7,
				[0x08] = 0x0108,
				[0x09] = 0x0109,
				[0x0a] = 0x010a,
				[0x0b] = EF.DG11,
				[0x0c] = EF.DG12,
				[0x0d] = EF.DG13,
				[0x0e] = EF.DG14,
				[0x0f] = EF.DG15,
				[0x10] = EF.DG16
			};
		}

		private static byte[] Status(byte Sw1, byte Sw2)
		{
			return [Sw1, Sw2];
		}
	}
}
