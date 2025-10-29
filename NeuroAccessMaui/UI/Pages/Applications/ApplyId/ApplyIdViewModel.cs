using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Data;
using NeuroAccessMaui.Services.Data.PersonalNumbers;
using NeuroAccessMaui.Services.Identity;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.UI.Photos;
using NeuroAccessMaui.UI.Controls;
using NeuroAccessMaui.UI.Pages.Identity.ViewIdentity;
using NeuroAccessMaui.UI.Pages.Registration;
using NeuroAccessMaui.UI.Pages.Utility.Images;
using NeuroAccessMaui.UI.Pages.Wallet.ServiceProviders;
using SkiaSharp;
using Waher.Content;
using Waher.Content.Html.Elements;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Contracts.EventArguments;
using Waher.Networking.XMPP.StanzaErrors;
using IServiceProvider = Waher.Networking.XMPP.Contracts.IServiceProvider;

namespace NeuroAccessMaui.UI.Pages.Applications.ApplyId
{

	/// <summary>
	/// The view model to bind to for when displaying the an application for a Personal ID.
	/// </summary>
	public partial class ApplyIdViewModel : RegisterIdentityModel
	{
		private const string profilePhotoFileName = "ProfilePhoto.jpg";
		private readonly string localPhotoFileName;
		private readonly PhotosLoader photosLoader;
		private LegalIdentityAttachment? photo;
		private ServiceProviderWithLegalId[]? peerReviewServices = null;
		private ApplicationReview? applicationReview;

		private const string passportFileName = "Passport.jpeg";
		private const string nationalIdFrontFileName = "IdCardFront.jpg";
		private const string nationalIdBackFileName = "IdCardBack.jpg";
		private const string driverLicenseFrontFileName = "DriverLicenseFront.jpeg";
		private const string driverLicenseBackFileName = "DriverLicenseBack.jpeg";
		private const string additionalPhotoPrefix = "AdditionalPhoto";

	private static readonly Dictionary<string, string> claimDisplayNameMap = BuildClaimDisplayNameMap();
	private static readonly Dictionary<string, Action<ApplyIdViewModel>> claimClearActions = BuildClaimClearActions();
	private static readonly HashSet<string> birthClaimKeys = new(StringComparer.OrdinalIgnoreCase)
	{
		Constants.XmppProperties.BirthDay,
			Constants.XmppProperties.BirthMonth,
			Constants.XmppProperties.BirthYear,
			"BirthDate"
		};

	private static readonly HashSet<string> claimsHiddenInPending = new(StringComparer.OrdinalIgnoreCase)
	{
		Constants.XmppProperties.EMail,
		Constants.XmppProperties.Phone,
		"EMail",
		"Email",
		"PhoneNumber",
		"Phone",
		"PhoneNr"
	};

	private static Dictionary<string, string> BuildClaimDisplayNameMap()
	{
		Dictionary<string, string> map = new(StringComparer.OrdinalIgnoreCase);
		AddDisplayMap(map, nameof(AppResources.FirstName), Constants.XmppProperties.FirstName, "FirstName", "firstName");
		AddDisplayMap(map, nameof(AppResources.MiddleNames), Constants.XmppProperties.MiddleNames, "MiddleNames", "middleNames");
		AddDisplayMap(map, nameof(AppResources.LastNames), Constants.XmppProperties.LastNames, "LastNames", "lastNames");
		AddDisplayMap(map, nameof(AppResources.PersonalNumber), Constants.XmppProperties.PersonalNumber, "PersonalNumber", "personalNumber");
		AddDisplayMap(map, nameof(AppResources.Address), Constants.XmppProperties.Address, "Address", "address", "ADDR1", "addr1");
		AddDisplayMap(map, nameof(AppResources.Address2), Constants.XmppProperties.Address2, "Address2", "address2");
		AddDisplayMap(map, nameof(AppResources.ZipCode), Constants.XmppProperties.ZipCode, "ZipCode", "zipCode", "PostalCode", "postalCode");
		AddDisplayMap(map, nameof(AppResources.Area), Constants.XmppProperties.Area, "Area", "area");
		AddDisplayMap(map, nameof(AppResources.City), Constants.XmppProperties.City, "City", "city", "CITY");
		AddDisplayMap(map, nameof(AppResources.Region), Constants.XmppProperties.Region, "Region", "region", "State", "state");
		AddDisplayMap(map, nameof(AppResources.Country), Constants.XmppProperties.Country, "Country", "country", "CountryCode", "countryCode");
		AddDisplayMap(map, nameof(AppResources.Nationality), Constants.XmppProperties.Nationality, "Nationality", "nationality", "NationalityCode", "nationalityCode");
		AddDisplayMap(map, nameof(AppResources.Gender), Constants.XmppProperties.Gender, "Gender", "gender", "GenderCode", "genderCode");
		AddDisplayMap(map, nameof(AppResources.BirthDate), Constants.XmppProperties.BirthDay, Constants.XmppProperties.BirthMonth, Constants.XmppProperties.BirthYear, "BirthDate", "birthDate");
		AddDisplayMap(map, nameof(AppResources.OrgName), Constants.XmppProperties.OrgName, "OrgName", "orgName");
		AddDisplayMap(map, nameof(AppResources.OrgNumber), Constants.XmppProperties.OrgNumber, "OrgNumber", "orgNumber");
		AddDisplayMap(map, nameof(AppResources.OrgDepartment), Constants.XmppProperties.OrgDepartment, "OrgDepartment", "orgDepartment");
		AddDisplayMap(map, nameof(AppResources.OrgRole), Constants.XmppProperties.OrgRole, "OrgRole", "orgRole");
		AddDisplayMap(map, nameof(AppResources.OrgAddress), Constants.XmppProperties.OrgAddress, "OrgAddress", "orgAddress", "OrgADDR", "orgAddr");
		AddDisplayMap(map, nameof(AppResources.OrgAddress2), Constants.XmppProperties.OrgAddress2, "OrgAddress2", "orgAddress2", "OrgADDR2", "orgAddr2");
		AddDisplayMap(map, nameof(AppResources.OrgZipCode), Constants.XmppProperties.OrgZipCode, "OrgZipCode", "orgZipCode", "OrgZIP", "orgZip");
		AddDisplayMap(map, nameof(AppResources.OrgArea), Constants.XmppProperties.OrgArea, "OrgArea", "orgArea");
		AddDisplayMap(map, nameof(AppResources.OrgCity), Constants.XmppProperties.OrgCity, "OrgCity", "orgCity");
		AddDisplayMap(map, nameof(AppResources.OrgRegion), Constants.XmppProperties.OrgRegion, "OrgRegion", "orgRegion", "OrgState", "orgState");
		AddDisplayMap(map, nameof(AppResources.OrgCountry), Constants.XmppProperties.OrgCountry, "OrgCountry", "orgCountry", "OrgCountryCode", "orgCountryCode");
		AddDisplayMap(map, nameof(AppResources.EMail), Constants.XmppProperties.EMail, "EMail", "Email", "email");
		AddDisplayMap(map, nameof(AppResources.Phone), Constants.XmppProperties.Phone, "Phone", "phone", "PhoneNr", "phoneNr", "PhoneNumber", "phoneNumber");
		return map;
	}

	private static void AddDisplayMap(Dictionary<string, string> map, string resourceKey, params string[] keys)
	{
		foreach (string key in keys)
		{
			map[key] = resourceKey;
		}
	}

	private static Dictionary<string, Action<ApplyIdViewModel>> BuildClaimClearActions()
	{
		Dictionary<string, Action<ApplyIdViewModel>> map = new(StringComparer.OrdinalIgnoreCase);
		AddClearAction(map, vm => vm.FirstName = string.Empty, Constants.XmppProperties.FirstName, "FirstName", "firstName");
		AddClearAction(map, vm => vm.MiddleNames = string.Empty, Constants.XmppProperties.MiddleNames, "MiddleNames", "middleNames");
		AddClearAction(map, vm => vm.LastNames = string.Empty, Constants.XmppProperties.LastNames, "LastNames", "lastNames");
		AddClearAction(map, vm => vm.PersonalNumber = string.Empty, Constants.XmppProperties.PersonalNumber, "PersonalNumber", "personalNumber");
		AddClearAction(map, vm => vm.Address = string.Empty, Constants.XmppProperties.Address, "Address", "address", "ADDR1", "addr1");
		AddClearAction(map, vm => vm.Address2 = string.Empty, Constants.XmppProperties.Address2, "Address2", "address2");
		AddClearAction(map, vm => vm.ZipCode = string.Empty, Constants.XmppProperties.ZipCode, "ZipCode", "zipCode", "PostalCode", "postalCode");
		AddClearAction(map, vm => vm.Area = string.Empty, Constants.XmppProperties.Area, "Area", "area");
		AddClearAction(map, vm => vm.City = string.Empty, Constants.XmppProperties.City, "City", "city", "CITY");
		AddClearAction(map, vm => vm.Region = string.Empty, Constants.XmppProperties.Region, "Region", "region", "State", "state");
		AddClearAction(map, vm => vm.CountryCode = string.Empty, Constants.XmppProperties.Country, "Country", "country", "CountryCode", "countryCode");
		AddClearAction(map, vm => { vm.Nationality = null; vm.NationalityCode = string.Empty; }, Constants.XmppProperties.Nationality, "Nationality", "nationality", "NationalityCode", "nationalityCode");
		AddClearAction(map, vm => { vm.Gender = null; vm.GenderCode = string.Empty; }, Constants.XmppProperties.Gender, "Gender", "gender", "GenderCode", "genderCode");
		AddClearAction(map, vm => vm.BirthDate = DateTime.Today, Constants.XmppProperties.BirthDay, Constants.XmppProperties.BirthMonth, Constants.XmppProperties.BirthYear, "BirthDate", "birthDate");
		AddClearAction(map, vm => vm.OrgName = string.Empty, Constants.XmppProperties.OrgName, "OrgName", "orgName");
		AddClearAction(map, vm => vm.OrgNumber = string.Empty, Constants.XmppProperties.OrgNumber, "OrgNumber", "orgNumber");
		AddClearAction(map, vm => vm.OrgDepartment = string.Empty, Constants.XmppProperties.OrgDepartment, "OrgDepartment", "orgDepartment");
		AddClearAction(map, vm => vm.OrgRole = string.Empty, Constants.XmppProperties.OrgRole, "OrgRole", "orgRole");
		AddClearAction(map, vm => vm.OrgAddress = string.Empty, Constants.XmppProperties.OrgAddress, "OrgAddress", "orgAddress", "OrgADDR", "orgAddr");
		AddClearAction(map, vm => vm.OrgAddress2 = string.Empty, Constants.XmppProperties.OrgAddress2, "OrgAddress2", "orgAddress2", "OrgADDR2", "orgAddr2");
		AddClearAction(map, vm => vm.OrgZipCode = string.Empty, Constants.XmppProperties.OrgZipCode, "OrgZipCode", "orgZipCode", "OrgZIP", "orgZip");
		AddClearAction(map, vm => vm.OrgArea = string.Empty, Constants.XmppProperties.OrgArea, "OrgArea", "orgArea");
		AddClearAction(map, vm => vm.OrgCity = string.Empty, Constants.XmppProperties.OrgCity, "OrgCity", "orgCity");
		AddClearAction(map, vm => vm.OrgRegion = string.Empty, Constants.XmppProperties.OrgRegion, "OrgRegion", "orgRegion", "OrgState", "orgState");
		AddClearAction(map, vm => vm.OrgCountryCode = string.Empty, Constants.XmppProperties.OrgCountry, "OrgCountry", "orgCountry", "OrgCountryCode", "orgCountryCode");
		AddClearAction(map, vm => vm.PhoneNr = string.Empty, Constants.XmppProperties.Phone, "Phone", "phone", "PhoneNr", "phoneNr", "PhoneNumber", "phoneNumber");
		AddClearAction(map, vm => vm.EMail = string.Empty, Constants.XmppProperties.EMail, "EMail", "Email", "email");
		AddClearAction(map, vm => vm.Consent = false, "Consent", "consent");
		AddClearAction(map, vm => vm.Correct = false, "Correct", "correct");
		return map;
	}

