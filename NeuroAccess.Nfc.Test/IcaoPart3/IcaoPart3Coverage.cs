using System.Globalization;
using NeuroAccess.Nfc.TravelDocuments;
using NeuroAccess.Nfc.TravelDocuments.PACE;
using NeuroAccess.Nfc.TravelDocuments.PACE.Id_PACE_ECDH_GM;
using NeuroAccess.Nfc.TravelDocuments.Security;
using Waher.Security.EllipticCurves;

namespace NeuroAccess.Nfc.Test.IcaoPart3
{
	/// <summary>
	/// Coverage status for an ICAO Part 3 test case.
	/// </summary>
	public enum IcaoPart3CoverageStatus
	{
		/// <summary>
		/// The case passed.
		/// </summary>
		Passed,

		/// <summary>
		/// The case was skipped because the active profile does not declare it.
		/// </summary>
		SkippedProfileNotDeclared,

		/// <summary>
		/// The case is blocked by missing secure messaging support.
		/// </summary>
		BlockedSecureMessaging,

		/// <summary>
		/// The case is blocked by a missing fixture file.
		/// </summary>
		BlockedMissingFixture,

		/// <summary>
		/// The case is blocked by unsupported protocol behavior.
		/// </summary>
		BlockedUnsupportedProtocol,

		/// <summary>
		/// The case failed.
		/// </summary>
		Failed
	}

	/// <summary>
	/// Represents the coverage result of one ICAO Part 3 case.
	/// </summary>
	public sealed class IcaoPart3CoverageResult
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="IcaoPart3CoverageResult"/> class.
		/// </summary>
		/// <param name="CaseId">Case identifier.</param>
		/// <param name="Status">Coverage status.</param>
		/// <param name="Reason">Coverage reason.</param>
		/// <param name="Capability">Implementation capability bucket.</param>
		public IcaoPart3CoverageResult(string CaseId, IcaoPart3CoverageStatus Status, string Reason,
			string Capability = "General")
		{
			this.CaseId = CaseId;
			this.Status = Status;
			this.Reason = Reason;
			this.Capability = Capability;
		}

		/// <summary>
		/// Gets the case identifier.
		/// </summary>
		public string CaseId { get; }

		/// <summary>
		/// Gets the coverage status.
		/// </summary>
		public IcaoPart3CoverageStatus Status { get; }

		/// <summary>
		/// Gets the coverage reason.
		/// </summary>
		public string Reason { get; }

