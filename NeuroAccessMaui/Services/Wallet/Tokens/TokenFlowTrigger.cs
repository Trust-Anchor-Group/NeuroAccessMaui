namespace NeuroAccessMaui.Services.Wallet.Tokens
{
	/// <summary>
	/// What makes a token's state machine move from one step to another.
	/// </summary>
	public enum TokenFlowTrigger
	{
		/// <summary>
		/// A condition on the token's values is met.
		/// </summary>
		Condition,

		/// <summary>
		/// A given date and time is reached.
		/// </summary>
		DateTime,

		/// <summary>
		/// A given time has passed.
		/// </summary>
		Duration,

		/// <summary>
		/// The token is sold or given away.
		/// </summary>
		Transferred,

		/// <summary>
		/// A payment arrives.
		/// </summary>
		PaymentReceived,

		/// <summary>
		/// A payment is sent.
		/// </summary>
		PaymentSent,

		/// <summary>
		/// The owner adds a note, for example by running a command.
		/// </summary>
		OwnerNote,

		/// <summary>
		/// Someone other than the owner adds a note.
		/// </summary>
		ExternalNote,

		/// <summary>
		/// A contract is signed.
		/// </summary>
		ContractSigned,

		/// <summary>
		/// The token is destroyed.
		/// </summary>
		Destroyed,

		/// <summary>
		/// A contract template is approved. The argument is the contract's name, when the event names one.
		/// </summary>
		TemplateApproved,

		/// <summary>
		/// An entry is added to a collection the token follows.
		/// </summary>
		EntryAdded,

		/// <summary>
		/// An entry in a collection the token follows changes.
		/// </summary>
		EntryUpdated,

		/// <summary>
		/// An entry is removed from a collection the token follows.
		/// </summary>
		EntryDeleted,

		/// <summary>
		/// Something else happens.
		/// </summary>
		Other
	}
}
