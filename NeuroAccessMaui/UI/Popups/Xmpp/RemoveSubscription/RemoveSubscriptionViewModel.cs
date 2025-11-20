using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages;

namespace NeuroAccessMaui.UI.Popups.Xmpp.RemoveSubscription
{
	/// <summary>
	/// View model for <see cref="RemoveSubscriptionPopup"/>
	/// </summary>
	public partial class RemoveSubscriptionViewModel : BaseViewModel
	{
		private readonly TaskCompletionSource<bool?> result = new();

		/// <summary>
		/// View model for <see cref="RemoveSubscriptionPopup"/>
		/// </summary>
		/// <param name="BareJid">Bare JID of sender of request.</param>
		public RemoveSubscriptionViewModel(string BareJid)
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
		/// Removes the subscription.
		/// </summary>
		[RelayCommand]
		private async Task Yes()
		{
			this.result.TrySetResult(true);
			await ServiceRef.PopupService.PopAsync();
		}

		/// <summary>
		/// Does not remove subscription.
		/// </summary>
		[RelayCommand]
		private async Task No()
		{
			this.result.TrySetResult(false);
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
