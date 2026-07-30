using System.Diagnostics;
using NeuroAccess.Nfc;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Nfc;

namespace NeuroAccessMaui.AndroidPlatform.Nfc
{
	/// <summary>
	/// Android-specific ISO-DEP NFC session service.
	/// </summary>
	public sealed class AndroidNfcIsoDepSessionService : INfcIsoDepSessionService, IDisposable
	{
		private readonly object syncObject = new object();
		private readonly SemaphoreSlim dispatchLock = new SemaphoreSlim(1, 1);
		private Guid? activeSessionId;
		private Func<IIsoDepInterface, CancellationToken, Task>? isoDepHandler;
		private Func<NfcIsoDepSessionFailure, CancellationToken, Task>? failureHandler;
		private bool suppressGenericDispatch;
		private bool isDisposed;

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
					return this.activeSessionId.HasValue;
				}
			}
		}

		/// <inheritdoc/>
		public bool SuppressGenericDispatch
		{
			get
			{
				lock (this.syncObject)
				{
					return this.suppressGenericDispatch;
				}
			}
		}

		/// <inheritdoc/>
		public Task SetGenericDispatchSuppressedAsync(bool Suppress, CancellationToken CancellationToken)
		{
			ObjectDisposedException.ThrowIf(this.isDisposed, this);
			CancellationToken.ThrowIfCancellationRequested();

			bool RaiseChanged = false;
			lock (this.syncObject)
			{
				if (this.suppressGenericDispatch != Suppress)
				{
					this.suppressGenericDispatch = Suppress;
					RaiseChanged = true;
				}
			}

			if (RaiseChanged)
			{
				WriteDiagnostic($"SetGenericDispatchSuppressedAsync suppress={Suppress} service={this.GetHashCode()}.");
				this.ActiveSessionChanged?.Invoke(this, EventArgs.Empty);
			}

			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public Task StartSessionAsync(
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

			lock (this.syncObject)
			{
				this.activeSessionId = SessionId;
				this.isoDepHandler = IsoDepHandler;
				this.failureHandler = FailureHandler;
			}

			WriteDiagnostic($"StartSessionAsync service={this.GetHashCode()} sessionId={SessionId}.");
			this.ActiveSessionChanged?.Invoke(this, EventArgs.Empty);
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public Task UpdateSessionAlertAsync(Guid SessionId, string AlertMessage, CancellationToken CancellationToken)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(AlertMessage);
			CancellationToken.ThrowIfCancellationRequested();
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public Task StopSessionAsync(Guid SessionId, CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();

			bool RaiseChanged = false;
			lock (this.syncObject)
			{
				if (this.activeSessionId == SessionId)
				{
					this.activeSessionId = null;
					this.isoDepHandler = null;
					this.failureHandler = null;
					RaiseChanged = true;
				}
			}

			if (RaiseChanged)
			{
				WriteDiagnostic($"StopSessionAsync service={this.GetHashCode()} sessionId={SessionId}.");
				this.ActiveSessionChanged?.Invoke(this, EventArgs.Empty);
			}

			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public async Task<bool> TryHandleTagAsync(INfcTag Tag, CancellationToken CancellationToken)
		{
			ObjectDisposedException.ThrowIf(this.isDisposed, this);
			ArgumentNullException.ThrowIfNull(Tag);
			CancellationToken.ThrowIfCancellationRequested();

			Guid? ActiveSessionId;
			Func<IIsoDepInterface, CancellationToken, Task>? IsoDepHandler;
			Func<NfcIsoDepSessionFailure, CancellationToken, Task>? FailureHandler;

			lock (this.syncObject)
			{
				ActiveSessionId = this.activeSessionId;
				IsoDepHandler = this.isoDepHandler;
				FailureHandler = this.failureHandler;
			}

			if (!ActiveSessionId.HasValue || IsoDepHandler is null || FailureHandler is null)
				return false;

			if (!await this.dispatchLock.WaitAsync(0, CancellationToken))
				return true;

			try
			{
				IIsoDepInterface? IsoDepInterface = Tag.Interfaces.OfType<IIsoDepInterface>().FirstOrDefault();
				if (IsoDepInterface is null)
				{
					await FailureHandler(new NfcIsoDepSessionFailure(
						NfcIsoDepSessionFailureCode.UnsupportedTag,
						nameof(AndroidNfcIsoDepSessionService),
						null,
						"NoIsoDepInterface",
						"Detected tag does not expose an ISO-DEP interface."), CancellationToken);
					return true;
				}

				await IsoDepHandler(IsoDepInterface, CancellationToken);
				return true;
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await FailureHandler(new NfcIsoDepSessionFailure(
					NfcIsoDepSessionFailureCode.SessionFailed,
					nameof(AndroidNfcIsoDepSessionService),
					null,
					Ex.GetType().Name,
					Ex.Message), CancellationToken);
				return true;
			}
			finally
			{
				this.dispatchLock.Release();
			}
		}

		/// <summary>
		/// Releases resources owned by the Android NFC session service.
		/// </summary>
		public void Dispose()
		{
			if (this.isDisposed)
				return;

			lock (this.syncObject)
			{
				this.activeSessionId = null;
				this.isoDepHandler = null;
				this.failureHandler = null;
				this.suppressGenericDispatch = false;
			}

			this.dispatchLock.Dispose();
			this.isDisposed = true;
			GC.SuppressFinalize(this);
		}

		private static void WriteDiagnostic(string Message)
		{
			Debug.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] {Message}");
		}
	}
}