	private static void AddClearAction(Dictionary<string, Action<ApplyIdViewModel>> map, Action<ApplyIdViewModel> action, params string[] keys)
	{
		foreach (string key in keys)
		{
			map[key] = action;
		}
	}

		/// <summary>
		/// Creates an instance of the <see cref="ApplyIdViewModel"/> class.
		/// </summary>
		public ApplyIdViewModel()
			: base()
		{
			this.localPhotoFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), profilePhotoFileName);
			this.photosLoader = new PhotosLoader();
			this.countries = new ObservableCollection<ISO_3166_Country>(ISO_3166_1.Countries);
			this.genders = new ObservableCollection<ISO_5218_Gender>(ISO_5218.Genders);
		}

		protected override async Task OnInitialize()
		{
			this.ApplicationId = null;

			if (ServiceRef.TagProfile.IdentityApplication is not null)
			{
				if (ServiceRef.TagProfile.IdentityApplication.IsDiscarded())
					await ServiceRef.TagProfile.SetIdentityApplication(null, true);
			}

			LegalIdentity? IdentityReference;

			if (ServiceRef.TagProfile.IdentityApplication is not null)
			{
				IdentityReference = ServiceRef.TagProfile.IdentityApplication;
				this.ApplicationSent = true;
				this.ApplicationId = IdentityReference.Id;
				this.NrReviews = ServiceRef.TagProfile.NrReviews;

				await Task.Run(this.LoadFeaturedPeerReviewers);
			}
			else
			{
				this.ApplicationSent = false;
				IdentityReference = ServiceRef.TagProfile.LegalIdentity;
				this.NrReviews = 0;
				this.peerReviewServices = null;
				this.HasFeaturedPeerReviewers = false;
			}

			ApplyIdNavigationArgs? Args = ServiceRef.UiService.PopLatestArgs<ApplyIdNavigationArgs>();

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

			if (IdentityReference is not null)
			{
				await this.SetProperties(IdentityReference, Args?.ReusePhoto ?? true, true, true, this.Organizational, true);

				if (string.IsNullOrEmpty(this.OrgCountryCode) && this.Organizational)
					this.OrgCountryCode = this.CountryCode;

				if (string.IsNullOrEmpty(this.NationalityCode) && this.RequiresNationality)
					this.NationalityCode = this.CountryCode;
			}

			this.RequiresOrgName = this.Organizational;
			this.RequiresOrgDepartment = this.Organizational;
			this.RequiresOrgRole = this.Organizational;
			this.RequiresOrgNumber = this.Organizational;

			this.LoadApplicationReview(ServiceRef.TagProfile.ApplicationReview);
			ServiceRef.TagProfile.Changed += this.TagProfile_Changed;
			ServiceRef.XmppService.IdentityApplicationChanged += this.XmppService_IdentityApplicationChanged;

			if (this.ApplicationSent && IdentityReference is not null)
			{
				await this.LoadAllAttachmentPhotos(IdentityReference);
			}

			await base.OnInitialize();

			if (!this.HasApplicationAttributes && this.IsConnected)
				await Task.Run(this.LoadApplicationAttributes);
		}

		protected override Task OnDispose()
		{
			this.photosLoader.CancelLoadPhotos();

			ServiceRef.TagProfile.Changed -= this.TagProfile_Changed;
			ServiceRef.XmppService.IdentityApplicationChanged -= this.XmppService_IdentityApplicationChanged;

			return base.OnDispose();
		}

		private Task XmppService_IdentityApplicationChanged(object? Sender, LegalIdentityEventArgs e)
		{
			MainThread.BeginInvokeOnMainThread(async () =>
			{
				this.ApplicationSent = ServiceRef.TagProfile.IdentityApplication is not null;
				this.NrReviews = ServiceRef.TagProfile.NrReviews;

				if (this.ApplicationId is not null && this.ApplicationId == ServiceRef.TagProfile.LegalIdentity?.Id)
					await ServiceRef.UiService.GoToAsync(nameof(ViewIdentityPage), BackMethod.Pop2);
				else
				{
					if (this.ApplicationSent)
					{
						if (this.peerReviewServices is null)
							await Task.Run(this.LoadFeaturedPeerReviewers);
					}
					else
					{
						this.peerReviewServices = null;
						this.HasFeaturedPeerReviewers = false;
					}

					if (!this.ApplicationSent && !this.IsRevoking)
					{
						await ServiceRef.UiService.DisplayAlert(
							ServiceRef.Localizer[nameof(AppResources.Rejected)],
							ServiceRef.Localizer[nameof(AppResources.YourApplicationWasRejected)]);
					}
				}
			});

			return Task.CompletedTask;
		}

		private void TagProfile_Changed(object? sender, PropertyChangedEventArgs e)
		{
			if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(TagProfile.ApplicationReview))
			{
				ApplicationReview? Review = ServiceRef.TagProfile.ApplicationReview;
				MainThread.BeginInvokeOnMainThread(() => this.LoadApplicationReview(Review));
			}
		}

		/// <inheritdoc/>
		protected override async Task XmppService_ConnectionStateChanged(object? Sender, XmppState NewState)
		{
			await base.XmppService_ConnectionStateChanged(Sender, NewState);
			this.OnPropertyChanged(nameof(this.ApplicationSentAndConnected));
			this.NotifyCommandsCanExecuteChanged();
		}

		/// <inheritdoc/>
		protected override async Task OnConnected()
		{
			await base.OnConnected();

			if (!this.HasApplicationAttributes && this.IsConnected)
				await Task.Run(this.LoadApplicationAttributes);

			this.NotifyCommandsCanExecuteChanged();
		}

		/// <inheritdoc/>
		public override void SetIsBusy(bool IsBusy)
		{
			base.SetIsBusy(IsBusy);
			this.NotifyCommandsCanExecuteChanged();
		}

		/// <summary>
		/// Sets the properties of the view model.
		/// </summary>
		/// <param name="Identity">Identity containing properties to set.</param>
		/// <param name="SetPhoto">If Photo is to be set.</param>
		/// <param name="ClearPropertiesNotFound">If properties should be cleared if they are not found in <paramref name="Identity"/>.</param>
		/// <param name="SetPersonalProperties">If personal properties are to be set.</param>
		/// <param name="SetOrganizationalProperties">If organizational properties are to be set.</param>
		/// <param name="SetAppProperties">If app-specific properties are to be set.</param>
		protected async Task SetProperties(LegalIdentity Identity, bool SetPhoto, bool ClearPropertiesNotFound, bool SetPersonalProperties,
			bool SetOrganizationalProperties, bool SetAppProperties)
		{
			await base.SetProperties(Identity, ClearPropertiesNotFound, SetPersonalProperties, SetOrganizationalProperties, SetAppProperties);

			int i = 0;

			foreach (ISO_3166_Country Country in this.Countries)
			{
				if (Country.Alpha2 == this.CountryCode)
				{
					this.Countries.Move(i, 0);
					break;
				}

				i++;
			}

			if (Identity?.Attachments is not null)
				await this.RestoreAttachmentStateAsync(Identity.Attachments, SetPhoto, ClearPropertiesNotFound);
			else if (ClearPropertiesNotFound)
				this.ResetAllPhotoState();
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
						bool RequiresNationality = false;
						bool RequiresGender = false;
						bool RequiresBirthDate = false;

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

								case Constants.XmppProperties.Nationality:
									RequiresNationality = true;
									break;

								case Constants.XmppProperties.Gender:
									RequiresGender = true;
									break;

								case Constants.XmppProperties.BirthDay:
								case Constants.XmppProperties.BirthMonth:
								case Constants.XmppProperties.BirthYear:
									RequiresBirthDate = true;
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
						this.RequiresNationality = RequiresNationality;
						this.RequiresGender = RequiresGender;
						this.RequiresBirthDate = RequiresBirthDate;
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
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		#region Properties

		/// <summary>
		/// The type of document the user provides
		/// </summary>
		[ObservableProperty]
		private IdentityDocumentType documentType = IdentityDocumentType.None;

		/// <summary>
		/// Collection with all document types.
		/// </summary>
		[ObservableProperty]
		private ObservableCollection<IdentityDocumentType> documentTypes =
			new ObservableCollection<IdentityDocumentType>(
				Enum.GetValues(typeof(IdentityDocumentType)).Cast<IdentityDocumentType>());

		/// <summary>
		/// Collection with all attached photos.
		/// </summary>
		[ObservableProperty]
		private ObservableCollection<ImageSource> allPhotos = [];

		/// <summary>
		/// IF the user has a front proof of ID.
		/// </summary>
		[ObservableProperty]
		private bool hasProofOfIdFront;

		/// <summary>
		/// The ImageSource of the front proof of ID.
		/// </summary>
		[ObservableProperty]
		private ImageSource? proofOfIdFrontImage;

		/// <summary>
		/// The binary representation of the front proof of ID.
		/// </summary>
		[ObservableProperty]
		private byte[]? proofOfIdFrontImageBin;

		/// <summary>
		/// if the user has a back proof of ID.
		/// </summary>
		[ObservableProperty]
		private bool hasProofOfIdBack;

		/// <summary>
		/// The ImageSource of the back proof of ID.
		/// </summary>
		[ObservableProperty]
		private ImageSource? proofOfIdBackImage;

		/// <summary>
		/// The binary representation of the back proof of ID.
		/// </summary>
		[ObservableProperty]
		private byte[]? proofOfIdBackImageBin;

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
		/// Number of reviews on current application.
		/// </summary>
		[ObservableProperty]
		private int nrReviews;

		/// <summary>
		/// If peer review is permitted.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(FeaturedPeerReviewers))]
		private bool peerReview;

		/// <summary>
		/// If an ID application has been sent.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyCanExecuteChangedFor(nameof(ScanQrCodeCommand))]
		[NotifyCanExecuteChangedFor(nameof(RequestReviewCommand))]
		[NotifyCanExecuteChangedFor(nameof(RevokeApplicationCommand))]
		[NotifyPropertyChangedFor(nameof(CanEdit))]
		[NotifyPropertyChangedFor(nameof(CanRemovePhoto))]
		[NotifyPropertyChangedFor(nameof(CanTakePhoto))]
		[NotifyPropertyChangedFor(nameof(ApplicationSentAndConnected))]
		[NotifyPropertyChangedFor(nameof(CanRequestFeaturedPeerReviewer))]
		[NotifyPropertyChangedFor(nameof(FeaturedPeerReviewers))]
		[NotifyPropertyChangedFor(nameof(ShowApplicationSection))]
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
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyCanExecuteChangedFor(nameof(RemovePhotoCommand))]
		private bool hasPhoto;

		/// <summary>
		/// Photo
		/// </summary>
		[ObservableProperty]
		private ImageSource? image;

		/// <summary>
		/// Binary representation of photo
		/// </summary>
		[ObservableProperty]
		private byte[]? imageBin;

		/// <summary>
		/// Rotation of <see cref="Image"/>
		/// </summary>
		[ObservableProperty]
		private int imageRotation;

		/// <summary>
		/// Indicates if review information is available for the current application.
		/// </summary>
		[ObservableProperty]
		private bool hasApplicationReview;

		/// <summary>
		/// Latest review message presented to the user.
		/// </summary>
		[ObservableProperty]
		private string applicationReviewMessage = string.Empty;

		/// <summary>
		/// Optional review code identifying the reason.
		/// </summary>
		[ObservableProperty]
		private string? applicationReviewCode;

		/// <summary>
		/// Timestamp for when the review was received.
		/// </summary>
		[ObservableProperty]
		private DateTime? applicationReviewTimestamp;

		/// <summary>
		/// Detailed invalid claim entries.
		/// </summary>
		public ObservableCollection<ApplicationReviewClaimDetail> InvalidClaimDetails { get; } = new ObservableCollection<ApplicationReviewClaimDetail>();

		/// <summary>
		/// Detailed invalid photo entries.
		/// </summary>
		public ObservableCollection<ApplicationReviewPhotoDetail> InvalidPhotoDetails { get; } = new ObservableCollection<ApplicationReviewPhotoDetail>();

		/// <summary>
		/// Claims still pending validation.
		/// </summary>
		public ObservableCollection<string> UnvalidatedClaims { get; } = new ObservableCollection<string>();

		/// <summary>
		/// Photos still pending validation.
		/// </summary>
		public ObservableCollection<string> UnvalidatedPhotos { get; } = new ObservableCollection<string>();

		/// <summary>
		/// True if there are invalid claims to address.
		/// </summary>
		public bool HasInvalidClaimDetails => this.InvalidClaimDetails.Count > 0;

		/// <summary>
		/// True if there are invalid photos to address.
		/// </summary>
		public bool HasInvalidPhotoDetails => this.InvalidPhotoDetails.Count > 0;

		/// <summary>
		/// True if there are claims that remain unvalidated.
		/// </summary>
		public bool HasUnvalidatedClaims => this.UnvalidatedClaims.Count > 0;

		/// <summary>
		/// True if there are photos that remain unvalidated.
		/// </summary>
		public bool HasUnvalidatedPhotos => this.UnvalidatedPhotos.Count > 0;

		/// <summary>
		/// True if any invalid items require attention.
		/// </summary>
		public bool HasInvalidItems => this.HasInvalidClaimDetails || this.HasInvalidPhotoDetails;

		/// <summary>
		/// True if the review contains only invalid items (no pending items).
		/// </summary>
		public bool HasInvalidOnly => this.HasInvalidItems && !this.HasOnlyUnvalidatedItems;

		/// <summary>
		/// True if only unvalidated items remain.
		/// </summary>
		public bool HasOnlyUnvalidatedItems => !this.HasInvalidItems && (this.HasUnvalidatedClaims || this.HasUnvalidatedPhotos);

		/// <summary>
		/// True if the user can start fixing invalid claims.
		/// </summary>
		public bool CanFixInvalidClaims => this.HasApplicationReview && this.HasInvalidItems;

		/// <summary>
		/// True if the user can resubmit without pending items.
		/// </summary>
		public bool CanPrepareReapplyWithoutPendingClaims => this.HasApplicationReview && this.HasOnlyUnvalidatedItems;

		/// <summary>
		/// If the form can be edited.
		/// </summary>
		public bool CanEdit => !this.ApplicationSent;

		/// <summary>
		/// If the form can be edited.
		/// </summary>
		public bool CanRemovePhoto => this.CanEdit && this.HasPhoto;

		/// <summary>
		/// If a photo can be taken.
		/// </summary>
		public bool CanTakePhoto => this.CanEdit && MediaPicker.IsCaptureSupported;

		/// <summary>
		/// If application has been sent and app is connected.
		/// </summary>
		public bool ApplicationSentAndConnected => this.ApplicationSent && this.IsConnected;

		/// <summary>
		/// If the application card (peer review actions) should be visible.
		/// </summary>
		public bool ShowApplicationSection => this.ApplicationSent && !this.HasInvalidItems;

		/// <summary>
		/// If app is in the processing of uploading application.
		/// </summary>
		[ObservableProperty]
		private bool isApplying;

		/// <summary>
		/// If app is in the processing of revoking an application.
		/// </summary>
		[ObservableProperty]
		private bool isRevoking;

		/// <summary>
		/// ID of application.
		/// </summary>
		[ObservableProperty]
		private string? applicationId;

		/// <summary>
		/// If view has featured peer reviewers.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(this.RequestReviewCommand))]
		[NotifyPropertyChangedFor(nameof(CanRequestFeaturedPeerReviewer))]
		[NotifyPropertyChangedFor(nameof(FeaturedPeerReviewers))]
		private bool hasFeaturedPeerReviewers;

		/// <summary>
		/// If the user can request a review from a featured peer reviewer.
		/// </summary>
		public bool CanRequestFeaturedPeerReviewer => this.ApplicationSent && this.HasFeaturedPeerReviewers;

		/// <summary>
		/// If the option to request featured peer reviewer should be shown.
		/// </summary>
		public bool FeaturedPeerReviewers => this.CanRequestFeaturedPeerReviewer && this.PeerReview;

		/// <summary>
		/// Available country definitions
		/// </summary>
		[ObservableProperty]
		private ObservableCollection<ISO_3166_Country> countries;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(NationalityCode))]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private ISO_3166_Country? nationality;

		/// <summary>
		/// Available gender definitions
		/// </summary>
		[ObservableProperty]
		private ObservableCollection<ISO_5218_Gender> genders;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(GenderCode))]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private ISO_5218_Gender? gender;

		/// <inheritdoc/>
		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(this.Nationality):
					this.NationalityCode = this.Nationality?.Alpha2 ?? string.Empty;
					break;

				case nameof(this.Gender):
					this.GenderCode = this.Gender?.Letter ?? string.Empty;
					break;
			}

			base.OnPropertyChanged(e);
		}

		#endregion

		private async Task RestoreAttachmentStateAsync(Attachment[] attachments, bool setPhoto, bool clearIfMissing)
		{
			if (!setPhoto)
			{
				if (clearIfMissing)
					this.ResetAllPhotoState();

				return;
			}

			await this.RestoreProfilePhotoAsync(attachments, clearIfMissing);
			await this.RestoreDocumentAttachmentsAsync(attachments, clearIfMissing);
			await this.RestoreAdditionalPhotosAsync(attachments, clearIfMissing);
		}

		private void ResetAllPhotoState()
		{
			this.RemovePhoto(false);
			this.RemoveProofOfIdFront();
			this.RemoveProofOfIdBack();
			this.DocumentType = IdentityDocumentType.None;
			MainThread.BeginInvokeOnMainThread(() => this.AdditionalPhotos.Clear());
		}

		private async Task RestoreProfilePhotoAsync(Attachment[] attachments, bool clearIfMissing)
		{
			Attachment? ProfileAttachment = null;

			foreach (Attachment Attachment in attachments)
			{
				if (IsProfilePhotoFileName(Attachment.FileName))
				{
					ProfileAttachment = Attachment;
					break;
				}
			}

			if (ProfileAttachment is null)
				ProfileAttachment = attachments.GetFirstImageAttachment();

			if (ProfileAttachment is not null)
			{
				(byte[]? Bin, string ContentType, int Rotation) = await this.photosLoader.LoadOnePhoto(ProfileAttachment, SignWith.LatestApprovedIdOrCurrentKeys);
				if (Bin is not null && Bin.Length > 0)
				{
					string FileName = GetProfilePhotoFileName(ProfileAttachment.FileName);
					string ContentTypeValue = string.IsNullOrWhiteSpace(ContentType) ? Constants.MimeTypes.Jpeg : ContentType;
					byte[] PhotoBytes = Bin;

					this.photo = new LegalIdentityAttachment(FileName, ContentTypeValue, PhotoBytes);
					this.ImageRotation = Rotation;
					this.Image = ImageSource.FromStream(() => new MemoryStream(PhotoBytes));
					this.ImageBin = PhotoBytes;
					this.HasPhoto = true;
					return;
				}
			}

			if (clearIfMissing)
				this.RemovePhoto(false);
		}

		private async Task RestoreDocumentAttachmentsAsync(Attachment[] attachments, bool clearIfMissing)
		{
			Attachment? PassportFrontAttachment = FindAttachmentByName(attachments, passportFileName);
			Attachment? NationalFrontAttachment = FindAttachmentByName(attachments, nationalIdFrontFileName);
			Attachment? DriverFrontAttachment = FindAttachmentByName(attachments, driverLicenseFrontFileName);
			Attachment? NationalBackAttachment = FindAttachmentByName(attachments, nationalIdBackFileName);
			Attachment? DriverBackAttachment = FindAttachmentByName(attachments, driverLicenseBackFileName);

			IdentityDocumentType DetectedDocumentType = IdentityDocumentType.None;
			Attachment? FrontAttachment = null;

			if (PassportFrontAttachment is not null)
			{
				DetectedDocumentType = IdentityDocumentType.Passport;
				FrontAttachment = PassportFrontAttachment;
			}
			else if (NationalFrontAttachment is not null)
			{
				DetectedDocumentType = IdentityDocumentType.NationalId;
				FrontAttachment = NationalFrontAttachment;
			}
			else if (DriverFrontAttachment is not null)
			{
				DetectedDocumentType = IdentityDocumentType.DriverLicense;
				FrontAttachment = DriverFrontAttachment;
			}

			if (FrontAttachment is not null)
				await this.ApplyProofOfIdFrontAsync(FrontAttachment, clearIfMissing);
			else if (clearIfMissing)
				this.RemoveProofOfIdFront();

			Attachment? BackAttachment = null;

			if (DetectedDocumentType == IdentityDocumentType.NationalId)
				BackAttachment = NationalBackAttachment;
			else if (DetectedDocumentType == IdentityDocumentType.DriverLicense)
				BackAttachment = DriverBackAttachment;

			if (BackAttachment is not null)
				await this.ApplyProofOfIdBackAsync(BackAttachment, clearIfMissing);
			else if (clearIfMissing)
				this.RemoveProofOfIdBack();

			if (DetectedDocumentType != IdentityDocumentType.None)
				this.DocumentType = DetectedDocumentType;
			else if (clearIfMissing)
				this.DocumentType = IdentityDocumentType.None;
		}

		private async Task RestoreAdditionalPhotosAsync(Attachment[] attachments, bool clearIfMissing)
		{
			Dictionary<int, ObservableAttachmentCard> AdditionalMap = new Dictionary<int, ObservableAttachmentCard>();

			foreach (Attachment Attachment in attachments)
			{
				string FileName = GetNormalizedFileName(Attachment.FileName);
				int Index;

				if (!IsAdditionalPhotoFileName(FileName, out Index))
					continue;

				(byte[]? Bin, _, int Rotation) = await this.photosLoader.LoadOnePhoto(Attachment, SignWith.LatestApprovedIdOrCurrentKeys);
				if (Bin is null || Bin.Length == 0)
					continue;

				byte[] PhotoBytes = Bin;

				ObservableAttachmentCard Card = new ObservableAttachmentCard
				{
					Image = ImageSource.FromStream(() => new MemoryStream(PhotoBytes)),
					ImageBin = PhotoBytes,
					ImageRotation = Rotation
				};

				AdditionalMap[Index] = Card;
			}

			if (AdditionalMap.Count > 0)
			{
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.AdditionalPhotos.Clear();

					List<int> OrderedIndexes = new List<int>(AdditionalMap.Keys);
					OrderedIndexes.Sort();
					foreach (int OrderedIndex in OrderedIndexes)
					{
						if (AdditionalMap.TryGetValue(OrderedIndex, out ObservableAttachmentCard? Card))
							this.AdditionalPhotos.Add(Card);
					}
				});
			}
			else if (clearIfMissing)
			{
				await MainThread.InvokeOnMainThreadAsync(() => this.AdditionalPhotos.Clear());
			}
		}

		private async Task ApplyProofOfIdFrontAsync(Attachment attachment, bool clearIfMissing)
		{
			(byte[]? Bin, _, _) = await this.photosLoader.LoadOnePhoto(attachment, SignWith.LatestApprovedIdOrCurrentKeys);
			if (Bin is null || Bin.Length == 0)
			{
				if (clearIfMissing)
					this.RemoveProofOfIdFront();
				return;
			}

			byte[] PhotoBytes = Bin;
			this.ProofOfIdFrontImage = ImageSource.FromStream(() => new MemoryStream(PhotoBytes));
			this.ProofOfIdFrontImageBin = PhotoBytes;
			this.HasProofOfIdFront = true;
		}

		private async Task ApplyProofOfIdBackAsync(Attachment attachment, bool clearIfMissing)
		{
			(byte[]? Bin, _, _) = await this.photosLoader.LoadOnePhoto(attachment, SignWith.LatestApprovedIdOrCurrentKeys);
			if (Bin is null || Bin.Length == 0)
			{
				if (clearIfMissing)
					this.RemoveProofOfIdBack();
				return;
			}

			byte[] PhotoBytes = Bin;
			this.ProofOfIdBackImage = ImageSource.FromStream(() => new MemoryStream(PhotoBytes));
			this.ProofOfIdBackImageBin = PhotoBytes;
			this.HasProofOfIdBack = true;
		}

		private static Attachment? FindAttachmentByName(IEnumerable<Attachment> attachments, string expectedFileName)
		{
			foreach (Attachment Attachment in attachments)
			{
				string FileName = GetNormalizedFileName(Attachment.FileName);

				if (FileNameMatches(FileName, expectedFileName))
					return Attachment;
			}

			return null;
		}

		private static bool IsProfilePhotoFileName(string? fileName)
		{
			if (string.IsNullOrWhiteSpace(fileName))
				return false;

			string Normalized = GetNormalizedFileName(fileName);

			return Normalized.StartsWith("ProfilePhoto", StringComparison.OrdinalIgnoreCase);
		}

		private static string GetProfilePhotoFileName(string? fileName)
		{
			if (string.IsNullOrWhiteSpace(fileName))
				return profilePhotoFileName;

			string Normalized = GetNormalizedFileName(fileName);

			return IsProfilePhotoFileName(Normalized) ? NormalizeProfilePhotoFileName(Normalized) : profilePhotoFileName;
		}

		private static string NormalizeProfilePhotoFileName(string fileName)
		{
			if (string.IsNullOrWhiteSpace(fileName))
				return profilePhotoFileName;

			string WithoutExtension = Path.GetFileNameWithoutExtension(fileName);
			if (string.Equals(WithoutExtension, "ProfilePhoto", StringComparison.OrdinalIgnoreCase))
				return profilePhotoFileName;

			return fileName;
		}

		private static bool IsAdditionalPhotoFileName(string fileName, out int index)
		{
			index = 0;

			if (string.IsNullOrWhiteSpace(fileName))
				return false;

			if (!fileName.StartsWith(additionalPhotoPrefix, StringComparison.OrdinalIgnoreCase))
				return false;

			string Suffix = fileName.Substring(additionalPhotoPrefix.Length);
			int DotIndex = Suffix.IndexOf('.', StringComparison.Ordinal);

			if (DotIndex >= 0)
				Suffix = Suffix.Substring(0, DotIndex);

			if (int.TryParse(Suffix, out int ParsedIndex) && ParsedIndex > 0)
			{
				index = ParsedIndex;
				return true;
			}

			return false;
		}

		private static string GetNormalizedFileName(string? fileName)
		{
			if (string.IsNullOrWhiteSpace(fileName))
				return string.Empty;

			return Path.GetFileName(fileName).Trim();
		}

		private static bool FileNameMatches(string fileName, string expectedFileName)
		{
			if (string.Equals(fileName, expectedFileName, StringComparison.OrdinalIgnoreCase))
				return true;

			string NormalizedName = Path.GetFileNameWithoutExtension(fileName);
			string ExpectedNormalized = Path.GetFileNameWithoutExtension(expectedFileName);

			return string.Equals(NormalizedName, ExpectedNormalized, StringComparison.OrdinalIgnoreCase);
		}

		private static bool SetContainsFileName(HashSet<string> set, string expectedFileName)
		{
			foreach (string Name in set)
			{
				if (FileNameMatches(Name, expectedFileName))
					return true;
			}

			return false;
		}

		#region Commands

		private async Task LoadAllAttachmentPhotos(LegalIdentity identity)
		{
			if (identity?.Attachments is null)
				return;
			await MainThread.InvokeOnMainThreadAsync(() => this.AllPhotos.Clear());
			try
			{
				// Get only attachments that are images.
				IEnumerable<Attachment> ImageAttachments = identity.Attachments.GetImageAttachments();

				foreach (Attachment Attachment in ImageAttachments)
				{
					// Use the static helper to load the photo.
					(byte[]? Bin, string ContentType, int Rotation) = await PhotosLoader.LoadPhoto(Attachment);
					if (Bin is not null)
					{
						ImageSource Source = ImageSource.FromStream(() => new MemoryStream(Bin));
						MainThread.BeginInvokeOnMainThread(() => this.AllPhotos.Add(Source));
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				throw;
			}
		}


		[RelayCommand]
		private async Task TakeProofOfIdFront()
		{
			bool Permitted = await ServiceRef.PermissionService.CheckCameraPermissionAsync();
			if (!Permitted)
				return;
			try
			{
				FileResult? Result = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
				{
					Title = ServiceRef.Localizer[nameof(AppResources.TakePhoto)]
				});
				if (Result is null)
					return;
				await using Stream Stream = await Result.OpenReadAsync();
				SKData? ImageData = CompressImage(Stream);
				if (ImageData is null)
					throw new Exception("Failed to compress photo");
				byte[] CompressedBin = ImageData.ToArray();
				using MemoryStream Ms = new MemoryStream(CompressedBin);
				await this.AddProofOfIdFront(Ms, Result.FullPath, true);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.FailedToLoadPhoto)]);
			}
		}

		[RelayCommand]
		private async Task PickProofOfIdFront()
		{
			try
			{
				FileResult? Result = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
				{
					Title = ServiceRef.Localizer[nameof(AppResources.PickPhoto)]
				});
				if (Result is null)
					return;
				await using Stream Stream = await Result.OpenReadAsync();
				SKData? ImageData = CompressImage(Stream);
				if (ImageData is null)
					throw new Exception("Failed to compress photo");
				byte[] CompressedBin = ImageData.ToArray();
				using MemoryStream Ms = new MemoryStream(CompressedBin);
				await this.AddProofOfIdFront(Ms, Result.FullPath, true);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.FailedToLoadPhoto)]);
			}
		}

		[RelayCommand]
		private void RemoveProofOfIdFront()
		{
			this.ProofOfIdFrontImage = null;
			this.ProofOfIdFrontImageBin = null;
			this.HasProofOfIdFront = false;
		}

		[RelayCommand]
		private async Task TakeProofOfIdBack()
		{
			bool Permitted = await ServiceRef.PermissionService.CheckCameraPermissionAsync();
			if (!Permitted)
				return;
			try
			{
				FileResult? Result = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
				{
					Title = ServiceRef.Localizer[nameof(AppResources.TakePhoto)]
				});
				if (Result is null)
					return;
				await using Stream Stream = await Result.OpenReadAsync();
				SKData? ImageData = CompressImage(Stream);
				if (ImageData is null)
					throw new Exception("Failed to compress photo");
				byte[] CompressedBin = ImageData.ToArray();
				using MemoryStream Ms = new MemoryStream(CompressedBin);
				await this.AddProofOfIdBack(Ms, Result.FullPath, true);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.FailedToLoadPhoto)]);
			}
		}

		[RelayCommand]
		private async Task PickProofOfIdBack()
		{
			try
			{
				FileResult? Result = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
				{
					Title = ServiceRef.Localizer[nameof(AppResources.PickPhoto)]
				});
				if (Result is null)
					return;
				await using Stream Stream = await Result.OpenReadAsync();
				SKData? ImageData = CompressImage(Stream);
				if (ImageData is null)
					throw new Exception("Failed to compress photo");
				byte[] CompressedBin = ImageData.ToArray();
				using MemoryStream Ms = new MemoryStream(CompressedBin);
				await this.AddProofOfIdBack(Ms, Result.FullPath, true);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.FailedToLoadPhoto)]);
			}
		}

		[RelayCommand]
		private void RemoveProofOfIdBack()
		{
			this.ProofOfIdBackImage = null;
			this.ProofOfIdBackImageBin = null;
			this.HasProofOfIdBack = false;
		}

		/// <summary>
		/// Helper method to process and set the front proof-of-ID image.
		/// </summary>
		public async Task AddProofOfIdFront(Stream inputStream, string filePath, bool saveLocalCopy)
		{
			SKData? ImageData = null;
			try
			{
				bool FallbackOriginal = true;
				if (saveLocalCopy)
				{
					ImageData = CompressImage(inputStream);
					if (ImageData is not null)
					{
						FallbackOriginal = false;
						byte[] Bin = ImageData.ToArray();
						this.ProofOfIdFrontImage = ImageSource.FromStream(() => new MemoryStream(Bin));
						this.ProofOfIdFrontImageBin = Bin;
						this.HasProofOfIdFront = true;
						return;
					}
				}
				if (FallbackOriginal)
				{
					byte[] Bin = await File.ReadAllBytesAsync(filePath);
					this.ProofOfIdFrontImage = ImageSource.FromStream(() => new MemoryStream(Bin));
					this.ProofOfIdFrontImageBin = Bin;
					this.HasProofOfIdFront = true;
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayAlert(
					 ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					 ServiceRef.Localizer[nameof(AppResources.FailedToLoadPhoto)]);
			}
			finally
			{
				ImageData?.Dispose();
			}
		}

		/// <summary>
		/// Helper method to process and set the back proof-of-ID image.
		/// </summary>
		public async Task AddProofOfIdBack(Stream inputStream, string filePath, bool saveLocalCopy)
		{
			SKData? ImageData = null;
			try
			{
				bool FallbackOriginal = true;
				if (saveLocalCopy)
				{
					ImageData = CompressImage(inputStream);
					if (ImageData is not null)
					{
						FallbackOriginal = false;
						byte[] Bin = ImageData.ToArray();
						this.ProofOfIdBackImage = ImageSource.FromStream(() => new MemoryStream(Bin));
						this.ProofOfIdBackImageBin = Bin;
						this.HasProofOfIdBack = true;
						return;
					}
				}
				if (FallbackOriginal)
				{
					byte[] Bin = await File.ReadAllBytesAsync(filePath);
					this.ProofOfIdBackImage = ImageSource.FromStream(() => new MemoryStream(Bin));
					this.ProofOfIdBackImageBin = Bin;
					this.HasProofOfIdBack = true;
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayAlert(
					 ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					 ServiceRef.Localizer[nameof(AppResources.FailedToLoadPhoto)]);
			}
			finally
			{
				ImageData?.Dispose();
			}
		}

		/// <summary>
		/// Toggles <see cref="Consent"/>
		/// </summary>
		[RelayCommand]
		public void ToggleConsent()
		{
			this.Consent = !this.Consent;
		}

		/// <summary>
		/// Toggles <see cref="Correct"/>
		/// </summary>
		[RelayCommand]
		public void ToggleCorrect()
		{
			this.Correct = !this.Correct;
		}

		/// <summary>
		/// If user can Apply for a new ID.
		/// </summary>
		public override bool CanApply
		{
			get
			{
				if (!this.CanExecuteCommands || !this.Consent || !this.Correct || this.ApplicationSent || !this.HasPhoto)
					return false;

				if (this.HasApplicationAttributes)
				{
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
						!this.CountryOk ||
						!this.NationalityOk ||
						!this.GenderOk ||
						!this.BirthDateOk)
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
			await this.SubmitApplicationAsync(skipConfirmation: false, skipAuthentication: false, allowResubmission: false);
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
				this.peerReviewServices = null;
				this.HasFeaturedPeerReviewers = false;
				return;
			}

			if (!await AreYouSure(ServiceRef.Localizer[nameof(AppResources.AreYouSureYouWantToRevokeTheCurrentIdApplication)]))
				return;

			if (!await App.AuthenticateUserAsync(AuthenticationPurpose.RevokeApplication, true))
				return;

			try
			{
				this.SetIsBusy(true);
				this.IsRevoking = true; // Will be cleared from event-handle.

				try
				{
					await ServiceRef.XmppService.ObsoleteLegalIdentity(Application.Id);
				}
				catch (ForbiddenException)
				{
					// Ignore. Application may have been rejected or elapsed outside of the
					// scope of the app.
				}

				await ServiceRef.TagProfile.SetIdentityApplication(null, true);
				this.ApplicationSent = false;
				this.peerReviewServices = null;
				this.HasFeaturedPeerReviewers = false;

				ServiceRef.TagProfile.SetApplicationReview(null);
				this.LoadApplicationReview(null);

				await this.GoBack();
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
				this.IsRevoking = false;
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

			await this.SendPeerReviewRequest(Constants.UriSchemes.RemoveScheme(Url));
		}

		private async Task SendPeerReviewRequest(string? ReviewerId)
		{
			LegalIdentity? ToReview = ServiceRef.TagProfile.IdentityApplication;
			if (ToReview is null || string.IsNullOrEmpty(ReviewerId))
				return;

			try
			{
				this.SetIsBusy(true);

				await ServiceRef.XmppService.PetitionPeerReviewId(ReviewerId, ToReview, Guid.NewGuid().ToString(),
					ServiceRef.Localizer[nameof(AppResources.CouldYouPleaseReviewMyIdentityInformation)]);

				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.PetitionSent)],
					ServiceRef.Localizer[nameof(AppResources.APetitionHasBeenSentToYourPeer)]);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
			finally
			{
				this.SetIsBusy(false);
			}
		}

		/// <summary>
		/// Takes a new photo
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanTakePhoto))]
		private async Task TakePhoto()
		{
			if (!this.CanTakePhoto)
				return;

			bool Permitted = await ServiceRef.PermissionService.CheckCameraPermissionAsync();

			if (!Permitted)
				return;

			try
			{
				FileResult? Result = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions()
				{
					Title = ServiceRef.Localizer[nameof(AppResources.TakePhotoOfYourself)]
				});

				if (Result is null)
					return;

				Stream Stream = await Result.OpenReadAsync();

				byte[] InputBin = Stream.ToByteArray() ?? throw new Exception("Failed to read photo stream");

				TaskCompletionSource<byte[]?> Tcs = new();
				await ServiceRef.UiService.GoToAsync(nameof(ImageCroppingPage), new ImageCroppingNavigationArgs(ImageSource.FromStream(() => new MemoryStream(InputBin)), Tcs));

				byte[] OutputBin = await Tcs.Task ?? throw new Exception("Failed to crop photo");

				MemoryStream Ms = new(OutputBin);


				await this.AddPhoto(Ms, Result.FullPath, true);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.FailedToLoadPhoto)]);
			}
		}

		/// <summary>
		/// Adds a photo from the specified path to use as a profile photo.
		/// </summary>
		/// <param name="Bin">Binary content</param>
		/// <param name="ContentType">Content-Type</param>
		/// <param name="Rotation">Rotation to use, to display the image correctly.</param>
		/// <param name="saveLocalCopy">Set to <c>true</c> to save a local copy, <c>false</c> otherwise.</param>
		/// <param name="showAlert">Set to <c>true</c> to show an alert if photo is too large; <c>false</c> otherwise.</param>
		public async Task AddPhoto(byte[] Bin, string ContentType, int Rotation, bool saveLocalCopy, bool showAlert)
		{
			if (Bin.Length > ServiceRef.TagProfile.HttpFileUploadMaxSize)
			{
				if (showAlert)
					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.PhotoIsTooLarge)]);

				return;
			}

			this.RemovePhoto(saveLocalCopy);

			if (saveLocalCopy)
			{
				try
				{
					File.WriteAllBytes(this.localPhotoFileName, Bin);
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
				}
			}

			this.photo = new LegalIdentityAttachment(profilePhotoFileName, ContentType, Bin);
			this.ImageRotation = Rotation;
			this.Image = ImageSource.FromStream(() => new MemoryStream(Bin));
			this.ImageBin = Bin;
			this.HasPhoto = true;
		}

		/// <summary>
		/// Adds a photo from a filestream to use as a profile photo.
		/// </summary>
		/// <param name="InputStream">Stream containing the photo.</param>
		/// <param name="FilePath">The full path to the file.</param>
		/// <param name="SaveLocalCopy">Set to <c>true</c> to save a local copy, <c>false</c> otherwise.</param>
		public async Task AddPhoto(Stream InputStream, string FilePath, bool SaveLocalCopy)
		{
			SKData? ImageData = null;

			try
			{
				bool FallbackOriginal = true;

				if (SaveLocalCopy)
				{
					// try to downscale and comress the image
					ImageData = CompressImage(InputStream);

					if (ImageData is not null)
					{
						FallbackOriginal = false;
						await this.AddPhoto(ImageData.ToArray(), Constants.MimeTypes.Jpeg, 0, SaveLocalCopy, true);
					}
				}

				if (FallbackOriginal)
				{
					byte[] Bin = File.ReadAllBytes(FilePath);
					if (!InternetContent.TryGetContentType(Path.GetExtension(FilePath), out string ContentType))
						ContentType = "application/octet-stream";

					await this.AddPhoto(Bin, ContentType, PhotosLoader.GetImageRotation(Bin), SaveLocalCopy, true);
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.FailedToLoadPhoto)]);
			}
			finally
			{
				ImageData?.Dispose();
			}
		}

		private static SKData? CompressImage(Stream inputStream)
		{
			try
			{
				using SKManagedStream ManagedStream = new(inputStream);
				using SKData ImageData = SKData.Create(ManagedStream);

				SKCodec Codec = SKCodec.Create(ImageData);
				SKBitmap SkBitmap = SKBitmap.Decode(ImageData);

				SkBitmap = HandleOrientation(SkBitmap, Codec.EncodedOrigin);

				bool Resize = false;
				int Height = SkBitmap.Height;
				int Width = SkBitmap.Width;

				// downdsample to FHD
				if ((Width >= Height) && (Width > 1920))
				{
					Height = (int)(Height * (1920.0 / Width) + 0.5);
					Width = 1920;
					Resize = true;
				}
				else if ((Height > Width) && (Height > 1920))
				{
					Width = (int)(Width * (1920.0 / Height) + 0.5);
					Height = 1920;
					Resize = true;
				}

				if (Resize)
				{
					SKImageInfo Info = SkBitmap.Info;
					SKImageInfo NewInfo = new(Width, Height, Info.ColorType, Info.AlphaType, Info.ColorSpace);
					SkBitmap = SkBitmap.Resize(NewInfo, SKFilterQuality.High);
				}

				return SkBitmap.Encode(SKEncodedImageFormat.Jpeg, 80);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				return null;
			}
		}

		private static SKBitmap HandleOrientation(SKBitmap Bitmap, SKEncodedOrigin Orientation)
		{
			SKBitmap Rotated;

			switch (Orientation)
			{
				case SKEncodedOrigin.BottomRight:
					Rotated = new SKBitmap(Bitmap.Width, Bitmap.Height);

					using (SKCanvas Surface = new(Rotated))
					{
						Surface.RotateDegrees(180, Bitmap.Width / 2, Bitmap.Height / 2);
						Surface.DrawBitmap(Bitmap, 0, 0);
					}
					break;

				case SKEncodedOrigin.RightTop:
					Rotated = new SKBitmap(Bitmap.Height, Bitmap.Width);

					using (SKCanvas Surface = new(Rotated))
					{
						Surface.Translate(Rotated.Width, 0);
						Surface.RotateDegrees(90);
						Surface.DrawBitmap(Bitmap, 0, 0);
					}
					break;

				case SKEncodedOrigin.LeftBottom:
					Rotated = new SKBitmap(Bitmap.Height, Bitmap.Width);

					using (SKCanvas Surface = new(Rotated))
					{
						Surface.Translate(0, Rotated.Height);
						Surface.RotateDegrees(270);
						Surface.DrawBitmap(Bitmap, 0, 0);
					}
					break;

				default:
					return Bitmap;
			}

			return Rotated;
		}

		private void RemovePhoto(bool RemoveFileOnDisc)
		{
			try
			{
				this.photo = null;
				this.Image = null;
				this.ImageBin = null;
				this.ImageRotation = 0;
				this.HasPhoto = false;

				if (RemoveFileOnDisc && File.Exists(this.localPhotoFileName))
					File.Delete(this.localPhotoFileName);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Takes a new photo
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanEdit))]
		private async Task PickPhoto()
		{
			try
			{
				FileResult? Result = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions()
				{
					Title = ServiceRef.Localizer[nameof(AppResources.PickPhotoOfYourself)]
				});

				if (Result is null)
					return;

				Stream Stream = await Result.OpenReadAsync();
				byte[] InputBin = Stream.ToByteArray() ?? throw new Exception("Failed to read photo stream");

				TaskCompletionSource<byte[]?> Tcs = new();
				await ServiceRef.UiService.GoToAsync(
					nameof(ImageCroppingPage),
					new ImageCroppingNavigationArgs(ImageSource.FromStream(() => new MemoryStream(InputBin)), Tcs)
				);

				byte[] OutputBin = await Tcs.Task ?? throw new Exception("Failed to crop photo");
				MemoryStream Ms = new(OutputBin);

				await this.AddPhoto(Ms, Result.FullPath, true);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}


		/// <summary>
		/// Removes the current photo.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanRemovePhoto))]
		private void RemovePhoto()
		{
			this.RemovePhoto(true);
		}

		/// <summary>
		/// Allows the user to start correcting invalid claims reported by the review.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanFixInvalidClaims))]
		private async Task FixInvalidClaims()
		{
			ApplicationReview? Review = this.applicationReview;
			if (Review is null)
				return;

			await this.EnterEditModeAsync();
		}

		/// <summary>
		/// Allows the user to resubmit without pending unvalidated items.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanPrepareReapplyWithoutPendingClaims))]
		private async Task PrepareReapplyWithoutPendingClaims()
		{
			ApplicationReview? Review = this.applicationReview;
			if (Review is null)
				return;

			await this.AutoReapplyAsync(
				Review.UnvalidatedClaims ?? Array.Empty<string>(),
				Array.Empty<ApplicationReviewPhotoDetail>(),
				Review.UnvalidatedPhotos ?? Array.Empty<string>());
		}

		private async Task LoadFeaturedPeerReviewers()
		{
			await ServiceRef.NetworkService.TryRequest(async () =>
			{
				this.peerReviewServices = await ServiceRef.XmppService.GetServiceProvidersForPeerReviewAsync();

				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.HasFeaturedPeerReviewers = this.peerReviewServices.Length > 0;
				});
			});
		}

		/// <summary>
		/// Select from a list of featured peer reviewers.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanRequestFeaturedPeerReviewer))]
		private async Task RequestReview()
		{
			if (this.peerReviewServices is null)
				await this.LoadFeaturedPeerReviewers();

			if ((this.peerReviewServices?.Length ?? 0) > 0)
			{
				List<ServiceProviderWithLegalId> ServiceProviders = [.. this.peerReviewServices, new RequestFromPeer()];

				ServiceProvidersNavigationArgs e = new([.. ServiceProviders],
					ServiceRef.Localizer[nameof(AppResources.RequestReview)],
					ServiceRef.Localizer[nameof(AppResources.SelectServiceProviderPeerReview)]);

				await ServiceRef.UiService.GoToAsync(nameof(ServiceProvidersPage), e, BackMethod.Pop);

				if (e.ServiceProvider is not null)
				{
					IServiceProvider? ServiceProvider = await e.ServiceProvider.Task;

					if (ServiceProvider is ServiceProviderWithLegalId ServiceProviderWithLegalId &&
						!string.IsNullOrEmpty(ServiceProviderWithLegalId.LegalId))
					{
						if (!ServiceProviderWithLegalId.External)
						{
							if (!await ServiceRef.NetworkService.TryRequest(async () =>
								await ServiceRef.XmppService.SelectPeerReviewService(ServiceProvider.Id, ServiceProvider.Type)))
							{
								return;
							}
						}

						await this.SendPeerReviewRequest(ServiceProviderWithLegalId.LegalId);
						return;
					}
					else if (ServiceProvider is null)
						return;
				}
			}

			await this.ScanQrCode();
		}

		private void LoadApplicationReview(ApplicationReview? review)
		{
			this.applicationReview = review;

			this.HasApplicationReview = review is not null;
			this.ApplicationReviewMessage = (review?.Message ?? string.Empty).Trim();
			this.ApplicationReviewCode = review?.Code;
			this.ApplicationReviewTimestamp = review?.ReceivedUtc;

			IEnumerable<ApplicationReviewClaimDetail> rawClaimDetails = review?.InvalidClaimDetails ?? Array.Empty<ApplicationReviewClaimDetail>();
			List<ApplicationReviewClaimDetail> localizedClaimDetails = new();
			foreach (ApplicationReviewClaimDetail detail in rawClaimDetails)
			{
				if (detail is null)
					continue;

				detail.DisplayName = this.GetClaimDisplayName(detail.Claim);
				localizedClaimDetails.Add(detail);
			}
			ReplaceCollection(this.InvalidClaimDetails, localizedClaimDetails);

			IEnumerable<ApplicationReviewPhotoDetail> PhotoDetails = review?.InvalidPhotoDetails ?? Array.Empty<ApplicationReviewPhotoDetail>();
			ReplaceCollection(this.InvalidPhotoDetails, PhotoDetails);

			IEnumerable<string> pendingClaimsRaw = review?.UnvalidatedClaims ?? Array.Empty<string>();
			List<string> pendingClaimsLocalized = new();
			bool hasBirthComponent = false;
			foreach (string pendingClaim in pendingClaimsRaw)
			{
				if (string.IsNullOrWhiteSpace(pendingClaim))
					continue;

				string normalizedClaim = pendingClaim.Trim();
				if (claimsHiddenInPending.Contains(normalizedClaim))
					continue;

				if (IsBirthComponent(normalizedClaim))
				{
					hasBirthComponent = true;
					continue;
				}

				string displayName = this.GetClaimDisplayName(normalizedClaim);
				if (string.IsNullOrWhiteSpace(displayName))
					continue;

				pendingClaimsLocalized.Add(displayName);
			}

			if (hasBirthComponent)
			{
				string birthDisplay = this.GetClaimDisplayName(Constants.XmppProperties.BirthDay);
				if (!string.IsNullOrWhiteSpace(birthDisplay))
					pendingClaimsLocalized.Add(birthDisplay);
			}
			ReplaceCollection(this.UnvalidatedClaims, pendingClaimsLocalized);

			IEnumerable<string> PendingPhotos = review?.UnvalidatedPhotos ?? Array.Empty<string>();
			ReplaceCollection(this.UnvalidatedPhotos, PendingPhotos);

			this.OnPropertyChanged(nameof(this.HasInvalidClaimDetails));
			this.OnPropertyChanged(nameof(this.HasInvalidPhotoDetails));
			this.OnPropertyChanged(nameof(this.HasUnvalidatedClaims));
			this.OnPropertyChanged(nameof(this.HasUnvalidatedPhotos));
			this.OnPropertyChanged(nameof(this.HasInvalidItems));
			this.OnPropertyChanged(nameof(this.HasInvalidOnly));
			this.OnPropertyChanged(nameof(this.HasOnlyUnvalidatedItems));
			this.OnPropertyChanged(nameof(this.ShowApplicationSection));
			this.OnPropertyChanged(nameof(this.CanFixInvalidClaims));
			this.OnPropertyChanged(nameof(this.CanPrepareReapplyWithoutPendingClaims));

			this.FixInvalidClaimsCommand?.NotifyCanExecuteChanged();

			this.PrepareReapplyWithoutPendingClaimsCommand?.NotifyCanExecuteChanged();
		}

		private async Task AutoReapplyAsync(IEnumerable<string> claimsToClear, IEnumerable<ApplicationReviewPhotoDetail> photosToClear, IEnumerable<string> pendingPhotosToClear)
		{
			if (this.IsApplying || this.IsBusy)
				return;

			try
			{
				this.SetIsBusy(true);

				string[] claims = claimsToClear?.ToArray() ?? Array.Empty<string>();
				ApplicationReviewPhotoDetail[] photoDetails = photosToClear?.ToArray() ?? Array.Empty<ApplicationReviewPhotoDetail>();
				string[] pendingPhotoNames = pendingPhotosToClear?.ToArray() ?? Array.Empty<string>();

				this.ClearClaims(claims);
				this.ClearInvalidPhotos(photoDetails);
				this.ClearPendingPhotoNames(pendingPhotoNames);

				ServiceRef.TagProfile.SetApplicationReview(null);
				this.LoadApplicationReview(null);

				LegalIdentity? currentApplication = ServiceRef.TagProfile.IdentityApplication;
				if (currentApplication is not null)
				{
					try
					{
						await ServiceRef.XmppService.ObsoleteLegalIdentity(currentApplication.Id);
					}
					catch (ForbiddenException)
					{
						// Ignore if the identity is already revoked or cannot be revoked.
					}
					catch (Exception ex)
					{
						ServiceRef.LogService.LogException(ex);
					}
				}

				await ServiceRef.TagProfile.SetIdentityApplication(null, true);
				this.ApplicationSent = false;
				this.IsRevoking = false;

				await this.SubmitApplicationAsync(skipConfirmation: true, skipAuthentication: false, allowResubmission: true);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
			finally
			{
				this.SetIsBusy(false);
			}
		}


		private async Task EnterEditModeAsync()
		{
			if (!this.ApplicationSent)
				return;

			try
			{
				this.SetIsBusy(true);

				ServiceRef.TagProfile.SetApplicationReview(null);
				this.LoadApplicationReview(null);

				try
				{
					await ServiceRef.TagProfile.SetIdentityApplication(null, true);
				}
				catch (Exception ex)
				{
				ServiceRef.LogService.LogException(ex);
			}

			this.ApplicationSent = false;
				this.IsRevoking = false;

				this.NotifyCommandsCanExecuteChanged();
				this.PickAdditionalPhotoCommand.NotifyCanExecuteChanged();
				this.PickPhotoCommand.NotifyCanExecuteChanged();
				this.PickProofOfIdBackCommand.NotifyCanExecuteChanged();
				this.PickProofOfIdFrontCommand.NotifyCanExecuteChanged();
				this.TakeAdditionalPhotoCommand.NotifyCanExecuteChanged();
				this.TakePhotoCommand.NotifyCanExecuteChanged();
				this.TakeProofOfIdBackCommand.NotifyCanExecuteChanged();
				this.TakeProofOfIdFrontCommand.NotifyCanExecuteChanged();
				this.RemovePhotoCommand?.NotifyCanExecuteChanged();
				this.removeAdditionalPhotoCommand?.NotifyCanExecuteChanged();
				this.removeProofOfIdFrontCommand?.NotifyCanExecuteChanged();
				this.removeProofOfIdBackCommand?.NotifyCanExecuteChanged();
				this.OnPropertyChanged(nameof(this.CanEdit));
				this.OnPropertyChanged(nameof(this.CanRemovePhoto));
				this.OnPropertyChanged(nameof(this.CanTakePhoto));
				this.OnPropertyChanged(nameof(this.ApplicationSentAndConnected));
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
			finally
			{
				this.SetIsBusy(false);
			}
		}

	private void ClearPendingPhotoNames(IEnumerable<string> pendingPhotoNames)
	{
		if (pendingPhotoNames is null)
			return;

		HashSet<string> Normalized = new HashSet<string>(pendingPhotoNames
			.Where(name => !string.IsNullOrWhiteSpace(name))
			.Select(name => GetNormalizedFileName(name.Trim())), StringComparer.OrdinalIgnoreCase);

		if (Normalized.Count == 0)
			return;

		bool RemoveProfilePhoto = false;
		foreach (string FileName in Normalized)
		{
			if (IsProfilePhotoFileName(FileName))
			{
				RemoveProfilePhoto = true;
				break;
			}
		}

		if (RemoveProfilePhoto)
			this.RemovePhoto(true);

		if (SetContainsFileName(Normalized, passportFileName) ||
			SetContainsFileName(Normalized, nationalIdFrontFileName) ||
			SetContainsFileName(Normalized, driverLicenseFrontFileName))
		{
			this.RemoveProofOfIdFront();
		}

		if (SetContainsFileName(Normalized, nationalIdBackFileName) ||
			SetContainsFileName(Normalized, driverLicenseBackFileName))
		{
			this.RemoveProofOfIdBack();
		}

		foreach (string FileName in Normalized)
		{
			int Index;

			if (IsAdditionalPhotoFileName(FileName, out Index))
				this.RemoveAdditionalPhotoByFileName(FileName);
		}
	}

		private static void ReplaceCollection<T>(ObservableCollection<T> collection, IEnumerable<T> items)
		{
			collection.Clear();
			foreach (T Item in items)
			{
				collection.Add(Item);
			}
		}

		private string GetClaimDisplayName(string claim)
		{
			if (string.IsNullOrWhiteSpace(claim))
				return string.Empty;

			if (claimDisplayNameMap.TryGetValue(claim, out string resourceKey))
			{
				try
				{
					string localized = ServiceRef.Localizer[resourceKey, false];
					if (!string.IsNullOrEmpty(localized) && !string.Equals(localized, resourceKey, StringComparison.Ordinal))
						return localized;

					return string.IsNullOrEmpty(localized) ? resourceKey : localized;
				}
				catch
				{
					return resourceKey;
				}
			}

			return claim;
		}

		private static bool IsBirthComponent(string claim)
		{
			return birthClaimKeys.Contains(claim);
		}

		private static bool IsProtectedClaim(string claim)
		{
			return claimsHiddenInPending.Contains(claim);
		}

		private void ClearClaims(IEnumerable<string> claims)
		{
			foreach (string Claim in claims)
			{
				if (string.IsNullOrWhiteSpace(Claim))
					continue;

				string NormalizedClaim = Claim.Trim();
				if (NormalizedClaim.Length == 0)
					continue;

				if (IsProtectedClaim(NormalizedClaim))
					continue;

				if (claimClearActions.TryGetValue(NormalizedClaim, out Action<ApplyIdViewModel> action))
					action(this);
			}
		}

		private void ClearInvalidPhotos(IEnumerable<ApplicationReviewPhotoDetail> photoDetails)
		{
			foreach (ApplicationReviewPhotoDetail Detail in photoDetails)
			{
				if (Detail is null || string.IsNullOrWhiteSpace(Detail.FileName))
					continue;

				string FileName = Detail.FileName;
				if (string.Equals(FileName, profilePhotoFileName, StringComparison.OrdinalIgnoreCase))
				{
					this.RemovePhoto(true);
				}
				else if (string.Equals(FileName, passportFileName, StringComparison.OrdinalIgnoreCase) ||
					string.Equals(FileName, nationalIdFrontFileName, StringComparison.OrdinalIgnoreCase) ||
					string.Equals(FileName, driverLicenseFrontFileName, StringComparison.OrdinalIgnoreCase))
				{
					this.RemoveProofOfIdFront();
				}
				else if (string.Equals(FileName, nationalIdBackFileName, StringComparison.OrdinalIgnoreCase) ||
					string.Equals(FileName, driverLicenseBackFileName, StringComparison.OrdinalIgnoreCase))
				{
					this.RemoveProofOfIdBack();
				}
				else if (FileName.StartsWith("AdditionalPhoto", StringComparison.OrdinalIgnoreCase))
				{
					this.RemoveAdditionalPhotoByFileName(FileName);
				}
			}
		}

		private void RemoveAdditionalPhotoByFileName(string fileName)
		{
			if (string.IsNullOrWhiteSpace(fileName))
				return;

			string Trimmed = fileName;
			if (Trimmed.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
			{
				Trimmed = Trimmed[..^5];
			}
			else if (Trimmed.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
			{
				Trimmed = Trimmed[..^4];
			}

			if (!Trimmed.StartsWith(additionalPhotoPrefix, StringComparison.OrdinalIgnoreCase))
				return;

			string IndexPart = Trimmed.Substring(additionalPhotoPrefix.Length);
			if (int.TryParse(IndexPart, out int Index) && Index > 0 && Index <= this.AdditionalPhotos.Count)
			{
				int ZeroBasedIndex = Index - 1;
				if (ZeroBasedIndex >= 0 && ZeroBasedIndex < this.AdditionalPhotos.Count)
					this.AdditionalPhotos.RemoveAt(ZeroBasedIndex);
			}
			else
			{
				this.AdditionalPhotos.Clear();
			}
		}

		#region Additional Photos


		// Command to take a new additional photo using the camera
		[RelayCommand]
		private async Task TakeAdditionalPhoto()
		{
			if (!MediaPicker.IsCaptureSupported)
				return;

			bool Permitted = await ServiceRef.PermissionService.CheckCameraPermissionAsync();
			if (!Permitted)
				return;

			try
			{
				FileResult? Result = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
				{
					Title = ServiceRef.Localizer[nameof(AppResources.TakePhoto)]
				});
				if (Result is null)
					return;
				await this.ProcessAdditionalPhoto(Result);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.FailedToLoadPhoto)]);
			}
		}

		// Command to pick a new additional photo from the gallery
		[RelayCommand]
		private async Task PickAdditionalPhoto()
		{
			try
			{
				FileResult? Result = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
				{
					Title = ServiceRef.Localizer[nameof(AppResources.PickPhoto)]
				});
				if (Result is null)
					return;
				await this.ProcessAdditionalPhoto(Result);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		// Command to remove an additional photo (parameter is the photo to remove)
		[RelayCommand]
		private void RemoveAdditionalPhoto(ObservableAttachmentCard photo)
		{
			MainThread.BeginInvokeOnMainThread(() => this.AdditionalPhotos.Remove(photo));
		}

		// Common logic to process a FileResult into an AdditionalPhoto,
		// including reading, cropping, and compressing the image.
		private async Task ProcessAdditionalPhoto(FileResult result)
		{
			// Open the image stream and read the bytes
			using Stream Stream = await result.OpenReadAsync();
			byte[] InputBin = Stream.ToByteArray() ?? throw new Exception("Failed to read photo stream");

			// Allow user to crop the image (uses your existing ImageCroppingPage)
			TaskCompletionSource<byte[]?> Tcs = new TaskCompletionSource<byte[]?>();
			await ServiceRef.UiService.GoToAsync(nameof(ImageCroppingPage),
				new ImageCroppingNavigationArgs(
					ImageSource.FromStream(() => new MemoryStream(InputBin)), Tcs));

			byte[] OutputBin = await Tcs.Task ?? throw new Exception("Failed to crop photo");
			using MemoryStream Ms = new MemoryStream(OutputBin);

			ObservableAttachmentCard? AdditionalPhoto = await CreateObservableAttachmentCardFromStream(Ms, result.FullPath, true);
			if (AdditionalPhoto != null)
			{
				MainThread.BeginInvokeOnMainThread(() => this.AdditionalPhotos.Add(AdditionalPhoto));
			}
		}

		// Helper method similar to your AddPhoto(Stream, ...) but returning an AdditionalPhoto instance.
		private static async Task<ObservableAttachmentCard?> CreateObservableAttachmentCardFromStream(Stream inputStream, string filePath, bool saveLocalCopy)
		{
			SKData? ImageData = null;
			try
			{
				bool FallbackOriginal = true;
				if (saveLocalCopy)
				{
					ImageData = CompressImage(inputStream);
					if (ImageData is not null)
					{
						FallbackOriginal = false;
						byte[] Bin = ImageData.ToArray();
						return new ObservableAttachmentCard
						{
							ImageBin = Bin,
							ImageRotation = 0, // You may adjust if needed
							Image = ImageSource.FromStream(() => new MemoryStream(Bin))
						};
					}
				}
				if (FallbackOriginal)
				{
					byte[] Bin = File.ReadAllBytes(filePath);
					int Rotation = PhotosLoader.GetImageRotation(Bin);
					return new ObservableAttachmentCard
					{
						ImageBin = Bin,
						ImageRotation = Rotation,
						Image = ImageSource.FromStream(() => new MemoryStream(Bin))
					};
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.FailedToLoadPhoto)]);
			}
			finally
			{
				ImageData?.Dispose();
			}
			return null;
		}
		#endregion

		#endregion

		private async Task SubmitApplicationAsync(bool skipConfirmation, bool skipAuthentication, bool allowResubmission)
		{
			if (this.ApplicationSent && !allowResubmission)
				return;

			if (!skipConfirmation)
			{
				if (!await AreYouSure(ServiceRef.Localizer[nameof(AppResources.AreYouSureYouWantToSendThisIdApplication)]))
					return;
			}

			if (!skipAuthentication)
			{
				if (!await App.AuthenticateUserAsync(AuthenticationPurpose.SignApplication, true))
					return;
			}

			try
			{
				List<LegalIdentityAttachment> localAttachments = this.BuildAttachments();

				this.SetIsBusy(true);
				this.IsApplying = true;
				NumberInformation info = await PersonalNumberSchemes.Validate(this.CountryCode!, this.PersonalNumber!);
				this.PersonalNumber = info.PersonalNumber;

				bool hasIdWithPrivateKey = ServiceRef.TagProfile.LegalIdentity is not null &&
					  await ServiceRef.XmppService.HasPrivateKey(ServiceRef.TagProfile.LegalIdentity.Id);

				(bool Succeeded, LegalIdentity? AddedIdentity) = await ServiceRef.NetworkService.TryRequest(() =>
					 ServiceRef.XmppService.AddLegalIdentity(this, !hasIdWithPrivateKey, localAttachments.ToArray()));

				if (Succeeded && AddedIdentity is not null)
				{
					await ServiceRef.TagProfile.SetIdentityApplication(AddedIdentity, true);
					this.ApplicationSent = true;
					this.ApplicationId = AddedIdentity.Id;

					await Task.Run(this.LoadFeaturedPeerReviewers);

					foreach (LegalIdentityAttachment localAttachment in localAttachments)
					{
						Attachment? matchingAttachment = AddedIdentity.Attachments
							 .FirstOrDefault(a => string.Equals(a.FileName, localAttachment.FileName, StringComparison.OrdinalIgnoreCase));
						if (matchingAttachment != null && localAttachment.Data is not null && localAttachment.ContentType is not null)
						{
							await ServiceRef.AttachmentCacheService.Add(
								 matchingAttachment.Url,
								 AddedIdentity.Id,
								 true,
								 localAttachment.Data,
								 localAttachment.ContentType);
						}
					}

					await this.LoadAllAttachmentPhotos(AddedIdentity);
					ServiceRef.TagProfile.SetApplicationReview(null);
					this.LoadApplicationReview(null);
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
			finally
			{
				this.SetIsBusy(false);
				this.IsApplying = false;
			}
		}

		private List<LegalIdentityAttachment> BuildAttachments()
		{
			List<LegalIdentityAttachment> localAttachments = new();

			if (this.photo is not null)
				localAttachments.Add(this.photo);

			if (this.DocumentType != IdentityDocumentType.None)
			{
				if (this.HasProofOfIdFront && this.ProofOfIdFrontImageBin is not null)
				{
					string frontFileName = this.DocumentType switch
					{
						IdentityDocumentType.Passport => passportFileName,
						IdentityDocumentType.NationalId => nationalIdFrontFileName,
						IdentityDocumentType.DriverLicense => driverLicenseFrontFileName,
						_ => nationalIdFrontFileName
					};

					LegalIdentityAttachment frontAttachment = new(frontFileName, "image/jpeg", this.ProofOfIdFrontImageBin);
					localAttachments.Add(frontAttachment);
				}

				if ((this.DocumentType == IdentityDocumentType.NationalId || this.DocumentType == IdentityDocumentType.DriverLicense) &&
					 this.HasProofOfIdBack && this.ProofOfIdBackImageBin is not null)
				{
					string backFileName = this.DocumentType switch
					{
						IdentityDocumentType.NationalId => nationalIdBackFileName,
						IdentityDocumentType.DriverLicense => driverLicenseBackFileName,
						_ => nationalIdBackFileName
					};

					LegalIdentityAttachment backAttachment = new(backFileName, "image/jpeg", this.ProofOfIdBackImageBin);
					localAttachments.Add(backAttachment);
				}
			}

			if (this.AdditionalPhotos.Count > 0)
			{
				int index = 1;
				foreach (ObservableAttachmentCard additional in this.AdditionalPhotos)
				{
					if (additional.ImageBin == null)
						continue;
					localAttachments.Add(new LegalIdentityAttachment($"AdditionalPhoto{index}.jpg", "image/jpeg", additional.ImageBin));
					index++;
				}
			}

			return localAttachments;
		}

	}
}
