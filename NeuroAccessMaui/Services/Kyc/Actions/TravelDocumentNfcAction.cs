using System.Threading.Tasks;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Kyc.Models;
using NeuroAccessMaui.UI.Pages.Kyc;

namespace NeuroAccessMaui.Services.Kyc.Actions
{
	/// <summary>
	/// Opens the travel-document NFC evidence flow for a KYC page.
	/// </summary>
	/// <remarks>
	/// Use <c>TravelDocumentNfc</c> in page metadata to attach this built-in action to a KYC page.
	/// </remarks>
	public class TravelDocumentNfcAction : IKycPageAction
	{
		/// <summary>
		/// Gets the metadata action name for travel-document NFC readout.
		/// </summary>
		public const string ActionName = "TravelDocumentNfc";

		/// <inheritdoc/>
		public string Name => ActionName;

		/// <inheritdoc/>
		public Task<KycPageActionState> GetStateAsync(KycPageActionContext Context)
		{
			if (!this.IsAvailable(Context)
				|| !string.IsNullOrWhiteSpace(Context.Reference.NfcReadoutXml))
			{
				return Task.FromResult(new KycPageActionState
				{
					IsVisible = false
				});
			}

			KycPageActionState State = new KycPageActionState
			{
				Title = ServiceRef.Localizer[nameof(AppResources.KycTravelDocumentSummaryTitle)],
				Description = ServiceRef.Localizer[nameof(AppResources.KycTravelDocumentDescription)],
				StatusText = this.ResolveStatusText(Context),
				PrimaryButtonText = ServiceRef.Localizer[nameof(AppResources.KycTravelDocumentOpenButton)]
			};

			return Task.FromResult(State);
		}

		/// <inheritdoc/>
		public async Task ExecuteAsync(KycPageActionContext Context)
		{
			Context.CancellationToken.ThrowIfCancellationRequested();
			if (!this.IsAvailable(Context))
			{
				return;
			}

			await Context.NavigationService.GoToAsync(
				nameof(KycTravelDocumentPage),
				new KycProcessNavigationArgs(Context.Reference));
		}

		private bool IsAvailable(KycPageActionContext Context)
		{
			return Context.Process.ApplicationPolicy.Mode == KycApplicationMode.Preview &&
				Context.Process.EvidencePolicy.TravelDocument.Nfc.Enabled &&
				ServiceRef.Provider.GetRequiredService<NeuroAccessMaui.Services.Nfc.INfcIsoDepSessionService>().IsPlatformSupported;
		}

		private string ResolveStatusText(KycPageActionContext Context)
		{
			if (!string.IsNullOrWhiteSpace(Context.Reference.NfcReadoutXml))
			{
				return ServiceRef.Localizer[nameof(AppResources.KycTravelDocumentSummaryReadoutReady)];
			}

			if (Context.Reference.HasFullTravelDocumentMrz)
			{
				return ServiceRef.Localizer[nameof(AppResources.KycTravelDocumentSummaryMrzReady)];
			}

			return ServiceRef.Localizer[nameof(AppResources.KycTravelDocumentSummaryMissing)];
		}
	}
}
