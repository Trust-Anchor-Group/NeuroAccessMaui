namespace NeuroAccessMaui.UI.Pages.Contracts.ViewContract
{
	/// <summary>
	/// Presents a token that was created or affected by the displayed agreement.
	/// </summary>
	public sealed class ContractRelatedTokenItem
	{
		/// <summary>
		/// Initializes a related-token presentation item.
		/// </summary>
		/// <param name="TokenId">The durable token identifier.</param>
		/// <param name="RelationshipText">Localized description of the token's relationship to the agreement.</param>
		public ContractRelatedTokenItem(string TokenId, string RelationshipText)
		{
			this.TokenId = TokenId;
			this.DisplayTokenId = BuildShortIdentifier(TokenId);
			this.RelationshipText = RelationshipText;
		}

		/// <summary>
		/// Gets the durable token identifier.
		/// </summary>
		public string TokenId { get; }

		/// <summary>
		/// Gets the shortened identifier shown by default.
		/// </summary>
		public string DisplayTokenId { get; }

		/// <summary>
		/// Gets the localized relationship description.
		/// </summary>
		public string RelationshipText { get; }

		private static string BuildShortIdentifier(string Value)
		{
			string NormalizedValue = Value.Trim();
			if (NormalizedValue.Length <= 18)
				return NormalizedValue;

			return NormalizedValue[..9] + "…" + NormalizedValue[^7..];
		}
	}
}
