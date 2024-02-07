using System;
using System.Threading.Tasks;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;

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
    /// Prompts the user for a response of a presence subscription request.
    /// </summary>
    public partial class SubscriptionRequestPopupPage : PopupPage
    {
        private readonly TaskCompletionSource<PresenceRequestAction> result = new();
        private readonly string bareJid;
        private readonly string friendlyName;
        private readonly string photoUrl;
        private readonly string primaryName;
        private readonly string secondaryName;
        private readonly int photoWidth;
        private readonly int photoHeight;
        private readonly bool hasPhoto;
        private readonly bool hasFriendlyName;

        /// <summary>
        /// Prompts the user for a response of a presence subscription request.
        /// </summary>
        /// <param name="BareJid">Bare JID of sender of request.</param>
        /// <param name="FriendlyName">Friendly Name</param>
        /// <param name="PhotoUrl">Photo URL</param>
        /// <param name="PhotoWidth">Photo Width</param>
        /// <param name="PhotoHeight">Photo Height</param>
        public SubscriptionRequestPopupPage(string BareJid, string FriendlyName, string PhotoUrl, int PhotoWidth, int PhotoHeight)
        {
            this.bareJid = BareJid;
            this.friendlyName = FriendlyName;
            this.photoUrl = PhotoUrl;
            this.photoWidth = PhotoWidth;
            this.photoHeight = PhotoHeight;
            this.hasFriendlyName = !string.IsNullOrEmpty(FriendlyName) && FriendlyName != BareJid;
            this.hasPhoto = !string.IsNullOrEmpty(PhotoUrl);

            if (this.hasFriendlyName)
            {
                this.primaryName = FriendlyName;
                this.secondaryName = " (" + BareJid + ")";
            }
            else
			{
                this.primaryName = BareJid;
                this.secondaryName = string.Empty;
			}

			this.InitializeComponent();

            this.BindingContext = this;
        }

        /// <summary>
        /// Bare JID of sender of request.
        /// </summary>
        public string BareJid => this.bareJid;

        /// <summary>
        /// Friendly Name
        /// </summary>
        public string FriendlyName => this.friendlyName;

        /// <summary>
        /// Primary Name to display
        /// </summary>
        public string PrimaryName => this.primaryName;

        /// <summary>
        /// Secondary name to display
        /// </summary>
        public string SecondaryName => this.secondaryName;

        /// <summary>
        /// If there's a friendly name to display
        /// </summary>
        public bool HasFriendlyName => this.hasFriendlyName;

        /// <summary>
        /// If there's a photo to display.
        /// </summary>
        public bool HasPhoto => this.hasPhoto;

        /// <summary>
        /// URL to photo.
        /// </summary>
        public string PhotoUrl => this.photoUrl;

        /// <summary>
        /// Width of photo
        /// </summary>
        public int PhotoWidth => this.photoWidth;

        /// <summary>
        /// Height of photo
        /// </summary>
        public int PhotoHeight => this.photoHeight;

        private void OnCloseButtonTapped(object Sender, EventArgs e)
        {
			this.Close();
        }

        /// <inheritdoc/>
        protected override bool OnBackgroundClicked()
        {
			this.Close();
            return false;
        }

        private async void Close()
        {
            await PopupNavigation.Instance.PopAsync();
            this.result.TrySetResult(PresenceRequestAction.Ignore);
        }

        private async void OnAccept(object Sender, EventArgs e)
        {
            await PopupNavigation.Instance.PopAsync();
            this.result.TrySetResult(PresenceRequestAction.Accept);
        }

        private async void OnReject(object Sender, EventArgs e)
        {
            await PopupNavigation.Instance.PopAsync();
            this.result.TrySetResult(PresenceRequestAction.Reject);
        }

        /// <summary>
        /// Task waiting for result.
        /// </summary>
        public Task<PresenceRequestAction> Result => this.result.Task;
    }
}
