namespace NeuroAccessMaui.Services.Wallet.Tokens
{
	/// <summary>
	/// A way out of a step of a token's state machine: what triggers it, and the step it leads to.
	/// </summary>
	/// <param name="Target">The step it leads to.</param>
	/// <param name="IsFailure">If it is the step taken when the action that goes with the move fails.</param>
	/// <param name="Trigger">What triggers it.</param>
	/// <param name="Argument">More about the trigger: the date, the time, or the name of the note, when given.</param>
	public sealed record TokenFlowExit(string Target, bool IsFailure, TokenFlowTrigger Trigger, string? Argument);
}
