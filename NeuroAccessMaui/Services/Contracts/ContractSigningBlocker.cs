namespace NeuroAccessMaui.Services.Contracts
{
	/// <summary>
	/// Something that stops the user from signing a contract, with an optional action that helps.
	/// </summary>
	public class ContractSigningBlocker
	{
		/// <summary>
		/// Creates an instance of the <see cref="ContractSigningBlocker"/> class.
		/// </summary>
		/// <param name="Message">The localized explanation.</param>
		/// <param name="ActionText">The localized label of the action, or null.</param>
		/// <param name="Action">The action that helps, or null.</param>
		public ContractSigningBlocker(string Message, string? ActionText = null, Func<Task>? Action = null)
		{
			this.Message = Message;
			this.ActionText = ActionText;
			this.Action = Action;
		}

		/// <summary>
		/// The localized explanation.
		/// </summary>
		public string Message { get; }

		/// <summary>
		/// The localized label of the action, or null.
		/// </summary>
		public string? ActionText { get; }

		/// <summary>
		/// The action that helps, or null.
		/// </summary>
		public Func<Task>? Action { get; }

		/// <summary>
		/// If the blocker has an action.
		/// </summary>
		public bool HasAction => this.Action is not null && !string.IsNullOrEmpty(this.ActionText);
	}
}
