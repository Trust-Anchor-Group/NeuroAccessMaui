using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.Navigation;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels;
using NeuroAccessMaui.UI.Pages.Contracts.ViewContract;
using System.Text;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.Services.Notification.Contracts
{
	/// <summary>
	/// Notification event for contract petition responses.
	/// </summary>
	public class ContractResponseNotificationEvent : ContractNotificationEvent
	{
		/// <summary>
		/// Notification event for contract petition responses.
		/// </summary>
		public ContractResponseNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Notification event for contract petition responses.
		/// </summary>
		/// <param name="e">Event arguments.</param>
		public ContractResponseNotificationEvent(ContractPetitionResponseEventArgs e)
			: base(e)
		{
			this.Response = e.Response;
		}

		/// <summary>
		/// Response
		/// </summary>
		public bool Response { get; set; }

		/// <summary>
		/// Opens the event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public override async Task Open()
		{
			Contract? Contract = await this.GetContract();

			if (!this.Response || Contract is null)
			{
				await ServiceRef.UiSerializer.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.Message)],
					ServiceRef.Localizer[nameof(AppResources.PetitionToViewContractWasDenied)], ServiceRef.Localizer[nameof(AppResources.Ok)]);
			}
			else
			{
				ViewContractNavigationArgs Args = new(Contract, false);

				await ServiceRef.NavigationService.GoToAsync(nameof(ViewContractPage), Args, BackMethod.Pop);
			}
		}

		/// <summary>
		/// Gets a descriptive text for the category of event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public override async Task<string> GetDescription()
		{
			Contract? Contract = await this.GetContract();
			StringBuilder Result = new();

			Result.Append(ServiceRef.Localizer[nameof(AppResources.ContractResponse)]);

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
