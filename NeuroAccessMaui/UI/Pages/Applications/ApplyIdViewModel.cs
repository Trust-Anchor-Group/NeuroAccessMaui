using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Data;
using NeuroAccessMaui.UI.Pages.Registration;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Applications
{
	/// <summary>
	/// The view model to bind to for when displaying the an application for a Personal ID.
	/// </summary>
	public partial class ApplyIdViewModel : RegisterIdentityModel
	{
		/// <summary>
		/// Creates an instance of the <see cref="ApplyIdViewModel"/> class.
		/// </summary>
		public ApplyIdViewModel()
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

			LegalIdentity? IdentityReference;

			if (ServiceRef.TagProfile.IdentityApplication is not null)
			{
				IdentityReference = ServiceRef.TagProfile.IdentityApplication;
				this.ApplicationSent = true;
			}
			else
			{
				this.ApplicationSent = false;
				IdentityReference = ServiceRef.TagProfile.LegalIdentity;
			}

			if (IdentityReference is not null)
			{
				this.SetProperties(IdentityReference.Properties, true);

				if (string.IsNullOrEmpty(this.OrgCountryCode))
				{
					this.OrgCountryCode = this.CountryCode;
					this.OrgCountryName = this.CountryName;
				}
			}

			ApplyIdNavigationArgs? Args = ServiceRef.NavigationService.PopLatestArgs<ApplyIdNavigationArgs>();

			if (Args is not null)
			{
				this.Personal = Args.Personal;
				this.Organizational = Args.Organizational;
			}
			else if (IdentityReference is not null)
			{
				this.Organizational = IdentityReference.IsOrganizational();
				this.Personal = !this.Organizational;
			}

			this.RequiresOrgName = this.Organizational;
			this.RequiresOrgDepartment = this.Organizational;
			this.RequiresOrgRole = this.Organizational;
			this.RequiresOrgNumber = this.Organizational;

			ServiceRef.XmppService.IdentityApplicationChanged += this.XmppService_IdentityApplicationChanged;

			await base.OnInitialize();

			if (!this.HasApplicationAttributes && this.IsConnected)
				await Task.Run(this.LoadApplicationAttributes);
		}

		protected override Task OnDispose()
		{
			ServiceRef.XmppService.IdentityApplicationChanged -= this.XmppService_IdentityApplicationChanged;

			return base.OnDispose();
		}

		private Task XmppService_IdentityApplicationChanged(object Sender, LegalIdentityEventArgs e)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.ApplicationSent = ServiceRef.TagProfile.IdentityApplication is not null;

			});

			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		protected override async Task XmppService_ConnectionStateChanged(object? Sender, XmppState NewState)
		{
			await base.XmppService_ConnectionStateChanged(Sender, NewState);
			this.OnPropertyChanged(nameof(this.ApplicationSentAndConnected));
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
						this.RequiresOrgAddress = this.Organizational && RequiresAddress;
						this.RequiresOrgAddress2 = this.Organizational && RequiresAddress2;
						this.RequiresOrgZipCode = this.Organizational && RequiresZipCode;
						this.RequiresOrgArea = this.Organizational && RequiresArea;
						this.RequiresOrgCity = this.Organizational && RequiresCity;
						this.RequiresOrgRegion = this.Organizational && RequiresRegion;
						this.RequiresOrgCountry = this.Organizational && RequiresCountry;
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
		[NotifyPropertyChangedFor(nameof(CanEdit))]
		[NotifyPropertyChangedFor(nameof(CanRemovePhoto))]
		[NotifyPropertyChangedFor(nameof(ApplicationSentAndConnected))]
		private bool applicationSent;

		/// <summary>
		/// If application is personal.
		/// </summary>
		[ObservableProperty]
		private bool personal;

		/// <summary>
		/// If application is organizational.
		/// </summary>
		[ObservableProperty]
		private bool organizational;

		/// <summary>
		/// If a photo is available.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanRemovePhoto))]
		private bool hasPhoto;

		/// <summary>
		/// Photo
		/// </summary>
		[ObservableProperty]
		private ImageSource? image;

		/// <summary>
		/// Rotation of <see cref="Image"/>
		/// </summary>
		[ObservableProperty]
		private int imageRotation;

		/// <summary>
		/// If the form can be edited.
		/// </summary>
		public bool CanEdit => !this.ApplicationSent;

		/// <summary>
		/// If the form can be edited.
		/// </summary>
		public bool CanRemovePhoto => this.CanEdit && this.HasPhoto;

		/// <summary>
		/// If application has been sent and app is connected.
		/// </summary>
		public bool ApplicationSentAndConnected => this.ApplicationSent && this.IsConnected;

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

					if (!this.FirstNameOk ||
						!this.MiddleNamesOk ||
						!this.LastNamesOk ||
						!this.PersonalNumberOk ||
						!this.AddressOk ||
						!this.Address2Ok ||
						!this.ZipCodeOk ||
						!this.AreaOk ||
						!this.CityOk ||
						!this.RegionOk ||
						!this.CountryOk)
					{
						return false;
					}

					if (this.RequiresCountryIso3166 && !ISO_3166_1.TryGetCountryByCode(this.CountryCode, out _))
						return false;

					if (this.Organizational)
					{
						if (!this.OrgNameOk ||
							!this.OrgDepartmentOk ||
							!this.OrgRoleOk ||
							!this.OrgNumberOk ||
							!this.OrgAddressOk ||
							!this.OrgAddress2Ok ||
							!this.OrgZipCodeOk ||
							!this.OrgAreaOk ||
							!this.OrgCityOk ||
							!this.OrgRegionOk ||
							!this.OrgCountryOk)
						{
							return false;
						}
					}
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
		[RelayCommand(CanExecute = nameof(ApplicationSent))]
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
		[RelayCommand(CanExecute = nameof(ApplicationSent))]
		private async Task ScanQrCode()
		{
			string? Url = await Services.UI.QR.QrCode.ScanQrCode(nameof(AppResources.QrPageTitleScanPeerId),
				[
					Constants.UriSchemes.IotId
				]);

			if (string.IsNullOrEmpty(Url) || !Constants.UriSchemes.StartsWithIdScheme(Url))
				return;

			string? ReviewerId = Constants.UriSchemes.RemoveScheme(Url);
			LegalIdentity? ToReview = ServiceRef.TagProfile.IdentityApplication;
			if (ToReview is null || string.IsNullOrEmpty(ReviewerId))
				return;

			try
			{
				this.SetIsBusy(true);

				await ServiceRef.XmppService.PetitionPeerReviewId(ReviewerId, ToReview, Guid.NewGuid().ToString(),
					ServiceRef.Localizer["CouldYouPleaseReviewMyIdentityInformation"]);

				await ServiceRef.UiSerializer.DisplayAlert(ServiceRef.Localizer["PetitionSent"], ServiceRef.Localizer["APetitionHasBeenSentToYourPeer"]);
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
		/// Removes the current photo.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanRemovePhoto))]
		private static async Task RemovePhoto()
		{
			// TODO
		}

		/// <summary>
		/// Takes a new photo
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanEdit))]
		private static async Task TakePhoto()
		{
			// TODO
		}

		/// <summary>
		/// Takes a new photo
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanEdit))]
		private static async Task PickPhoto()
		{
			// TODO
		}

		/// <summary>
		/// Select from a list of featured peer reviewers.
		/// </summary>
		[RelayCommand(CanExecute = nameof(ApplicationSent))]
		private static async Task FeaturedPeerReviewers()
		{
			// TODO
		}

		#endregion
	}
}
