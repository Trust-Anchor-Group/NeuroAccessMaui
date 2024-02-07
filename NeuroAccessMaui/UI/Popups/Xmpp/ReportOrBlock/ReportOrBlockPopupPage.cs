using System;
using System.Threading.Tasks;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;

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
    /// Prompts the user for a response of a presence subscription request.
    /// </summary>
    public partial class ReportOrBlockPopupPage : PopupPage
    {
        private readonly TaskCompletionSource<ReportOrBlockAction> result = new();
        private readonly string bareJid;

        /// <summary>
        /// Prompts the user for a response of a presence subscription request.
        /// </summary>
        /// <param name="BareJid">Bare JID of sender of request.</param>
        public ReportOrBlockPopupPage(string BareJid)
        {
            this.bareJid = BareJid;

			this.InitializeComponent();

            this.BindingContext = this;
        }

        /// <summary>
        /// Bare JID of sender of request.
        /// </summary>
        public string BareJid => this.bareJid;

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
            this.result.TrySetResult(ReportOrBlockAction.Ignore);
        }

        private async void OnBlock(object Sender, EventArgs e)
        {
            await PopupNavigation.Instance.PopAsync();
            this.result.TrySetResult(ReportOrBlockAction.Block);
        }

        private async void OnReport(object Sender, EventArgs e)
        {
            await PopupNavigation.Instance.PopAsync();
            this.result.TrySetResult(ReportOrBlockAction.Report);
        }

        private async void OnIgnore(object Sender, EventArgs e)
        {
            await PopupNavigation.Instance.PopAsync();
            this.result.TrySetResult(ReportOrBlockAction.Ignore);
        }

        /// <summary>
        /// Task waiting for result.
        /// </summary>
        public Task<ReportOrBlockAction> Result => this.result.Task;
    }
}
