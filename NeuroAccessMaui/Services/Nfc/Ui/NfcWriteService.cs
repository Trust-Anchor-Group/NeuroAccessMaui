using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;

#if IOS
using CoreFoundation;
using CoreNFC;
using Foundation;
using Microsoft.Extensions.DependencyInjection;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
#endif

namespace NeuroAccessMaui.Services.Nfc.Ui
{
	/// <summary>
	/// Default implementation of <see cref="INfcWriteService"/>.
	/// </summary>
	[Singleton]
	public sealed class NfcWriteService : INfcWriteService
	{
		private readonly object gate = new();
		private TaskCompletionSource<bool>? pendingWrite;
		private object[]? pendingItems;
#if IOS
		private readonly INfcTagSnapshotService nfcTagSnapshotService = ServiceRef.Provider.GetRequiredService<INfcTagSnapshotService>();
		private NFCTagReaderSession? iosSession;
#endif

		/// <inheritdoc />
		public bool HasPendingWrite
		{
			get
			{
				lock (this.gate)
				{
					return this.pendingWrite is not null;
				}
			}
		}

		/// <inheritdoc />
		public Task<bool> WriteUriAsync(string Uri, CancellationToken CancellationToken)
		{
			if (!System.Uri.TryCreate(Uri?.Trim(), UriKind.Absolute, out System.Uri? Parsed))
				return Task.FromResult(false);

			return this.BeginWrite([Parsed], CancellationToken);
		}

		/// <inheritdoc />
		public Task<bool> WriteTextAsync(string Text, CancellationToken CancellationToken)
		{
			string Value = Text?.Trim() ?? string.Empty;
			if (string.IsNullOrEmpty(Value))
				return Task.FromResult(false);

			return this.BeginWrite([Value], CancellationToken);
		}

		private Task<bool> BeginWrite(object[] Items, CancellationToken CancellationToken)
		{
			TaskCompletionSource<bool> TaskSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

			lock (this.gate)
			{
				this.pendingWrite?.TrySetResult(false);
				this.pendingWrite = TaskSource;
				this.pendingItems = Items;
			}

#if IOS
			this.StartWriteSessionIos(TaskSource);
#endif

			if (CancellationToken.CanBeCanceled)
			{
				CancellationToken.Register(() =>
				{
					bool ShouldCancel = false;
					lock (this.gate)
					{
						if (this.pendingWrite == TaskSource)
						{
							this.pendingWrite = null;
							this.pendingItems = null;
							ShouldCancel = true;
						}
					}

					if (ShouldCancel)
					{
#if IOS
						this.CancelIosSession();
#endif
						TaskSource.TrySetResult(false);
					}
				});
			}

			return TaskSource.Task;
		}

		/// <inheritdoc />
		public async Task<bool> TryHandleWriteAsync(NfcService.WriteItems WriteCallback)
		{
			TaskCompletionSource<bool>? TaskSource;
			object[]? Items;

			lock (this.gate)
			{
				TaskSource = this.pendingWrite;
				Items = this.pendingItems;
				if (TaskSource is null || Items is null)
					return false;

				this.pendingWrite = null;
				this.pendingItems = null;
			}

			try
			{
				bool Ok = await WriteCallback(Items);
				TaskSource.TrySetResult(Ok);
				return true;
			}
			catch
			{
				TaskSource.TrySetResult(false);
				return true;
			}
		}

		/// <inheritdoc />
		public void CancelPendingWrite()
		{
#if IOS
			this.CancelIosSession();
#endif
			lock (this.gate)
			{
				this.pendingWrite?.TrySetResult(false);
				this.pendingWrite = null;
				this.pendingItems = null;
			}
		}

#if IOS
		private void StartWriteSessionIos(TaskCompletionSource<bool> TaskSource)
		{
			if (!NFCReaderSession.ReadingAvailable)
			{
				this.TryCompletePendingWrite(TaskSource, false);
				return;
			}

			this.CancelIosSession();

			NdefWriteSessionDelegate Delegate = new(this, TaskSource);
			NFCTagReaderSession Session = new(NFCPollingOption.Iso14443 | NFCPollingOption.Iso15693, Delegate, DispatchQueue.MainQueue)
			{
				AlertMessage = ServiceRef.Localizer[nameof(AppResources.NfcTapTag)]
			};

			lock (this.gate)
			{
				this.iosSession = Session;
			}

			Session.BeginSession();
		}

		private void CancelIosSession()
		{
			NFCTagReaderSession? Session;
			lock (this.gate)
			{
				Session = this.iosSession;
				this.iosSession = null;
			}

			if (Session is null)
				return;

			try
			{
				Session.InvalidateSession();
			}
			catch
			{
			}
		}

		private void ClearIosSession(NFCTagReaderSession Session)
		{
			lock (this.gate)
			{
				if (this.iosSession == Session)
					this.iosSession = null;
			}
		}

