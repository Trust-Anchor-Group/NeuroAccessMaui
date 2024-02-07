using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mopups.Services;
using NeuroAccessMaui.UI.Pages;
using Waher.Networking.XMPP.Abuse;

namespace NeuroAccessMaui.UI.Popups.Xmpp.ReportType
{
	/// <summary>
	/// View model for <see cref="ReportTypePopup"/>
	/// </summary>
	public partial class ReportTypeViewModel : BaseViewModel
	{
		private readonly TaskCompletionSource<ReportingReason?> result = new();

		/// <summary>
		/// View model for <see cref="ReportTypePopup"/>
		/// </summary>
		/// <param name="BareJid">Bare JID of sender of request.</param>
		public ReportTypeViewModel(string BareJid)
			: base()
		{
			this.BareJid = BareJid;
		}

		/// <summary>
		/// Result will be provided here.
		/// </summary>
		public Task<ReportingReason?> Result => this.result.Task;

		/// <summary>
		/// Bare JID of sender of request.
		/// </summary>
		[ObservableProperty]
		private string bareJid;

		/// <summary>
		/// Reports as spam.
		/// </summary>
		[RelayCommand]
		private async Task Spam()
		{
			this.result.TrySetResult(ReportingReason.Spam);
			await MopupService.Instance.PopAsync();
		}

		/// <summary>
		/// Reports as abuse.
		/// </summary>
		[RelayCommand]
		private async Task Abuse()
		{
			this.result.TrySetResult(ReportingReason.Abuse);
			await MopupService.Instance.PopAsync();
		}

		/// <summary>
		/// Reports as other.
		/// </summary>
		[RelayCommand]
		private async Task Other()
		{
			this.result.TrySetResult(ReportingReason.Other);
			await MopupService.Instance.PopAsync();
		}

		/// <summary>
		/// Ignores the request.
		/// </summary>
		[RelayCommand]
		private async Task Ignore()
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
