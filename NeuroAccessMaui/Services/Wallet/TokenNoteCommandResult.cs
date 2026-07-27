namespace NeuroAccessMaui.Services.Wallet
{
	/// <summary>
	/// Identifies a token-defined action validation or submission outcome.
	/// </summary>
	public enum TokenNoteCommandStatus
	{
		/// <summary>
		/// The action is eligible and its parameters are valid.
		/// </summary>
		Ready,

		/// <summary>
		/// The server confirmed the generated update.
		/// </summary>
		Succeeded,

		/// <summary>
		/// One or more user-entered parameters are invalid.
		/// </summary>
		ValidationFailed,

		/// <summary>
		/// The action is no longer applicable to the current role or state.
		/// </summary>
		NotAvailable,

		/// <summary>
		/// An externally supplied expression failed the safety policy.
		/// </summary>
		Unsafe,

		/// <summary>
		/// The generation expression returned an unsupported result type.
		/// </summary>
		UnsupportedResult,

		/// <summary>
		/// Generation or definite protocol submission failed.
		/// </summary>
		Failed,

		/// <summary>
		/// The submission outcome could not be confirmed safely.
		/// </summary>
		OutcomeUncertain
	}

	/// <summary>
	/// Reports a safe, structured token-defined action result without performing UI work.
	/// </summary>
	public sealed class TokenNoteCommandResult
	{
		/// <summary>
		/// Initializes a token-defined action result.
		/// </summary>
		/// <param name="Status">Result status.</param>
		/// <param name="Message">Localized token-defined message, when safely available.</param>
		/// <param name="ParameterErrors">Invalid parameter names and safe error messages.</param>
		internal TokenNoteCommandResult(
			TokenNoteCommandStatus Status,
			string Message,
			IReadOnlyDictionary<string, string>? ParameterErrors = null)
		{
			this.Status = Status;
			this.Message = Message?.Trim() ?? string.Empty;
			this.ParameterErrors = ParameterErrors ??
				new Dictionary<string, string>(StringComparer.Ordinal);
		}

		/// <summary>
		/// Gets the result status.
		/// </summary>
		public TokenNoteCommandStatus Status { get; }

		/// <summary>
		/// Gets localized token-defined outcome copy, when safely available.
		/// </summary>
		public string Message { get; }

		/// <summary>
		/// Gets invalid parameter names and safe error messages.
		/// </summary>
		public IReadOnlyDictionary<string, string> ParameterErrors { get; }

		/// <summary>
		/// Gets a value indicating whether validation passed without submission.
		/// </summary>
		public bool IsReady => this.Status == TokenNoteCommandStatus.Ready;

		/// <summary>
		/// Gets a value indicating whether the server confirmed the generated update.
		/// </summary>
		public bool Succeeded => this.Status == TokenNoteCommandStatus.Succeeded;
	}
}
