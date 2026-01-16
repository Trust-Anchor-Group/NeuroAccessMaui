using System;
using System.Threading;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;

#if IOS
using CoreFoundation;
using CoreNFC;
using Foundation;
#endif

namespace NeuroAccessMaui.Services.Nfc
{
    /// <summary>
    /// Default NFC scan service.
    /// </summary>
    /// <remarks>
    /// On Android, this service waits for the app's NFC intent pipeline to detect a tag and then completes the pending scan.
    /// On iOS, this service creates an <see cref="NFCNdefReaderSession"/> and extracts a URI from NDEF records.
    /// </remarks>
    [Singleton]
    public class NfcScanService : INfcScanService
    {
        private readonly object gate = new();
        private TaskCompletionSource<string?>? pendingScan;
        private string[]? allowedSchemes;

        /// <inheritdoc />
        public bool HasActiveScan
        {
            get
            {
                lock (this.gate)
                {
                    return this.pendingScan is not null;
                }
            }
        }

        /// <inheritdoc />
        public Task<string?> ScanUriAsync(string Prompt, string[]? AllowedSchemes, CancellationToken CancellationToken)
        {
#if IOS
			return this.ScanUriIosAsync(Prompt, AllowedSchemes, CancellationToken);
#else
            return this.ScanUriPassiveAsync(AllowedSchemes, CancellationToken);
#endif
        }

        /// <inheritdoc />
        public bool TryHandleDetectedUri(string Uri)
        {
            if (string.IsNullOrWhiteSpace(Uri))
                return false;

            TaskCompletionSource<string?>? Pending;
            string[]? Allowed;
            lock (this.gate)
            {
                Pending = this.pendingScan;
                Allowed = this.allowedSchemes;
                if (Pending is null)
                    return false;
            }

            if (!this.IsAllowed(Uri, Allowed))
                return false;

            lock (this.gate)
            {
                if (this.pendingScan is null)
                    return false;

                TaskCompletionSource<string?> TaskSource = this.pendingScan;
                this.pendingScan = null;
                this.allowedSchemes = null;
                TaskSource.TrySetResult(Uri);
                return true;
            }
        }

        private Task<string?> ScanUriPassiveAsync(string[]? AllowedSchemes, CancellationToken CancellationToken)
        {
            TaskCompletionSource<string?> TaskSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

            lock (this.gate)
            {
                this.pendingScan?.TrySetResult(null);
                this.pendingScan = TaskSource;
                this.allowedSchemes = AllowedSchemes;
            }

            if (CancellationToken.CanBeCanceled)
            {
                CancellationToken.Register(() =>
                {
                    lock (this.gate)
                    {
                        if (this.pendingScan == TaskSource)
                        {
                            this.pendingScan = null;
                            this.allowedSchemes = null;
                            TaskSource.TrySetResult(null);
                        }
                    }
                });
            }

            return TaskSource.Task;
        }

