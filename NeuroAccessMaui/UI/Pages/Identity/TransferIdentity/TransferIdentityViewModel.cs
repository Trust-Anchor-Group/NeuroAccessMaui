﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;

namespace NeuroAccessMaui.UI.Pages.Identity.TransferIdentity
{
	/// <summary>
	/// The view model to bind to for when displaying identities.
	/// </summary>
	public partial class TransferIdentityViewModel : QrXmppViewModel, IDisposable
	{
		private bool isDisposed;
		private Timer? timer;

		/// <summary>
		/// Creates an instance of the <see cref="TransferIdentityViewModel"/> class.
		/// </summary>
		/// <param name="Args">Navigation arguments.</param>
		public TransferIdentityViewModel(TransferIdentityNavigationArgs? Args)
			: base()
		{
			if (Args is not null)
				this.Uri = Args.Uri;
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (this.Uri is not null)
			{
				this.QrCodeWidth = 400;
				this.QrCodeHeight = 400;
				this.GenerateQrCode(this.Uri);
			}

			this.timer = new Timer(this.Timeout, null, 60000, 60000);
		}

		private void Timeout(object? _)
		{
			this.timer?.Dispose();
			this.timer = null;

			MainThread.BeginInvokeOnMainThread(async () =>
			{
				await this.GoBack();
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
		/// Uri date of the identity
		/// </summary>
		[ObservableProperty]
		private string? uri;

		#endregion

		[RelayCommand]
		private async Task CopyUriToClipboard()
		{
			await Clipboard.SetTextAsync(this.Uri);

			await ServiceRef.UiService.DisplayAlert(
				ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
				ServiceRef.Localizer[nameof(AppResources.TagValueCopiedToClipboard)]);
		}

		#region ILinkableView

		/// <summary>
		/// Title of the current view
		/// </summary>
		public override Task<string> Title => Task.FromResult(ContactInfo.GetFriendlyName(ServiceRef.TagProfile.LegalIdentity!));

		#endregion

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		protected virtual void Dispose(bool disposing)
		{
			if (this.isDisposed)
				return;

			if (disposing)
			{
				this.timer?.Dispose();
				this.timer = null;
			}

			this.isDisposed = true;
		}
	}
}
