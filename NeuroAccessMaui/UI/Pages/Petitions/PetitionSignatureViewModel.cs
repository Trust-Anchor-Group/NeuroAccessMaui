﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Data;
using NeuroAccessMaui.Services.UI.Photos;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Petitions
{
	/// <summary>
	/// The view model to bind to when displaying petitioning of a signature in a view or page.
	/// </summary>
	public partial class PetitionSignatureViewModel : QrXmppViewModel
	{
		private readonly PhotosLoader photosLoader;
		private string? requestorFullJid;
		private string? signatoryIdentityId;
		private string? petitionId;
		private byte[]? contentToSign;

		/// <summary>
		/// Creates a new instance of the <see cref="PetitionSignatureViewModel"/> class.
		/// </summary>
		public PetitionSignatureViewModel()
			: base()
		{
			this.CopyCommand = new Command(async (Item) => await this.CopyToClipboard(Item));

			this.photosLoader = new PhotosLoader(this.Photos);
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (ServiceRef.NavigationService.TryGetArgs(out PetitionSignatureNavigationArgs? Args))
			{
				this.RequestorIdentity = Args.RequestorIdentity;
				this.requestorFullJid = Args.RequestorFullJid;
				this.signatoryIdentityId = Args.SignatoryIdentityId;
				this.contentToSign = Args.ContentToSign;
				this.petitionId = Args.PetitionId;
				this.Purpose = Args.Purpose;
			}

			this.AssignProperties();

			if (this.RequestorIdentity?.Attachments is not null)
				_ = this.photosLoader.LoadPhotos(this.RequestorIdentity.Attachments, SignWith.LatestApprovedId);
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			this.photosLoader.CancelLoadPhotos();

			await base.OnDispose();
		}

		/// <summary>
		/// The command for copying data to clipboard.
		/// </summary>
		public ICommand CopyCommand { get; }

		/// <summary>
		/// The list of photos related to the identity being petitioned.
		/// </summary>
		public ObservableCollection<Photo> Photos { get; } = [];

		/// <summary>
		/// The identity of the requestor.
		/// </summary>
		public LegalIdentity? RequestorIdentity { get; private set; }

		[RelayCommand]
		private async Task Accept()
		{
			if (!await App.AuthenticateUser())
				return;

			bool Succeeded = await ServiceRef.NetworkService.TryRequest(async () =>
			{
				byte[] Signature = await ServiceRef.XmppService.Sign(this.contentToSign!, SignWith.LatestApprovedId);

				await ServiceRef.XmppService.SendPetitionSignatureResponse(this.signatoryIdentityId, this.contentToSign!, Signature,
					this.petitionId!, this.requestorFullJid!, true);
			});

			if (Succeeded)
				await ServiceRef.NavigationService.GoBackAsync();
		}

		[RelayCommand]
		private async Task Decline()
		{
			bool Succeeded = await ServiceRef.NetworkService.TryRequest(() =>
			{
				return ServiceRef.XmppService.SendPetitionSignatureResponse(this.signatoryIdentityId,
					this.contentToSign!, [], this.petitionId!, this.requestorFullJid!, false);
			});

			if (Succeeded)
				await ServiceRef.NavigationService.GoBackAsync();
		}

		[RelayCommand]
		private static async Task Ignore()
		{
			await ServiceRef.NavigationService.GoBackAsync();
		}

		/// <summary>
		/// Title of the current view
		/// </summary>
		public override Task<string> Title => Task.FromResult(this.FullName);

		// Full name of requesting entity.
		public string FullName => ContactInfo.GetFullName(this.FirstName, this.MiddleNames, this.LastNames);

		#region Properties

		/// <summary>
		/// Created date of the identity
		/// </summary>
		[ObservableProperty]
		private DateTime? created;

		/// <summary>
		/// Updated date of the identity
		/// </summary>
		[ObservableProperty]
		private DateTime? updated;

		/// <summary>
		/// When the identity expires
		/// </summary>
		[ObservableProperty]
		private DateTime? expires;

		/// <summary>
		/// Identifier of Legal ID
		/// </summary>
		[ObservableProperty]
		private string? legalId;

		/// <summary>
		/// Network ID
		/// </summary>
		[ObservableProperty]
		private string? networkId;

		/// <summary>
		/// Current state of the identity
		/// </summary>
		[ObservableProperty]
		private IdentityState? state;

		/// <summary>
		/// From date (validity range) of the identity
		/// </summary>
		[ObservableProperty]
		private DateTime? from;

		/// <summary>
		/// To date (validity range) of the identity
		/// </summary>
		[ObservableProperty]
		private DateTime? to;

		/// <summary>
		/// First name of the identity
		/// </summary>
		[ObservableProperty]
		private string? firstName;

		/// <summary>
		/// Middle name(s) of the identity
		/// </summary>
		[ObservableProperty]
		private string? middleNames;

		/// <summary>
		/// Last name(s) of the identity
		/// </summary>
		[ObservableProperty]
		private string? lastNames;

		/// <summary>
		/// Personal number of the identity
		/// </summary>
		[ObservableProperty]
		private string? personalNumber;

		/// <summary>
		/// Address of the identity
		/// </summary>
		[ObservableProperty]
		private string? address;

		/// <summary>
		/// Address (line 2) of the identity
		/// </summary>
		[ObservableProperty]
		private string? address2;

		/// <summary>
		/// Zip code of the identity
		/// </summary>
		[ObservableProperty]
		private string? zipCode;

		/// <summary>
		/// Area of the identity
		/// </summary>
		[ObservableProperty]
		private string? area;

		/// <summary>
		/// City of the identity
		/// </summary>
		[ObservableProperty]
		private string? city;

		/// <summary>
		/// Region of the identity
		/// </summary>
		[ObservableProperty]
		private string? region;

		/// <summary>
		/// Country code of the identity
		/// </summary>
		[ObservableProperty]
		private string? countryCode;

		/// <summary>
		/// Country of the identity
		/// </summary>
		[ObservableProperty]
		private string? country;

		/// <summary>
		/// The legal identity's organization name property
		/// </summary>
		[ObservableProperty]
		private string? orgName;

		/// <summary>
		/// The legal identity's organization number property
		/// </summary>
		[ObservableProperty]
		private string? orgNumber;

		/// <summary>
		/// The legal identity's organization department property
		/// </summary>
		[ObservableProperty]
		private string? orgDepartment;

		/// <summary>
		/// The legal identity's organization role property
		/// </summary>
		[ObservableProperty]
		private string? orgRole;

		/// <summary>
		/// The legal identity's organization address property
		/// </summary>
		[ObservableProperty]
		private string? orgAddress;

		/// <summary>
		/// The legal identity's organization address line 2 property
		/// </summary>
		[ObservableProperty]
		private string? orgAddress2;

		/// <summary>
		/// The legal identity's organization zip code property
		/// </summary>
		[ObservableProperty]
		private string? orgZipCode;

		/// <summary>
		/// The legal identity's organization area property
		/// </summary>
		[ObservableProperty]
		private string? orgArea;

		/// <summary>
		/// The legal identity's organization city property
		/// </summary>
		[ObservableProperty]
		private string? orgCity;

		/// <summary>
		/// The legal identity's organization region property
		/// </summary>
		[ObservableProperty]
		private string? orgRegion;

		/// <summary>
		/// The legal identity's organization country code property
		/// </summary>
		[ObservableProperty]
		private string? orgCountryCode;

		/// <summary>
		/// The legal identity's organization country property
		/// </summary>
		[ObservableProperty]
		private string? orgCountry;

		/// <summary>
		/// If organization information is available.
		/// </summary>
		[ObservableProperty]
		private bool hasOrg;

		/// <summary>
		/// If photos are available.
		/// </summary>
		[ObservableProperty]
		private bool hasPhotos;

		/// <summary>
		/// PhoneNr of the identity
		/// </summary>
		[ObservableProperty]
		private string? phoneNr;

		/// <summary>
		/// EMail of the identity
		/// </summary>
		[ObservableProperty]
		private string? eMail;

		/// <summary>
		/// Is the contract approved?
		/// </summary>
		[ObservableProperty]
		private bool isApproved;

		/// <summary>
		/// What's the purpose of the petition?
		/// </summary>
		[ObservableProperty]
		private string? purpose;

		#endregion

		private void AssignProperties()
		{
			if (this.RequestorIdentity is not null)
			{
				this.Created = this.RequestorIdentity.Created;
				this.Updated = this.RequestorIdentity.Updated.GetDateOrNullIfMinValue();
				this.Expires = this.RequestorIdentity.To.GetDateOrNullIfMinValue();
				this.LegalId = this.RequestorIdentity.Id;
				this.NetworkId = this.RequestorIdentity.GetJid();
				this.State = this.RequestorIdentity.State;
				this.From = this.RequestorIdentity.From.GetDateOrNullIfMinValue();
				this.To = this.RequestorIdentity.To.GetDateOrNullIfMinValue();
				this.FirstName = this.RequestorIdentity[Constants.XmppProperties.FirstName];
				this.MiddleNames = this.RequestorIdentity[Constants.XmppProperties.MiddleNames];
				this.LastNames = this.RequestorIdentity[Constants.XmppProperties.LastNames];
				this.PersonalNumber = this.RequestorIdentity[Constants.XmppProperties.PersonalNumber];
				this.Address = this.RequestorIdentity[Constants.XmppProperties.Address];
				this.Address2 = this.RequestorIdentity[Constants.XmppProperties.Address2];
				this.ZipCode = this.RequestorIdentity[Constants.XmppProperties.ZipCode];
				this.Area = this.RequestorIdentity[Constants.XmppProperties.Area];
				this.City = this.RequestorIdentity[Constants.XmppProperties.City];
				this.Region = this.RequestorIdentity[Constants.XmppProperties.Region];
				this.CountryCode = this.RequestorIdentity[Constants.XmppProperties.Country];
				this.Country = ISO_3166_1.ToName(this.CountryCode);
				this.OrgName = this.RequestorIdentity[Constants.XmppProperties.OrgName];
				this.OrgNumber = this.RequestorIdentity[Constants.XmppProperties.OrgNumber];
				this.OrgDepartment = this.RequestorIdentity[Constants.XmppProperties.OrgDepartment];
				this.OrgRole = this.RequestorIdentity[Constants.XmppProperties.OrgRole];
				this.OrgAddress = this.RequestorIdentity[Constants.XmppProperties.OrgAddress];
				this.OrgAddress2 = this.RequestorIdentity[Constants.XmppProperties.OrgAddress2];
				this.OrgZipCode = this.RequestorIdentity[Constants.XmppProperties.OrgZipCode];
				this.OrgArea = this.RequestorIdentity[Constants.XmppProperties.OrgArea];
				this.OrgCity = this.RequestorIdentity[Constants.XmppProperties.OrgCity];
				this.OrgRegion = this.RequestorIdentity[Constants.XmppProperties.OrgRegion];
				this.OrgCountryCode = this.RequestorIdentity[Constants.XmppProperties.OrgCountry];
				this.OrgCountry = ISO_3166_1.ToName(this.OrgCountryCode);
				this.HasOrg =
					!string.IsNullOrEmpty(this.OrgName) ||
					!string.IsNullOrEmpty(this.OrgNumber) ||
					!string.IsNullOrEmpty(this.OrgDepartment) ||
					!string.IsNullOrEmpty(this.OrgRole) ||
					!string.IsNullOrEmpty(this.OrgAddress) ||
					!string.IsNullOrEmpty(this.OrgAddress2) ||
					!string.IsNullOrEmpty(this.OrgZipCode) ||
					!string.IsNullOrEmpty(this.OrgArea) ||
					!string.IsNullOrEmpty(this.OrgCity) ||
					!string.IsNullOrEmpty(this.OrgRegion) ||
					!string.IsNullOrEmpty(this.OrgCountryCode) ||
					!string.IsNullOrEmpty(this.OrgCountry);
				this.HasPhotos = this.Photos.Count > 0;
				this.PhoneNr = this.RequestorIdentity[Constants.XmppProperties.Phone];
				this.EMail = this.RequestorIdentity[Constants.XmppProperties.EMail];
				this.IsApproved = this.RequestorIdentity.State == IdentityState.Approved;

				this.QrCodeWidth = 250;
				this.QrCodeHeight = 250;
				this.GenerateQrCode(Constants.UriSchemes.CreateIdUri(this.LegalId));
			}
			else
			{
				this.Created = DateTime.MinValue;
				this.Updated = null;
				this.LegalId = Constants.NotAvailableValue;
				this.NetworkId = Constants.NotAvailableValue;
				this.State = IdentityState.Compromised;
				this.From = null;
				this.To = null;
				this.FirstName = Constants.NotAvailableValue;
				this.MiddleNames = Constants.NotAvailableValue;
				this.LastNames = Constants.NotAvailableValue;
				this.PersonalNumber = Constants.NotAvailableValue;
				this.Address = Constants.NotAvailableValue;
				this.Address2 = Constants.NotAvailableValue;
				this.ZipCode = Constants.NotAvailableValue;
				this.Area = Constants.NotAvailableValue;
				this.City = Constants.NotAvailableValue;
				this.Region = Constants.NotAvailableValue;
				this.CountryCode = Constants.NotAvailableValue;
				this.Country = Constants.NotAvailableValue;
				this.OrgName = Constants.NotAvailableValue;
				this.OrgNumber = Constants.NotAvailableValue;
				this.OrgDepartment = Constants.NotAvailableValue;
				this.OrgRole = Constants.NotAvailableValue;
				this.OrgAddress = Constants.NotAvailableValue;
				this.OrgAddress2 = Constants.NotAvailableValue;
				this.OrgZipCode = Constants.NotAvailableValue;
				this.OrgArea = Constants.NotAvailableValue;
				this.OrgCity = Constants.NotAvailableValue;
				this.OrgRegion = Constants.NotAvailableValue;
				this.OrgCountryCode = Constants.NotAvailableValue;
				this.OrgCountry = Constants.NotAvailableValue;
				this.HasOrg = false;
				this.HasPhotos = false;
				this.PhoneNr = Constants.NotAvailableValue;
				this.EMail = Constants.NotAvailableValue;
				this.IsApproved = false;
			}
		}

		/// <summary>
		/// Copies Item to clipboard
		/// </summary>
		[RelayCommand]
		private async Task CopyToClipboard(object Item)
		{
			try
			{
				if (Item is string Label)
				{
					if (Label == this.LegalId)
					{
						await Clipboard.SetTextAsync(Constants.UriSchemes.IotId + ":" + this.LegalId);
						await ServiceRef.UiSerializer.DisplayAlert(
							ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
							ServiceRef.Localizer[nameof(AppResources.IdCopiedSuccessfully)]);
					}
					else
					{
						await Clipboard.SetTextAsync(Label);
						await ServiceRef.UiSerializer.DisplayAlert(
							ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
							ServiceRef.Localizer[nameof(AppResources.TagValueCopiedToClipboard)]);
					}
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiSerializer.DisplayException(ex);
			}
		}
	}
}
