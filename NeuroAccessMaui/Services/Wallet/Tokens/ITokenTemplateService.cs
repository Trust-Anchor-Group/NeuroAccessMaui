using NeuroAccessMaui.UI.Pages.Contracts.NewContract;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.Services.Wallet.Tokens
{
	/// <summary>
	/// Finds the contract templates for token actions on the user's Neuron, and prepares contracts from them.
	/// </summary>
	public interface ITokenTemplateService
	{
		/// <summary>
		/// Checks if a template is known for an action. Only cached and stored information is used when it is at hand, so
		/// the answer may be found without asking the Neuron.
		/// </summary>
		/// <param name="Action">The token action.</param>
		/// <returns>If a template is known for the action.</returns>
		Task<bool> HasTemplateAsync(TokenAction Action);

		/// <summary>
		/// Gets a fresh copy of the template to use for an action.
		/// </summary>
		/// <param name="Action">The token action.</param>
		/// <returns>The template, or null when there is none.</returns>
		Task<Contract?> GetTemplateAsync(TokenAction Action);

		/// <summary>
		/// Gets the IDs of the known templates for creating tokens: those found on the Neuron, those the user has saved, and
		/// those known for the Neuron's domain.
		/// </summary>
		/// <returns>Template IDs, without duplicates.</returns>
		Task<string[]> GetCreationTemplatesAsync();

		/// <summary>
		/// Fills in a template for an action: the user's roles, the other parties that are known, the trust provider, and
		/// the token, currency and commission parameters.
		/// </summary>
		/// <param name="Action">The token action.</param>
		/// <param name="Template">The template. It is changed in place, so it should be a fresh copy.</param>
		/// <param name="Context">What is known about the action.</param>
		/// <returns>Navigation arguments for the new contract page.</returns>
		Task<NewContractNavigationArgs> PrepareAsync(TokenAction Action, Contract Template, TokenContractContext Context);
	}
}
