using System.Threading;
using CoreNFC;
using Foundation;
using CoreFoundation;
using Microsoft.Maui.ApplicationModel;
using NeuroAccess.Nfc;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Nfc;

namespace NeuroAccessMaui.Platforms.iOS.Nfc
{
	/// <summary>
	/// iOS-specific ISO-DEP NFC session service backed by CoreNFC tag reader sessions.
	/// </summary>
	public sealed class IosNfcIsoDepSessionService : INfcIsoDepSessionService
	{
		private readonly object syncObject = new object();
		private SessionContext? activeSessionContext;

		/// <summary>
		/// Initializes a new instance of the <see cref="IosNfcIsoDepSessionService"/> class.
		/// </summary>
		public IosNfcIsoDepSessionService()
		{
		}

		/// <inheritdoc/>
		public event EventHandler? ActiveSessionChanged;

		/// <inheritdoc/>
		public bool IsPlatformSupported => true;

		/// <inheritdoc/>
		public bool HasActiveSession
		{
			get
			{
				lock (this.syncObject)
				{
					return this.activeSessionContext is not null;
				}
			}
		}

		/// <inheritdoc/>
		public bool SuppressGenericDispatch => false;

		/// <inheritdoc/>
		public Task SetGenericDispatchSuppressedAsync(bool Suppress, CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public async Task StartSessionAsync(
			Guid SessionId,
			Func<IIsoDepInterface, CancellationToken, Task> IsoDepHandler,
			Func<NfcIsoDepSessionFailure, CancellationToken, Task> FailureHandler,
			NfcIsoDepPollingPreference PollingPreference,
			string AlertMessage,
			CancellationToken CancellationToken)
		{
			ArgumentNullException.ThrowIfNull(IsoDepHandler);
			ArgumentNullException.ThrowIfNull(FailureHandler);
			ArgumentException.ThrowIfNullOrWhiteSpace(AlertMessage);
			CancellationToken.ThrowIfCancellationRequested();

			if (!NFCReaderSession.ReadingAvailable)
			{
				await FailureHandler(new NfcIsoDepSessionFailure(NfcIsoDepSessionFailureCode.Unavailable), CancellationToken);
				return;
			}

			SessionContext Context = new SessionContext(
				SessionId,
				IsoDepHandler,
				FailureHandler,
				new CancellationTokenSource(),
				new TagReaderSessionDelegate(this, SessionId));

			await this.ReplaceActiveContextAsync(Context, CancellationToken);

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				Context.ReaderSession = new NFCTagReaderSession(
					this.ResolvePollingOption(PollingPreference),
					Context.Delegate,
					DispatchQueue.MainQueue);
				Context.ReaderSession.AlertMessage = AlertMessage;
				Context.ReaderSession.BeginSession();
			});
		}

		/// <inheritdoc/>
		public async Task UpdateSessionAlertAsync(Guid SessionId, string AlertMessage, CancellationToken CancellationToken)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(AlertMessage);
			CancellationToken.ThrowIfCancellationRequested();

