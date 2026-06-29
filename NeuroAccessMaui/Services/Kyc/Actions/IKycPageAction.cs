using System.Threading.Tasks;

namespace NeuroAccessMaui.Services.Kyc.Actions
{
	/// <summary>
	/// Defines a built-in action that can be attached to a KYC page through metadata.
	/// </summary>
	/// <remarks>
	/// The action <see cref="Name"/> is the value used in page metadata, for example <c>TravelDocumentNfc</c>.
	/// </remarks>
	public interface IKycPageAction
	{
		/// <summary>
		/// Gets the metadata action name handled by the action.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets the current UI state for the action.
		/// </summary>
		/// <param name="Context">The action execution context.</param>
		/// <returns>A task that returns the current action state.</returns>
		Task<KycPageActionState> GetStateAsync(KycPageActionContext Context);

		/// <summary>
		/// Executes the action.
		/// </summary>
		/// <param name="Context">The action execution context.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		Task ExecuteAsync(KycPageActionContext Context);
	}
}
