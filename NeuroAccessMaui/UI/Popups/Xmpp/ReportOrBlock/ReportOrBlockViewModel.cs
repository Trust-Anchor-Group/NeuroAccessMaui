using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mopups.Services;
using NeuroAccessMaui.UI.Pages;

namespace NeuroAccessMaui.UI.Popups.Xmpp.ReportOrBlock
{
	/// <summary>
	/// How to continue when rejecting a subscription request.
	/// </summary>
	public enum ReportOrBlockAction
	{
		/// <summary>
		/// Block sender
		/// </summary>
		Block,

		/// <summary>
		/// Report sender
		/// </summary>
		Report,

		/// <summary>
		/// Ignore sender
		/// </summary>
		Ignore
	}

	/// <summary>
	/// View model for <see cref="ReportOrBlockPopup"/>
	/// </summary>
	public partial class ReportOrBlockViewModel : BaseViewModel
	{
		private readonly TaskCompletionSource<ReportOrBlockAction> result = new();

		/// <summary>
		/// View model for <see cref="ReportOrBlockPopup"/>
		/// </summary>
		/// <param name="BareJid">Bare JID of sender of request.</param>
		public ReportOrBlockViewModel(string BareJid)
			: base()
		{
			this.BareJid = BareJid;
		}

		/// <summary>
		/// Result will be provided here.
		/// </summary>
		public Task<ReportOrBlockAction> Result => this.result.Task;

		/// <summary>
		/// Bare JID of sender of request.
		/// </summary>
		[ObservableProperty]
		private string bareJid;

		/// <summary>
		/// Blocks the account.
		/// </summary>
		[RelayCommand]
		private async Task Block()
		{
			this.result.TrySetResult(ReportOrBlockAction.Block);
			await MopupService.Instance.PopAsync();
		}

		/// <summary>
		/// Reports the account.
		/// </summary>
		[RelayCommand]
		private async Task Report()
		{
			this.result.TrySetResult(ReportOrBlockAction.Report);
			await MopupService.Instance.PopAsync();
		}

		/// <summary>
		/// Ignores the request.
		/// </summary>
		[RelayCommand]
		private async Task Ignore()
		{
			this.result.TrySetResult(ReportOrBlockAction.Ignore);
			await MopupService.Instance.PopAsync();
		}

		/// <summary>
		/// Closes
		/// </summary>
		internal void Close()
		{
			this.result.TrySetResult(ReportOrBlockAction.Ignore);
		}
	}
}
