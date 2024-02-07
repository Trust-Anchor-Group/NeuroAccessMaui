using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mopups.Services;
using NeuroAccessMaui.UI.Pages;

namespace NeuroAccessMaui.UI.Popups.Xmpp.SubscribeTo
{
	/// <summary>
	/// View model for <see cref="SubscribeToPage"/>
	/// </summary>
	public partial class SubscribeToViewModel : BaseViewModel
	{
		private readonly TaskCompletionSource<bool?> result = new();

		/// <summary>
		/// View model for <see cref="SubscribeToPage"/>
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
		/// Adds the note
		/// </summary>
		/// <returns></returns>
		[RelayCommand]
		private async Task Yes()
		{
			this.result.TrySetResult(true);
			await MopupService.Instance.PopAsync();
		}

		/// <summary>
		/// Cancels PIN-entry
		/// </summary>
		[RelayCommand]
		private async Task No()
		{
			this.result.TrySetResult(false);
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
