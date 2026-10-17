using NeuroFeatures.EventArguments;

namespace NeuroAccessMaui.Services.Wallet.Tokens
{
	/// <summary>
	/// What is known about a token action when its contract is prepared: the token, the people involved, and the token
	/// service's trust provider, currency and commission.
	/// </summary>
	public sealed class TokenContractContext
	{
		/// <summary>
		/// ID of the token the action is about. Not set when tokens are created.
		/// </summary>
		public string? TokenId { get; init; }

		/// <summary>
		/// ID of the contract that made the current owner the owner of the token.
		/// </summary>
		public string? OwnershipContract { get; init; }

		/// <summary>
		/// Legal ID of the token's current owner.
		/// </summary>
		public string? OwnerLegalId { get; init; }

		/// <summary>
		/// Legal ID of the person a token is given to.
		/// </summary>
		public string? RecipientLegalId { get; init; }

		/// <summary>
		/// Category of the token.
		/// </summary>
		public string? Category { get; init; }

		/// <summary>
		/// Name of the token.
		/// </summary>
		public string? FriendlyName { get; init; }

		/// <summary>
		/// The token service's trust provider, currency and commission. When not set, they are fetched when the contract is
		/// prepared.
		/// </summary>
		public CreationAttributesEventArgs? Attributes { get; init; }
	}
}
