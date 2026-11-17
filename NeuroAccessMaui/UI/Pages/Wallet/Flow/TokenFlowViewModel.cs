using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Wallet.Tokens;
using NeuroAccessMaui.UI.Pages.Wallet.TokenDetails;

namespace NeuroAccessMaui.UI.Pages.Wallet.Flow
{
	/// <summary>
	/// View model for the page that shows a token's whole flow as a map, with what moves the token on from the selected
	/// step.
	/// </summary>
	public partial class TokenFlowViewModel : BaseViewModel
	{
		/// <summary>
		/// Creates the view model for a token's flow.
		/// </summary>
		/// <param name="Args">The flow to show, if any.</param>
		public TokenFlowViewModel(TokenFlowNavigationArgs? Args)
		{
			this.Steps = Args?.Steps ?? [];
			this.Variables = Args?.Variables ?? new Dictionary<string, object>();
			this.SelectedStep = this.Steps.FirstOrDefault(Step => Step.IsCurrent) ?? this.Steps.FirstOrDefault();
		}

		/// <summary>
		/// The steps of the flow, with the first step first.
		/// </summary>
		public IReadOnlyList<TokenFlowStepItem> Steps { get; }

		/// <summary>
		/// The token's values now, by name.
		/// </summary>
		public IReadOnlyDictionary<string, object> Variables { get; }

		/// <summary>
		/// The step whose ways on are shown in the sheet.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(SheetTitle))]
		private TokenFlowStepItem? selectedStep;

		/// <summary>
		/// The name of the selected step.
		/// </summary>
		public string SheetTitle => this.SelectedStep?.Name ?? string.Empty;

		/// <summary>
		/// The way on whose arrow label the user tapped on the map, marked in the sheet. Null when a step's card was tapped.
		/// </summary>
		[ObservableProperty]
		private TokenFlowExit? markedExit;

		/// <summary>
		/// What moves the token on from the selected step, one line per way.
		/// </summary>
		public ObservableCollection<TokenFlowSheetLine> SheetLines { get; } = [];

		/// <summary>
		/// Lists the ways on from the newly selected step.
		/// </summary>
		/// <param name="Value">The selected step.</param>
		partial void OnSelectedStepChanged(TokenFlowStepItem? Value)
		{
			this.UpdateSheetLines();
		}

		/// <summary>
		/// Marks the way on whose arrow label was tapped.
		/// </summary>
		/// <param name="Value">The way on, or null.</param>
		partial void OnMarkedExitChanged(TokenFlowExit? Value)
		{
			this.UpdateSheetLines();
		}

		/// <summary>
		/// Lists the ways on from the selected step, with the one whose arrow label was tapped marked.
		/// </summary>
		private void UpdateSheetLines()
		{
			this.SheetLines.Clear();

			TokenFlowStepItem? Step = this.SelectedStep;

			if (Step is null)
				return;

			if (Step.IsEnd)
			{
				this.SheetLines.Add(new TokenFlowSheetLine(Geometries.InfoCirclePath,
					ServiceRef.Localizer[nameof(AppResources.FlowStaysHere)], string.Empty, false));
				return;
			}

			// The map draws one arrow for all ways to the same step, so all of them are marked.
			TokenFlowExit? Marked = this.MarkedExit is TokenFlowExit Tapped && Step.Exits.Contains(Tapped) ? Tapped : null;

			foreach (TokenFlowExit Exit in Step.Exits.Distinct())
			{
				string Target = this.Steps.FirstOrDefault(Other => Other.Id == Exit.Target)?.Name ?? TokenText.ToWords(Exit.Target);
				string Title = Exit.IsFailure
					? ServiceRef.Localizer[nameof(AppResources.FlowIncomingFailure)]
					: TokenFlowStepItem.DescribeTrigger(Exit, this.Variables);
				string Subtitle = Exit.Target == Step.Id
					? ServiceRef.Localizer[nameof(AppResources.FlowStaysHere)]
					: ServiceRef.Localizer[nameof(AppResources.FlowGoesTo), Target];
				bool IsMarked = Marked is not null && Exit.Target == Marked.Target && Exit.IsFailure == Marked.IsFailure;

				this.SheetLines.Add(new TokenFlowSheetLine(TokenFlowStepItem.IconOf(Exit), Title, Subtitle, IsMarked));
			}
		}
	}
}
