using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Data;
using NeuroAccessMaui.UI.Pages.Registration;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Applications
{
	/// <summary>
	/// The view model to bind to for when displaying the an application for a Personal ID.
	/// </summary>
	public partial class ApplyPersonalIdViewModel : RegisterIdentityModel
	{
		/// <summary>
		/// Creates an instance of the <see cref="ApplyPersonalIdViewModel"/> class.
		/// </summary>
		public ApplyPersonalIdViewModel()
			: base()
		{
		}

		protected override async Task OnInitialize()
		{
			if (ServiceRef.TagProfile.IdentityApplication is not null)
			{
				if (ServiceRef.TagProfile.IdentityApplication.IsDiscarded())
					ServiceRef.TagProfile.IdentityApplication = null;
			}

			if (ServiceRef.TagProfile.IdentityApplication is not null)
			{
				this.ApplicationSent = true;
				this.SetProperties(ServiceRef.TagProfile.IdentityApplication.Properties, true);
			}
			else
			{
				this.ApplicationSent = false;
				if (ServiceRef.TagProfile.LegalIdentity is not null)
					this.SetProperties(ServiceRef.TagProfile.LegalIdentity.Properties, true);
			}

			await base.OnInitialize();

			if (!this.HasApplicationAttributes && this.IsConnected)
				await Task.Run(this.LoadApplicationAttributes);
		}

		/// <inheritdoc/>
		protected override async Task OnConnected()
		{
			await base.OnConnected();

			if (!this.HasApplicationAttributes && this.IsConnected)
				await Task.Run(this.LoadApplicationAttributes);
		}

		/// <inheritdoc/>
		public override void SetIsBusy(bool IsBusy)
		{
			base.SetIsBusy(IsBusy);
			this.NotifyCommandsCanExecuteChanged();
		}

		private async Task LoadApplicationAttributes()
		{
			try
			{
				IdApplicationAttributesEventArgs e = await ServiceRef.XmppService.GetIdApplicationAttributes();
				if (e.Ok)
				{
					MainThread.BeginInvokeOnMainThread(() =>
					{
						bool RequiresFirstName = false;
						bool RequiresMiddleNames = false;
						bool RequiresLastNames = false;
						bool RequiresPersonalNumber = false;
						bool RequiresAddress = false;
						bool RequiresAddress2 = false;
						bool RequiresZipCode = false;
						bool RequiresArea = false;
						bool RequiresCity = false;
						bool RequiresRegion = false;
						bool RequiresCountry = false;

						foreach (string Name in e.RequiredProperties)
						{
							switch (Name)
							{
								case Constants.XmppProperties.FirstName:
									RequiresFirstName = true;
									break;

								case Constants.XmppProperties.MiddleNames:
									RequiresMiddleNames = true;
									break;

								case Constants.XmppProperties.LastNames:
									RequiresLastNames = true;
									break;

								case Constants.XmppProperties.PersonalNumber:
									RequiresPersonalNumber = true;
									break;

								case Constants.XmppProperties.Address:
									RequiresAddress = true;
									break;

								case Constants.XmppProperties.Address2:
									RequiresAddress2 = true;
									break;

								case Constants.XmppProperties.Area:
									RequiresArea = true;
									break;

								case Constants.XmppProperties.City:
									RequiresCity = true;
									break;

								case Constants.XmppProperties.ZipCode:
									RequiresZipCode = true;
									break;

								case Constants.XmppProperties.Region:
									RequiresRegion = true;
									break;

								case Constants.XmppProperties.Country:
									RequiresCountry = true;
									break;
							}
						}

						this.PeerReview = e.PeerReview;
						this.NrPhotos = e.NrPhotos;
						this.NrReviewers = e.NrReviewers;
						this.RequiresCountryIso3166 = e.Iso3166;
						this.RequiresFirstName = RequiresFirstName;
						this.RequiresMiddleNames = RequiresMiddleNames;
						this.RequiresLastNames = RequiresLastNames;
						this.RequiresPersonalNumber = RequiresPersonalNumber;
						this.RequiresAddress = RequiresAddress;
						this.RequiresAddress2 = RequiresAddress2;
						this.RequiresZipCode = RequiresZipCode;
						this.RequiresArea = RequiresArea;
						this.RequiresCity = RequiresCity;
						this.RequiresRegion = RequiresRegion;
						this.RequiresCountry = RequiresCountry;
						this.HasApplicationAttributes = true;
					});
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		#region Properties

		/// <summary>
		/// If the user consents to the processing of the information.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private bool consent;

		/// <summary>
		/// If the user affirms information provided is correct.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private bool correct;

		/// <summary>
		/// If the view model has ID Application attributes loaded.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private bool hasApplicationAttributes;

		/// <summary>
		/// Number of photos required.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private int nrPhotos;

		/// <summary>
		/// Number of reviewers required.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private int nrReviewers;

		/// <summary>
		/// If peer review is permitted.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private bool peerReview;

		/// <summary>
		/// If an ID application has been sent.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private bool applicationSent;

		#endregion

		#region Commands

		/// <summary>
		/// Used to find out if an ICommand can execute
		/// </summary>
		public override bool CanApply
		{
			get
			{
				if (!this.CanExecuteCommands || !this.Consent || !this.Correct || this.ApplicationSent)
					return false;

				if (this.HasApplicationAttributes)
				{
					//if (this.NrPhotos > 0)
					//	return false;     // TODO

					if (!this.FirstNameOk)
						return false;

					if (!this.MiddleNamesOk)
						return false;

					if (!this.LastNamesOk)
						return false;

					if (!this.PersonalNumberOk)
						return false;

					if (!this.AddressOk)
						return false;

					if (!this.Address2Ok)
						return false;

					if (!this.ZipCodeOk)
						return false;

					if (!this.AreaOk)
						return false;

					if (!this.CityOk)
						return false;

					if (!this.RegionOk)
						return false;

					if (!this.CountryOk)
						return false;

					if (this.RequiresCountryIso3166 && !ISO_3166_1.TryGetCountryByCode(this.CountryCode, out _))
						return false;
				}

				return true;
			}
		}

		/// <summary>
		/// Executes the application command.
		/// </summary>
		protected override async Task Apply()
		{
			if (this.ApplicationSent)
				return;

			if (!await AreYouSure(ServiceRef.Localizer[nameof(AppResources.AreYouSureYouWantToSendThisIdApplication)]))
				return;

			if (!await App.AuthenticateUser(true))
				return;

			try
			{
				this.SetIsBusy(true);

				(bool Succeeded, LegalIdentity? AddedIdentity) = await ServiceRef.NetworkService.TryRequest(() =>
					ServiceRef.XmppService.AddLegalIdentity(this, false));

				if (Succeeded && AddedIdentity is not null)
				{
					ServiceRef.TagProfile.IdentityApplication = AddedIdentity;
					this.ApplicationSent = true;
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiSerializer.DisplayException(ex);
			}
			finally
			{
				this.SetIsBusy(false);
			}
		}

		/// <summary>
		/// Revokes the current application.
		/// </summary>
		[RelayCommand]
		private async Task RevokeApplication()
		{
			LegalIdentity? Application = ServiceRef.TagProfile.IdentityApplication;
			if (Application is null)
			{
				this.ApplicationSent = false;
				return;
			}

			if (!await AreYouSure(ServiceRef.Localizer[nameof(AppResources.AreYouSureYouWantToRevokeTheCurrentIdApplication)]))
				return;

			if (!await App.AuthenticateUser(true))
				return;

			try
			{
				this.SetIsBusy(true);

				await ServiceRef.XmppService.ObsoleteLegalIdentity(Application.Id);

				ServiceRef.TagProfile.IdentityApplication = null;
				this.ApplicationSent = false;
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiSerializer.DisplayException(ex);
			}
			finally
			{
				this.SetIsBusy(false);
			}
		}

		/// <summary>
		/// Scan a QR-code belonging to a peer
		/// </summary>
		[RelayCommand]
		private static async Task ScanQrCode()
		{
			string? Url = await Services.UI.QR.QrCode.ScanQrCode(nameof(AppResources.QrPageTitleScanPeerId),
				[
					Constants.UriSchemes.IotId
				]);

			if (string.IsNullOrEmpty(Url))
				return;

			await App.OpenUrlAsync(Url);     // TODO: Send peer review petition
		}

		/// <summary>
		/// Select from a list of featured peer reviewers.
		/// </summary>
		[RelayCommand]
		private static async Task FeaturedPeerReviewers()
		{
			// TODO
		}

		#endregion
	}
}
