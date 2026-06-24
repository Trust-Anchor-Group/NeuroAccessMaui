using System.Text;
using System.Xml;
using NeuroAccess.Nfc;
using NeuroAccess.Nfc.TravelDocuments;
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
			string? ErrorMessage)
		{
			this.Status = Status;
			this.Xml = Xml;
			this.AuthenticationResult = AuthenticationResult;
			this.ReadResult = ReadResult;
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
		/// Gets the unexpected error message, if one was captured.
		/// </summary>
		public string? ErrorMessage { get; }

		/// <summary>
		/// Gets a value indicating whether the readout completed successfully.
		/// </summary>
		public bool IsSuccess => this.Status == TravelDocumentReadoutStatus.Success;
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

			TravelDocumentReadoutService.InitializeReadoutXml(XmlOutput, InMemoryXmlWriterSniffer, Request.MrzText);

			try
			{
				await IsoDepInterface.OpenIfClosed();
				CancellationToken.ThrowIfCancellationRequested();
				IsoDepInterface.SetTimeout(300000);

				ISniffer[] Sniffers = TravelDocumentReadoutService.CreateSniffers(InMemoryXmlWriterSniffer);
				byte[]? LocalKeySeed = TravelDocumentReadoutService.CreateLocalKeySeed(Request.ApplicationIdentityId);

				using TravelDocumentsClient Client = new TravelDocumentsClient(IsoDepInterface, Request.DocumentInformation, LocalKeySeed, Sniffers);
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
						null);
				}

				Client.Information("Readout completed.");

				return new TravelDocumentReadoutResult(
					TravelDocumentReadoutStatus.Success,
					await TravelDocumentReadoutService.CompleteReadoutXmlAsync(InMemoryXmlWriterSniffer, XmlOutput, XmlBuilder),
					AuthenticationResult,
					ReadResult,
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
					Ex.Message);
			}
			finally
			{
				IsoDepInterface.CloseIfOpen();
			}
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
