using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.UI.Pages.Wallet.TokenDetails;

namespace NeuroAccessMaui.UI.Pages.Wallet.Flow
{
	/// <summary>
	/// Navigation arguments for the page that shows a token's whole flow as a map.
	/// </summary>
	public class TokenFlowNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Creates empty navigation arguments.
		/// </summary>
		public TokenFlowNavigationArgs()
		{
		}

		/// <summary>
		/// Creates navigation arguments for a token's flow.
		/// </summary>
		/// <param name="Steps">The steps of the flow, with the first step first and the step the token is in now marked.</param>
		/// <param name="Variables">The token's values now, by name.</param>
		public TokenFlowNavigationArgs(IReadOnlyList<TokenFlowStepItem> Steps, IReadOnlyDictionary<string, object> Variables)
		{
			this.Steps = Steps;
			this.Variables = Variables;
		}

		/// <summary>
		/// The steps of the flow, with the first step first and the step the token is in now marked.
		/// </summary>
		public IReadOnlyList<TokenFlowStepItem> Steps { get; } = [];

		/// <summary>
		/// The token's values now, by name.
		/// </summary>
		public IReadOnlyDictionary<string, object> Variables { get; } = new Dictionary<string, object>();
	}
}
