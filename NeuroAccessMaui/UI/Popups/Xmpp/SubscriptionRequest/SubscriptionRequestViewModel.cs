using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages;

namespace NeuroAccessMaui.UI.Popups.Xmpp.SubscriptionRequest
{
	/// <summary>
	/// How to respond to a presence subscription request.
	/// </summary>
	public enum PresenceRequestAction
	{
		/// <summary>
		/// Accept request
		/// </summary>
		Accept,

		/// <summary>
		/// Reject request
		/// </summary>
		Reject,

		/// <summary>
		/// Ignore request.
		/// </summary>
		Ignore
	}

	/// <summary>
	/// View model for <see cref="SubscriptionRequestPopup"/>
	/// </summary>
	public partial class SubscriptionRequestViewModel : BaseViewModel
	{
		private readonly TaskCompletionSource<PresenceRequestAction> result = new();

		/// <summary>
		/// View model for <see cref="SubscriptionRequestPopup"/>
		/// </summary>
		/// <param name="BareJid">Bare JID of sender of request.</param>
		/// <param name="FriendlyName">Friendly Name</param>
		/// <param name="PhotoUrl">Photo URL</param>
		/// <param name="PhotoWidth">Photo Width</param>
		/// <param name="PhotoHeight">Photo Height</param>
		public SubscriptionRequestViewModel(string BareJid, string FriendlyName, string? PhotoUrl, int PhotoWidth, int PhotoHeight)
			: base()
		{
			this.BareJid = BareJid;
			this.FriendlyName = FriendlyName;
			this.PhotoUrl = PhotoUrl;
			this.PhotoWidth = PhotoWidth;
			this.PhotoHeight = PhotoHeight;
			this.HasFriendlyName = !string.IsNullOrEmpty(FriendlyName) && FriendlyName != BareJid;
			this.HasPhoto = !string.IsNullOrEmpty(PhotoUrl);

			if (this.hasFriendlyName)
			{
				this.PrimaryName = FriendlyName;
				this.SecondaryName = " (" + BareJid + ")";
			}
			else
			{
				this.PrimaryName = BareJid;
				this.SecondaryName = string.Empty;
			}
		}

		/// <summary>
		/// Bare JID of sender of request.
		/// </summary>
		[ObservableProperty]
		private string bareJid;

		/// <summary>
		/// Friendly Name
		/// </summary>
		[ObservableProperty]
		private string friendlyName;

		/// <summary>
		/// Primary Name to display
		/// </summary>
		[ObservableProperty]
		private string primaryName;

		/// <summary>
		/// Secondary name to display
		/// </summary>
		[ObservableProperty]
		private string secondaryName;

		/// <summary>
		/// If there's a friendly name to display
		/// </summary>
		[ObservableProperty]
		private bool hasFriendlyName;

		/// <summary>
		/// If there's a photo to display.
		/// </summary>
		[ObservableProperty]
		private bool hasPhoto;

		/// <summary>
		/// URL to photo.
		/// </summary>
		[ObservableProperty]
		private string? photoUrl;

		/// <summary>
		/// Width of photo
		/// </summary>
		[ObservableProperty]
		private int photoWidth;

		/// <summary>
		/// Height of photo
		/// </summary>
		[ObservableProperty]
		private int photoHeight;

		/// <summary>
		/// Result will be provided here.
		/// </summary>
		public Task<PresenceRequestAction> Result => this.result.Task;

		/// <summary>
		/// Accepts subscription request
		/// </summary>
		[RelayCommand]
		private async Task Accept()
		{
			this.result.TrySetResult(PresenceRequestAction.Accept);
			await ServiceRef.PopupService.PopAsync();
		}

		/// <summary>
		/// Reject subscription request.
		/// </summary>
		[RelayCommand]
		private async Task Reject()
		{
			this.result.TrySetResult(PresenceRequestAction.Reject);
			await ServiceRef.PopupService.PopAsync();
		}

		[RelayCommand]
		private async Task Ignore()
		{
			this.result.TrySetResult(PresenceRequestAction.Ignore);
			await ServiceRef.PopupService.PopAsync();
		}

		/// <summary>
		/// Closes
		/// </summary>
		internal void Close()
		{
			this.result.TrySetResult(PresenceRequestAction.Ignore);
		}
	}
}
