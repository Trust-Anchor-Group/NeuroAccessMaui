using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Services;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Signatures.ServerSignature
{
	/// <summary>
	/// The view model to bind to for when displaying server signatures.
	/// </summary>
	public partial class ServerSignatureViewModel : BaseViewModel
	{
		private Contract? contract;

		/// <summary>
		/// Creates an instance of the <see cref="ServerSignatureViewModel"/> class.
		/// </summary>
		protected internal ServerSignatureViewModel()
		{
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (ServiceRef.NavigationService.TryGetArgs(out ServerSignatureNavigationArgs? args))
				this.contract = args.Contract;

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
	}
}
