using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mopups.Services;
using NeuroAccessMaui.UI.Pages;

namespace NeuroAccessMaui.UI.Popups.Pin
{
	/// <summary>
	/// View model for page letting the user enter a PIN to be verified with the PIN defined by the user earlier.
	/// </summary>
	public partial class CheckPinViewModel : BaseViewModel
    {
		private readonly TaskCompletionSource<string?> result = new();

		/// <summary>
		/// View model for page letting the user enter a PIN to be verified with the PIN defined by the user earlier.
		/// </summary>
		public CheckPinViewModel()
			: base()
		{
		}

		/// <summary>
		/// Result will be provided here. If dialog is cancelled, null is returned.
		/// </summary>
		public Task<string?> Result => this.result.Task;

		/// <summary>
		/// PIN text entry
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(EnterPinCommand))]
		private string pinText = string.Empty;

		/// <summary>
		/// If PIN can be entered
		/// </summary>
		public bool CanEnterPin => !string.IsNullOrEmpty(this.PinText);

		/// <summary>
		/// Enters the PIN provided by the user.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanEnterPin))]
		private async Task EnterPin()
		{
			this.result.TrySetResult(this.PinText);
			await MopupService.Instance.PopAsync();
		}

		/// <summary>
		/// Cancels PIN-entry
		/// </summary>
		[RelayCommand()]
		private async Task Cancel()
		{
			this.result.TrySetResult(null);
			await MopupService.Instance.PopAsync();
		}

		/// <summary>
		/// Closes
		/// </summary>
		internal void Close()
		{
			this.result.TrySetResult(null);
		}
	}
}