		private void TryCompletePendingWrite(TaskCompletionSource<bool> TaskSource, bool Result)
		{
			bool ShouldComplete = false;

			lock (this.gate)
			{
				if (this.pendingWrite == TaskSource)
				{
					this.pendingWrite = null;
					this.pendingItems = null;
					ShouldComplete = true;
				}
			}

			if (ShouldComplete)
				TaskSource.TrySetResult(Result);
		}

		private async Task<bool> WriteNdefAsync(INFCNdefTag Tag, object[] Items)
		{
			NFCNdefMessage? Message = this.TryCreateNdefMessage(Items);
			if (Message is null)
				return false;

			try
			{
				(NFCNdefStatus Status, nuint _) = await this.QueryNdefStatusAsync(Tag);
				if (Status != NFCNdefStatus.ReadWrite)
					return false;

				bool Written = await this.WriteMessageAsync(Tag, Message);
				if (Written)
					this.PublishSnapshot(Message);

				return Written;
			}
			catch
			{
				return false;
			}
		}

		private NFCNdefMessage? TryCreateNdefMessage(object[] Items)
		{
			List<NFCNdefPayload> Records = new List<NFCNdefPayload>();

			foreach (object Item in Items)
			{
				if (Item is null)
					continue;

				if (Item is Uri UriItem)
					Records.Add(NFCNdefPayload.CreateWellKnownTypePayload(UriItem.AbsoluteUri));
				else if (Item is string Text)
					Records.Add(NFCNdefPayload.CreateWellKnownTypePayload(Text, NSLocale.CurrentLocale));
				else
					return null;
			}

			if (Records.Count == 0)
				return null;

			NFCNdefPayload[] RecordArray = Records.ToArray();
			return new NFCNdefMessage(RecordArray);
		}

		private Task<(NFCNdefStatus Status, nuint Capacity)> QueryNdefStatusAsync(INFCNdefTag Tag)
		{
			TaskCompletionSource<(NFCNdefStatus Status, nuint Capacity)> TaskSource =
				new TaskCompletionSource<(NFCNdefStatus Status, nuint Capacity)>(TaskCreationOptions.RunContinuationsAsynchronously);

			Tag.QueryNdefStatus((NFCNdefStatus Status, nuint Capacity, NSError Error) =>
			{
				if (Error is not null)
					TaskSource.TrySetException(new NSErrorException(Error));
				else
					TaskSource.TrySetResult((Status, Capacity));
			});

			return TaskSource.Task;
		}

		private Task<bool> WriteMessageAsync(INFCNdefTag Tag, NFCNdefMessage Message)
		{
			TaskCompletionSource<bool> TaskSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

			Tag.WriteNdef(Message, (NSError Error) =>
			{
				TaskSource.TrySetResult(Error is null);
			});

			return TaskSource.Task;
		}

		private void PublishSnapshot(NFCNdefMessage Message)
		{
			IReadOnlyList<NfcNdefRecordSnapshot> Records = this.BuildNdefRecordSnapshots(Message, out string? ExtractedUri, out string? NdefSummary);

			NfcTagSnapshot Snapshot = new NfcTagSnapshot(
				"N/A",
				DateTimeOffset.UtcNow,
				"NDEF",
				"CoreNFC",
				NdefSummary,
				ExtractedUri,
				Records);

			this.nfcTagSnapshotService.Publish(Snapshot);
		}

