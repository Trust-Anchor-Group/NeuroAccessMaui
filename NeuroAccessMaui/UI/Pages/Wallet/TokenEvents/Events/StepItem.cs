using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Wallet.Tokens;
using NeuroFeatures.Events;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenEvents.Events
{
	/// <summary>
	/// A step the token moved to, seen live while its history was open. The token's Neuron doesn't list steps among its
	/// events, so these are only shown on this device.
	/// </summary>
	/// <param name="TokenId">The token's ID.</param>
	/// <param name="State">The code name of the step.</param>
	/// <param name="Timestamp">When the step was seen.</param>
	public class StepItem(string TokenId, string State, DateTime Timestamp)
		: EventItem(new NoteText { TokenId = TokenId, Timestamp = Timestamp, Note = State })
	{
		/// <inheritdoc/>
		public override EventType Type => EventType.Step;

		/// <summary>
		/// What the event says, such as "Step: Open for investment".
		/// </summary>
		public string Title => ServiceRef.Localizer[nameof(AppResources.HistoryStep), TokenText.ToWords(State)];
	}
}
