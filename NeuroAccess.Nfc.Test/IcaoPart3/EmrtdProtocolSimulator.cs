using System.Globalization;
using NeuroAccess.Nfc.TravelDocuments.PACE;
using NeuroAccess.Nfc.TravelDocuments.PACE.Id_PACE_ECDH_GM;
using NeuroAccess.Nfc.TravelDocuments.Security;
using NeuroAccess.Nfc.TravelDocuments;
using Waher.Security;
using Waher.Security.EllipticCurves;

namespace NeuroAccess.Nfc.Test.IcaoPart3
{
	/// <summary>
	/// Represents the high-level state of the test-only eMRTD protocol simulator.
	/// </summary>
	public enum EmrtdProtocolSimulatorState
	{
		/// <summary>
		/// No file or application has been selected.
		/// </summary>
		Idle,

		/// <summary>
		/// The master file has been selected.
		/// </summary>
		MasterFileSelected,

		/// <summary>
		/// The LDS application has been selected.
		/// </summary>
		LdsApplicationSelected,

		/// <summary>
		/// An elementary file has been selected.
		/// </summary>
		ElementaryFileSelected,

		/// <summary>
		/// A PACE security environment has been selected.
		/// </summary>
		PaceSecurityEnvironmentSelected,

		/// <summary>
		/// Secure messaging has been established.
		/// </summary>
		SecureMessagingEstablished
	}

	/// <summary>
	/// Configures the test-only eMRTD APDU simulator.
	/// </summary>
	public sealed class EmrtdProtocolSimulatorOptions
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="EmrtdProtocolSimulatorOptions"/> class.
		/// </summary>
		/// <param name="Files">Elementary files keyed by file identifier.</param>
		public EmrtdProtocolSimulatorOptions(Dictionary<ushort, byte[]> Files)
		{
			this.Files = Files;
		}

		/// <summary>
		/// Gets elementary files keyed by file identifier.
		/// </summary>
		public Dictionary<ushort, byte[]> Files { get; }

		/// <summary>
		/// Gets or sets if LDS data group reads require secure messaging.
		/// </summary>
		public bool RequireSecureMessagingForDataGroups { get; set; }

		/// <summary>
		/// Gets or sets if the simulator exposes BAC APDU entry points.
		/// </summary>
		public bool EnableBacEntryPoints { get; set; }

		/// <summary>
		/// Gets or sets if the simulator exposes PACE APDU entry points.
		/// </summary>
		public bool EnablePaceEntryPoints { get; set; }

		/// <summary>
		/// Gets or sets the deterministic BAC challenge.
		/// </summary>
		public byte[] BacChallenge { get; set; } = [0x46, 0x08, 0xf9, 0x19, 0x88, 0x70, 0x22, 0x12];

		/// <summary>
		/// Gets or sets the document information used for card-side PACE key derivation.
		/// </summary>
		public DocumentInformation? DocumentInformation { get; set; }

		/// <summary>
		/// Gets or sets deterministic PACE nonce bytes.
		/// </summary>
		public byte[] PaceNonce { get; set; } =
		[
			0x3f, 0x00, 0xc4, 0xd3, 0x9d, 0x15, 0x3f, 0x2b,
			0x2a, 0x21, 0x4a, 0x07, 0x8d, 0x89, 0x9b, 0x22
		];

