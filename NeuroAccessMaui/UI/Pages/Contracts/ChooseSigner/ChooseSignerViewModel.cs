using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Popups;

namespace NeuroAccessMaui.UI.Pages.Contracts.ChooseSigner
{
	/// <summary>
	/// View model for the sheet where the user chooses who signs a role of a new contract: themselves, a contact, someone
	/// whose ID code they scan, or anyone with the link.
	/// </summary>
	/// <param name="RoleName">The role.</param>
	/// <param name="CanChooseMe">If the user can take the role themselves.</param>
	/// <param name="CanChooseAnyone">If the contract can be opened to anyone with the link.</param>
	public partial class ChooseSignerViewModel(string RoleName, bool CanChooseMe, bool CanChooseAnyone) : ReturningPopupViewModel<SignerChoice?>
	{
		/// <summary>
		/// The heading, such as "Who signs as Buyer?".
		/// </summary>
		public string Title { get; } = ServiceRef.Localizer[nameof(AppResources.ContractWhoSignsAs), RoleName];

		/// <summary>
		/// If the user can take the role themselves.
		/// </summary>
		public bool CanChooseMe { get; } = CanChooseMe;

		/// <summary>
		/// If the contract can be opened to anyone with the link.
		/// </summary>
		public bool CanChooseAnyone { get; } = CanChooseAnyone;

		/// <summary>
		/// Shows the sheet, and waits until the user chooses or closes it.
		/// </summary>
		/// <param name="RoleName">The role.</param>
		/// <param name="CanChooseMe">If the user can take the role themselves.</param>
		/// <param name="CanChooseAnyone">If the contract can be opened to anyone with the link.</param>
		/// <returns>The choice, or null if the user closed the sheet.</returns>
		public static async Task<SignerChoice?> ShowAsync(string RoleName, bool CanChooseMe, bool CanChooseAnyone)
		{
			ChooseSignerViewModel ViewModel = new(RoleName, CanChooseMe, CanChooseAnyone);
			return await ServiceRef.PopupService.PushAsync<ChooseSignerPopup, ChooseSignerViewModel, SignerChoice?>(ViewModel);
		}

		/// <summary>
		/// Closes the sheet with a choice.
		/// </summary>
		/// <param name="Choice">The choice.</param>
		[RelayCommand]
		private async Task ChooseAsync(SignerChoice Choice)
		{
			this.TrySetResult(Choice);
			await ServiceRef.PopupService.PopAsync();
		}

		/// <summary>
		/// Closes the sheet without a choice.
		/// </summary>
		[RelayCommand]
		private async Task CloseAsync()
		{
			this.TrySetResult(null);
			await ServiceRef.PopupService.PopAsync();
		}

		/// <inheritdoc/>
		protected override Task OnPopAsync()
		{
			this.TrySetResult(null);
			return base.OnPopAsync();
		}
	}
}
