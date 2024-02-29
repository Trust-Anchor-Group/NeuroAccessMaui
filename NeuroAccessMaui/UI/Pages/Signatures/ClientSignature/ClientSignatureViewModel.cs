using CommunityToolkit.Mvvm.ComponentModel;
using System.Globalization;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Services;
using Waher.Networking.XMPP.Contracts;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.Contacts;
using CommunityToolkit.Mvvm.Input;

namespace NeuroAccessMaui.UI.Pages.Signatures.ClientSignature
{
	/// <summary>
	/// The view model to bind to for when displaying client signatures.
	/// </summary>
	public partial class ClientSignatureViewModel : BaseViewModel, ILinkableView
	{
		private Waher.Networking.XMPP.Contracts.ClientSignature? clientSignature;
		private LegalIdentity? identity;

		/// <summary>
		/// Creates an instance of the <see cref="ClientSignatureViewModel"/> class.
		/// </summary>
		protected internal ClientSignatureViewModel()
		{
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (ServiceRef.UiService.TryGetArgs(out ClientSignatureNavigationArgs? args))
			{
				this.clientSignature = args.Signature;
				this.identity = args.Identity;
			}

			this.AssignProperties();
		}

		#region Properties

		/// <summary>
		/// The Created date of the signature
		/// </summary>
		[ObservableProperty]
		private DateTime? created;

		/// <summary>
		/// The Updated timestamp of the signature
		/// </summary>
		[ObservableProperty]
		private DateTime? updated;

		/// <summary>
		/// The Legal id of the signature
		/// </summary>
		[ObservableProperty]
		private string? legalId;

		/// <summary>
		/// The current state of the signature
		/// </summary>
		[ObservableProperty]
		private IdentityState? state;

		/// <summary>
		/// The from date of the signature
		/// </summary>
		[ObservableProperty]
		private DateTime? from;

		/// <summary>
		/// The to date of the signature
		/// </summary>
		[ObservableProperty]
		private DateTime? to;

		/// <summary>
		/// The legal identity's first name property
		/// </summary>
		[ObservableProperty]
		private string? firstName;

		/// <summary>
		/// The legal identity's middle names property
		/// </summary>
		[ObservableProperty]
		private string? middleNames;

		/// <summary>
		/// The legal identity's last names property
		/// </summary>
		[ObservableProperty]
		private string? lastNames;

		/// <summary>
		/// The legal identity's personal number property
		/// </summary>
		[ObservableProperty]
		private string? personalNumber;

		/// <summary>
		/// The legal identity's address property
		/// </summary>
		[ObservableProperty]
		private string? address;

		/// <summary>
		/// The legal identity's address line 2 property
		/// </summary>
		[ObservableProperty]
		private string? address2;

		/// <summary>
		/// The legal identity's zip code property
		/// </summary>
		[ObservableProperty]
		private string? zipCode;

		/// <summary>
		/// The legal identity's Area property
		/// </summary>
		[ObservableProperty]
		private string? area;

		/// <summary>
		/// The legal identity's city property
		/// </summary>
		[ObservableProperty]
		private string? city;

		/// <summary>
		/// The legal identity's region property
		/// </summary>
		[ObservableProperty]
		private string? region;

		/// <summary>
		/// The legal identity's country code property
		/// </summary>
		[ObservableProperty]
		private string? countryCode;

		/// <summary>
		/// Nationality (ISO code)
		/// </summary>
		[ObservableProperty]
		private string? nationalityCode;

		/// <summary>
		/// Gender
		/// </summary>
		[ObservableProperty]
		private string? gender;

		/// <summary>
		/// Birth Date
		/// </summary>
		[ObservableProperty]
		private DateTime? birthDate;

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
		/// If organization information is available.
		/// </summary>
		[ObservableProperty]
		private bool? hasOrg;

		/// <summary>
		/// Determines whether the identity is approved or not.
		/// </summary>
		[ObservableProperty]
		private bool? isApproved;

		/// <summary>
		/// The role of the signature
		/// </summary>
		[ObservableProperty]
		private string? role;

		/// <summary>
		/// The signature's timestamp.
		/// </summary>
		[ObservableProperty]
		private string? timestamp;

		/// <summary>
		/// Determines whether the signature is transferable or not.
		/// </summary>
		[ObservableProperty]
		private string? isTransferable;

		/// <summary>
		/// Gets or sets the Bare Jid of the signature.
		/// </summary>
		[ObservableProperty]
		private string? bareJid;

		/// <summary>
		/// Gets or sets the Bare Jid of the signature.
		/// </summary>
		[ObservableProperty]
		private string? phoneNr;

		/// <summary>
		/// Gets or sets the Bare Jid of the signature.
		/// </summary>
		[ObservableProperty]
		private string? eMail;

		/// <summary>
		/// The signature in plain text.
		/// </summary>
		[ObservableProperty]
		private string? signature;

		#endregion

