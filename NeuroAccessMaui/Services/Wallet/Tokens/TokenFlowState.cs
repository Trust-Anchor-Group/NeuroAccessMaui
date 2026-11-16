namespace NeuroAccessMaui.Services.Wallet.Tokens
{
	/// <summary>
	/// A step of a token's state machine, with the ways out of it.
	/// </summary>
	/// <param name="Id">The step's code name.</param>
	/// <param name="Exits">The ways out of the step. A step without any is an end.</param>
	public sealed record TokenFlowState(string Id, IReadOnlyList<TokenFlowExit> Exits)
	{
		/// <summary>
		/// If the flow ends in this step.
		/// </summary>
		public bool IsEnd => this.Exits.Count == 0;
	}
}