		/// <summary>
		/// Gets or sets deterministic key seed bytes used by the card side of PACE.
		/// </summary>
		public byte[] PaceKeySeed { get; set; } =
		[
			0x4a, 0x41, 0x4d, 0x45, 0x53, 0x2d, 0x42, 0x4f,
			0x4e, 0x44, 0x2d, 0x50, 0x41, 0x43, 0x45, 0x31
		];
	}

	/// <summary>
	/// Test-only, standards-shaped eMRTD APDU simulator.
	/// </summary>
	/// <remarks>
	/// The simulator intentionally models command routing, file selection, read offsets, and security state.
	/// Cryptographic BAC/PACE secure messaging is not faked; unsupported protected protocol branches return
	/// deterministic ISO 7816 status words and remain inconclusive in the ICAO coverage report.
	/// </remarks>
	public sealed class EmrtdProtocolSimulator
	{
		private static readonly byte[] SupportedPaceOid = [0x04, 0x00, 0x7f, 0x00, 0x07, 0x02, 0x02, 0x04, 0x02, 0x02];

		private readonly EmrtdProtocolSimulatorOptions options;
		private readonly Dictionary<byte, ushort> sfiToFileId;
		private ushort? selectedFileId;
		private bool secureMessagingEstablished;
		private PaceEcdhProtocol? paceProtocol;
		private CMac? paceMac;
		private byte[]? paceKsEnc;
		private byte[]? paceKsMac;
		private byte[]? paceSendSequenceCounter;
		private byte[]? paceTerminalMappingPublicKey;
		private byte[]? paceChipMappingPublicKey;
		private byte[]? paceTerminalEphemeralPublicKey;
		private byte[]? paceChipEphemeralPublicKey;
		private byte[]? paceChipEphemeralPrivateKey;
		private PointOnCurve? paceMappedGenerator;
		private bool processingUnwrappedSecureMessagingCommand;
		private bool paceNonceReturned;
		private bool paceMappingPerformed;
		private bool paceKeyAgreementPerformed;

		/// <summary>
		/// Initializes a new instance of the <see cref="EmrtdProtocolSimulator"/> class.
		/// </summary>
		/// <param name="Options">Simulator options.</param>
		public EmrtdProtocolSimulator(EmrtdProtocolSimulatorOptions Options)
		{
			this.options = Options;
			this.sfiToFileId = CreateSfiMap();
		}

		/// <summary>
		/// Gets the current simulator state.
		/// </summary>
		public EmrtdProtocolSimulatorState State { get; private set; }

		/// <summary>
		/// Gets the currently selected elementary file identifier.
		/// </summary>
		public ushort? SelectedFileId => this.selectedFileId;

		/// <summary>
		/// Gets if an extended READ BINARY command was seen.
		/// </summary>
		public bool SawExtendedReadBinary { get; private set; }

		/// <summary>
		/// Gets if the simulator saw a PACE MSE:Set AT command.
		/// </summary>
		public bool SawPaceSetAt { get; private set; }

		/// <summary>
		/// Gets if the simulator saw a GENERAL AUTHENTICATE command.
		/// </summary>
		public bool SawGeneralAuthenticate { get; private set; }

		/// <summary>
		/// Gets if the simulator saw BAC GET CHALLENGE.
		/// </summary>
		public bool SawBacGetChallenge { get; private set; }

		/// <summary>
		/// Gets if the PACE encrypted nonce step has completed.
		/// </summary>
		public bool PaceNonceReturned => this.paceNonceReturned;

		/// <summary>
		/// Gets if the PACE mapping step has completed.
		/// </summary>
		public bool PaceMappingPerformed => this.paceMappingPerformed;

		/// <summary>
		/// Gets if the PACE key-agreement step has completed.
		/// </summary>
		public bool PaceKeyAgreementPerformed => this.paceKeyAgreementPerformed;

		/// <summary>
		/// Transmits one command APDU to the simulator.
		/// </summary>
		/// <param name="Command">Command APDU.</param>
		/// <returns>Response APDU.</returns>
		public byte[] Transmit(byte[] Command)
		{
			if (Command.Length < 4)
				return Status(0x67, 0x00);

			if (IsSecureMessagingClass(Command[0]))
				return this.HandleSecureMessagingCommand(Command);

			if (Command[0] == ISO_7816.Classes.Chaining && Command[1] == ISO_7816.Instructions.GeneralAuthenticate)
				return this.GeneralAuthenticate(Command);

			if (Command[0] != ISO_7816.Classes.Basic)
				return Status(0x6e, 0x00);

			return Command[1] switch
			{
				ISO_7816.Instructions.Select => this.Select(Command),
				ISO_7816.Instructions.ReadBinary => this.ReadBinary(Command),
				ISO_7816.Instructions.ReadBinary + 1 => this.ReadBinaryOddInstruction(Command),
				ISO_7816.Instructions.GetChallenge => this.GetChallenge(Command),
				ISO_7816.Instructions.ExternalAuthenticate => this.ExternalAuthenticate(Command),
				ISO_7816.Instructions.MessageSecurityEnvironment => this.MessageSecurityEnvironment(Command),
				ISO_7816.Instructions.GeneralAuthenticate => this.GeneralAuthenticate(Command),
				_ => Status(0x6d, 0x00)
			};
		}

		private byte[] Select(byte[] Command)
		{
			if (Command.Length < 5)
				return Status(0x67, 0x00);

			if (Command[2] == 0x00)
				return this.SelectMaster(Command);

			if (Command[2] == 0x04)
				return this.SelectApplication(Command);

			if (Command[2] == 0x02)
				return this.SelectElementaryFile(Command);

			return Status(0x6a, 0x86);
		}

		private byte[] SelectMaster(byte[] Command)
		{
			if (Command.Length != 7 || Command[4] != 0x02 || Command[5] != 0x3f || Command[6] != 0x00)
				return Status(0x6a, 0x86);

			this.State = EmrtdProtocolSimulatorState.MasterFileSelected;
			this.selectedFileId = null;
			return Status(0x90, 0x00);
		}

		private byte[] SelectApplication(byte[] Command)
		{
			int Length = Command[4];
			if (Command.Length != 5 + Length)
				return Status(0x67, 0x00);

			byte[] Application = new byte[Length];
			Buffer.BlockCopy(Command, 5, Application, 0, Length);
			if (!Application.SequenceEqual(Applications.DF1))
				return Status(0x6a, 0x82);

			this.State = EmrtdProtocolSimulatorState.LdsApplicationSelected;
			this.selectedFileId = null;
			return Status(0x90, 0x00);
		}

		private byte[] SelectElementaryFile(byte[] Command)
		{
			if (Command.Length != 7 || Command[4] != 0x02)
				return Status(0x67, 0x00);

			ushort FileId = (ushort)((Command[5] << 8) | Command[6]);
			if (!this.options.Files.ContainsKey(FileId))
				return Status(0x6a, 0x82);

			if (!this.CanSelectFile(FileId))
				return Status(0x69, 0x86);

			if (this.RequiresSecureMessagingForSelection(FileId))
				return Status(0x69, 0x82);

			this.selectedFileId = FileId;
			this.State = EmrtdProtocolSimulatorState.ElementaryFileSelected;
			return Status(0x90, 0x00);
		}

		private byte[] ReadBinary(byte[] Command)
		{
			if (Command.Length != 5)
				return Status(0x67, 0x00);

			int Length = Command[4] == 0 ? 256 : Command[4];
			if ((Command[2] & 0x80) != 0)
			{
				byte Sfi = (byte)(Command[2] & 0x1f);
				if (!this.sfiToFileId.TryGetValue(Sfi, out ushort FileId))
					return Status(0x6a, 0x82);

				if (this.RequiresSecureMessagingForRead(FileId))
					return Status(0x69, 0x82);

				this.selectedFileId = FileId;
				this.State = EmrtdProtocolSimulatorState.ElementaryFileSelected;
				return this.ReadFile(FileId, Command[3], Length);
			}

			if (!this.selectedFileId.HasValue)
				return Status(0x69, 0x86);

			if (this.RequiresSecureMessagingForRead(this.selectedFileId.Value))
				return Status(0x69, 0x82);

			uint Offset = (uint)((Command[2] << 8) | Command[3]);
			return this.ReadFile(this.selectedFileId.Value, Offset, Length);
		}

		private byte[] ReadBinaryOddInstruction(byte[] Command)
		{
			this.SawExtendedReadBinary = true;

			if (Command.Length < 9 || Command[4] + 6 != Command.Length || Command[5] != 0x54)
				return Status(0x67, 0x00);

			int OffsetLength = Command[6];
			if (OffsetLength is < 1 or > 4 || Command.Length != 8 + OffsetLength)
				return Status(0x67, 0x00);

			ushort FileId;
			if (this.selectedFileId.HasValue)
				FileId = this.selectedFileId.Value;
			else if (Command[2] == 0x00 && this.sfiToFileId.TryGetValue(Command[3], out ushort SfiFileId))
				FileId = SfiFileId;
			else
				return Status(0x69, 0x86);

			if (this.RequiresSecureMessagingForRead(FileId))
				return Status(0x69, 0x82);

			uint Offset = 0;
			for (int i = 0; i < OffsetLength; i++)
				Offset = (Offset << 8) | Command[7 + i];

			int Length = Command[^1] == 0 ? 256 : Command[^1];
			this.selectedFileId = FileId;
			this.State = EmrtdProtocolSimulatorState.ElementaryFileSelected;
			return this.ReadFile(FileId, Offset, Length);
		}

		private byte[] GetChallenge(byte[] Command)
		{
			if (!this.options.EnableBacEntryPoints)
				return Status(0x6d, 0x00);

			if (Command.Length != 5 || Command[4] != 0x08)
				return Status(0x67, 0x00);

			this.SawBacGetChallenge = true;
			return WithStatus(this.options.BacChallenge, 0x90, 0x00);
		}

		private byte[] ExternalAuthenticate(byte[] Command)
		{
			if (!this.options.EnableBacEntryPoints)
				return Status(0x6d, 0x00);

			// TODO: Implement BAC mutual-authentication and 3DES/Retail-MAC secure messaging.
			if (Command.Length < 6)
				return Status(0x67, 0x00);

			return Status(0x69, 0x82);
		}

		private byte[] MessageSecurityEnvironment(byte[] Command)
		{
			if (Command[2] == 0xc1 && Command[3] == 0xa4)
			{
				this.SawPaceSetAt = true;
				if (!this.options.EnablePaceEntryPoints)
					return Status(0x6a, 0x81);

				if (!TryValidatePaceSetAt(Command))
					return Status(0x6a, 0x80);

				this.State = EmrtdProtocolSimulatorState.PaceSecurityEnvironmentSelected;
				this.InitializePaceProtocol();
				return Status(0x90, 0x00);
			}

			return Status(0x6a, 0x86);
		}

		private static bool TryValidatePaceSetAt(byte[] Command)
		{
			if (Command.Length < 5)
				return false;

			int Lc = Command[4];
			if (Lc == 0 || Command.Length != 5 + Lc)
				return false;

			int Cursor = 5;
			int End = Command.Length;
			byte[]? AlgorithmReference = null;
			byte[]? KeyReference = null;
			byte[]? ParameterReference = null;

			while (Cursor < End)
			{
				byte Tag = Command[Cursor++];
				if (!TryReadDerLength(Command, ref Cursor, End, out int Length))
					return false;

				if (Cursor + Length > End)
					return false;

				byte[] Value = new byte[Length];
				Buffer.BlockCopy(Command, Cursor, Value, 0, Length);
				Cursor += Length;

				switch (Tag)
				{
					case 0x80:
						if (AlgorithmReference is not null)
							return false;

						AlgorithmReference = Value;
						break;

					case 0x83:
						if (KeyReference is not null)
							return false;

						KeyReference = Value;
						break;

					case 0x84:
						if (ParameterReference is not null)
							return false;

						ParameterReference = Value;
						break;

					default:
						return false;
				}
			}

			if (AlgorithmReference is null ||
				!AlgorithmReference.SequenceEqual(SupportedPaceOid))
			{
				return false;
			}

			if (KeyReference is null ||
				KeyReference.Length != 1 ||
				KeyReference[0] != 0x01)
			{
				return false;
			}

			if (ParameterReference is not null &&
				(ParameterReference.Length != 1 || ParameterReference[0] != 0x0d))
			{
				return false;
			}

			return true;
		}

		private byte[] GeneralAuthenticate(byte[] Command)
		{
			this.SawGeneralAuthenticate = true;
			if (!this.options.EnablePaceEntryPoints)
				return Status(0x6a, 0x81);

			if (this.State != EmrtdProtocolSimulatorState.PaceSecurityEnvironmentSelected)
				return Status(0x69, 0x86);

			if (!TryParseDynamicAuthenticationData(Command, out byte RequestTag, out byte[] RequestData))
				return Status(0x67, 0x00);

			if (RequestTag == 0x85)
			{
				if (Command[0] != ISO_7816.Classes.Basic)
					return Status(0x68, 0x83);
			}
			else if (Command[0] != ISO_7816.Classes.Chaining)
				return Status(0x68, 0x83);

			return RequestTag switch
			{
				0x00 => this.GetPaceEncryptedNonce(),
				0x81 => this.GetPaceMappingPublicKey(RequestData),
				0x83 => this.GetPaceEphemeralPublicKey(RequestData),
				0x85 => this.GetPaceVerificationToken(RequestData),
				_ => Status(0x6a, 0x80)
			};
		}

		private byte[] HandleSecureMessagingCommand(byte[] Command)
		{
			if (!this.secureMessagingEstablished)
				return Status(0x69, 0x82);

			if (!this.TryUnwrapSecureMessagingCommand(Command, out byte[] PlainCommand))
				return Status(0x69, 0x88);

			this.processingUnwrappedSecureMessagingCommand = true;
			try
			{
				byte[] PlainResponse = this.Transmit(PlainCommand);
				return this.WrapSecureMessagingResponse(PlainResponse);
			}
			finally
			{
				this.processingUnwrappedSecureMessagingCommand = false;
			}
		}

		private bool CanSelectFile(ushort FileId)
		{
			if (FileId is EF.CardAccess or EF.DIR or EF.ATR)
				return this.State is EmrtdProtocolSimulatorState.Idle or
					EmrtdProtocolSimulatorState.MasterFileSelected or
					EmrtdProtocolSimulatorState.ElementaryFileSelected;

			if (FileId == EF.SOD)
				return this.State is EmrtdProtocolSimulatorState.MasterFileSelected or
					EmrtdProtocolSimulatorState.LdsApplicationSelected or
					EmrtdProtocolSimulatorState.ElementaryFileSelected or
					EmrtdProtocolSimulatorState.SecureMessagingEstablished;

			return this.State is EmrtdProtocolSimulatorState.LdsApplicationSelected or
				EmrtdProtocolSimulatorState.ElementaryFileSelected or
				EmrtdProtocolSimulatorState.SecureMessagingEstablished;
		}

		private bool RequiresSecureMessagingForSelection(ushort FileId)
		{
			if (!this.options.RequireSecureMessagingForDataGroups)
				return false;

			if (this.secureMessagingEstablished && this.processingUnwrappedSecureMessagingCommand)
				return false;

			return IsDataGroupFile(FileId);
		}

		private bool RequiresSecureMessagingForRead(ushort FileId)
		{
			if (!this.options.RequireSecureMessagingForDataGroups)
				return false;

			if (this.secureMessagingEstablished && this.processingUnwrappedSecureMessagingCommand)
				return false;

			return IsLdsFile(FileId);
		}

		private byte[] ReadFile(ushort FileId, uint Offset, int Length)
		{
			if (!this.options.Files.TryGetValue(FileId, out byte[]? FileData))
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

		private static bool IsSecureMessagingClass(byte Class)
		{
			return (Class & ISO_7816.Classes.SecureMessaging) == ISO_7816.Classes.SecureMessaging;
		}

		private void InitializePaceProtocol()
		{
			Id_PACE_ECDH_GM_AES_CBC_CMAC_128 Protocol = new Id_PACE_ECDH_GM_AES_CBC_CMAC_128();
			Protocol.Configure(new Vector(new object[]
			{
				Protocol,
				new System.Numerics.BigInteger(2),
				new System.Numerics.BigInteger(13)
			}, []));

			this.paceProtocol = Protocol;
			this.paceNonceReturned = false;
			this.paceMappingPerformed = false;
			this.paceKeyAgreementPerformed = false;
			int KeyIndex = 0;
			this.paceChipMappingPublicKey = Protocol.CreateNewKey(this.options.PaceKeySeed, ref KeyIndex);
		}

		private byte[] GetPaceEncryptedNonce()
		{
			if (this.paceProtocol is null || this.options.DocumentInformation is null)
				return Status(0x69, 0x85);

			if (this.paceNonceReturned)
				return Status(0x69, 0x85);

			byte[] Kpi = this.paceProtocol.KDFπ(this.options.DocumentInformation);
			byte[] EncryptedNonce = this.paceProtocol.Encrypt(Kpi, new byte[this.paceProtocol.BlockLength],
				this.options.PaceNonce);
			this.paceNonceReturned = true;
			return DynamicAuthenticationResponse(0x80, EncryptedNonce);
		}

		private byte[] GetPaceMappingPublicKey(byte[] RequestData)
		{
			if (this.paceProtocol is null || this.paceChipMappingPublicKey is null)
				return Status(0x69, 0x85);

			if (!this.paceNonceReturned)
				return Status(0x69, 0x85);

			if (!TryDecodePacePublicKey(RequestData, out byte[] TerminalPublicKey))
				return Status(0x6a, 0x80);

			PointOnCurve MappedGenerator;
			try
			{
				MappedGenerator = this.paceProtocol.GetGenericMap(this.options.PaceNonce, TerminalPublicKey);
			}
			catch (ArgumentException)
			{
				return Status(0x6a, 0x80);
			}

			this.paceTerminalMappingPublicKey = TerminalPublicKey;
			this.paceMappedGenerator = MappedGenerator;
			this.paceMappingPerformed = true;
			return DynamicAuthenticationResponse(0x82, EncodePacePublicKey(this.paceChipMappingPublicKey));
		}

		private byte[] GetPaceEphemeralPublicKey(byte[] RequestData)
		{
			if (this.paceProtocol is null || this.paceMappedGenerator is null)
				return Status(0x69, 0x85);

			if (!this.paceMappingPerformed)
				return Status(0x69, 0x85);

			if (!TryDecodePacePublicKey(RequestData, out byte[] TerminalEphemeralPublicKey))
				return Status(0x6a, 0x80);

			int KeyIndex = 1;
			this.paceChipEphemeralPrivateKey = this.paceProtocol.GenerateSecret(this.options.PaceKeySeed, ref KeyIndex);
			PointOnCurve PublicPoint = this.paceProtocol.Curve!.ScalarMultiplication(
				this.paceChipEphemeralPrivateKey, this.paceMappedGenerator.Value, true);
			this.paceChipEphemeralPublicKey = this.paceProtocol.Curve.Encode(PublicPoint, true);

			this.paceTerminalEphemeralPublicKey = TerminalEphemeralPublicKey;
			try
			{
				this.EstablishPaceSessionKeys();
			}
			catch (ArgumentException)
			{
				this.paceTerminalEphemeralPublicKey = null;
				return Status(0x6a, 0x80);
			}

			this.paceKeyAgreementPerformed = true;
			return DynamicAuthenticationResponse(0x84, EncodePacePublicKey(this.paceChipEphemeralPublicKey));
		}

		private byte[] GetPaceVerificationToken(byte[] RequestData)
		{
			if (this.paceProtocol is null ||
				this.paceMac is null ||
				this.paceChipEphemeralPublicKey is null ||
				this.paceTerminalEphemeralPublicKey is null ||
				!this.paceKeyAgreementPerformed)
			{
				return Status(0x69, 0x85);
			}

			byte[] ExpectedTerminalAssociatedData = PaceProtocol.CreateAssociatedData(
				this.paceProtocol.Oid, this.paceChipEphemeralPublicKey);
			if (!this.paceMac.Verify(ExpectedTerminalAssociatedData, RequestData))
				return Status(0x69, 0x82);

			byte[] ChipAssociatedData = PaceProtocol.CreateAssociatedData(
				this.paceProtocol.Oid, this.paceTerminalEphemeralPublicKey);
			byte[] Token = this.paceMac.Sign(ChipAssociatedData, 8);

			this.secureMessagingEstablished = true;
			this.State = EmrtdProtocolSimulatorState.SecureMessagingEstablished;
			this.paceSendSequenceCounter = new byte[this.paceProtocol.BlockLength];

			return DynamicAuthenticationResponse(0x86, Token);
		}

		private void EstablishPaceSessionKeys()
		{
			if (this.paceProtocol is null ||
				this.paceChipEphemeralPrivateKey is null ||
				this.paceTerminalEphemeralPublicKey is null)
			{
				throw new InvalidOperationException("PACE ephemeral keys are incomplete.");
			}

			PointOnCurve TerminalPoint = DecodePoint(this.paceProtocol.Curve!, this.paceTerminalEphemeralPublicKey);
			PointOnCurve SharedPoint = this.paceProtocol.Curve!.ScalarMultiplication(
				this.paceChipEphemeralPrivateKey, TerminalPoint, true);
			byte[] SharedPointX = SharedPoint.X.ToByteArray();

			if (SharedPointX.Length != this.paceProtocol.Curve.OrderBytes)
				Array.Resize(ref SharedPointX, this.paceProtocol.Curve.OrderBytes);

			Array.Reverse(SharedPointX);
			this.paceKsEnc = this.paceProtocol.KDF_Enc(SharedPointX);
			this.paceKsMac = this.paceProtocol.KDF_Mac(SharedPointX);
			this.paceMac = this.paceProtocol.GetAuthenticator(this.paceKsMac);
		}

		private bool TryUnwrapSecureMessagingCommand(byte[] Command, out byte[] PlainCommand)
		{
			PlainCommand = [];
			if (this.paceProtocol is null ||
				this.paceKsEnc is null ||
				this.paceMac is null ||
				this.paceSendSequenceCounter is null ||
				Command.Length < 7)
			{
				return false;
			}

			byte ProtectedClass = Command[0];
			byte Ins = Command[1];
			byte P1 = Command[2];
			byte P2 = Command[3];
			int Lc = Command[4];
			if (Command.Length != Lc + 6)
				return false;

			int Cursor = 5;
			int DataObjectsEnd = 5 + Lc;
			byte[]? EncryptedData = null;
			byte? Le = null;
			byte[]? Mac = null;
			byte[] DataObjectsForMac = [];

			while (Cursor < DataObjectsEnd)
			{
				int ObjectStart = Cursor;
				byte Tag = Command[Cursor++];
				if (!TryReadDerLength(Command, ref Cursor, DataObjectsEnd, out int Length))
					return false;

				if (Cursor + Length > DataObjectsEnd)
					return false;

				if (Tag is 0x87 or 0x85)
				{
					if (Length < 1 || Command[Cursor] != 0x01)
						return false;

					EncryptedData = new byte[Length - 1];
					Buffer.BlockCopy(Command, Cursor + 1, EncryptedData, 0, EncryptedData.Length);
				}
				else if (Tag == 0x97)
				{
					if (Length != 1)
						return false;

					Le = Command[Cursor];
				}
				else if (Tag == 0x8e)
				{
					if (Length != 8)
						return false;

					Mac = new byte[8];
					Buffer.BlockCopy(Command, Cursor, Mac, 0, Mac.Length);
				}

				Cursor += Length;

				if (Tag != 0x8e)
				{
					int ObjectLength = Cursor - ObjectStart;
					byte[] NewDataObjects = new byte[DataObjectsForMac.Length + ObjectLength];
					Buffer.BlockCopy(DataObjectsForMac, 0, NewDataObjects, 0, DataObjectsForMac.Length);
					Buffer.BlockCopy(Command, ObjectStart, NewDataObjects, DataObjectsForMac.Length, ObjectLength);
					DataObjectsForMac = NewDataObjects;
				}
			}

			if (Mac is null)
				return false;

			this.IncrementPaceCounter();
			byte[] Header = [ProtectedClass, Ins, P1, P2];
			byte[] HeaderPadding = new byte[this.paceProtocol.BlockLength - 4];
			HeaderPadding[0] = 0x80;
			byte[] AssociatedDataPadding = CreatePadding(DataObjectsForMac.Length, this.paceProtocol.BlockLength);
			byte[] AssociatedData = TravelDocumentsClient.CONCAT(
				this.paceSendSequenceCounter,
				Header,
				HeaderPadding,
				DataObjectsForMac,
				AssociatedDataPadding);

			if (!this.paceMac.Verify(AssociatedData, Mac))
				return false;

			byte[] CommandData = [];
			if (EncryptedData is not null)
			{
				byte[] Iv = this.paceProtocol.Encrypt(this.paceKsEnc, new byte[this.paceProtocol.BlockLength],
					this.paceSendSequenceCounter);
				CommandData = this.paceProtocol.Decrypt(this.paceKsEnc, Iv, EncryptedData);
				if (!TryRemovePadding(ref CommandData))
					return false;
			}

			if (CommandData.Length == 0)
			{
				PlainCommand = [ISO_7816.Classes.Basic, Ins, P1, P2, Le ?? 0x00];
			}
			else if (Le.HasValue && (Le.Value != 0x00 || Ins == ISO_7816.Instructions.ReadBinary + 1))
			{
				PlainCommand = TravelDocumentsClient.CONCAT(
					[ISO_7816.Classes.Basic, Ins, P1, P2, (byte)CommandData.Length],
					CommandData,
					[Le.Value]);
			}
			else
			{
				PlainCommand = TravelDocumentsClient.CONCAT(
					[ISO_7816.Classes.Basic, Ins, P1, P2, (byte)CommandData.Length],
					CommandData);
			}

			return true;
		}

		private byte[] WrapSecureMessagingResponse(byte[] PlainResponse)
		{
			if (this.paceProtocol is null ||
				this.paceKsEnc is null ||
				this.paceMac is null ||
				this.paceSendSequenceCounter is null ||
				PlainResponse.Length < 2)
			{
				return Status(0x69, 0x88);
			}

			byte Sw1 = PlainResponse[^2];
			byte Sw2 = PlainResponse[^1];
			byte[] ResponseData = new byte[PlainResponse.Length - 2];
			Buffer.BlockCopy(PlainResponse, 0, ResponseData, 0, ResponseData.Length);

			byte[] ProtectedData = [];
			if (ResponseData.Length > 0)
			{
				byte[] PaddedData = AddPadding(ResponseData, this.paceProtocol.BlockLength);
				this.IncrementPaceCounter();
				byte[] Iv = this.paceProtocol.Encrypt(this.paceKsEnc, new byte[this.paceProtocol.BlockLength],
					this.paceSendSequenceCounter);
				byte[] EncryptedData = this.paceProtocol.Encrypt(this.paceKsEnc, Iv, PaddedData);
				ProtectedData = EncodeDataObject(0x87, TravelDocumentsClient.CONCAT([0x01], EncryptedData));
			}
			else
				this.IncrementPaceCounter();

			byte[] StatusData = [0x99, 0x02, Sw1, Sw2];
			byte[] DataObjects = TravelDocumentsClient.CONCAT(ProtectedData, StatusData);
			byte[] Padding = CreatePadding(DataObjects.Length, this.paceProtocol.BlockLength);
			byte[] AssociatedData = TravelDocumentsClient.CONCAT(
				this.paceSendSequenceCounter,
				DataObjects,
				Padding);
			byte[] Signature = this.paceMac.Sign(AssociatedData, 8);

			return TravelDocumentsClient.CONCAT(
				DataObjects,
				[0x8e, 0x08],
				Signature,
				[0x90, 0x00]);
		}

		private static bool TryParseDynamicAuthenticationData(byte[] Command, out byte RequestTag,
			out byte[] RequestData)
		{
			RequestTag = 0;
			RequestData = [];

			if (Command.Length < 7)
				return false;

			int Lc = Command[4];
			if (Command.Length != Lc + 6)
				return false;

			if (Command[5] != 0x7c)
				return false;

			int Cursor = 6;
			if (!TryReadDerLength(Command, ref Cursor, 5 + Lc, out int DynamicLength))
				return false;

			if (Cursor + DynamicLength != 5 + Lc)
				return false;

			if (DynamicLength == 0)
				return true;

			RequestTag = Command[Cursor++];
			if (!TryReadDerLength(Command, ref Cursor, 5 + Lc, out int ValueLength))
				return false;

			if (Cursor + ValueLength != 5 + Lc)
				return false;

			RequestData = new byte[ValueLength];
			Buffer.BlockCopy(Command, Cursor, RequestData, 0, ValueLength);
			return true;
		}

		private static byte[] DynamicAuthenticationResponse(byte ResponseTag, byte[] ResponseData)
		{
			byte[] Inner = EncodeDataObject(ResponseTag, ResponseData);
			byte[] Outer = EncodeDataObject(0x7c, Inner);
			return WithStatus(Outer, 0x90, 0x00);
		}

		private static bool TryDecodePacePublicKey(byte[] EncodedPublicKey, out byte[] PublicKey)
		{
			PublicKey = [];
			if (EncodedPublicKey.Length == 0 || EncodedPublicKey[0] != 0x04)
				return false;

			PublicKey = new byte[EncodedPublicKey.Length - 1];
			Buffer.BlockCopy(EncodedPublicKey, 1, PublicKey, 0, PublicKey.Length);
			return true;
		}

		private static byte[] EncodePacePublicKey(byte[] PublicKey)
		{
			return TravelDocumentsClient.CONCAT([0x04], PublicKey);
		}

		private static PointOnCurve DecodePoint(EllipticCurve Curve, byte[] PublicKey)
		{
			int Length = PublicKey.Length;
			int CoordinateLength = Length >> 1;
			byte[] X = new byte[CoordinateLength];
			byte[] Y = new byte[CoordinateLength];

			Buffer.BlockCopy(PublicKey, 0, X, 0, CoordinateLength);
			Buffer.BlockCopy(PublicKey, CoordinateLength, Y, 0, CoordinateLength);
			Array.Reverse(X);
			Array.Reverse(Y);

			return new PointOnCurve(EllipticCurve.ToInt(X), EllipticCurve.ToInt(Y));
		}

		private void IncrementPaceCounter()
		{
			if (this.paceSendSequenceCounter is null)
				throw new InvalidOperationException("PACE send sequence counter has not been initialized.");

			int Index = this.paceSendSequenceCounter.Length;
			while (++this.paceSendSequenceCounter[--Index] == 0 && Index >= 0)
			{
			}
		}

		private static byte[] AddPadding(byte[] Data, int BlockLength)
		{
			int PaddedLength = (Data.Length + BlockLength - 1) & ~(BlockLength - 1);
			if (PaddedLength == Data.Length)
				return (byte[])Data.Clone();

			byte[] Result = new byte[PaddedLength];
			Buffer.BlockCopy(Data, 0, Result, 0, Data.Length);
			Result[Data.Length] = 0x80;
			return Result;
		}

		private static bool TryRemovePadding(ref byte[] Data)
		{
			int Index = Data.Length;
			while (Index > 0 && Data[Index - 1] == 0x00)
				Index--;

			if (Index > 0 && Data[Index - 1] == 0x80)
			{
				Array.Resize(ref Data, Index - 1);
				return true;
			}

			return true;
		}

		private static byte[] CreatePadding(int DataLength, int BlockLength)
		{
			int Remainder = DataLength % BlockLength;
			if (Remainder == 0)
				return [];

			byte[] Padding = new byte[BlockLength - Remainder];
			Padding[0] = 0x80;
			return Padding;
		}

		private static byte[] EncodeDataObject(byte Tag, byte[] Value)
		{
			return TravelDocumentsClient.CONCAT([Tag], EncodeLength(Value.Length), Value);
		}

		private static byte[] EncodeLength(int Length)
		{
			if (Length < 0x80)
				return [(byte)Length];

			if (Length <= 0xff)
				return [0x81, (byte)Length];

			return [0x82, (byte)(Length >> 8), (byte)Length];
		}

		private static bool TryReadDerLength(byte[] Data, ref int Cursor, int End, out int Length)
		{
			Length = 0;
			if (Cursor >= End)
				return false;

			int First = Data[Cursor++];
			if ((First & 0x80) == 0)
			{
				Length = First;
				return true;
			}

			int Count = First & 0x7f;
			if (Count is 0 or > 4 || Cursor + Count > End)
				return false;

			for (int i = 0; i < Count; i++)
				Length = (Length << 8) | Data[Cursor++];

			return true;
		}

		private static bool IsDataGroupFile(ushort FileId)
		{
			return FileId is >= EF.DG1 and <= EF.DG16;
		}

		private static bool IsLdsFile(ushort FileId)
		{
			return FileId is EF.COM or EF.SOD || IsDataGroupFile(FileId);
		}

		private static Dictionary<byte, ushort> CreateSfiMap()
		{
			return new Dictionary<byte, ushort>()
			{
				[0x1e] = EF.COM,
				[0x1d] = EF.SOD,
				[0x1c] = EF.CardAccess,
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

		private static byte[] WithStatus(byte[] Data, byte Sw1, byte Sw2)
		{
			byte[] Response = new byte[Data.Length + 2];
			Buffer.BlockCopy(Data, 0, Response, 0, Data.Length);
			Response[^2] = Sw1;
			Response[^1] = Sw2;
			return Response;
		}

		/// <summary>
		/// Formats a file identifier for diagnostics.
		/// </summary>
		/// <param name="FileId">File identifier.</param>
		/// <returns>Upper-case hexadecimal file identifier.</returns>
		public static string FormatFileId(ushort FileId)
		{
			return FileId.ToString("X4", CultureInfo.InvariantCulture);
		}
	}
}
