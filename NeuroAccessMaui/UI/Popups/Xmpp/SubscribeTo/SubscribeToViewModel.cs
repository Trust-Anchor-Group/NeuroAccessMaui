using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages;

namespace NeuroAccessMaui.UI.Popups.Xmpp.SubscribeTo
{
	/// <summary>
	/// View model for <see cref="SubscribeToPopup"/>
	/// </summary>
	public partial class SubscribeToViewModel : BaseViewModel
	{
		private readonly TaskCompletionSource<bool?> result = new();

		/// <summary>
		/// View model for <see cref="SubscribeToPopup"/>
		/// </summary>
		/// <param name="BareJid">Bare JID of sender of request.</param>
		public SubscribeToViewModel(string BareJid)
			: base()
		{
			this.BareJid = BareJid;
		}

		/// <summary>
		/// Bare JID of sender of request.
		/// </summary>
		[ObservableProperty]
		private string bareJid;

		/// <summary>
		/// Result will be provided here. If dialog is cancelled, null is returned.
		/// </summary>
		public Task<bool?> Result => this.result.Task;

		/// <summary>
		/// Subscribes to contact.
		/// </summary>
		[RelayCommand]
		private async Task Confirm()
		{
			this.result.TrySetResult(true);
			await ServiceRef.PopupService.PopAsync();
		}

		/// <summary>
		/// Does not subscribe to contact.
		/// </summary>
		[RelayCommand]
		private async Task Decline()
		{
			this.result.TrySetResult(false);
			await ServiceRef.PopupService.PopAsync();
		}

		[RelayCommand]
		private async Task Cancel()
		{
			this.result.TrySetResult(null);
			await ServiceRef.PopupService.PopAsync();
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
