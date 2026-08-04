using System.Text;
using System.Xml;
using NeuroAccess.Nfc;
using NeuroAccess.Nfc.TravelDocuments;
using NeuroAccess.Nfc.TravelDocuments.Certificates;
using NeuroAccess.Nfc.TravelDocuments.DataObjects;
using Waher.Content.Xml;
using Waher.Networking.Sniffers;
using Waher.Networking.Sniffers.Model;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.TravelDocuments
{
	/// <summary>
	/// Identifies the outcome of a travel-document chip readout.
	/// </summary>
	public enum TravelDocumentReadoutStatus
	{
		/// <summary>
		/// The chip was read successfully.
		/// </summary>
		Success = 0,

		/// <summary>
		/// The chip could not be authenticated.
		/// </summary>
		AuthenticationFailed = 1,

		/// <summary>
		/// The chip authenticated, but its travel-document data could not be read.
		/// </summary>
		ReadFailed = 2,

		/// <summary>
		/// An unexpected exception interrupted the readout.
		/// </summary>
		UnexpectedError = 3
	}

	/// <summary>
	/// Identifies the certificate and passive-authentication outcome of a travel-document readout.
	/// </summary>
	public enum TravelDocumentCertificateValidationStatus
	{
		/// <summary>The readout did not reach certificate verification.</summary>
		NotVerified = 0,

		/// <summary>The certificate and passive-authentication checks completed successfully.</summary>
		Valid = 1,

		/// <summary>The certificate or passive-authentication checks failed.</summary>
		Invalid = 2,

		/// <summary>Certificate verification succeeded, but full document verification did not complete.</summary>
		Incomplete = 3
	}

	/// <summary>
	/// Identifies the reason for a travel-document certificate-validation outcome.
	/// </summary>
	public enum TravelDocumentCertificateValidationReason
	{
		/// <summary>No additional reason is available.</summary>
		None = 0,

		/// <summary>Chip authentication did not complete.</summary>
		AuthenticationFailed,

		/// <summary>The LDS travel-document application was not found.</summary>
		LdsApplicationNotFound,

		/// <summary>Application-level information could not be read.</summary>
		ApplicationInformationReadFailed,

		/// <summary>Application-level information could not be parsed.</summary>
		ApplicationInformationParseFailed,

		/// <summary>EF.SOD could not be read.</summary>
		SecurityObjectReadFailed,

		/// <summary>EF.SOD could not be parsed.</summary>
		SecurityObjectParseFailed,

		/// <summary>EF.SOD did not contain a signer certificate.</summary>
		NoCertificate,

		/// <summary>EF.SOD contained more than one signer certificate.</summary>
		MultipleCertificates,

		/// <summary>The signer certificate chain, revocation, or signature checks failed.</summary>
		InvalidCertificate,

		/// <summary>A certificate used in the travel document has been revoked.</summary>
		RevokedCertificate,

		/// <summary>Unable to validate the revocation status of a certificate used in the travel document.</summary>
		RevocationStatusUnknown,

		/// <summary>The signer certificate is not valid yet.</summary>
		CertificateNotYetValid,

		/// <summary>The signer certificate has expired.</summary>
		CertificateExpired,

		/// <summary>A data-group hash did not match EF.SOD.</summary>
		DataGroupHashMismatch,

		/// <summary>A data group could not be read after certificate verification.</summary>
		DataGroupReadFailed,

		/// <summary>A data group could not be parsed after certificate verification.</summary>
		DataGroupParseFailed,

		/// <summary>An unexpected error interrupted verification.</summary>
		UnexpectedError
	}

	/// <summary>
	/// Describes the document and application context used when reading a travel-document chip.
	/// </summary>
	public sealed class TravelDocumentReadoutRequest
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TravelDocumentReadoutRequest"/> class.
		/// </summary>
		/// <param name="DocumentInformation">The document information parsed from the MRZ.</param>
		/// <param name="MrzText">The normalized MRZ text used to authenticate the chip.</param>
		/// <param name="ApplicationIdentityId">The application identity identifier used for readout key seeding, if available.</param>
		public TravelDocumentReadoutRequest(DocumentInformation DocumentInformation, string MrzText, string? ApplicationIdentityId)
		{
			ArgumentNullException.ThrowIfNull(DocumentInformation);
			ArgumentException.ThrowIfNullOrWhiteSpace(MrzText);

			this.DocumentInformation = DocumentInformation;
			this.MrzText = MrzText;
			this.ApplicationIdentityId = ApplicationIdentityId;
		}

		/// <summary>
		/// Gets the document information parsed from the MRZ.
		/// </summary>
		public DocumentInformation DocumentInformation { get; }

		/// <summary>
		/// Gets the normalized MRZ text used to authenticate the chip.
		/// </summary>
		public string MrzText { get; }

		/// <summary>
		/// Gets the application identity identifier used for readout key seeding, if available.
		/// </summary>
		public string? ApplicationIdentityId { get; }
	}

	/// <summary>
	/// Contains the result of a travel-document chip readout.
	/// </summary>
	public sealed class TravelDocumentReadoutResult
	{
		internal TravelDocumentReadoutResult(
			TravelDocumentReadoutStatus Status,
			string Xml,
			AuthenticateResult? AuthenticationResult,
			ReadTravelDocumentResult? ReadResult,
			TravelDocumentData? DocumentData,
			TravelDocumentCertificateData CertificateData,
			string? ErrorMessage)
		{
			this.Status = Status;
			this.Xml = Xml;
			this.AuthenticationResult = AuthenticationResult;
			this.ReadResult = ReadResult;
			this.DocumentData = DocumentData;
			this.CertificateData = CertificateData;
			this.ErrorMessage = ErrorMessage;
		}

		/// <summary>
		/// Gets the readout status.
		/// </summary>
		public TravelDocumentReadoutStatus Status { get; }

		/// <summary>
		/// Gets the NFC readout XML captured from the chip session.
		/// </summary>
		public string Xml { get; }

		/// <summary>
		/// Gets the authentication result when authentication was attempted.
		/// </summary>
		public AuthenticateResult? AuthenticationResult { get; }

		/// <summary>
		/// Gets the travel-document read result when document data was requested.
		/// </summary>
		public ReadTravelDocumentResult? ReadResult { get; }

		/// <summary>
		/// Gets the best available decoded document data captured during the readout.
		/// </summary>
		public TravelDocumentData? DocumentData { get; }

		/// <summary>
		/// Gets the certificate and passive-authentication result captured during the readout.
		/// </summary>
		public TravelDocumentCertificateData CertificateData { get; }

		/// <summary>
		/// Gets the unexpected error message, if one was captured.
		/// </summary>
		public string? ErrorMessage { get; }

		/// <summary>
		/// Gets a value indicating whether the readout completed successfully.
		/// </summary>
		public bool IsSuccess => this.Status == TravelDocumentReadoutStatus.Success;
	}

	/// <summary>
	/// Contains an immutable snapshot of travel-document signer certificate metadata and validation state.
	/// </summary>
	public sealed class TravelDocumentCertificateData
	{
		internal TravelDocumentCertificateData(
			TravelDocumentCertificateValidationStatus Status,
			TravelDocumentCertificateValidationReason Reason,
			string? Subject,
			string? Issuer,
			string? SerialNumber,
			DateTimeOffset? NotBefore,
			DateTimeOffset? NotAfter)
		{
			this.Status = Status;
			this.Reason = Reason;
			this.Subject = Subject;
			this.Issuer = Issuer;
			this.SerialNumber = SerialNumber;
			this.NotBefore = NotBefore;
			this.NotAfter = NotAfter;
		}

		/// <summary>Gets the validation status.</summary>
		public TravelDocumentCertificateValidationStatus Status { get; }

		/// <summary>Gets the typed reason for the validation status.</summary>
		public TravelDocumentCertificateValidationReason Reason { get; }

		/// <summary>Gets the document signer subject.</summary>
		public string? Subject { get; }

		/// <summary>Gets the document signer issuer.</summary>
		public string? Issuer { get; }

		/// <summary>Gets the document signer serial number in hexadecimal form.</summary>
		public string? SerialNumber { get; }

		/// <summary>Gets the beginning of the document signer's validity period.</summary>
		public DateTimeOffset? NotBefore { get; }

		/// <summary>Gets the end of the document signer's validity period.</summary>
		public DateTimeOffset? NotAfter { get; }
	}

	/// <summary>
	/// Contains an immutable snapshot of human-readable travel-document data.
	/// </summary>
	public sealed class TravelDocumentData
	{
		internal TravelDocumentData(DocumentInformation DocumentInformation, AdditionalPersonalDetails? PersonalInformation)
		{
			this.DocumentType = DocumentInformation.DocumentType;
			this.IssuingState = DocumentInformation.IssuingState;
			this.DocumentNumber = DocumentInformation.DocumentNumber;
			this.PrimaryIdentifier = JoinValues(DocumentInformation.PrimaryIdentifier);
			this.SecondaryIdentifier = JoinValues(DocumentInformation.SecondaryIdentifier);
			this.Nationality = DocumentInformation.Nationality;
			this.DateOfBirth = DocumentInformation.DateOfBirth;
			this.Gender = DocumentInformation.Gender;
			this.ExpiryDate = DocumentInformation.ExpiryDate;
			this.OptionalData = DocumentInformation.OptionalData;
			this.FullName = PersonalInformation?.FullName;
			this.OtherNames = JoinValues(PersonalInformation?.OtherNames);
			this.PersonalNumber = PersonalInformation?.PersonalNumber;
			this.AdditionalDateOfBirth = PersonalInformation?.DateOfBirth;
			this.PlaceOfBirth = PersonalInformation?.PlaceOfBirth;
			this.PermanentAddress = PersonalInformation?.PermanentAddress;
			this.Telephone = PersonalInformation?.Telephone;
			this.Profession = PersonalInformation?.Profession;
			this.Title = PersonalInformation?.Title;
			this.PersonalSummary = PersonalInformation?.PersonalSummary;
			this.OtherNumbers = JoinValues(PersonalInformation?.OtherNumbers);
			this.CustodyInformation = PersonalInformation?.CustodyInformation;
		}

		/// <summary>Gets the document type.</summary>
		public string? DocumentType { get; }

		/// <summary>Gets the issuing state code.</summary>
		public string? IssuingState { get; }

		/// <summary>Gets the document number.</summary>
		public string? DocumentNumber { get; }

		/// <summary>Gets the primary identifier or family names.</summary>
		public string? PrimaryIdentifier { get; }

		/// <summary>Gets the secondary identifier or given names.</summary>
		public string? SecondaryIdentifier { get; }

		/// <summary>Gets the nationality code.</summary>
		public string? Nationality { get; }

		/// <summary>Gets the date of birth from DG1.</summary>
		public string? DateOfBirth { get; }

		/// <summary>Gets the gender or sex marker.</summary>
		public string? Gender { get; }

		/// <summary>Gets the document expiry date.</summary>
		public string? ExpiryDate { get; }

		/// <summary>Gets optional DG1 data.</summary>
		public string? OptionalData { get; }

		/// <summary>Gets the full name from additional personal details.</summary>
		public string? FullName { get; }

		/// <summary>Gets other names from additional personal details.</summary>
		public string? OtherNames { get; }

		/// <summary>Gets the personal number.</summary>
		public string? PersonalNumber { get; }

		/// <summary>Gets the date of birth from additional personal details.</summary>
		public string? AdditionalDateOfBirth { get; }

		/// <summary>Gets the place of birth.</summary>
		public string? PlaceOfBirth { get; }

		/// <summary>Gets the permanent address.</summary>
		public string? PermanentAddress { get; }

		/// <summary>Gets the telephone number.</summary>
		public string? Telephone { get; }

		/// <summary>Gets the profession.</summary>
		public string? Profession { get; }

		/// <summary>Gets the title.</summary>
		public string? Title { get; }

		/// <summary>Gets the personal summary.</summary>
		public string? PersonalSummary { get; }

		/// <summary>Gets other document numbers.</summary>
		public string? OtherNumbers { get; }

		/// <summary>Gets custody information.</summary>
		public string? CustodyInformation { get; }

		private static string? JoinValues(string[]? Values)
		{
			string Joined = string.Join(" ", Values ?? Array.Empty<string>()).Trim();
			return string.IsNullOrWhiteSpace(Joined) ? null : Joined;
		}
	}

	/// <summary>
	/// Reads ICAO travel-document chips through an ISO-DEP NFC interface.
	/// </summary>
	[DefaultImplementation(typeof(TravelDocumentReadoutService))]
	public interface ITravelDocumentReadoutService
	{
		/// <summary>
		/// Reads a travel-document chip using a parsed MRZ and optional application identity context.
		/// </summary>
		/// <param name="IsoDepInterface">The ISO-DEP interface connected to the document chip.</param>
		/// <param name="Request">The readout request.</param>
		/// <param name="CancellationToken">The cancellation token.</param>
		/// <returns>The readout result.</returns>
		Task<TravelDocumentReadoutResult> ReadAsync(
			IIsoDepInterface IsoDepInterface,
			TravelDocumentReadoutRequest Request,
			CancellationToken CancellationToken);
	}

	/// <summary>
	/// Default travel-document chip readout service.
	/// </summary>
	public sealed class TravelDocumentReadoutService : ITravelDocumentReadoutService
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TravelDocumentReadoutService"/> class.
		/// </summary>
		public TravelDocumentReadoutService()
		{
		}

		/// <inheritdoc/>
		public async Task<TravelDocumentReadoutResult> ReadAsync(
			IIsoDepInterface IsoDepInterface,
			TravelDocumentReadoutRequest Request,
			CancellationToken CancellationToken)
		{
			ArgumentNullException.ThrowIfNull(IsoDepInterface);
			ArgumentNullException.ThrowIfNull(Request);
			CancellationToken.ThrowIfCancellationRequested();

			StringBuilder XmlBuilder = new StringBuilder();
			using XmlWriter XmlOutput = XmlWriter.Create(XmlBuilder, XML.WriterSettings(false, true));
			XmlWriterSniffer InMemoryXmlWriterSniffer = new XmlWriterSniffer(XmlOutput, BinaryPresentationMethod.Base64, "NFC");
			TravelDocumentsClient? Client = null;

			TravelDocumentReadoutService.InitializeReadoutXml(XmlOutput, InMemoryXmlWriterSniffer, Request.MrzText);

			try
			{
				await IsoDepInterface.OpenIfClosed();
				CancellationToken.ThrowIfCancellationRequested();
				IsoDepInterface.SetTimeout(300000);

				ISniffer[] Sniffers = TravelDocumentReadoutService.CreateSniffers(InMemoryXmlWriterSniffer);
				byte[]? LocalKeySeed = TravelDocumentReadoutService.CreateLocalKeySeed(Request.ApplicationIdentityId);

				Client = new TravelDocumentsClient(IsoDepInterface, Request.DocumentInformation, LocalKeySeed, Sniffers);
				TravelDocumentReadoutService.RegisterReadoutEvents(Client);

				Client.Information("Starting readout.");
				CancellationToken.ThrowIfCancellationRequested();

				AuthenticateResult AuthenticationResult = await Client.Authenticate();
				CancellationToken.ThrowIfCancellationRequested();
				if (AuthenticationResult != AuthenticateResult.Success)
				{
					return new TravelDocumentReadoutResult(
						TravelDocumentReadoutStatus.AuthenticationFailed,
						await TravelDocumentReadoutService.CompleteReadoutXmlAsync(InMemoryXmlWriterSniffer, XmlOutput, XmlBuilder),
						AuthenticationResult,
						null,
						TravelDocumentReadoutService.CreateDocumentData(Client, Request.DocumentInformation),
						TravelDocumentReadoutService.CreateCertificateData(Client, null, false),
						null);
				}

				ReadTravelDocumentResult ReadResult = await Client.ReadTravelDocument(Constants.Domains.IdDomain);
				CancellationToken.ThrowIfCancellationRequested();
				if (ReadResult != ReadTravelDocumentResult.Success)
				{
					return new TravelDocumentReadoutResult(
						TravelDocumentReadoutStatus.ReadFailed,
						await TravelDocumentReadoutService.CompleteReadoutXmlAsync(InMemoryXmlWriterSniffer, XmlOutput, XmlBuilder),
						AuthenticationResult,
						ReadResult,
						TravelDocumentReadoutService.CreateDocumentData(Client, Request.DocumentInformation),
						TravelDocumentReadoutService.CreateCertificateData(Client, ReadResult, true),
						null);
				}

				Client.Information("Readout completed.");

				return new TravelDocumentReadoutResult(
					TravelDocumentReadoutStatus.Success,
					await TravelDocumentReadoutService.CompleteReadoutXmlAsync(InMemoryXmlWriterSniffer, XmlOutput, XmlBuilder),
					AuthenticationResult,
					ReadResult,
					TravelDocumentReadoutService.CreateDocumentData(Client, Request.DocumentInformation),
					TravelDocumentReadoutService.CreateCertificateData(Client, ReadResult, true),
					null);
			}
			catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception Ex)
			{
				return new TravelDocumentReadoutResult(
					TravelDocumentReadoutStatus.UnexpectedError,
					await TravelDocumentReadoutService.CompleteReadoutXmlAsync(InMemoryXmlWriterSniffer, XmlOutput, XmlBuilder),
					null,
					null,
					TravelDocumentReadoutService.CreateDocumentData(Client, Request.DocumentInformation),
					TravelDocumentReadoutService.CreateCertificateData(Client, null, false),
					Ex.Message);
			}
			finally
			{
				Client?.Dispose();
				IsoDepInterface.CloseIfOpen();
			}
		}

		private static TravelDocumentData CreateDocumentData(TravelDocumentsClient? Client, DocumentInformation FallbackDocumentInformation)
		{
			DocumentInformation DocumentInformation = Client?.Mrz?.DocumentInformation ?? FallbackDocumentInformation;
			return new TravelDocumentData(DocumentInformation, Client?.PersonalInformation);
		}

		private static TravelDocumentCertificateData CreateCertificateData(
			TravelDocumentsClient? Client,
			ReadTravelDocumentResult? ReadResult,
			bool AuthenticationSucceeded)
		{
			Certificate[] Certificates = Client?.SecurityInfo?.SignedData?.Certificates ?? Array.Empty<Certificate>();
			Certificate? Certificate = Certificates.Length == 1 ? Certificates[0] : null;
			TravelDocumentCertificateValidationStatus Status;
			TravelDocumentCertificateValidationReason Reason;

			if (!AuthenticationSucceeded)
			{
				Status = TravelDocumentCertificateValidationStatus.NotVerified;
				Reason = ReadResult.HasValue
					? TravelDocumentReadoutService.GetCertificateValidationReason(ReadResult.Value)
					: TravelDocumentCertificateValidationReason.AuthenticationFailed;
			}
			else
			{
				(Status, Reason) = TravelDocumentReadoutService.GetCertificateValidationOutcome(ReadResult, Client?.SecurityInfo is not null);
			}

			if (Status == TravelDocumentCertificateValidationStatus.Valid && Certificate is not null)
			{
				DateTimeOffset Now = DateTimeOffset.UtcNow;
				if (Now < Certificate.NotBefore.ToUniversalTime())
				{
					Status = TravelDocumentCertificateValidationStatus.Invalid;
					Reason = TravelDocumentCertificateValidationReason.CertificateNotYetValid;
				}
				else if (Now > Certificate.NotAfter.ToUniversalTime())
				{
					Status = TravelDocumentCertificateValidationStatus.Invalid;
					Reason = TravelDocumentCertificateValidationReason.CertificateExpired;
				}
			}

			return new TravelDocumentCertificateData(
				Status,
				Reason,
				Certificate is null ? null : TravelDocumentReadoutService.FormatNames(Certificate.Subject),
				Certificate is null ? null : TravelDocumentReadoutService.FormatNames(Certificate.Issuer),
				Certificate?.SerialNumber.ToString("X", System.Globalization.CultureInfo.InvariantCulture),
				Certificate?.NotBefore,
				Certificate?.NotAfter);
		}

		private static (TravelDocumentCertificateValidationStatus Status, TravelDocumentCertificateValidationReason Reason)
			GetCertificateValidationOutcome(ReadTravelDocumentResult? ReadResult, bool HasSecurityInfo)
		{
			if (!ReadResult.HasValue)
				return (TravelDocumentCertificateValidationStatus.NotVerified, TravelDocumentCertificateValidationReason.UnexpectedError);

			return ReadResult.Value switch
			{
				ReadTravelDocumentResult.Success => (TravelDocumentCertificateValidationStatus.Valid, TravelDocumentCertificateValidationReason.None),
				ReadTravelDocumentResult.NoCertificates => (TravelDocumentCertificateValidationStatus.Invalid, TravelDocumentCertificateValidationReason.NoCertificate),
				ReadTravelDocumentResult.MultipleCertificates => (TravelDocumentCertificateValidationStatus.Invalid, TravelDocumentCertificateValidationReason.MultipleCertificates),
				ReadTravelDocumentResult.InvalidCertificate => (TravelDocumentCertificateValidationStatus.Invalid, TravelDocumentCertificateValidationReason.InvalidCertificate),
				ReadTravelDocumentResult.RevokedCertificate => (TravelDocumentCertificateValidationStatus.Invalid, TravelDocumentCertificateValidationReason.RevokedCertificate),
				ReadTravelDocumentResult.RevocationStatusUnknown => (TravelDocumentCertificateValidationStatus.Incomplete, TravelDocumentCertificateValidationReason.RevocationStatusUnknown),
				ReadTravelDocumentResult.DgHashDigestInvalid => (TravelDocumentCertificateValidationStatus.Invalid, TravelDocumentCertificateValidationReason.DataGroupHashMismatch),
				ReadTravelDocumentResult.UnableToReadEfDg when HasSecurityInfo => (TravelDocumentCertificateValidationStatus.Incomplete, TravelDocumentCertificateValidationReason.DataGroupReadFailed),
				ReadTravelDocumentResult.UnableToParseEfDg when HasSecurityInfo => (TravelDocumentCertificateValidationStatus.Incomplete, TravelDocumentCertificateValidationReason.DataGroupParseFailed),
				_ => (TravelDocumentCertificateValidationStatus.NotVerified, TravelDocumentReadoutService.GetCertificateValidationReason(ReadResult.Value))
			};
		}

		private static TravelDocumentCertificateValidationReason GetCertificateValidationReason(ReadTravelDocumentResult ReadResult)
		{
			return ReadResult switch
			{
				ReadTravelDocumentResult.Lds1ApplicationNotFound => TravelDocumentCertificateValidationReason.LdsApplicationNotFound,
				ReadTravelDocumentResult.UnableToReadEfCom => TravelDocumentCertificateValidationReason.ApplicationInformationReadFailed,
				ReadTravelDocumentResult.UnableToParseEfCom => TravelDocumentCertificateValidationReason.ApplicationInformationParseFailed,
				ReadTravelDocumentResult.UnableToReadEfSod => TravelDocumentCertificateValidationReason.SecurityObjectReadFailed,
				ReadTravelDocumentResult.UnableToParseEfSod => TravelDocumentCertificateValidationReason.SecurityObjectParseFailed,
				ReadTravelDocumentResult.UnableToReadEfDg => TravelDocumentCertificateValidationReason.DataGroupReadFailed,
				ReadTravelDocumentResult.UnableToParseEfDg => TravelDocumentCertificateValidationReason.DataGroupParseFailed,
				_ => TravelDocumentCertificateValidationReason.UnexpectedError
			};
		}

		private static string? FormatNames(Names Names)
		{
			List<string> Values = new List<string>();
			TravelDocumentReadoutService.AddNameValue(Values, "CN", Names.CommonName);
			TravelDocumentReadoutService.AddNameValue(Values, "O", Names.OrganizationName);
			TravelDocumentReadoutService.AddNameValue(Values, "OU", Names.OrganizationUnitName);
			TravelDocumentReadoutService.AddNameValue(Values, "C", Names.CountryName);
			return Values.Count == 0 ? null : string.Join(", ", Values);
		}

		private static void AddNameValue(List<string> Values, string Label, string Value)
		{
			if (!string.IsNullOrWhiteSpace(Value))
				Values.Add(Label + "=" + Value.Trim());
		}

		private static void InitializeReadoutXml(XmlWriter XmlOutput, XmlWriterSniffer InMemoryXmlWriterSniffer, string MrzText)
		{
			XmlOutput.WriteStartDocument();
			XmlOutput.WriteStartElement("SnifferOutput", "http://waher.se/Schema/SnifferOutput.xsd");
			InMemoryXmlWriterSniffer.Information(MrzText);
		}

		private static ISniffer[] CreateSniffers(XmlWriterSniffer InMemoryXmlWriterSniffer)
		{
			List<ISniffer> Sniffers = new List<ISniffer>
			{
				InMemoryXmlWriterSniffer
			};
			Sniffers.AddRange(ServiceRef.XmppService.RemoteSniffers);
			return Sniffers.ToArray();
		}

		private static byte[]? CreateLocalKeySeed(string? ApplicationIdentityId)
		{
			if (string.IsNullOrWhiteSpace(ApplicationIdentityId))
				return null;

			string LocalKeySeedSource = ApplicationIdentityId.Trim();
			int AtIndex = LocalKeySeedSource.IndexOf('@', StringComparison.Ordinal);
			if (AtIndex >= 0)
				LocalKeySeedSource = LocalKeySeedSource[..AtIndex];

			return Encoding.UTF8.GetBytes(LocalKeySeedSource);
		}

		private static void RegisterReadoutEvents(TravelDocumentsClient Client)
		{
			Client.StateChanged += (_, _) => Task.CompletedTask;
			Client.AppInfoUpdated += (_, _) => Task.CompletedTask;
			Client.SecurityInfoUpdated += (_, _) => Task.CompletedTask;
			Client.MrzUpdated += (_, _) => Task.CompletedTask;
			Client.BiometricEncodingFaceUpdated += (_, _) => Task.CompletedTask;
			Client.BiometricEncodingFingersUpdated += (_, _) => Task.CompletedTask;
			Client.BiometricEncodingIrisesUpdated += (_, _) => Task.CompletedTask;
			Client.DisplayedSignaturesUpdated += (_, _) => Task.CompletedTask;
			Client.PersonalInformationUpdated += (_, _) => Task.CompletedTask;
		}

		private static async Task<string> CompleteReadoutXmlAsync(
			XmlWriterSniffer InMemoryXmlWriterSniffer,
			XmlWriter XmlOutput,
			StringBuilder XmlBuilder)
		{
			await InMemoryXmlWriterSniffer.FlushAsync();
			XmlOutput.WriteEndElement();
			XmlOutput.WriteEndDocument();
			XmlOutput.Flush();

			return XmlBuilder.ToString();
		}
	}
}