			SessionContext? Context = this.GetActiveContext(SessionId);
			if (Context?.ReaderSession is null)
				return;

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				Context.ReaderSession.AlertMessage = AlertMessage;
			});
		}

		/// <inheritdoc/>
		public async Task StopSessionAsync(Guid SessionId, CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();

			SessionContext? Context = this.DetachActiveContext(SessionId);
			if (Context is null)
				return;

			this.ActiveSessionChanged?.Invoke(this, EventArgs.Empty);
			await this.InvalidateSessionAsync(Context, CancellationToken);
		}

		/// <inheritdoc/>
		public Task<bool> TryHandleTagAsync(INfcTag Tag, CancellationToken CancellationToken)
		{
			ArgumentNullException.ThrowIfNull(Tag);
			CancellationToken.ThrowIfCancellationRequested();
			return Task.FromResult(false);
		}

		internal Task HandleSessionActivatedAsync(Guid SessionId)
		{
			return Task.CompletedTask;
		}

		internal async Task HandleDetectedTagsAsync(Guid SessionId, NFCTagReaderSession ReaderSession, INFCTag[] Tags)
		{
			ArgumentNullException.ThrowIfNull(ReaderSession);
			ArgumentNullException.ThrowIfNull(Tags);

			SessionContext? Context = this.GetActiveContext(SessionId);
			if (Context is null)
				return;

			if (Interlocked.CompareExchange(ref Context.TagDispatchInProgress, 1, 0) != 0)
				return;

			try
			{
				if (Tags.Length != 1)
				{
					await this.CompleteWithFailureAsync(SessionId, ReaderSession, new NfcIsoDepSessionFailure(NfcIsoDepSessionFailureCode.MultipleTagsDetected));
					return;
				}

				INFCTag Tag = Tags[0];
				INFCIso7816Tag? Iso7816Tag = Tag.AsNFCIso7816Tag;
				if (Iso7816Tag is null)
				{
					await this.CompleteWithFailureAsync(SessionId, ReaderSession, new NfcIsoDepSessionFailure(NfcIsoDepSessionFailureCode.UnsupportedTag));
					return;
				}

				await ReaderSession.ConnectToAsync(Tag);
				if (this.GetActiveContext(SessionId) is null)
					return;

				IosIsoDepInterface IsoDepInterface = new IosIsoDepInterface(Tag, Iso7816Tag, Context.CancellationTokenSource.Token);
				await Context.IsoDepHandler(IsoDepInterface, Context.CancellationTokenSource.Token);
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);

				if (this.GetActiveContext(SessionId) is not null)
					await this.CompleteWithFailureAsync(SessionId, ReaderSession, new NfcIsoDepSessionFailure(NfcIsoDepSessionFailureCode.SessionFailed));
			}
			finally
			{
				Interlocked.Exchange(ref Context.TagDispatchInProgress, 0);
			}
		}

		internal async Task HandleInvalidationAsync(Guid SessionId, NSError Error)
		{
			ArgumentNullException.ThrowIfNull(Error);

			SessionContext? Context = this.DetachActiveContext(SessionId);
			if (Context is null)
				return;

			this.ActiveSessionChanged?.Invoke(this, EventArgs.Empty);
			this.CancelContext(Context);
			this.DisposeContext(Context);

			NfcIsoDepSessionFailure? Failure = this.MapReaderError(Error);
			if (Failure is null)
				return;

			try
			{
				await Context.FailureHandler(Failure, CancellationToken.None);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private async Task ReplaceActiveContextAsync(SessionContext NewContext, CancellationToken CancellationToken)
		{
			SessionContext? PreviousContext;
			lock (this.syncObject)
			{
				PreviousContext = this.activeSessionContext;
				this.activeSessionContext = NewContext;
			}

			if (PreviousContext is not null)
				await this.InvalidateSessionAsync(PreviousContext, CancellationToken);

			this.ActiveSessionChanged?.Invoke(this, EventArgs.Empty);
		}

		private SessionContext? GetActiveContext(Guid SessionId)
		{
			lock (this.syncObject)
			{
				return this.activeSessionContext?.SessionId == SessionId ? this.activeSessionContext : null;
			}
		}

		private SessionContext? DetachActiveContext(Guid SessionId)
		{
			lock (this.syncObject)
			{
				if (this.activeSessionContext?.SessionId != SessionId)
					return null;

				SessionContext Context = this.activeSessionContext;
				this.activeSessionContext = null;
				return Context;
			}
		}

		private async Task CompleteWithFailureAsync(Guid SessionId, NFCTagReaderSession ReaderSession, NfcIsoDepSessionFailure Failure)
		{
			SessionContext? Context = this.DetachActiveContext(SessionId);
			if (Context is null)
				return;

			this.ActiveSessionChanged?.Invoke(this, EventArgs.Empty);
			this.CancelContext(Context);

			await MainThread.InvokeOnMainThreadAsync(ReaderSession.InvalidateSession);
			this.DisposeContext(Context);

			try
			{
				await Context.FailureHandler(Failure, CancellationToken.None);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private async Task InvalidateSessionAsync(SessionContext Context, CancellationToken CancellationToken)
		{
			ArgumentNullException.ThrowIfNull(Context);
			CancellationToken.ThrowIfCancellationRequested();

			this.CancelContext(Context);

			if (Context.ReaderSession is not null)
			{
				NFCTagReaderSession ReaderSession = Context.ReaderSession;
				try
				{
					await MainThread.InvokeOnMainThreadAsync(ReaderSession.InvalidateSession);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}
			}

			this.DisposeContext(Context);
		}

		private void CancelContext(SessionContext Context)
		{
			try
			{
				Context.CancellationTokenSource.Cancel();
			}
			catch (ObjectDisposedException)
			{
			}
		}

		private void DisposeContext(SessionContext Context)
		{
			Context.CancellationTokenSource.Dispose();
		}

		private NfcIsoDepSessionFailure MapReaderError(NSError Error)
		{
			NFCReaderError ReaderError = (NFCReaderError)(long)Error.Code;
			NfcIsoDepSessionFailureCode FailureCode = ReaderError switch
			{
				NFCReaderError.ReaderSessionInvalidationErrorUserCanceled => NfcIsoDepSessionFailureCode.Cancelled,
				NFCReaderError.ReaderSessionInvalidationErrorSessionTimeout => NfcIsoDepSessionFailureCode.TimedOut,
				NFCReaderError.ReaderTransceiveErrorTagConnectionLost => NfcIsoDepSessionFailureCode.ConnectionLost,
				NFCReaderError.ReaderTransceiveErrorTagNotConnected => NfcIsoDepSessionFailureCode.ConnectionLost,
				NFCReaderError.ReaderTransceiveErrorRetryExceeded => NfcIsoDepSessionFailureCode.ConnectionLost,
				NFCReaderError.RadioDisabled => NfcIsoDepSessionFailureCode.Unavailable,
				NFCReaderError.UnsupportedFeature => NfcIsoDepSessionFailureCode.NotSupported,
				NFCReaderError.SecurityViolation => NfcIsoDepSessionFailureCode.EntitlementMissing,
				NFCReaderError.AccessNotAccepted => NfcIsoDepSessionFailureCode.EntitlementMissing,
				NFCReaderError.ReaderSessionInvalidationErrorSessionTerminatedUnexpectedly => NfcIsoDepSessionFailureCode.SessionFailed,
				NFCReaderError.ReaderSessionInvalidationErrorSystemIsBusy => NfcIsoDepSessionFailureCode.SessionFailed,
				NFCReaderError.ReaderTransceiveErrorSessionInvalidated => NfcIsoDepSessionFailureCode.SessionFailed,
				_ => NfcIsoDepSessionFailureCode.SessionFailed
			};

			return new NfcIsoDepSessionFailure(
				FailureCode,
				Error.Domain,
				(long)Error.Code,
				ReaderError.ToString(),
				Error.LocalizedDescription);
		}

		private NFCPollingOption ResolvePollingOption(NfcIsoDepPollingPreference PollingPreference)
		{
			// iOS 16+ requires PACE polling for some PACE travel documents, but Apple treats
			// it as an exclusive mode that cannot be combined with standard Iso14443 polling.
			// Reference: https://github.com/AndyQ/NFCPassportReader/issues/164
			return PollingPreference switch
			{
				NfcIsoDepPollingPreference.Pace when OperatingSystem.IsIOSVersionAtLeast(16) => IosNfcIsoDepSessionService.GetPacePollingOption(),
				_ => NFCPollingOption.Iso14443
			};
		}

		[System.Runtime.Versioning.SupportedOSPlatform("ios16.0")]
		private static NFCPollingOption GetPacePollingOption()
		{
			return NFCPollingOption.Pace;
		}

		private sealed class SessionContext
		{
			public SessionContext(
				Guid SessionId,
				Func<IIsoDepInterface, CancellationToken, Task> IsoDepHandler,
				Func<NfcIsoDepSessionFailure, CancellationToken, Task> FailureHandler,
				CancellationTokenSource CancellationTokenSource,
				TagReaderSessionDelegate Delegate)
			{
				this.SessionId = SessionId;
				this.IsoDepHandler = IsoDepHandler;
				this.FailureHandler = FailureHandler;
				this.CancellationTokenSource = CancellationTokenSource;
				this.Delegate = Delegate;
			}

			public Guid SessionId { get; }

			public Func<IIsoDepInterface, CancellationToken, Task> IsoDepHandler { get; }

			public Func<NfcIsoDepSessionFailure, CancellationToken, Task> FailureHandler { get; }

			public CancellationTokenSource CancellationTokenSource { get; }

			public TagReaderSessionDelegate Delegate { get; }

			public NFCTagReaderSession? ReaderSession { get; set; }

			public int TagDispatchInProgress;
		}

		private sealed class TagReaderSessionDelegate : NFCTagReaderSessionDelegate
		{
			private readonly IosNfcIsoDepSessionService owner;
			private readonly Guid sessionId;

			public TagReaderSessionDelegate(IosNfcIsoDepSessionService Owner, Guid SessionId)
			{
				ArgumentNullException.ThrowIfNull(Owner);

				this.owner = Owner;
				this.sessionId = SessionId;
			}

			public override async void DidBecomeActive(NFCTagReaderSession Session)
			{
				await this.owner.HandleSessionActivatedAsync(this.sessionId);
			}

			public override async void DidDetectTags(NFCTagReaderSession Session, INFCTag[] Tags)
			{
				if (Session is NFCTagReaderSession TagReaderSession)
					await this.owner.HandleDetectedTagsAsync(this.sessionId, TagReaderSession, Tags);
			}

			public override async void DidInvalidate(NFCTagReaderSession Session, NSError Error)
			{
				await this.owner.HandleInvalidationAsync(this.sessionId, Error);
			}
		}
	}
}
