using System;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using NeuroAccess.Nfc.TravelDocuments;
using Waher.Content.Xml;
using Waher.Networking;

namespace NeuroAccess.Nfc
{
	/// <summary>
	/// Class implementing the <see cref="IIsoDepInterface"/> interface for serial communication with
	/// an NFC chip, based on replaying recorded IsoDep communication.
	/// </summary>
	public class IsoDepReplay : IIsoDepInterface
	{
		private readonly XmlDocument replayXml;
		private readonly IEnumerator replayEnumerator;
		private readonly DocumentInformation docInfo;
		private readonly string mrz;
		private bool eof = false;
		private bool disposed = false;

		/// <summary>
		/// Class implementing the <see cref="IIsoDepInterface"/> interface for serial communication with
		/// an NFC chip, based on replaying recorded IsoDep communication.
		/// </summary>
		/// <param name="ReplayFileName">File name of XML sniffer file to replay.</param>
		public IsoDepReplay(string ReplayFileName)
			: this(XML.LoadFromFile(ReplayFileName))
		{
		}

		/// <summary>
		/// Class implementing the <see cref="IIsoDepInterface"/> interface for serial communication with
		/// an NFC chip, based on replaying recorded IsoDep communication.
		/// </summary>
		/// <param name="ReplayXml">XML document containing recorded IsoDep communication to replay.</param>
		public IsoDepReplay(XmlDocument ReplayXml)
		{
			if (ReplayXml?.DocumentElement is null)
				throw new ArgumentNullException(nameof(ReplayXml), "Missing XML.");

			if (ReplayXml.DocumentElement.Name != "SnifferOutput" ||
				ReplayXml.DocumentElement.NamespaceURI != "http://waher.se/Schema/SnifferOutput.xsd")
			{
				throw new ArgumentException("Not a sniffer output XML file.", nameof(ReplayXml));
			}

			this.replayXml = ReplayXml;
			this.replayEnumerator = this.replayXml.DocumentElement.GetEnumerator();

			if (!this.replayEnumerator.MoveNext() ||
				this.replayEnumerator.Current is not XmlElement E ||
				E.LocalName != "Info")
			{
				throw new ArgumentException("Not an NFC replay sniffer output XML file.", nameof(ReplayXml));
			}

			this.mrz = GetRows(E).Replace("\r\n", "\n").Replace('\r', '\n').Trim();

			if (!MrzExtensions.ParseMrz(this.mrz, out DocumentInformation? DocInfo))
				throw new ArgumentException("Invalid Document Information in MRZ in NFC replay sniffer output XML file.", nameof(ReplayXml));

			this.docInfo = DocInfo;
		}

		/// <summary>
		/// MRZ field found in replay file.
		/// </summary>
		public string Mrz => this.mrz;

		/// <summary>
		/// Parsed document information, based on the MRZ field found in replay file.
		/// </summary>
		public DocumentInformation DocumentInfo => this.docInfo;

		/// <summary>
		/// NFC Tag
		/// </summary>
		public INfcTag? Tag => null;

		/// <summary>
		/// Connects the interface, if not connected.
		/// </summary>
		/// <returns></returns>
		public Task OpenIfClosed()
		{
			this.replayEnumerator.Reset();
			return Task.CompletedTask;
		}

		/// <summary>
		/// Closes the interface, if connected.
		/// </summary>
		public void CloseIfOpen() { }

		/// <summary>
		/// Return the higher layer response bytes for NfcB tags.
		/// </summary>
		public Task<byte[]> GetHighLayerResponse() => Task.FromResult<byte[]>([]);

		/// <summary>
		/// Return the ISO-DEP historical bytes for NfcA tags.
		/// </summary>
		public Task<byte[]> GetHistoricalBytes() => Task.FromResult<byte[]>([]);

		/// <summary>
		/// Sets communication timeout.
		/// </summary>
		/// <param name="Timeout">Timeout, in milliseconds.</param>
		public void SetTimeout(int Timeout) { }

		/// <summary>
		/// Disposes the object.
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
			if (this.disposed)
				return;

			if (Disposing)
				this.CloseIfOpen();

			this.disposed = true;
		}

		/// <summary>
		/// Executes an ISO 14443-4 command on the tag.
		/// </summary>
		/// <param name="Command">Command</param>
		/// <param name="CommunicationLayer">Communication Layer</param>
		/// <returns>Response</returns>
		public Task<byte[]> ExecuteCommand(byte[] Command, ICommunicationLayer CommunicationLayer)
		{
			CommunicationLayer.TransmitBinary(false, Command);

			if (this.eof)
				throw Error("End of replay reached.", CommunicationLayer);

			while (this.replayEnumerator.MoveNext())
			{
				if (this.replayEnumerator.Current is XmlElement E &&
					E.LocalName == "Tx" &&
					AreSame(Command, GetBin(E)))
				{
					while (this.replayEnumerator.MoveNext())
					{
						if (this.replayEnumerator.Current is XmlElement E2 &&
							E2.LocalName == "Rx")
						{
							return Task.FromResult(GetBin(E2));
						}
					}
				}
			}

			this.eof = true;
			throw Error("Command not found in replay.", CommunicationLayer);
		}

		public string GetInfo(string Prefix, ICommunicationLayer CommunicationLayer)
		{
			if (this.eof)
				throw Error("End of replay reached.", CommunicationLayer);

			while (this.replayEnumerator.MoveNext())
			{
				if (this.replayEnumerator.Current is XmlElement E &&
					E.LocalName == "Info")
				{
					string Info = GetRows(E, true);

					if (Info.StartsWith(Prefix, StringComparison.InvariantCultureIgnoreCase))
						return Info[Prefix.Length..].Trim();
				}
			}

			throw Error("Information not found: " + Prefix, CommunicationLayer);
		}

		private static byte[] GetBin(XmlElement E)
		{
			return Convert.FromBase64String(GetRows(E, false));
		}

		private static string GetRows(XmlElement E)
		{
			return GetRows(E, true);
		}

		private static string GetRows(XmlElement E, bool MultiRow)
		{
			StringBuilder sb = new();

			foreach (XmlNode N in E.ChildNodes)
			{
				if (N is XmlElement E2 && E2.LocalName == "Row")
				{
					sb.Append(E2.InnerText);
					if (MultiRow)
						sb.AppendLine();
				}
			}

			return sb.ToString();
		}

		private static bool AreSame(byte[] Bin1, byte[] Bin2)
		{
			int i, c = Bin1.Length;

			if (Bin2.Length != c)
				return false;

			for (i = 0; i < c; i++)
			{
				if (Bin1[i] != Bin2[i])
					return false;
			}

			return true;
		}

		private static Exception Error(string Message, ICommunicationLayer CommunicationLayer)
		{
			CommunicationLayer.Error(Message);
			return new Exception(Message);
		}

	}
}
