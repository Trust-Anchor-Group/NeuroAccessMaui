using NeuroAccess.Nfc;

namespace NeuroAccessMaui.Services.Nfc
{
	/// <summary>
	/// Specifies the preferred polling mode for an ISO-DEP NFC reader session.
	/// </summary>
	public enum NfcIsoDepPollingPreference
	{
		/// <summary>
		/// Lets the platform service choose the default polling mode.
		/// </summary>
		Auto,

		/// <summary>
		/// Uses standard ISO 14443 tag polling.
		/// </summary>
		Iso14443,

		/// <summary>
		/// Uses iOS PACE polling when the platform supports it.
		/// </summary>
		Pace
	}

	/// <summary>
	/// Identifies a low-level ISO-DEP NFC session failure.
	/// </summary>
	public enum NfcIsoDepSessionFailureCode
	{
		/// <summary>
		/// The platform does not support the requested ISO-DEP session flow.
		/// </summary>
		NotSupported,

		/// <summary>
		/// NFC is unavailable because the radio or runtime configuration is disabled.
		/// </summary>
		Unavailable,

		/// <summary>
		/// The active session was cancelled by the user or system.
		/// </summary>
		Cancelled,

		/// <summary>
		/// The active session timed out before completion.
		/// </summary>
		TimedOut,

		/// <summary>
		/// The platform session failed unexpectedly.
		/// </summary>
		SessionFailed,

		/// <summary>
		/// More than one NFC tag was detected.
		/// </summary>
		MultipleTagsDetected,

		/// <summary>
		/// The detected NFC tag does not expose a usable ISO-DEP interface.
		/// </summary>
		UnsupportedTag,

		/// <summary>
		/// The connection to the detected tag was lost.
		/// </summary>
		ConnectionLost,

		/// <summary>
		/// A required platform entitlement or permission is missing.
		/// </summary>
		EntitlementMissing
	}

	/// <summary>
	/// Represents a normalized low-level ISO-DEP session failure.
	/// </summary>
	public sealed class NfcIsoDepSessionFailure
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NfcIsoDepSessionFailure"/> class.
		/// </summary>
		/// <param name="FailureCode">The normalized failure code.</param>
		/// <param name="NativeErrorDomain">The native error domain, if available.</param>
		/// <param name="NativeErrorCode">The native error code, if available.</param>
		/// <param name="NativeErrorName">The native error name, if available.</param>
		/// <param name="NativeErrorMessage">A sanitized native error message, if available.</param>
		public NfcIsoDepSessionFailure(
			NfcIsoDepSessionFailureCode FailureCode,
			string? NativeErrorDomain = null,
			long? NativeErrorCode = null,
			string? NativeErrorName = null,
			string? NativeErrorMessage = null)
		{
			this.FailureCode = FailureCode;
			this.NativeErrorDomain = NativeErrorDomain;
			this.NativeErrorCode = NativeErrorCode;
			this.NativeErrorName = NativeErrorName;
			this.NativeErrorMessage = NativeErrorMessage;
		}

		/// <summary>
		/// Gets the normalized failure code.
		/// </summary>
		public NfcIsoDepSessionFailureCode FailureCode { get; }

		/// <summary>
		/// Gets the native error domain, if available.
		/// </summary>
		public string? NativeErrorDomain { get; }

		/// <summary>
		/// Gets the native error code, if available.
		/// </summary>
		public long? NativeErrorCode { get; }

		/// <summary>
		/// Gets the native error name, if available.
		/// </summary>
		public string? NativeErrorName { get; }