        private bool IsAllowed(string UriString, string[]? AllowedSchemes)
        {
            if (!System.Uri.TryCreate(UriString.Trim(), UriKind.Absolute, out System.Uri? Uri))
                return false;

            if (AllowedSchemes is null || AllowedSchemes.Length == 0)
                return true;

            foreach (string Scheme in AllowedSchemes)
            {
                if (string.Equals(Uri.Scheme, Scheme, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

#if IOS
		private Task<string?> ScanUriIosAsync(string Prompt, string[]? AllowedSchemes, CancellationToken CancellationToken)
		{
			if (!NFCNdefReaderSession.ReadingAvailable)
				return Task.FromResult<string?>(null);

			TaskCompletionSource<string?> TaskSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

			NdefSessionDelegate Delegate = new(this, TaskSource, AllowedSchemes);
			NFCNdefReaderSession Session = new(Delegate, DispatchQueue.MainQueue, true)
			{
				AlertMessage = Prompt ?? string.Empty
			};

			Delegate.SetSession(Session);

			if (CancellationToken.CanBeCanceled)
			{
				CancellationToken.Register(() =>
				{
					try { Session.InvalidateSession(); } catch { }
					TaskSource.TrySetResult(null);
				});
			}

			Session.BeginSession();
			return TaskSource.Task;
		}

		private sealed class NdefSessionDelegate : NFCNdefReaderSessionDelegate
		{
			private readonly NfcScanService owner;
			private readonly TaskCompletionSource<string?> taskSource;
			private readonly string[]? allowedSchemes;
			private NFCNdefReaderSession? session;

			public NdefSessionDelegate(NfcScanService Owner, TaskCompletionSource<string?> TaskSource, string[]? AllowedSchemes)
			{
				this.owner = Owner;
				this.taskSource = TaskSource;
				this.allowedSchemes = AllowedSchemes;
			}

			public void SetSession(NFCNdefReaderSession Session)
			{
				this.session = Session;
			}

			public override void DidInvalidate(NFCNdefReaderSession Session, NSError Error)
			{
				this.taskSource.TrySetResult(null);
			}

			public override void DidDetect(NFCNdefReaderSession Session, NFCNdefMessage[] Messages)
			{
				try
				{
					foreach (NFCNdefMessage Message in Messages)
					{
						foreach (NFCNdefPayload Record in Message.Records)
						{
							string? Uri = TryExtractUri(Record);
							if (string.IsNullOrWhiteSpace(Uri))
								continue;

							if (!this.owner.IsAllowed(Uri, this.allowedSchemes))
								continue;

							this.taskSource.TrySetResult(Uri);
							try { Session.InvalidateSession(); } catch { }
							return;
						}
					}
				}
				catch
				{
					this.taskSource.TrySetResult(null);
				}
			}

			private static string? TryExtractUri(NFCNdefPayload Record)
			{
				if (Record is null)
					return null;
				if (Record.TypeNameFormat != NFCTypeNameFormat.NFCWellKnown)
					return null;

				string Type = NSString.FromData(Record.Type, NSStringEncoding.UTF8);
				NSData Payload = Record.Payload;
				byte[] PayloadBytes = Payload?.ToArray() ?? Array.Empty<byte>();

				if (string.Equals(Type, "U", StringComparison.OrdinalIgnoreCase))
					return DecodeWellKnownUri(PayloadBytes);

				if (string.Equals(Type, "T", StringComparison.OrdinalIgnoreCase))
					return DecodeWellKnownTextAsUri(PayloadBytes);

				return null;
			}

			private static string? DecodeWellKnownUri(byte[] Payload)
			{
				if (Payload is null || Payload.Length == 0)
					return null;

				int PrefixCode = Payload[0];
				string Prefix = PrefixCode < UriPrefixes.Length ? UriPrefixes[PrefixCode] : string.Empty;
				string Suffix = System.Text.Encoding.UTF8.GetString(Payload, 1, Payload.Length - 1);
				string Uri = (Prefix + Suffix).Trim();

				return System.Uri.TryCreate(Uri, UriKind.Absolute, out _) ? Uri : null;
			}

			private static string? DecodeWellKnownTextAsUri(byte[] Payload)
			{
				if (Payload is null || Payload.Length < 2)
					return null;

				byte Status = Payload[0];
				int LanguageCodeLength = Status & 0x3F;
				bool IsUtf16 = (Status & 0x80) != 0;

				int TextIndex = 1 + LanguageCodeLength;
				if (TextIndex >= Payload.Length)
					return null;

				System.Text.Encoding Encoding = IsUtf16 ? System.Text.Encoding.Unicode : System.Text.Encoding.UTF8;
				string Text = Encoding.GetString(Payload, TextIndex, Payload.Length - TextIndex).Trim();

				return System.Uri.TryCreate(Text, UriKind.Absolute, out _) ? Text : null;
			}

			private static readonly string[] UriPrefixes =
			[
				string.Empty,
				"http://www.",
				"https://www.",
				"http://",
				"https://",
				"tel:",
				"mailto:",
				"ftp://anonymous:anonymous@",
				"ftp://ftp.",
				"ftps://",
				"sftp://",
				"smb://",
				"nfs://",
				"ftp://",
				"dav://",
				"news:",
				"telnet://",
				"imap:",
				"rtsp://",
				"urn:",
				"pop:",
				"sip:",
				"sips:",
				"tftp:",
				"btspp://",
				"btl2cap://",
				"btgoep://",
				"tcpobex://",
				"irdaobex://",
				"file://",
				"urn:epc:id:",
				"urn:epc:tag:",
				"urn:epc:pat:",
				"urn:epc:raw:",
				"urn:epc:",
				"urn:nfc:"
			];
		}
#endif
    }
}