		private void AssignProperties()
		{
			if (this.identity is not null)
			{
				this.Created = this.identity.Created;
				this.Updated = this.identity.Updated.GetDateOrNullIfMinValue();
				this.LegalId = this.identity.Id;
				this.State = this.identity.State;
				this.From = this.identity.From.GetDateOrNullIfMinValue();
				this.To = this.identity.To.GetDateOrNullIfMinValue();
				this.FirstName = this.identity[Constants.XmppProperties.FirstName];
				this.MiddleNames = this.identity[Constants.XmppProperties.MiddleNames];
				this.LastNames = this.identity[Constants.XmppProperties.LastNames];
				this.PersonalNumber = this.identity[Constants.XmppProperties.PersonalNumber];
				this.Address = this.identity[Constants.XmppProperties.Address];
				this.Address2 = this.identity[Constants.XmppProperties.Address2];
				this.ZipCode = this.identity[Constants.XmppProperties.ZipCode];
				this.Area = this.identity[Constants.XmppProperties.Area];
				this.City = this.identity[Constants.XmppProperties.City];
				this.Region = this.identity[Constants.XmppProperties.Region];
				this.CountryCode = this.identity[Constants.XmppProperties.Country];
				this.NationalityCode = this.identity[Constants.XmppProperties.Nationality];
				this.Gender = this.identity[Constants.XmppProperties.Gender];

				string BirthDayStr = this.identity[Constants.XmppProperties.BirthDay];
				string BirthMonthStr = this.identity[Constants.XmppProperties.BirthMonth];
				string BirthYearStr = this.identity[Constants.XmppProperties.BirthYear];

				if (!string.IsNullOrEmpty(BirthDayStr) && int.TryParse(BirthDayStr, out int BirthDay) &&
					!string.IsNullOrEmpty(BirthMonthStr) && int.TryParse(BirthMonthStr, out int BirthMonth) &&
					!string.IsNullOrEmpty(BirthYearStr) && int.TryParse(BirthYearStr, out int BirthYear))
				{
					try
					{
						this.BirthDate = new DateTime(BirthYear, BirthMonth, BirthDay);
					}
					catch (Exception ex)
					{
						ServiceRef.LogService.LogException(ex);
						this.BirthDate = null;
					}
				}

				this.OrgName = this.identity[Constants.XmppProperties.OrgName];
				this.OrgNumber = this.identity[Constants.XmppProperties.OrgNumber];
				this.OrgDepartment = this.identity[Constants.XmppProperties.OrgDepartment];
				this.OrgRole = this.identity[Constants.XmppProperties.OrgRole];
				this.OrgAddress = this.identity[Constants.XmppProperties.OrgAddress];
				this.OrgAddress2 = this.identity[Constants.XmppProperties.OrgAddress2];
				this.OrgZipCode = this.identity[Constants.XmppProperties.OrgZipCode];
				this.OrgArea = this.identity[Constants.XmppProperties.OrgArea];
				this.OrgCity = this.identity[Constants.XmppProperties.OrgCity];
				this.OrgRegion = this.identity[Constants.XmppProperties.OrgRegion];
				this.OrgCountryCode = this.identity[Constants.XmppProperties.OrgCountry];
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
					!string.IsNullOrEmpty(this.OrgCountryCode);
				this.IsApproved = this.identity.State == IdentityState.Approved;
				this.BareJid = this.identity.GetJid(Constants.NotAvailableValue);
				this.PhoneNr = this.identity[Constants.XmppProperties.Phone];
				this.EMail = this.identity[Constants.XmppProperties.EMail];
			}
			else
			{
				this.Created = DateTime.MinValue;
				this.Updated = DateTime.MinValue;
				this.LegalId = Constants.NotAvailableValue;
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
				this.NationalityCode = Constants.NotAvailableValue;
				this.Gender = Constants.NotAvailableValue;
				this.BirthDate = null;
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
				this.HasOrg = false;
				this.IsApproved = false;
				this.BareJid = Constants.NotAvailableValue;
				this.PhoneNr = Constants.NotAvailableValue;
				this.EMail = Constants.NotAvailableValue;
			}
			if (this.clientSignature is not null)
			{
				this.Role = this.clientSignature.Role;
				this.Timestamp = this.clientSignature.Timestamp.ToString(CultureInfo.CurrentCulture);
				this.IsTransferable = this.clientSignature.Transferable.ToYesNo();
				this.BareJid = this.clientSignature.BareJid;
				this.Signature = Convert.ToBase64String(this.clientSignature.DigitalSignature);
			}
			else
			{
				this.Role = Constants.NotAvailableValue;
				this.Timestamp = Constants.NotAvailableValue;
				this.IsTransferable = ServiceRef.Localizer[nameof(AppResources.No)];
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

		#region ILinkableView

		/// <summary>
		/// If the current view is linkable.
		/// </summary>
		public bool IsLinkable => true;

		/// <summary>
		/// If App links should be encoded with the link.
		/// </summary>
		public bool EncodeAppLinks => true;

		/// <summary>
		/// Link to the current view
		/// </summary>
		public string Link => Constants.UriSchemes.IotId + ":" + this.LegalId;

		/// <summary>
		/// Title of the current view
		/// </summary>
		public Task<string> Title => this.GetTitle();

		private async Task<string> GetTitle()
		{
			if (this.identity is null)
				return await ContactInfo.GetFriendlyName(this.LegalId);
			else
				return ContactInfo.GetFriendlyName(this.identity);
		}

		/// <summary>
		/// If linkable view has media associated with link.
		/// </summary>
		public bool HasMedia => false;

		/// <summary>
		/// Encoded media, if available.
		/// </summary>
		public byte[]? Media => null;

		/// <summary>
		/// Content-Type of associated media.
		/// </summary>
		public string? MediaContentType => null;

		#endregion

	}
}