		/// <summary>
		/// Gets a sanitized native error message, if available.
		/// </summary>
		public string? NativeErrorMessage { get; }
	}

	/// <summary>
	/// Provides platform-specific ISO-DEP NFC session handling.
	/// </summary>
	public interface INfcIsoDepSessionService
	{
		/// <summary>
		/// Occurs when the active ISO-DEP session state changes.
		/// </summary>
		event EventHandler? ActiveSessionChanged;

		/// <summary>
		/// Gets a value indicating whether the platform supports ISO-DEP reader sessions.
		/// </summary>
		bool IsPlatformSupported { get; }

		/// <summary>
		/// Gets a value indicating whether an ISO-DEP session is currently active.
		/// </summary>
		bool HasActiveSession { get; }

		/// <summary>
		/// Gets a value indicating whether generic NFC dispatch should pause while no ISO-DEP session is active.
		/// </summary>
		bool SuppressGenericDispatch { get; }

		/// <summary>
		/// Sets whether generic NFC dispatch should pause while no ISO-DEP session is active.
		/// </summary>
		/// <param name="Suppress">If generic NFC dispatch should be suppressed.</param>
		/// <param name="CancellationToken">The cancellation token.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		Task SetGenericDispatchSuppressedAsync(bool Suppress, CancellationToken CancellationToken);

		/// <summary>
		/// Starts an ISO-DEP reader session.
		/// </summary>
		/// <param name="SessionId">The logical session identifier.</param>
		/// <param name="IsoDepHandler">The callback invoked when an ISO-DEP transport is detected.</param>
		/// <param name="FailureHandler">The callback invoked for normalized low-level session failures.</param>
		/// <param name="PollingPreference">The preferred NFC polling mode.</param>
		/// <param name="AlertMessage">The platform guidance shown when the NFC session begins.</param>
		/// <param name="CancellationToken">The cancellation token.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		Task StartSessionAsync(
			Guid SessionId,
			Func<IIsoDepInterface, CancellationToken, Task> IsoDepHandler,
			Func<NfcIsoDepSessionFailure, CancellationToken, Task> FailureHandler,
			NfcIsoDepPollingPreference PollingPreference,
			string AlertMessage,
			CancellationToken CancellationToken);

		/// <summary>
		/// Updates platform guidance for an active ISO-DEP reader session.
		/// </summary>
		/// <param name="SessionId">The logical session identifier.</param>
		/// <param name="AlertMessage">The platform guidance to show.</param>
		/// <param name="CancellationToken">The cancellation token.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		Task UpdateSessionAlertAsync(Guid SessionId, string AlertMessage, CancellationToken CancellationToken);

		/// <summary>
		/// Stops the active ISO-DEP session for the supplied identifier.
		/// </summary>
		/// <param name="SessionId">The logical session identifier.</param>
		/// <param name="CancellationToken">The cancellation token.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		Task StopSessionAsync(Guid SessionId, CancellationToken CancellationToken);

		/// <summary>
		/// Tries to consume a detected shared NFC tag for the active ISO-DEP session.
		/// </summary>
		/// <param name="Tag">The shared NFC tag.</param>
		/// <param name="CancellationToken">The cancellation token.</param>
		/// <returns><c>true</c> if the active session consumed the tag; otherwise, <c>false</c>.</returns>
		Task<bool> TryHandleTagAsync(INfcTag Tag, CancellationToken CancellationToken);
	}

	/// <summary>
	/// Fallback ISO-DEP session service for unsupported platforms.
	/// </summary>
	public sealed class DefaultNfcIsoDepSessionService : INfcIsoDepSessionService
	{
		/// <inheritdoc/>
		public event EventHandler? ActiveSessionChanged
		{
			add { }
			remove { }
		}

		/// <inheritdoc/>
		public bool IsPlatformSupported => false;

		/// <inheritdoc/>
		public bool HasActiveSession => false;

		/// <inheritdoc/>
		public bool SuppressGenericDispatch => false;

		/// <inheritdoc/>
		public Task SetGenericDispatchSuppressedAsync(bool Suppress, CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();
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

			return FailureHandler(new NfcIsoDepSessionFailure(NfcIsoDepSessionFailureCode.NotSupported), CancellationToken);
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
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public Task<bool> TryHandleTagAsync(INfcTag Tag, CancellationToken CancellationToken)
		{
			ArgumentNullException.ThrowIfNull(Tag);
			CancellationToken.ThrowIfCancellationRequested();
			return Task.FromResult(false);
		}
	}
}
