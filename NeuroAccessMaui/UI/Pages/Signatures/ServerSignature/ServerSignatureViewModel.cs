using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Signatures.ServerSignature
{
	/// <summary>
	/// The view model to bind to for when displaying server signatures.
	/// </summary>
	public partial class ServerSignatureViewModel : BaseViewModel
	{
		private readonly Contract? contract;

		/// <summary>
		/// Creates an instance of the <see cref="ServerSignatureViewModel"/> class.
		/// </summary>
		/// <param name="Args">Navigation arguments</param>
		public ServerSignatureViewModel(ServerSignatureNavigationArgs? Args)
		{
			if (Args is not null)
				this.contract = Args.Contract;
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			this.AssignProperties();
		}

		#region Properties

		/// <summary>
		/// The provider of the server signature contract.
		/// </summary>
		[ObservableProperty]
		private string? provider;

		/// <summary>
		/// The time stamp of the server signature contract.
		/// </summary>
		[ObservableProperty]
		private string? timestamp;

		/// <summary>
		/// The signature of the server signature contract.
		/// </summary>
		[ObservableProperty]
		private string? signature;

		#endregion

		private void AssignProperties()
		{
			if (this.contract is not null)
			{
				this.Provider = this.contract.Provider;
				this.Timestamp = this.contract.ServerSignature.Timestamp.ToString(CultureInfo.CurrentCulture);
				this.Signature = Convert.ToBase64String(this.contract.ServerSignature.DigitalSignature);
			}
			else
			{
				this.Provider = Constants.NotAvailableValue;
				this.Timestamp = Constants.NotAvailableValue;
				this.Signature = Constants.NotAvailableValue;
			}
		}

		/// <summary>
		/// Copies Item to clipboard
		/// </summary>
		[RelayCommand]
		private static async Task Copy(object Item)
		{
			try
			{
				if (Item is string Label)
					await Clipboard.SetTextAsync(Label);
				else
					await Clipboard.SetTextAsync(Item?.ToString() ?? string.Empty);

				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
					ServiceRef.Localizer[nameof(AppResources.TagValueCopiedToClipboard)]);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}
	}
}
