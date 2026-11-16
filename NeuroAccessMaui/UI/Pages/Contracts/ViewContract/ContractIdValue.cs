namespace NeuroAccessMaui.UI.Pages.Contracts.ViewContract
{
	/// <summary>
	/// A value that signing takes from the user's ID and puts into the contract, shown before the user signs.
	/// </summary>
	/// <param name="Label">What the value is.</param>
	/// <param name="Value">The value, or a note that the ID doesn't have it.</param>
	/// <param name="IsMissing">If the ID doesn't have the value, so the server may refuse the signature.</param>
	public sealed record ContractIdValue(string Label, string Value, bool IsMissing);
}
