using NeuroFeatures;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Wallet
{
	/// <summary>
	/// Discovers and safely executes state-machine actions that generate token updates.
	/// </summary>
	[DefaultImplementation(typeof(TokenNoteCommandService))]
	public interface ITokenNoteCommandService
	{
		/// <summary>
		/// Discovers actions applicable to the current user and live token state.
		/// </summary>
		/// <param name="Token">Authoritative token definition.</param>
		/// <param name="Language">Preferred language for token-defined copy.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		/// <returns>Applicable, safely evaluated action descriptors.</returns>
		Task<IReadOnlyList<TokenNoteCommandDescriptor>> DiscoverAsync(
			Token Token,
			string Language,
			CancellationToken CancellationToken = default);

		/// <summary>
		/// Revalidates action eligibility and parameter values without generating or submitting an update.
		/// </summary>
		/// <param name="Token">Current authoritative token.</param>
		/// <param name="Descriptor">Previously discovered action descriptor.</param>
		/// <param name="ParameterValues">User-entered parameter values keyed by parameter name.</param>
		/// <param name="Language">Preferred language for token-defined copy.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		/// <returns>A structured readiness or failure result.</returns>
		Task<TokenNoteCommandResult> ValidateAsync(
			Token Token,
			TokenNoteCommandDescriptor Descriptor,
			IReadOnlyDictionary<string, object?> ParameterValues,
			string Language,
			CancellationToken CancellationToken = default);

		/// <summary>
		/// Revalidates, generates, and submits an applicable token-defined action.
		/// </summary>
		/// <param name="Token">Current authoritative token.</param>
		/// <param name="Descriptor">Previously discovered action descriptor.</param>
		/// <param name="ParameterValues">User-entered parameter values keyed by parameter name.</param>
		/// <param name="Language">Preferred language for token-defined copy.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		/// <returns>A structured submission result.</returns>
		Task<TokenNoteCommandResult> ExecuteAsync(
			Token Token,
			TokenNoteCommandDescriptor Descriptor,
			IReadOnlyDictionary<string, object?> ParameterValues,
			string Language,
			CancellationToken CancellationToken = default);
	}
}
