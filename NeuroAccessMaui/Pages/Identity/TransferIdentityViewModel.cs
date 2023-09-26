using IdApp.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IdApp.Pages.Identity.TransferIdentity
{
	/// <summary>
	/// The view model to bind to for when displaying identities.
	/// </summary>
	public class TransferIdentityViewModel : QrXmppViewModel
	{
		private Timer timer;

		/// <summary>
		/// Creates an instance of the <see cref="TransferIdentityViewModel"/> class.
		/// </summary>
		public TransferIdentityViewModel()
			: base()
		{
			this.CopyUriToClipboard = new Command(async () => await this.CopyUriToClipboardClicked());
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (this.NavigationService.TryGetArgs(out TransferIdentityNavigationArgs args))
				this.Uri = args.Uri;

			this.QrCodeWidth = 400;
			this.QrCodeHeight = 400;
			this.GenerateQrCode(this.Uri);

			this.timer = new Timer(this.Timeout, null, 60000, 60000);
		}

		private void Timeout(object P)
		{
			this.timer?.Dispose();
			this.timer = null;

			this.UiSerializer.BeginInvokeOnMainThread(async () =>
			{
				try
				{
					await this.NavigationService.GoBackAsync();
				}
				catch (Exception ex)
				{
					this.LogService.LogException(ex);
				}
			});
		}

		/// <inheritdoc/>
		protected override Task OnDispose()
		{
			this.timer?.Dispose();
			this.timer = null;

			return base.OnDispose();
		}

		#region Properties

		/// <summary>
		/// Command to copy URI to clipboard.
		/// </summary>
		public ICommand CopyUriToClipboard
		{
			get;
		}

		/// <summary>
		/// See <see cref="Uri"/>
		/// </summary>
		public static readonly BindableProperty UriProperty =
			BindableProperty.Create(nameof(Uri), typeof(string), typeof(TransferIdentityViewModel), default(string));

		/// <summary>
		/// Uri date of the identity
		/// </summary>
		public string Uri
		{
			get => (string)this.GetValue(UriProperty);
			set => this.SetValue(UriProperty, value);
		}

		#endregion

		private async Task CopyUriToClipboardClicked()
		{
			await Clipboard.SetTextAsync(this.Uri);
			await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["SuccessTitle"], LocalizationResourceManager.Current["TagValueCopiedToClipboard"]);
		}

		#region ILinkableView

		/// <summary>
		/// Title of the current view
		/// </summary>
		public override Task<string> Title => Task.FromResult<string>(ContactInfo.GetFriendlyName(this.TagProfile.LegalIdentity));

		#endregion

	}
}
