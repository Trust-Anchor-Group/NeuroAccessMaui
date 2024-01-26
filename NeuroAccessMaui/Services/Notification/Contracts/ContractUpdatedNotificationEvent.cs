using IdApp.Pages.Contracts.MyContracts.ObjectModels;
using IdApp.Pages.Contracts.ViewContract;
using IdApp.Services.Navigation;
using System.Text;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Contracts;
using Xamarin.CommunityToolkit.Helpers;

namespace IdApp.Services.Notification.Contracts
{
	/// <summary>
	/// Notification event for when a contract has been updated.
	/// </summary>
	public class ContractUpdatedNotificationEvent : ContractNotificationEvent
	{
		/// <summary>
		/// Notification event for when a contract has been updated.
		/// </summary>
		public ContractUpdatedNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Notification event for when a contract has been updated.
		/// </summary>
		/// <param name="Contract">Requested contract.</param>
		/// <param name="e">Event arguments.</param>
		public ContractUpdatedNotificationEvent(Contract Contract, ContractReferenceEventArgs e)
			: base(Contract, e)
		{
		}

		/// <summary>
		/// Opens the event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public override async Task Open(IServiceReferences ServiceReferences)
		{
			Contract Contract = await this.GetContract();
			ViewContractNavigationArgs Args = new(Contract, false);

			await ServiceReferences.NavigationService.GoToAsync(nameof(ViewContractPage), Args, BackMethod.Pop);
		}

		/// <summary>
		/// Gets a descriptive text for the category of event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public override async Task<string> GetDescription(IServiceReferences ServiceReferences)
		{
			Contract Contract = await this.GetContract();
			StringBuilder Result = new();

			Result.Append(LocalizationResourceManager.Current["ContractUpdateReceived"]);

			if (Contract is not null)
			{
				Result.Append(": ");
				Result.Append(await ContractModel.GetCategory(Contract));
			}

			Result.Append('.');

			return Result.ToString();
		}
	}
}
