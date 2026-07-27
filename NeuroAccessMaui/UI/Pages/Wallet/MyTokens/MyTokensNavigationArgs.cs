using NeuroAccessMaui.Services.UI;
using NeuroFeatures;

namespace NeuroAccessMaui.UI.Pages.Wallet.MyTokens
{
	/// <summary>
	/// Holds navigation parameters for viewing or selecting tokens.
	/// </summary>
	public class MyTokensNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Creates navigation arguments for selecting a token.
		/// </summary>
		public MyTokensNavigationArgs()
		{
			this.TokenProvider = new();
			this.IsSelectionMode = true;
		}

		/// <summary>
		/// Creates navigation arguments for a token that could not be opened directly.
		/// </summary>
		/// <param name="RequestedTokenId">Identifier of the token that was requested.</param>
		/// <param name="InitialErrorMessage">Localized error to present with the collection.</param>
		public MyTokensNavigationArgs(string? RequestedTokenId, string? InitialErrorMessage)
		{
			this.TokenProvider = new();
			this.RequestedTokenId = RequestedTokenId;
			this.InitialErrorMessage = InitialErrorMessage;
			this.IsSelectionMode = false;
		}

		/// <summary>
		/// Creates navigation arguments for browsing tokens created by a contract.
		/// </summary>
		/// <param name="RelatedContractId">Identifier of the contract whose tokens should be shown.</param>
		public MyTokensNavigationArgs(string RelatedContractId)
		{
			this.TokenProvider = new();
			this.RelatedContractId = RelatedContractId;
			this.IsSelectionMode = false;
		}

		/// <summary>
		/// Gets the completion source used to return a selected authoritative token.
		/// </summary>
		public TaskCompletionSource<Token?> TokenProvider { get; }

		/// <summary>
		/// Gets a value indicating whether the collection is selecting a token for another flow.
		/// </summary>
		public bool IsSelectionMode { get; }

		/// <summary>
		/// Gets the token identifier that could not be opened directly.
		/// </summary>
		public string? RequestedTokenId { get; }

		/// <summary>
		/// Gets the localized error that should be shown when the collection opens.
		/// </summary>
		public string? InitialErrorMessage { get; }

		/// <summary>
		/// Gets the contract identifier used to scope this collection to created tokens.
		/// </summary>
		public string? RelatedContractId { get; }
	}
}
