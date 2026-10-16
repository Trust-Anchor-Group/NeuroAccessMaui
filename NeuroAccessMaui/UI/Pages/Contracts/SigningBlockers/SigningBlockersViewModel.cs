using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contracts;
using NeuroAccessMaui.UI.Popups;

namespace NeuroAccessMaui.UI.Pages.Contracts.SigningBlockers
{
	/// <summary>
	/// View model for the sheet that explains why the user can't sign a contract yet.
	/// The result is true if the user chose one of the actions.
	/// </summary>
	public partial class SigningBlockersViewModel : ReturningPopupViewModel<bool>
	{
		/// <summary>
		/// Creates a new instance of the <see cref="SigningBlockersViewModel"/> class.
		/// </summary>
		/// <param name="Blockers">What stops the user from signing.</param>
		public SigningBlockersViewModel(IEnumerable<ContractSigningBlocker> Blockers)
		{
			foreach (ContractSigningBlocker Blocker in Blockers)
				this.Blockers.Add(Blocker);
		}

		/// <summary>
		/// What stops the user from signing, in the order the server checks it.
		/// </summary>
		public ObservableCollection<ContractSigningBlocker> Blockers { get; } = [];

		/// <summary>
		/// Shows the sheet, and waits until the user closes it or chooses an action.
		/// </summary>
		/// <param name="Blockers">What stops the user from signing.</param>
		/// <returns>True if the user chose an action.</returns>
		public static async Task<bool> ShowAsync(IEnumerable<ContractSigningBlocker> Blockers)
		{
			SigningBlockersViewModel ViewModel = new(Blockers);
			return await ServiceRef.PopupService.PushAsync<SigningBlockersPopup, SigningBlockersViewModel, bool>(ViewModel);
		}

		/// <summary>
		/// Closes the sheet and runs the action of a blocker.
		/// </summary>
		/// <param name="Blocker">The blocker whose action was chosen.</param>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task RunActionAsync(ContractSigningBlocker? Blocker)
		{
			this.TrySetResult(Blocker?.Action is not null);
			await ServiceRef.PopupService.PopAsync();

			if (Blocker?.Action is null)
				return;

			try
			{
				await Blocker.Action();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Closes the sheet.
		/// </summary>
		[RelayCommand]
		private async Task CloseAsync()
		{
			this.TrySetResult(false);
			await ServiceRef.PopupService.PopAsync();
		}

		/// <inheritdoc/>
		protected override Task OnPopAsync()
		{
			this.TrySetResult(false);
			return base.OnPopAsync();
		}
	}
}