		/// <summary>
		/// Gets the implementation capability bucket for roadmap reporting.
		/// </summary>
		public string Capability { get; }
	}

	/// <summary>
	/// Classifies ICAO Part 3 cases against the current committed fixtures.
	/// </summary>
	public sealed class IcaoPart3CoverageClassifier
	{
		private static readonly HashSet<string> UnsupportedProtocolTokens =
		[
			"PACE-CAM",
			"CA",
			"TA",
			"EAC",
			"AA",
			"AA-ECDSA",
			"AA-RSA",
			"LDS2"
		];

		private readonly JmrtdFixture primaryFixture;
		private readonly JmrtdFixture? iso39794Fixture;
		private static readonly byte[] PaceOidEcdhGmAesCbcCmac128 =
			HexToBytes("04007F00070202040202");
		private static readonly byte[] PaceValidationNonce =
		[
			0x3f, 0x00, 0xc4, 0xd3, 0x9d, 0x15, 0x3f, 0x2b,
			0x2a, 0x21, 0x4a, 0x07, 0x8d, 0x89, 0x9b, 0x22
		];
		private static readonly byte[] AppendixGValidMappingPublicKey =
			HexToBytes("824FBA91C9CBE26BEF53A0EBE7342A3BF178CEA9F45DE0B70AA601651FBA3F5730D8C879AAA9C9F73991E61B58F4D52EB87A0A0C709A49DC63719363CCD13C54");
		private static readonly byte[] AppendixGTerminalEphemeralPrivateKey =
			PrimeFieldCurve.ToByteSecret(
			[
				0xa73fb703, 0xac1436a1, 0x8e0cfa5a, 0xbb3f7bec,
				0x7a070e7a, 0x6788486b, 0xee230c4a, 0x22762595
			]);

		/// <summary>
		/// Initializes a new instance of the <see cref="IcaoPart3CoverageClassifier"/> class.
		/// </summary>
		/// <param name="PrimaryFixture">Primary fixture to test against.</param>
		/// <param name="Iso39794Fixture">ISO/IEC 39794 fixture to test DG2 39794 cases against.</param>
		public IcaoPart3CoverageClassifier(JmrtdFixture PrimaryFixture, JmrtdFixture? Iso39794Fixture = null)
		{
			this.primaryFixture = PrimaryFixture;
			this.iso39794Fixture = Iso39794Fixture;
		}

		/// <summary>
		/// Classifies one ICAO Part 3 case.
		/// </summary>
		/// <param name="Case">Case to classify.</param>
		/// <returns>Coverage result.</returns>
		public IcaoPart3CoverageResult Classify(IcaoPart3Case Case)
		{
			foreach (string Token in Case.ProfileTokens)
			{
				if (UnsupportedProtocolTokens.Contains(Token))
				{
					return new IcaoPart3CoverageResult(Case.Id,
						IcaoPart3CoverageStatus.BlockedUnsupportedProtocol,
						"Profile token is not supported by the v1 virtual card: " + Token,
						GetUnsupportedTokenCapability(Token));
				}
			}

			JmrtdFixture Fixture = this.GetFixture(Case);

			if (Case.Category == "ISO7816_P" &&
				this.TryClassifyPaceProtocolCase(Case, Fixture, out IcaoPart3CoverageResult PaceResult))
			{
				return PaceResult;
			}

			if (Case.RequiresSecureMessaging || Case.RequiresRuntimeApduGeneration)
			{
				if (this.TryClassifyProtectedClientCase(Case, Fixture, out IcaoPart3CoverageResult? ProtectedResult))
					return ProtectedResult;

				return new IcaoPart3CoverageResult(Case.Id,
					IcaoPart3CoverageStatus.BlockedSecureMessaging,
					"Case requires protected APDU runtime generation.",
					GetProtectedRuntimeCapability(Case));
			}

			if (TryGetRequiredFileIds(Case, out ushort[] RequiredFileIds))
			{
				foreach (ushort FileId in RequiredFileIds)
				{
					if (!Fixture.Files.ContainsKey(FileId))
					{
						return new IcaoPart3CoverageResult(Case.Id,
							IcaoPart3CoverageStatus.BlockedMissingFixture,
							"Fixture does not contain " + FileId.ToString("X4", CultureInfo.InvariantCulture) + ".",
							"FixtureCoverage");
					}
				}
			}

			if (Case.Category.StartsWith("LDS_", StringComparison.Ordinal))
				return this.ClassifyLds(Case, Fixture);

			if (Case.Category is "ISO7816_A" or "ISO7816_B")
				return this.ClassifyPlainApdu(Case, Fixture);

			if (Case.Category == "ISO7816_Q")
				return this.ClassifyCardAccessApdu(Case, Fixture);

			return new IcaoPart3CoverageResult(Case.Id,
				IcaoPart3CoverageStatus.BlockedUnsupportedProtocol,
				"Category is not executable in the v1 coverage harness: " + Case.Category,
				GetUnsupportedCategoryCapability(Case));
		}

		private IcaoPart3CoverageResult ClassifyLds(IcaoPart3Case Case, JmrtdFixture Fixture)
		{
			if (Case.Category is not ("LDS_A" or "LDS_B" or "LDS_C" or "LDS_D" or "LDS_N"))
			{
				return new IcaoPart3CoverageResult(Case.Id,
					IcaoPart3CoverageStatus.BlockedUnsupportedProtocol,
					"LDS category is outside the v1 fixture scope: " + Case.Category,
					GetUnsupportedCategoryCapability(Case));
			}

			if (!TryGetLdsCategoryFileId(Case.Category, out ushort FileId))
			{
				return new IcaoPart3CoverageResult(Case.Id,
					IcaoPart3CoverageStatus.BlockedUnsupportedProtocol,
					"LDS category does not map to a v1 fixture file.",
					"LdsFixtureCoverage");
			}

			if (!Fixture.Files.TryGetValue(FileId, out byte[]? Data) || Data.Length == 0)
			{
				return new IcaoPart3CoverageResult(Case.Id,
					IcaoPart3CoverageStatus.BlockedMissingFixture,
					"Fixture data is missing for " + FileId.ToString("X4", CultureInfo.InvariantCulture) + ".",
					"LdsFixtureCoverage");
			}

			return new IcaoPart3CoverageResult(Case.Id,
				IcaoPart3CoverageStatus.Passed,
				"Fixture contains a non-empty LDS file for " + Case.Category + ".",
				"LdsFixturePresence");
		}

		private IcaoPart3CoverageResult ClassifyPlainApdu(IcaoPart3Case Case, JmrtdFixture Fixture)
		{
			EmrtdProtocolSimulatorOptions Options = new EmrtdProtocolSimulatorOptions(Fixture.Files)
			{
				RequireSecureMessagingForDataGroups = true
			};
			EmrtdProtocolSimulator Card = new EmrtdProtocolSimulator(Options);
			if (Case.Category != "ISO7816_A" &&
				Case.Preconditions.Contains("lds_application_selected", StringComparer.Ordinal))
			{
				Card.Transmit([0x00, 0xa4, 0x04, 0x0c, 0x07, 0xa0, 0x00, 0x00, 0x02, 0x47, 0x10, 0x01]);
			}

			foreach (IcaoPart3Step Step in Case.Steps)
			{
				foreach (IcaoPart3Apdu Apdu in Step.Apdus)
				{
					if (Apdu.Template || Apdu.Protected)
					{
						return new IcaoPart3CoverageResult(Case.Id,
							IcaoPart3CoverageStatus.BlockedSecureMessaging,
							"Step contains a protected or templated APDU.",
							GetProtectedRuntimeCapability(Case));
					}

					if (!TryParseHex(Apdu.Value, out byte[] Command))
					{
						return new IcaoPart3CoverageResult(Case.Id,
							IcaoPart3CoverageStatus.BlockedUnsupportedProtocol,
							"APDU is not concrete hex.",
							"ApduTemplateRuntimeGeneration");
					}

					byte[] Response = Card.Transmit(Command);
					if (!MatchesExpected(Response, Step.Expected))
					{
						return new IcaoPart3CoverageResult(Case.Id,
							IcaoPart3CoverageStatus.Failed,
							"Unexpected response for step " + Step.Number.ToString(CultureInfo.InvariantCulture) + ".",
							"PlainApduHarness");
					}
				}
			}

			return new IcaoPart3CoverageResult(Case.Id,
				IcaoPart3CoverageStatus.Passed,
				"Concrete APDUs matched expected status/data rules.",
				"PlainApduHarness");
		}

		private IcaoPart3CoverageResult ClassifyCardAccessApdu(IcaoPart3Case Case, JmrtdFixture Fixture)
		{
			if (!Fixture.Files.ContainsKey(EF.CardAccess))
			{
				return new IcaoPart3CoverageResult(Case.Id,
					IcaoPart3CoverageStatus.BlockedMissingFixture,
					"Fixture does not contain EF.CardAccess.",
					"PaceCardAccessFile");
			}

			EmrtdProtocolSimulator Card = new EmrtdProtocolSimulator(new EmrtdProtocolSimulatorOptions(Fixture.Files));
			bool SawRead = false;

			foreach (IcaoPart3Step Step in Case.Steps)
			{
				foreach (IcaoPart3Apdu Apdu in Step.Apdus)
				{
					if (Apdu.Action == "apdu_command")
					{
						return new IcaoPart3CoverageResult(Case.Id,
							IcaoPart3CoverageStatus.BlockedUnsupportedProtocol,
							"Case contains a response-data fragment imported as an APDU command.",
							"MatrixImportCleanup");
					}

					if (Apdu.Template || Apdu.Protected || !TryParseHex(Apdu.Value, out byte[] Command))
					{
						return new IcaoPart3CoverageResult(Case.Id,
							IcaoPart3CoverageStatus.BlockedUnsupportedProtocol,
							"CardAccess case is not a concrete unprotected APDU sequence.",
							"PaceCardAccessFile");
					}

					byte[] Response = Card.Transmit(Command);
					if (Response.Length < 2 || Response[^2] != 0x90 || Response[^1] != 0x00)
					{
						return new IcaoPart3CoverageResult(Case.Id,
							IcaoPart3CoverageStatus.Failed,
							"Unexpected EF.CardAccess response for step " + Step.Number.ToString(CultureInfo.InvariantCulture) + ".",
							"PaceCardAccessFile");
					}

					if (Apdu.Action is "read_binary" or "read_binary_odd_ins")
					{
						SawRead = true;
						if (Response.Length <= 2)
						{
							return new IcaoPart3CoverageResult(Case.Id,
								IcaoPart3CoverageStatus.Failed,
								"EF.CardAccess read returned no data.",
								"PaceCardAccessFile");
						}
					}
				}
			}

			if (!SawRead)
			{
				return new IcaoPart3CoverageResult(Case.Id,
					IcaoPart3CoverageStatus.BlockedUnsupportedProtocol,
					"CardAccess case does not contain a concrete read step.",
					"PaceCardAccessFile");
			}

			return new IcaoPart3CoverageResult(Case.Id,
				IcaoPart3CoverageStatus.Passed,
				"Concrete EF.CardAccess APDUs returned successful data.",
				"PaceCardAccessFile");
		}

		private bool TryClassifyPaceProtocolCase(IcaoPart3Case Case, JmrtdFixture Fixture,
			out IcaoPart3CoverageResult Result)
		{
			if (Case.Id is "ISO7816_P_1" or "ISO7816_P_78")
				return this.TryClassifyPositivePaceClientCase(Case, Fixture, out Result);

			if (Case.Id is "ISO7816_P_3" or "ISO7816_P_80")
				return this.TryClassifyPostPaceUnprotectedCommandCase(Case, Fixture, out Result);

			if (GetPaceProtocolCapability(Case) == "PaceMseSetAtValidation")
				return this.TryClassifyPaceMseSetAtCase(Case, Fixture, out Result);

			if (GetPaceProtocolCapability(Case) == "PaceNonceStepValidation")
				return this.TryClassifyPaceGeneralAuthenticateValidationCase(Case, Fixture, out Result);

			if (GetPaceProtocolCapability(Case) == "PaceMappingStepValidation")
				return this.TryClassifyPaceGeneralAuthenticateValidationCase(Case, Fixture, out Result);

			if (GetPaceProtocolCapability(Case) == "PaceKeyAgreementStepValidation")
				return this.TryClassifyPaceGeneralAuthenticateValidationCase(Case, Fixture, out Result);

			if (GetPaceProtocolCapability(Case) == "PaceMutualAuthenticationStepValidation")
				return this.TryClassifyPaceGeneralAuthenticateValidationCase(Case, Fixture, out Result);

			Result = new IcaoPart3CoverageResult(Case.Id,
				IcaoPart3CoverageStatus.BlockedSecureMessaging,
				"PACE case requires APDU runtime generation.",
				GetPaceProtocolCapability(Case));
			return true;
		}

		private bool TryClassifyPaceMseSetAtCase(IcaoPart3Case Case, JmrtdFixture Fixture,
			out IcaoPart3CoverageResult Result)
		{
			if (!TryGetPaceMseSetAtValidationCommand(Case.Id, out byte[] Command))
			{
				Result = new IcaoPart3CoverageResult(Case.Id,
					IcaoPart3CoverageStatus.BlockedSecureMessaging,
					"PACE MSE:Set AT case needs a concrete validation command.",
					"PaceMseSetAtValidation");
				return true;
			}

			EmrtdProtocolSimulator Simulator = this.CreatePaceSimulator(Fixture);
			byte[] Response = Simulator.Transmit(Command);
			bool Rejected = Response.Length == 2 &&
				(Response[0] != 0x90 || Response[1] != 0x00) &&
				Simulator.State != EmrtdProtocolSimulatorState.PaceSecurityEnvironmentSelected;

			if (Rejected)
			{
				Result = new IcaoPart3CoverageResult(Case.Id,
					IcaoPart3CoverageStatus.Passed,
					"Simulator rejected the invalid PACE MSE:Set AT command without entering PACE state.",
					"PaceMseSetAtValidation");
				return true;
			}

			Result = new IcaoPart3CoverageResult(Case.Id,
				IcaoPart3CoverageStatus.Failed,
				"Simulator accepted an invalid PACE MSE:Set AT command.",
				"PaceMseSetAtValidation");
			return true;
		}

		private bool TryClassifyPaceGeneralAuthenticateValidationCase(IcaoPart3Case Case, JmrtdFixture Fixture,
			out IcaoPart3CoverageResult Result)
		{
			string Capability = GetPaceProtocolCapability(Case);
			if (!TryGetPaceGeneralAuthenticateValidationCommand(Case.Id, out byte[] Command))
			{
				Result = new IcaoPart3CoverageResult(Case.Id,
					IcaoPart3CoverageStatus.BlockedSecureMessaging,
					"PACE GENERAL AUTHENTICATE case needs a concrete validation command.",
					Capability);
				return true;
			}

			EmrtdProtocolSimulator Simulator = this.CreatePaceSimulator(Fixture);
			byte[] MseResponse = Simulator.Transmit(BuildValidPaceMseSetAtCommand());
			if (!MseResponse.SequenceEqual(new byte[] { 0x90, 0x00 }))
			{
				Result = new IcaoPart3CoverageResult(Case.Id,
					IcaoPart3CoverageStatus.Failed,
					"Simulator could not initialize PACE before GENERAL AUTHENTICATE validation.",
					Capability);
				return true;
			}

			if (Capability == "PaceMappingStepValidation")
			{
				if (!RunValidPaceNonce(Simulator, out Result, Case.Id, Capability))
				{
					return true;
				}
			}
			else if (Capability == "PaceKeyAgreementStepValidation")
			{
				if (!RunValidPaceNonce(Simulator, out Result, Case.Id, Capability) ||
					!RunValidPaceMapping(Simulator, out Result, Case.Id, Capability))
				{
					return true;
				}
			}
			else if (Capability == "PaceMutualAuthenticationStepValidation")
			{
				if (!RunValidPaceNonce(Simulator, out Result, Case.Id, Capability) ||
					!RunValidPaceMapping(Simulator, out Result, Case.Id, Capability) ||
					!RunValidPaceKeyAgreement(Simulator, out Result, Case.Id, Capability))
				{
					return true;
				}
			}

			byte[] Response = Simulator.Transmit(Command);
			bool Rejected = Response.Length == 2 &&
				(Response[0] != 0x90 || Response[1] != 0x00) &&
				Simulator.State == EmrtdProtocolSimulatorState.PaceSecurityEnvironmentSelected &&
				(
					Capability == "PaceNonceStepValidation" && !Simulator.PaceNonceReturned ||
					Capability == "PaceMappingStepValidation" && Simulator.PaceNonceReturned && !Simulator.PaceMappingPerformed ||
					Capability == "PaceKeyAgreementStepValidation" && Simulator.PaceMappingPerformed && !Simulator.PaceKeyAgreementPerformed ||
					Capability == "PaceMutualAuthenticationStepValidation" && Simulator.PaceKeyAgreementPerformed
				);

			if (Rejected)
			{
				Result = new IcaoPart3CoverageResult(Case.Id,
					IcaoPart3CoverageStatus.Passed,
					"Simulator rejected the invalid PACE GENERAL AUTHENTICATE command before advancing protocol state.",
					Capability);
				return true;
			}

			Result = new IcaoPart3CoverageResult(Case.Id,
				IcaoPart3CoverageStatus.Failed,
				"Simulator accepted an invalid PACE GENERAL AUTHENTICATE command.",
				Capability);
			return true;
		}

		private static bool RunValidPaceNonce(EmrtdProtocolSimulator Simulator, out IcaoPart3CoverageResult Result,
			string CaseId, string Capability)
		{
			byte[] NonceResponse = Simulator.Transmit(BuildPaceGeneralAuthenticateCommand(
				ISO_7816.Classes.Chaining, 0x00, []));
			if (NonceResponse.Length >= 2 && NonceResponse[^2] == 0x90 && NonceResponse[^1] == 0x00 &&
				Simulator.PaceNonceReturned)
			{
				Result = null!;
				return true;
			}

			Result = new IcaoPart3CoverageResult(CaseId,
				IcaoPart3CoverageStatus.Failed,
				"Simulator could not complete the PACE nonce step before validation.",
				Capability);
			return false;
		}

		private static bool RunValidPaceMapping(EmrtdProtocolSimulator Simulator, out IcaoPart3CoverageResult Result,
			string CaseId, string Capability)
		{
			byte[] MappingResponse = Simulator.Transmit(BuildValidPaceMappingCommand());
			if (MappingResponse.Length >= 2 && MappingResponse[^2] == 0x90 && MappingResponse[^1] == 0x00 &&
				Simulator.PaceMappingPerformed)
			{
				Result = null!;
				return true;
			}

			Result = new IcaoPart3CoverageResult(CaseId,
				IcaoPart3CoverageStatus.Failed,
				"Simulator could not complete the PACE mapping step before validation.",
				Capability);
			return false;
		}

		private static bool RunValidPaceKeyAgreement(EmrtdProtocolSimulator Simulator,
			out IcaoPart3CoverageResult Result, string CaseId, string Capability)
		{
			byte[] KeyAgreementResponse = Simulator.Transmit(BuildValidPaceKeyAgreementCommand());
			if (KeyAgreementResponse.Length >= 2 &&
				KeyAgreementResponse[^2] == 0x90 &&
				KeyAgreementResponse[^1] == 0x00 &&
				Simulator.PaceKeyAgreementPerformed)
			{
				Result = null!;
				return true;
			}

			Result = new IcaoPart3CoverageResult(CaseId,
				IcaoPart3CoverageStatus.Failed,
				"Simulator could not complete the PACE key agreement step before validation.",
				Capability);
			return false;
		}

		private bool TryClassifyPositivePaceClientCase(IcaoPart3Case Case, JmrtdFixture Fixture,
			out IcaoPart3CoverageResult Result)
		{
			try
			{
				EmrtdProtocolSimulator Simulator = this.CreatePaceSimulator(Fixture);
				EmrtdProtocolSimulatorIsoDepInterface IsoDep = new EmrtdProtocolSimulatorIsoDepInterface(Simulator);
				using TravelDocumentsClient Client = new TravelDocumentsClient(IsoDep, CreatePaceDocumentInformation(), null);

				if (Client.Authenticate().GetAwaiter().GetResult() == AuthenticateResult.Success &&
					Simulator.State == EmrtdProtocolSimulatorState.SecureMessagingEstablished)
				{
					Result = new IcaoPart3CoverageResult(Case.Id,
						IcaoPart3CoverageStatus.Passed,
						"TravelDocumentsClient completed the positive PACE MRZ flow.",
						"PacePositiveMrzFlow");
					return true;
				}

				Result = new IcaoPart3CoverageResult(Case.Id,
					IcaoPart3CoverageStatus.Failed,
					"TravelDocumentsClient did not establish PACE secure messaging.",
					"PacePositiveMrzFlow");
				return true;
			}
			catch (Exception Ex) when (Ex is InvalidOperationException or ArgumentException)
			{
				Result = new IcaoPart3CoverageResult(Case.Id,
					IcaoPart3CoverageStatus.Failed,
					"TravelDocumentsClient positive PACE flow failed: " + Ex.Message,
					"PacePositiveMrzFlow");
				return true;
			}
		}

		private bool TryClassifyPostPaceUnprotectedCommandCase(IcaoPart3Case Case, JmrtdFixture Fixture,
			out IcaoPart3CoverageResult Result)
		{
			try
			{
				EmrtdProtocolSimulator Simulator = this.CreatePaceSimulator(Fixture);
				EmrtdProtocolSimulatorIsoDepInterface IsoDep = new EmrtdProtocolSimulatorIsoDepInterface(Simulator);
				using TravelDocumentsClient Client = new TravelDocumentsClient(IsoDep, CreatePaceDocumentInformation(), null);

				if (Client.Authenticate().GetAwaiter().GetResult() != AuthenticateResult.Success)
				{
					Result = new IcaoPart3CoverageResult(Case.Id,
						IcaoPart3CoverageStatus.Failed,
						"TravelDocumentsClient did not establish PACE before the negative command.",
						"PacePostAuthenticationSecurityPolicy");
					return true;
				}

				byte[] Response = Case.Id == "ISO7816_P_3"
					? Simulator.Transmit([0x00, 0xb0, 0x81, 0x00, 0x01])
					: Simulator.Transmit([0x00, 0xa4, 0x02, 0x0c, 0x02, 0x01, 0x01]);

				if (Response.SequenceEqual(new byte[] { 0x69, 0x82 }))
				{
					Result = new IcaoPart3CoverageResult(Case.Id,
						IcaoPart3CoverageStatus.Passed,
						"Simulator rejected an unprotected command after PACE was established.",
						"PacePostAuthenticationSecurityPolicy");
					return true;
				}

				Result = new IcaoPart3CoverageResult(Case.Id,
					IcaoPart3CoverageStatus.Failed,
					"Simulator did not reject the unprotected post-PACE command.",
					"PacePostAuthenticationSecurityPolicy");
				return true;
			}
			catch (Exception Ex) when (Ex is InvalidOperationException or ArgumentException)
			{
				Result = new IcaoPart3CoverageResult(Case.Id,
					IcaoPart3CoverageStatus.Failed,
					"Post-PACE security policy flow failed: " + Ex.Message,
					"PacePostAuthenticationSecurityPolicy");
				return true;
			}
		}

		private bool TryClassifyProtectedClientCase(IcaoPart3Case Case, JmrtdFixture Fixture,
			out IcaoPart3CoverageResult? Result)
		{
			Result = null;
			if (!Case.ProfileTokens.Contains("PACE", StringComparer.Ordinal) &&
				Case.Category != "ISO7816_O")
			{
				return false;
			}

			bool HasExecutableStep = false;

			foreach (IcaoPart3Step Step in Case.Steps)
			{
				if (Step.Apdus.Length == 0 || Step.Expected.SwAny.Length == 0)
					continue;

				if (!Step.Expected.SwAny.Any(Sw => String.Equals(Sw, "9000", StringComparison.OrdinalIgnoreCase)))
					return false;

				HasExecutableStep = true;

				foreach (IcaoPart3Apdu Apdu in Step.Apdus)
				{
					if (!Apdu.Protected)
						return false;

					if (Apdu.Action == "select_file")
					{
						if (!TryGetStepFileId(Step, out _))
							return false;
					}
					else if (Apdu.Action == "read_binary")
					{
						if (!TryGetProtectedReadBinaryParameters(Apdu, out _, out _))
							return false;
					}
					else
						return false;
				}
			}

			if (!HasExecutableStep)
				return false;

			try
			{
				if (this.ExecuteProtectedClientCaseAsync(Case, Fixture).GetAwaiter().GetResult())
				{
					Result = new IcaoPart3CoverageResult(Case.Id,
						IcaoPart3CoverageStatus.Passed,
						"TravelDocumentsClient executed equivalent protected PACE SELECT/READ steps.",
						"PaceProtectedReadClient");
					return true;
				}

				Result = new IcaoPart3CoverageResult(Case.Id,
					IcaoPart3CoverageStatus.Failed,
					"TravelDocumentsClient protected PACE flow did not match expected success.",
					"PaceProtectedReadClient");
				return true;
			}
			catch (Exception Ex) when (Ex is InvalidOperationException or ArgumentException)
			{
				Result = new IcaoPart3CoverageResult(Case.Id,
					IcaoPart3CoverageStatus.Failed,
					"TravelDocumentsClient protected PACE flow failed: " + Ex.Message,
					"PaceProtectedReadClient");
				return true;
			}
		}

		private async Task<bool> ExecuteProtectedClientCaseAsync(IcaoPart3Case Case, JmrtdFixture Fixture)
		{
			Waher.Runtime.Inventory.Types.Initialize(typeof(TravelDocumentsClient).Assembly);

			Dictionary<ushort, byte[]> Files = new Dictionary<ushort, byte[]>(Fixture.Files)
			{
				[EF.CardAccess] = HexToBytes("31143012060A04007F0007020204020202010202010D")
			};
			DocumentInformation Information = new DocumentInformation()
			{
				MRZ_Information = "T22000129364081251010318"
			};
			EmrtdProtocolSimulatorOptions Options = new EmrtdProtocolSimulatorOptions(Files)
			{
				DocumentInformation = Information,
				RequireSecureMessagingForDataGroups = true,
				EnablePaceEntryPoints = true
			};
			EmrtdProtocolSimulator Simulator = new EmrtdProtocolSimulator(Options);
			EmrtdProtocolSimulatorIsoDepInterface IsoDep = new EmrtdProtocolSimulatorIsoDepInterface(Simulator);

			using TravelDocumentsClient Client = new TravelDocumentsClient(IsoDep, Information, null);
			if (await Client.Authenticate() != AuthenticateResult.Success)
				return false;

			if (!await Client.SelectApplication(Applications.DF1))
				return false;

			ushort? SelectedFileId = null;
			foreach (IcaoPart3Step Step in Case.Steps)
			{
				if (Step.Apdus.Length == 0 || Step.Expected.SwAny.Length == 0)
					continue;

				foreach (IcaoPart3Apdu Apdu in Step.Apdus)
				{
					if (Apdu.Action == "select_file")
					{
						if (!TryGetStepFileId(Step, out ushort FileId))
							return false;

						if (!await Client.SelectFile(FileId))
							return false;

						SelectedFileId = FileId;
					}
					else if (Apdu.Action == "read_binary")
					{
						if (!TryGetProtectedReadBinaryParameters(Apdu, out uint Offset, out byte Length))
							return false;

						if (TryGetStepFileId(Step, out ushort FileId) && SelectedFileId != FileId)
						{
							if (!await Client.SelectFile(FileId))
								return false;

							SelectedFileId = FileId;
						}

						KeyValuePair<byte[]?, bool> Response = await Client.ReadBinary(Offset, Length);
						if (Response.Key is null)
							return false;
					}
				}
			}

			return true;
		}

		private EmrtdProtocolSimulator CreatePaceSimulator(JmrtdFixture Fixture)
		{
			Waher.Runtime.Inventory.Types.Initialize(typeof(TravelDocumentsClient).Assembly);

			Dictionary<ushort, byte[]> Files = new Dictionary<ushort, byte[]>(Fixture.Files)
			{
				[EF.CardAccess] = HexToBytes("31143012060A04007F0007020204020202010202010D")
			};
			EmrtdProtocolSimulatorOptions Options = new EmrtdProtocolSimulatorOptions(Files)
			{
				DocumentInformation = CreatePaceDocumentInformation(),
				PaceNonce = PaceValidationNonce,
				RequireSecureMessagingForDataGroups = true,
				EnablePaceEntryPoints = true
			};

			return new EmrtdProtocolSimulator(Options);
		}

		private static DocumentInformation CreatePaceDocumentInformation()
		{
			return new DocumentInformation()
			{
				MRZ_Information = "T22000129364081251010318"
			};
		}

		private JmrtdFixture GetFixture(IcaoPart3Case Case)
		{
			if (Case.ProfileTokens.Contains("39794-5", StringComparer.Ordinal) &&
				this.iso39794Fixture is not null)
			{
				return this.iso39794Fixture;
			}

			return this.primaryFixture;
		}

		private static bool MatchesExpected(byte[] Response, IcaoPart3ExpectedResult Expected)
		{
			if (Response.Length < 2)
				return false;

			string Status = Response[^2].ToString("X2", CultureInfo.InvariantCulture) +
				Response[^1].ToString("X2", CultureInfo.InvariantCulture);
			if (Expected.SwAny.Length > 0 &&
				!Expected.SwAny.Any(Sw => String.Equals(Sw, Status, StringComparison.OrdinalIgnoreCase)))
			{
				return false;
			}

			byte[] Data = new byte[Response.Length - 2];
			Buffer.BlockCopy(Response, 0, Data, 0, Data.Length);

			if (Expected.ResponseDataEmpty.HasValue &&
				Expected.ResponseDataEmpty.Value != (Data.Length == 0))
			{
				return false;
			}

			if (!string.IsNullOrEmpty(Expected.DataPrefix))
			{
				if (!TryParseHex(Expected.DataPrefix, out byte[] Prefix) ||
					Data.Length < Prefix.Length)
				{
					return false;
				}

				for (int i = 0; i < Prefix.Length; i++)
				{
					if (Data[i] != Prefix[i])
						return false;
				}
			}

			return true;
		}

		private static bool TryParseHex(string Hex, out byte[] Data)
		{
			string Normalized = Hex.Replace(" ", string.Empty, StringComparison.Ordinal)
				.Replace("\r", string.Empty, StringComparison.Ordinal)
				.Replace("\n", string.Empty, StringComparison.Ordinal);
			Data = [];

			if (Normalized.Length == 0 || Normalized.Length % 2 != 0)
				return false;

			byte[] Result = new byte[Normalized.Length / 2];
			for (int i = 0; i < Result.Length; i++)
			{
				if (!Byte.TryParse(Normalized.Substring(i * 2, 2),
					NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte Value))
				{
					return false;
				}

				Result[i] = Value;
			}

			Data = Result;
			return true;
		}

		private static byte[] HexToBytes(string Hex)
		{
			byte[] Result = new byte[Hex.Length / 2];
			for (int i = 0; i < Result.Length; i++)
				Result[i] = Byte.Parse(Hex.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

			return Result;
		}

		private static bool TryGetPaceMseSetAtValidationCommand(string CaseId, out byte[] Command)
		{
			Command = CaseId switch
			{
				"ISO7816_P_5" => BuildPaceMseSetAtCommand(
					BuildDataObject(0x81, PaceOidEcdhGmAesCbcCmac128),
					BuildDataObject(0x83, [0x01]),
					BuildDataObject(0x84, [0x0d])),

				"ISO7816_P_6" or "ISO7816_P_68" => BuildPaceMseSetAtCommand(
					BuildDataObject(0x80, HexToBytes("04007F00070202040203")),
					BuildDataObject(0x83, [0x01]),
					BuildDataObject(0x84, [0x0d])),

				"ISO7816_P_7" => BuildPaceMseSetAtCommand(
					BuildDataObject(0x80, HexToBytes("060A04007F00070202040202")),
					BuildDataObject(0x83, [0x01]),
					BuildDataObject(0x84, [0x0d])),

				"ISO7816_P_8" => BuildPaceMseSetAtCommand(
					BuildDataObject(0x80, PaceOidEcdhGmAesCbcCmac128),
					BuildDataObject(0x83, [0xff]),
					BuildDataObject(0x84, [0x0d])),

				"ISO7816_P_9" => BuildPaceMseSetAtCommand(
					BuildDataObject(0x80, PaceOidEcdhGmAesCbcCmac128),
					BuildDataObject(0x83, [0x01]),
					BuildDataObject(0x84, [0xff])),

				"ISO7816_P_64" => BuildPaceMseSetAtCommand(
					BuildDataObject(0x83, [0x01]),
					BuildDataObject(0x84, [0x0d])),

				"ISO7816_P_65" => BuildPaceMseSetAtCommand(
					BuildDataObject(0x80, []),
					BuildDataObject(0x83, [0x01]),
					BuildDataObject(0x84, [0x0d])),

				"ISO7816_P_66" => BuildPaceMseSetAtCommand(
					BuildDataObject(0x80, HexToBytes("04007F0007020204020200")),
					BuildDataObject(0x83, [0x01]),
					BuildDataObject(0x84, [0x0d])),

				"ISO7816_P_67" => BuildPaceMseSetAtCommand(
					BuildDataObject(0x80, HexToBytes("04007F000702020402")),
					BuildDataObject(0x83, [0x01]),
					BuildDataObject(0x84, [0x0d])),

				"ISO7816_P_69" => BuildPaceMseSetAtCommand(
					BuildDataObject(0x80, PaceOidEcdhGmAesCbcCmac128),
					BuildDataObject(0x84, [0x0d])),

				"ISO7816_P_70" => BuildPaceMseSetAtCommand(
					BuildDataObject(0x80, PaceOidEcdhGmAesCbcCmac128),
					BuildDataObject(0x83, []),
					BuildDataObject(0x84, [0x0d])),

				"ISO7816_P_71" => BuildPaceMseSetAtCommand(
					HexToBytes("800A04007F000702020402028303018401")),

				"ISO7816_P_72" => BuildPaceMseSetAtCommand(
					BuildDataObject(0x80, PaceOidEcdhGmAesCbcCmac128),
					BuildDataObject(0x83, [0x01, 0xff]),
					BuildDataObject(0x84, [0x0d])),

				"ISO7816_P_75" => BuildPaceMseSetAtCommand(
					BuildDataObject(0x80, PaceOidEcdhGmAesCbcCmac128),
					BuildDataObject(0x83, [0x01]),
					BuildDataObject(0x84, [])),

				"ISO7816_P_77" => BuildPaceMseSetAtCommand(
					BuildDataObject(0x06, PaceOidEcdhGmAesCbcCmac128),
					BuildDataObject(0x83, [0x01]),
					BuildDataObject(0x84, [0x0d])),

				_ => []
			};

			return Command.Length > 0;
		}

		private static bool TryGetPaceGeneralAuthenticateValidationCommand(string CaseId, out byte[] Command)
		{
			Command = CaseId switch
			{
				"ISO7816_P_11" => [0x10, 0x86, 0x00, 0x00, 0x02, 0x7d, 0x00, 0x00],
				"ISO7816_P_12" => [0x10, 0x86, 0x00, 0x00, 0x00, 0x00],
				"ISO7816_P_13" => [0x10, 0x86, 0x00, 0x00, 0x06, 0x7c, 0x04, 0x00, 0x00, 0x81, 0x00, 0x00],
				"ISO7816_P_54" => [0x00, 0x86, 0x00, 0x00, 0x02, 0x7c, 0x00, 0x00],
				"ISO7816_P_15" => [0x10, 0x86, 0x00, 0x00, 0x05, 0x7d, 0x03, 0x81, 0x01, 0x04, 0x00],
				"ISO7816_P_16" => [0x10, 0x86, 0x00, 0x00, 0x00, 0x00],
				"ISO7816_P_17" => [0x10, 0x86, 0x00, 0x00, 0x05, 0x7c, 0x03, 0x80, 0x01, 0x04, 0x00],
				"ISO7816_P_55" => BuildPaceGeneralAuthenticateCommand(
					ISO_7816.Classes.Basic, 0x81, [0x04]),
				"ISO7816_P_19" => [0x10, 0x86, 0x00, 0x00, 0x05, 0x7d, 0x03, 0x83, 0x01, 0x04, 0x00],
				"ISO7816_P_20" => [0x10, 0x86, 0x00, 0x00, 0x00, 0x00],
				"ISO7816_P_21" => [0x10, 0x86, 0x00, 0x00, 0x05, 0x7c, 0x03, 0x81, 0x01, 0x04, 0x00],
				"ISO7816_P_22" => [0x10, 0x86, 0x00, 0x00, 0x08, 0x7c, 0x06, 0x83, 0x01, 0x04, 0x81, 0x01, 0x04, 0x00],
				"ISO7816_P_56" => BuildPaceGeneralAuthenticateCommand(
					ISO_7816.Classes.Basic, 0x83, [0x04]),
				"ISO7816_P_31" => BuildRawPaceGeneralAuthenticateCommand(
					ISO_7816.Classes.Basic, BuildDataObject(0x7d, BuildDataObject(0x85, new byte[8]))),
				"ISO7816_P_32" => BuildRawPaceGeneralAuthenticateCommand(
					ISO_7816.Classes.Basic, BuildDataObject(0x85, new byte[8])),
				"ISO7816_P_33" => BuildPaceGeneralAuthenticateCommand(
					ISO_7816.Classes.Basic, 0x84, new byte[8]),
				"ISO7816_P_34" => BuildRawPaceGeneralAuthenticateCommand(
					ISO_7816.Classes.Basic,
					BuildDataObject(0x7c, TravelDocumentsClient.CONCAT(
						BuildDataObject(0x85, new byte[8]),
						BuildDataObject(0x81, [0x00])))),
				"ISO7816_P_35" => BuildPaceGeneralAuthenticateCommand(
					ISO_7816.Classes.Basic, 0x85, new byte[8]),
				"ISO7816_P_36" => BuildPaceGeneralAuthenticateCommand(
					ISO_7816.Classes.Basic, 0x85, [0x00]),
				"ISO7816_P_57" => BuildPaceGeneralAuthenticateCommand(
					ISO_7816.Classes.Chaining, 0x85, new byte[8]),
				_ => []
			};

			return Command.Length > 0;
		}

		private static byte[] BuildValidPaceMseSetAtCommand()
		{
			return BuildPaceMseSetAtCommand(
				BuildDataObject(0x80, PaceOidEcdhGmAesCbcCmac128),
				BuildDataObject(0x83, [0x01]),
				BuildDataObject(0x84, [0x0d]));
		}

		private static byte[] BuildValidPaceMappingCommand()
		{
			return BuildPaceGeneralAuthenticateCommand(
				ISO_7816.Classes.Chaining, 0x81, EncodePacePublicKey(AppendixGValidMappingPublicKey));
		}

		private static byte[] BuildValidPaceKeyAgreementCommand()
		{
			return BuildPaceGeneralAuthenticateCommand(
				ISO_7816.Classes.Chaining, 0x83, EncodePacePublicKey(CreateValidPaceTerminalEphemeralPublicKey()));
		}

		private static byte[] CreateValidPaceTerminalEphemeralPublicKey()
		{
			Id_PACE_ECDH_GM_AES_CBC_CMAC_128 Protocol = new Id_PACE_ECDH_GM_AES_CBC_CMAC_128();
			Protocol.Configure(new Vector(new object[]
			{
				Protocol,
				new System.Numerics.BigInteger(2),
				new System.Numerics.BigInteger(13)
			}, []));

			PointOnCurve MappedGenerator = Protocol.GetGenericMap(
				PaceValidationNonce, AppendixGValidMappingPublicKey);
			PointOnCurve PublicPoint = Protocol.Curve!.ScalarMultiplication(
				AppendixGTerminalEphemeralPrivateKey, MappedGenerator, true);
			return Protocol.Curve.Encode(PublicPoint, true);
		}

		private static byte[] EncodePacePublicKey(byte[] PublicKey)
		{
			byte[] EncodedPublicKey = new byte[PublicKey.Length + 1];
			EncodedPublicKey[0] = 0x04;
			Buffer.BlockCopy(PublicKey, 0, EncodedPublicKey, 1, PublicKey.Length);
			return EncodedPublicKey;
		}

		private static byte[] BuildPaceGeneralAuthenticateCommand(byte Class, byte RequestTag, byte[] Data)
		{
			byte[] Inner = RequestTag == 0x00 && Data.Length == 0
				? []
				: BuildDataObject(RequestTag, Data);
			byte[] DynamicAuthenticationData = BuildDataObject(0x7c, Inner);
			byte[] Command = new byte[6 + DynamicAuthenticationData.Length];

			Command[0] = Class;
			Command[1] = 0x86;
			Command[2] = 0x00;
			Command[3] = 0x00;
			Command[4] = (byte)DynamicAuthenticationData.Length;
			Buffer.BlockCopy(DynamicAuthenticationData, 0, Command, 5, DynamicAuthenticationData.Length);
			Command[^1] = 0x00;
			return Command;
		}

		private static byte[] BuildRawPaceGeneralAuthenticateCommand(byte Class, byte[] Data)
		{
			byte[] Command = new byte[6 + Data.Length];
			Command[0] = Class;
			Command[1] = 0x86;
			Command[2] = 0x00;
			Command[3] = 0x00;
			Command[4] = (byte)Data.Length;
			Buffer.BlockCopy(Data, 0, Command, 5, Data.Length);
			Command[^1] = 0x00;
			return Command;
		}

		private static byte[] BuildPaceMseSetAtCommand(params byte[][] DataObjects)
		{
			int Length = 0;
			foreach (byte[] DataObject in DataObjects)
				Length += DataObject.Length;

			byte[] Data = new byte[Length];
			int Offset = 0;
			foreach (byte[] DataObject in DataObjects)
			{
				Buffer.BlockCopy(DataObject, 0, Data, Offset, DataObject.Length);
				Offset += DataObject.Length;
			}

			return BuildPaceMseSetAtCommand(Data);
		}

		private static byte[] BuildPaceMseSetAtCommand(byte[] Data)
		{
			byte[] Command = new byte[5 + Data.Length];
			Command[0] = 0x00;
			Command[1] = 0x22;
			Command[2] = 0xc1;
			Command[3] = 0xa4;
			Command[4] = (byte)Data.Length;
			Buffer.BlockCopy(Data, 0, Command, 5, Data.Length);
			return Command;
		}

		private static byte[] BuildDataObject(byte Tag, byte[] Value)
		{
			byte[] DataObject = new byte[2 + Value.Length];
			DataObject[0] = Tag;
			DataObject[1] = (byte)Value.Length;
			Buffer.BlockCopy(Value, 0, DataObject, 2, Value.Length);
			return DataObject;
		}

		private static bool TryGetStepFileId(IcaoPart3Step Step, out ushort FileId)
		{
			FileId = 0;
			if (!string.IsNullOrEmpty(Step.TargetFileId) &&
				UInt16.TryParse(Step.TargetFileId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out FileId))
			{
				return true;
			}

			return !string.IsNullOrEmpty(Step.TargetFile) &&
				TryMapFileName(Step.TargetFile, out FileId);
		}

		private static bool TryGetProtectedReadBinaryParameters(IcaoPart3Apdu Apdu, out uint Offset, out byte Length)
		{
			Offset = 0;
			Length = 0;

			string[] Tokens = Apdu.Value
				.Replace("\r", " ", StringComparison.Ordinal)
				.Replace("\n", " ", StringComparison.Ordinal)
				.Split(' ', StringSplitOptions.RemoveEmptyEntries);

			if (Tokens.Length < 5 ||
				!TryParseByteToken(Tokens[1], out byte Instruction) ||
				Instruction != ISO_7816.Instructions.ReadBinary ||
				!TryParseByteToken(Tokens[2], out byte P1) ||
				!TryParseByteToken(Tokens[3], out byte P2))
			{
				return false;
			}

			Offset = (P1 & 0x80) != 0 ? P2 : (uint)((P1 << 8) | P2);

			for (int i = 0; i < Tokens.Length - 2; i++)
			{
				if (String.Equals(Tokens[i], "97", StringComparison.OrdinalIgnoreCase) &&
					String.Equals(Tokens[i + 1], "01", StringComparison.OrdinalIgnoreCase) &&
					TryParseByteToken(Tokens[i + 2], out Length))
				{
					return true;
				}
			}

			return TryParseByteToken(Tokens[^1], out Length);
		}

		private static bool TryParseByteToken(string Token, out byte Value)
		{
			Value = 0;
			return Token.Length == 2 &&
				Byte.TryParse(Token, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out Value);
		}

		private static bool TryGetRequiredFileIds(IcaoPart3Case Case, out ushort[] FileIds)
		{
			HashSet<ushort> Result = [];

			foreach (string TargetFile in Case.TargetFiles)
			{
				if (TryMapFileName(TargetFile, out ushort FileId))
					Result.Add(FileId);
			}

			foreach (IcaoPart3Step Step in Case.Steps)
			{
				if (!string.IsNullOrEmpty(Step.TargetFile) &&
					TryMapFileName(Step.TargetFile, out ushort FileId))
				{
					Result.Add(FileId);
				}
			}

			foreach (string Token in Case.ProfileTokens)
			{
				if (Token.StartsWith("DG", StringComparison.Ordinal) &&
					Int32.TryParse(Token[2..], out int DataGroupNumber) &&
					TryMapDataGroup(DataGroupNumber, out ushort FileId))
				{
					Result.Add(FileId);
				}
			}

			FileIds = [.. Result];
			return FileIds.Length > 0;
		}

		private static bool TryGetLdsCategoryFileId(string Category, out ushort FileId)
		{
			FileId = Category switch
			{
				"LDS_A" => EF.COM,
				"LDS_B" => EF.DG1,
				"LDS_C" => EF.DG2,
				"LDS_D" => EF.SOD,
				"LDS_N" => EF.DG11,
				_ => (ushort)0
			};

			return FileId != 0;
		}

		private static bool TryMapFileName(string FileName, out ushort FileId)
		{
			FileId = FileName switch
			{
				"EF.COM" => EF.COM,
				"EF.SOD" => EF.SOD,
				"EF.CardAccess" => EF.CardAccess,
				"EF.CardSecurity" => EF.CardSecurity,
				"EF.DG1" => EF.DG1,
				"EF.DG2" => EF.DG2,
				"EF.DG3" => EF.DG3,
				"EF.DG4" => EF.DG4,
				"EF.DG5" => EF.DG5,
				"EF.DG6" => 0x0106,
				"EF.DG7" => EF.DG7,
				"EF.DG8" => 0x0108,
				"EF.DG9" => 0x0109,
				"EF.DG10" => 0x010a,
				"EF.DG11" => EF.DG11,
				"EF.DG12" => EF.DG12,
				"EF.DG13" => EF.DG13,
				"EF.DG14" => EF.DG14,
				"EF.DG15" => EF.DG15,
				"EF.DG16" => EF.DG16,
				"EF.DIR" => EF.DIR,
				"EF.ATR/INFO" => EF.ATR,
				_ => (ushort)0
			};

			return FileId != 0;
		}

		private static bool TryMapDataGroup(int DataGroupNumber, out ushort FileId)
		{
			FileId = DataGroupNumber switch
			{
				1 => EF.DG1,
				2 => EF.DG2,
				3 => EF.DG3,
				4 => EF.DG4,
				5 => EF.DG5,
				6 => 0x0106,
				7 => EF.DG7,
				8 => 0x0108,
				9 => 0x0109,
				10 => 0x010a,
				11 => EF.DG11,
				12 => EF.DG12,
				13 => EF.DG13,
				14 => EF.DG14,
				15 => EF.DG15,
				16 => EF.DG16,
				_ => (ushort)0
			};

			return FileId != 0;
		}

		private static string GetUnsupportedTokenCapability(string Token)
		{
			return Token switch
			{
				"PACE-CAM" => "PaceCamProtocol",
				"CA" => "ChipAuthenticationProtocol",
				"TA" => "TerminalAuthenticationProtocol",
				"EAC" => "ExtendedAccessControlPolicy",
				"AA" or "AA-ECDSA" or "AA-RSA" => "ActiveAuthenticationProtocol",
				"LDS2" => "Lds2ApplicationSupport",
				_ => "UnsupportedProfileToken"
			};
		}

		private static string GetProtectedRuntimeCapability(IcaoPart3Case Case)
		{
			if (Case.Category == "ISO7816_C")
				return "BacSecureMessaging";

			if (Case.Category == "ISO7816_P")
				return GetPaceProtocolCapability(Case);

			if (Case.Category == "ISO7816_O")
				return "PaceProtectedReadRuntime";

			return "ProtectedApduRuntimeGeneration";
		}

		private static string GetUnsupportedCategoryCapability(IcaoPart3Case Case)
		{
			return Case.Category switch
			{
				"ISO7816_C" => "BacSecureMessaging",
				"ISO7816_O" => "PaceAccessControlPolicy",
				"ISO7816_P" => GetPaceProtocolCapability(Case),
				"ISO7816_Q" => "PaceCardAccessFile",
				"ISO7816_S" => "CardSecurityFileSupport",
				"ISO7816_T" => "AtrInfoFileSupport",
				"ISO7816_U" => "Lds2ApplicationSupport",
				"LDS_E" or "LDS_I" or "LDS_K" => "SecurityInfoSemanticParsers",
				"LDS_G" => "FingerprintSemanticParser",
				"LDS_H" => "IrisSemanticParser",
				"LDS_J" => "ActiveAuthenticationSemanticParser",
				"LDS_O" => "AdditionalPersonalDetailsSemanticParser",
				_ => "UnsupportedCategory"
			};
		}

		private static string GetPaceProtocolCapability(IcaoPart3Case Case)
		{
			string Purpose = Case.Purpose;

			if (Purpose.Contains("CAN password", StringComparison.OrdinalIgnoreCase))
				return "PaceCanPasswordFlow";

			if (Purpose.Contains("Valid PACE protocol", StringComparison.OrdinalIgnoreCase) ||
				Purpose.Contains("complete sequence of PACE", StringComparison.OrdinalIgnoreCase))
			{
				return "PacePostAuthenticationSecurityPolicy";
			}

			if (Purpose.Contains("MSE: Set AT", StringComparison.OrdinalIgnoreCase))
				return "PaceMseSetAtValidation";

			if (Purpose.Contains("encrypted nonce", StringComparison.OrdinalIgnoreCase))
				return "PaceNonceStepValidation";

			if (Purpose.Contains("map the nonce", StringComparison.OrdinalIgnoreCase))
				return "PaceMappingStepValidation";

			if (Purpose.Contains("key agreement", StringComparison.OrdinalIgnoreCase))
				return "PaceKeyAgreementStepValidation";

			if (Purpose.Contains("Mutual Authenticate", StringComparison.OrdinalIgnoreCase))
				return "PaceMutualAuthenticationStepValidation";

			return "PaceProtocolRuntimeGeneration";
		}
	}
}