		private IReadOnlyList<NfcNdefRecordSnapshot> BuildNdefRecordSnapshots(
			NFCNdefMessage Message,
			out string? ExtractedUri,
			out string? NdefSummary)
		{
			List<NfcNdefRecordSnapshot> Records = new List<NfcNdefRecordSnapshot>();
			List<string> RecordTypes = new List<string>();
			ExtractedUri = null;

			NFCNdefPayload[]? Payloads = Message.Records;
			if (Payloads is null || Payloads.Length == 0)
			{
				NdefSummary = string.Empty;
				return Records;
			}

			for (int Index = 0; Index < Payloads.Length; Index++)
			{
				NFCNdefPayload Payload = Payloads[Index];
				string RecordType = Payload.TypeNameFormat.ToString();
				string RecordTnf = RecordType;
				string? WellKnownType = null;
				string? ContentType = null;
				string? ExternalType = null;
				string? Uri = null;
				string? Text = null;
				byte[]? PayloadBytes = Payload.Payload?.ToArray();
				bool IsPayloadDerived = false;

				if (Payload.TypeNameFormat == NFCTypeNameFormat.NFCWellKnown)
				{
					string? TypeValue = TryDecodeType(Payload.Type);
					if (string.Equals(TypeValue, "U", StringComparison.OrdinalIgnoreCase))
					{
						RecordType = "URI";
						RecordTnf = "Uri";
						WellKnownType = "U";
						Uri = Payload.WellKnownTypeUriPayload?.AbsoluteString;

						if (!string.IsNullOrWhiteSpace(Uri) && ExtractedUri is null)
							ExtractedUri = Uri;
					}
					else if (string.Equals(TypeValue, "T", StringComparison.OrdinalIgnoreCase))
					{
						RecordType = "Text";
						RecordTnf = "WellKnownType";
						WellKnownType = "T";
						Text = Payload.GetWellKnownTypeTextPayload(out NSLocale _);

						if (!string.IsNullOrWhiteSpace(Text) &&
							System.Uri.TryCreate(Text, UriKind.Absolute, out _) &&
							ExtractedUri is null)
						{
							ExtractedUri = Text;
						}
					}
					else
					{
						RecordType = "WellKnown";
						RecordTnf = "WellKnownType";
						WellKnownType = TypeValue;
					}
				}
				else if (Payload.TypeNameFormat == NFCTypeNameFormat.Media)
				{
					RecordType = "MIME";
					RecordTnf = "MimeType";
					ContentType = TryDecodeType(Payload.Type);
				}
				else if (Payload.TypeNameFormat == NFCTypeNameFormat.NFCExternal)
				{
					RecordType = "External";
					RecordTnf = "ExternalType";
					ExternalType = TryDecodeType(Payload.Type);
				}
				else if (Payload.TypeNameFormat == NFCTypeNameFormat.AbsoluteUri)
				{
					RecordType = "URI";
					RecordTnf = "Uri";
					Uri = TryDecodeType(Payload.Payload);
				}

				RecordTypes.Add(RecordType);
				Records.Add(new NfcNdefRecordSnapshot(
					Index,
					RecordType,
					RecordTnf,
					WellKnownType,
					ContentType,
					ExternalType,
					Uri,
					Text,
					PayloadBytes,
					IsPayloadDerived));
			}

			NdefSummary = RecordTypes.Count > 0 ? string.Join(", ", RecordTypes) : string.Empty;
			return Records;
		}

		private static string? TryDecodeType(NSData? Data)
		{
			if (Data is null)
				return null;

			try
			{
				return NSString.FromData(Data, NSStringEncoding.UTF8);
			}
			catch
			{
				return null;
			}
		}

		private sealed class NdefWriteSessionDelegate : NFCTagReaderSessionDelegate
		{
			private readonly NfcWriteService owner;
			private readonly TaskCompletionSource<bool> taskSource;
			private int completed;

			public NdefWriteSessionDelegate(NfcWriteService Owner, TaskCompletionSource<bool> TaskSource)
			{
				this.owner = Owner;
				this.taskSource = TaskSource;
			}

			public override void DidInvalidate(NFCTagReaderSession Session, NSError Error)
			{
				this.owner.ClearIosSession(Session);
				this.owner.TryCompletePendingWrite(this.taskSource, false);
			}

			public override void DidDetectTags(NFCTagReaderSession Session, INFCTag[] Tags)
			{
				if (Tags is null || Tags.Length == 0)
					return;

				if (Interlocked.Exchange(ref this.completed, 1) != 0)
					return;

				_ = this.HandleTagAsync(Session, Tags[0]);
			}

			private async Task HandleTagAsync(NFCTagReaderSession Session, INFCTag Tag)
			{
				try
				{
					await this.ConnectAsync(Session, Tag);

					INFCNdefTag? NdefTag = this.GetNdefTag(Tag);
					if (NdefTag is null)
					{
						try { Session.InvalidateSession(); } catch { }
						this.owner.TryCompletePendingWrite(this.taskSource, false);
						return;
					}

					bool Handled = await this.owner.TryHandleWriteAsync(Items => this.owner.WriteNdefAsync(NdefTag, Items));
					if (!Handled)
						this.owner.TryCompletePendingWrite(this.taskSource, false);

					try { Session.InvalidateSession(); } catch { }
				}
				catch
				{
					try { Session.InvalidateSession(); } catch { }
					this.owner.TryCompletePendingWrite(this.taskSource, false);
				}
			}

			private Task ConnectAsync(NFCTagReaderSession Session, INFCTag Tag)
			{
				TaskCompletionSource<bool> TaskSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

				Session.ConnectTo(Tag, (NSError Error) =>
				{
					if (Error is not null)
						TaskSource.TrySetException(new NSErrorException(Error));
					else
						TaskSource.TrySetResult(true);
				});

				return TaskSource.Task;
			}

			private INFCNdefTag? GetNdefTag(INFCTag Tag)
			{
				INFCNdefTag? NdefTag = Tag as INFCNdefTag;
				if (NdefTag is not null)
					return NdefTag;

				NdefTag = Tag.AsNFCMiFareTag as INFCNdefTag;
				if (NdefTag is not null)
					return NdefTag;

				NdefTag = Tag.AsNFCIso7816Tag as INFCNdefTag;
				if (NdefTag is not null)
					return NdefTag;

				NdefTag = Tag.AsNFCIso15693Tag as INFCNdefTag;
				if (NdefTag is not null)
					return NdefTag;

				NdefTag = Tag.AsNFCFeliCaTag as INFCNdefTag;
				return NdefTag;
			}
		}
#endif
	}
}
