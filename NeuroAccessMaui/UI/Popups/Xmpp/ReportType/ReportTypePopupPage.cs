using System;
using System.Threading.Tasks;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using Waher.Networking.XMPP.Abuse;

namespace NeuroAccessMaui.UI.Popups.Xmpp.ReportType
{
    /// <summary>
    /// Prompts the user for a response of a presence subscription request.
    /// </summary>
    public partial class ReportTypePopupPage : PopupPage
    {
        private readonly TaskCompletionSource<ReportingReason?> result = new();
        private readonly string bareJid;

        /// <summary>
        /// Prompts the user for a response of a presence subscription request.
        /// </summary>
        /// <param name="BareJid">Bare JID of sender of request.</param>
        public ReportTypePopupPage(string BareJid)
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
            this.result.TrySetResult(null);
        }

        private async void OnSpam(object Sender, EventArgs e)
        {
            await PopupNavigation.Instance.PopAsync();
            this.result.TrySetResult(ReportingReason.Spam);
        }

        private async void OnAbuse(object Sender, EventArgs e)
        {
            await PopupNavigation.Instance.PopAsync();
            this.result.TrySetResult(ReportingReason.Abuse);
        }

        private async void OnOther(object Sender, EventArgs e)
        {
            await PopupNavigation.Instance.PopAsync();
            this.result.TrySetResult(ReportingReason.Other);
        }

        /// <summary>
        /// Task waiting for result.
        /// </summary>
        public Task<ReportingReason?> Result => this.result.Task;
    }
}
