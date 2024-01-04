using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Data;
using NeuroAccessMaui.Services.Data.PersonalNumbers;
using NeuroAccessMaui.Services.Xmpp;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Registration
{
	/// <summary>
	/// The data model for registering an identity.
	/// </summary>
	public partial class RegisterIdentityModel : XmppViewModel
	{
		/// <summary>
		/// The data model for registering an identity.
		/// </summary>
		public RegisterIdentityModel()
			: base()
		{
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			this.NotifyCommandsCanExecuteChanged();
		}

		/// <inheritdoc/>
		protected override Task XmppService_ConnectionStateChanged(object? Sender, XmppState NewState)
		{
			return MainThread.InvokeOnMainThreadAsync(async () =>
			{
				await base.XmppService_ConnectionStateChanged(Sender, NewState);
				await this.OnConnected();
			});
		}

		/// <summary>
		/// Gets called when the app gets connected.
		/// </summary>
		protected virtual Task OnConnected()
		{
			this.NotifyCommandsCanExecuteChanged();
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public override void SetIsBusy(bool IsBusy)
		{
			base.SetIsBusy(IsBusy);
			this.NotifyCommandsCanExecuteChanged();
		}

		/// <summary>
		/// Updates status of commands.
		/// </summary>
		protected virtual void NotifyCommandsCanExecuteChanged()
		{
			this.ApplyCommand.NotifyCanExecuteChanged();
		}

		/// <summary>
		/// First name
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(FirstNameOk))]
		private string? firstName;

		/// <summary>
		/// Middle name(s) as one string
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(MiddleNamesOk))]
		private string? middleNames;

		/// <summary>
		/// Last name(s) as one string
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(LastNamesOk))]
		private string? lastNames;

		/// <summary>
		/// Personal number
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(PersonalNumberOk))]
		private string? personalNumber;

		/// <summary>
		/// Address, line 1
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(AddressOk))]
		private string? address;

		/// <summary>
		/// Address, line 2
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(Address2Ok))]
		private string? address2;

		/// <summary>
		/// Zip code (postal code)
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(ZipCodeOk))]
		private string? zipCode;

		/// <summary>
		/// Area
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(AreaOk))]
		private string? area;

		/// <summary>
		/// City
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(CityOk))]
		private string? city;

		/// <summary>
		/// Region
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(RegionOk))]
		private string? region;

		/// <summary>
		/// Country (ISO code)
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(CountryOk))]
		private string? countryCode;

		/// <summary>
		/// Country Name
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(CountryOk))]
		private string? countryName;

		/// <summary>
		/// Organization name
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(OrgNameOk))]
		private string? orgName;

		/// <summary>
		/// Organization number
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(OrgNumberOk))]
		private string? orgNumber;

		/// <summary>
		/// Organization Address, line 1
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(OrgAddressOk))]
		private string? orgAddress;

		/// <summary>
		/// Organization Address, line 2
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(OrgAddress2Ok))]
		private string? orgAddress2;

		/// <summary>
		/// Organization Zip code (postal code)
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(OrgZipCodeOk))]
		private string? orgZipCode;

		/// <summary>
		/// Organization Area
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(OrgAreaOk))]
		private string? orgArea;

		/// <summary>
		/// Organization City
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(OrgCityOk))]
		private string? orgCity;

		/// <summary>
		/// Organization Region
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(OrgRegionOk))]
		private string? orgRegion;

		/// <summary>
		/// Organization Country (ISO Code)
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private string? orgCountryCode;

		/// <summary>
		/// Organization Country Name
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(OrgCountryOk))]
		private string? orgCountryName;

		/// <summary>
		/// Organization Department
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(OrgDepartmentOk))]
		private string? orgDepartment;

		/// <summary>
		/// Organization Role
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(OrgRoleOk))]
		private string? orgRole;

		/// <summary>
		/// Phone Number
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private string? phoneNr;

		/// <summary>
		/// EMail
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private string? eMail;

		/// <summary>
		/// Device Id
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private string? deviceId;

		/// <summary>
		/// Jabber Id
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private string? jid;

		/// <summary>
		/// If first name is required.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(FirstNameOk))]
		private bool requiresFirstName;

		/// <summary>
		/// If middle names are required.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(MiddleNamesOk))]
		private bool requiresMiddleNames;

		/// <summary>
		/// If last names are required.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(LastNamesOk))]
		private bool requiresLastNames;

		/// <summary>
		/// If personal number is required.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(PersonalNumberOk))]
		private bool requiresPersonalNumber;

		/// <summary>
		/// If address is required.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(AddressOk))]
		private bool requiresAddress;

		/// <summary>
		/// If address (2nd row) is required.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(Address2Ok))]
		private bool requiresAddress2;

		/// <summary>
		/// If ZIP code is required.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(ZipCodeOk))]
		private bool requiresZipCode;

		/// <summary>
		/// If Area is required.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(AreaOk))]
		private bool requiresArea;

		/// <summary>
		/// If City is required.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(CityOk))]
		private bool requiresCity;

		/// <summary>
		/// If region is required.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(RegionOk))]
		private bool requiresRegion;

		/// <summary>
		/// If Country is required.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(CountryOk))]
		private bool requiresCountry;

		/// <summary>
		/// If Country is required to be an ISO 3166 code.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(CountryOk))]
		private bool requiresCountryIso3166;


		/// <summary>
		/// If organization name is required.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(OrgNameOk))]
		private bool requiresOrgName;

		/// <summary>
		/// If organization department is required.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(OrgDepartmentOk))]
		private bool requiresOrgDepartment;

		/// <summary>
		/// If organization role is required.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(OrgRoleOk))]
		private bool requiresOrgRole;

		/// <summary>
		/// If organization number is required.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(OrgNumberOk))]
		private bool requiresOrgNumber;

		/// <summary>
		/// If organization address is required.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(OrgAddressOk))]
		private bool requiresOrgAddress;

		/// <summary>
		/// If organization address (2nd row) is required.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(OrgAddress2Ok))]
		private bool requiresOrgAddress2;

		/// <summary>
		/// If organization ZIP code is required.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(OrgZipCodeOk))]
		private bool requiresOrgZipCode;

		/// <summary>
		/// If organization Area is required.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(OrgAreaOk))]
		private bool requiresOrgArea;

		/// <summary>
		/// If organization City is required.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(OrgCityOk))]
		private bool requiresOrgCity;

		/// <summary>
		/// If organization region is required.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(OrgRegionOk))]
		private bool requiresOrgRegion;

		/// <summary>
		/// If organization Country is required.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		[NotifyPropertyChangedFor(nameof(OrgCountryOk))]
		private bool requiresOrgCountry;

		/// <summary>
		/// If <see cref="FirstName"/> is OK.
		/// </summary>
		public bool FirstNameOk => !this.RequiresFirstName || !string.IsNullOrWhiteSpace(this.FirstName);

		/// <summary>
		/// If <see cref="MiddleNames"/> is OK.
		/// </summary>
		public bool MiddleNamesOk => !this.RequiresMiddleNames || !string.IsNullOrWhiteSpace(this.MiddleNames);

		/// <summary>
		/// If <see cref="LastNames"/> is OK.
		/// </summary>
		public bool LastNamesOk => !this.RequiresLastNames || !string.IsNullOrWhiteSpace(this.LastNames);

		/// <summary>
		/// If <see cref="PersonalNumber"/> is OK.
		/// </summary>
		public bool PersonalNumberOk
		{
			get
			{
				if (string.IsNullOrWhiteSpace(this.PersonalNumber))
					return !this.RequiresPersonalNumber;

				if (string.IsNullOrWhiteSpace(this.CountryCode))
					return false;

				NumberInformation Info = PersonalNumberSchemes.Validate(this.CountryCode, this.PersonalNumber).Result;

				return !Info.IsValid.HasValue || Info.IsValid.Value;
			}
		}

		/// <summary>
		/// If <see cref="Address"/> is OK.
		/// </summary>
		public bool AddressOk => !this.RequiresAddress || !string.IsNullOrWhiteSpace(this.Address);

		/// <summary>
		/// If <see cref="Address2"/> is OK.
		/// </summary>
		public bool Address2Ok => !this.RequiresAddress2 || !string.IsNullOrWhiteSpace(this.Address2);

		/// <summary>
		/// If <see cref="ZipCode"/> is OK.
		/// </summary>
		public bool ZipCodeOk => !this.RequiresZipCode || !string.IsNullOrWhiteSpace(this.ZipCode);

		/// <summary>
		/// If <see cref="Area"/> is OK.
		/// </summary>
		public bool AreaOk => !this.RequiresArea || !string.IsNullOrWhiteSpace(this.Area);

		/// <summary>
		/// If <see cref="City"/> is OK.
		/// </summary>
		public bool CityOk => !this.RequiresCity || !string.IsNullOrWhiteSpace(this.City);

		/// <summary>
		/// If <see cref="Region"/> is OK.
		/// </summary>
		public bool RegionOk => !this.RequiresRegion || !string.IsNullOrWhiteSpace(this.Region);

		/// <summary>
		/// If <see cref="CountryCode"/> is OK.
		/// </summary>
		public bool CountryOk
		{
			get
			{
				if (string.IsNullOrWhiteSpace(this.CountryCode))
					return !this.RequiresCountry;

				if (this.RequiresCountryIso3166 && !ISO_3166_1.TryGetCountryByCode(this.CountryCode, out _))
					return false;

				return true;
			}
		}

		/// <summary>
		/// If <see cref="OrgName"/> is OK.
		/// </summary>
		public bool OrgNameOk => !this.RequiresOrgName || !string.IsNullOrWhiteSpace(this.OrgName);

		/// <summary>
		/// If <see cref="OrgDepartment"/> is OK.
		/// </summary>
		public bool OrgDepartmentOk => !this.RequiresOrgDepartment || !string.IsNullOrWhiteSpace(this.OrgDepartment);

		/// <summary>
		/// If <see cref="OrgRole"/> is OK.
		/// </summary>
		public bool OrgRoleOk => !this.RequiresOrgRole || !string.IsNullOrWhiteSpace(this.OrgRole);

		/// <summary>
		/// If <see cref="OrgNumber"/> is OK.
		/// </summary>
		public bool OrgNumberOk => !this.RequiresOrgNumber || !string.IsNullOrEmpty(this.OrgNumber);

		/// <summary>
		/// If <see cref="OrgAddress"/> is OK.
		/// </summary>
		public bool OrgAddressOk => !this.RequiresOrgAddress || !string.IsNullOrWhiteSpace(this.OrgAddress);

		/// <summary>
		/// If <see cref="OrgAddress2"/> is OK.
		/// </summary>
		public bool OrgAddress2Ok => !this.RequiresOrgAddress2 || !string.IsNullOrWhiteSpace(this.OrgAddress2);

		/// <summary>
		/// If <see cref="OrgZipCode"/> is OK.
		/// </summary>
		public bool OrgZipCodeOk => !this.RequiresOrgZipCode || !string.IsNullOrWhiteSpace(this.OrgZipCode);

		/// <summary>
		/// If <see cref="OrgArea"/> is OK.
		/// </summary>
		public bool OrgAreaOk => !this.RequiresOrgArea || !string.IsNullOrWhiteSpace(this.OrgArea);

		/// <summary>
		/// If <see cref="OrgCity"/> is OK.
		/// </summary>
		public bool OrgCityOk => !this.RequiresOrgCity || !string.IsNullOrWhiteSpace(this.OrgCity);

		/// <summary>
		/// If <see cref="OrgRegion"/> is OK.
		/// </summary>
		public bool OrgRegionOk => !this.RequiresOrgRegion || !string.IsNullOrWhiteSpace(this.OrgRegion);

		/// <summary>
		/// If <see cref="OrgCountryCode"/> is OK.
		/// </summary>
		public bool OrgCountryOk
		{
			get
			{
				if (string.IsNullOrWhiteSpace(this.OrgCountryCode))
					return !this.RequiresOrgCountry;

				if (this.RequiresCountryIso3166 && !ISO_3166_1.TryGetCountryByCode(this.OrgCountryCode, out _))
					return false;

				return true;
			}
		}

		/// <summary>
		/// Converts the <see cref="RegisterIdentityModel"/> to an array of <inheritdoc cref="Property"/>.
		/// </summary>
		/// <param name="XmppService">The XMPP service to use for accessing the Bare Jid.</param>
		/// <returns>The <see cref="RegisterIdentityModel"/> as a list of properties.</returns>
		public Property[] ToProperties(IXmppService XmppService)
		{
			List<Property> Properties = [];

			AddProperty(Properties, Constants.XmppProperties.FirstName, this.FirstName);
			AddProperty(Properties, Constants.XmppProperties.MiddleNames, this.MiddleNames);
			AddProperty(Properties, Constants.XmppProperties.LastNames, this.LastNames);
			AddProperty(Properties, Constants.XmppProperties.PersonalNumber, this.PersonalNumber);
			AddProperty(Properties, Constants.XmppProperties.Address, this.Address);
			AddProperty(Properties, Constants.XmppProperties.Address2, this.Address2);
			AddProperty(Properties, Constants.XmppProperties.ZipCode, this.ZipCode);
			AddProperty(Properties, Constants.XmppProperties.Area, this.Area);
			AddProperty(Properties, Constants.XmppProperties.City, this.City);
			AddProperty(Properties, Constants.XmppProperties.Region, this.Region);
			AddProperty(Properties, Constants.XmppProperties.Country, ISO_3166_1.ToCode(this.CountryCode));
			AddProperty(Properties, Constants.XmppProperties.OrgName, this.OrgName);
			AddProperty(Properties, Constants.XmppProperties.OrgNumber, this.OrgNumber);
			AddProperty(Properties, Constants.XmppProperties.OrgDepartment, this.OrgDepartment);
			AddProperty(Properties, Constants.XmppProperties.OrgRole, this.OrgRole);
			AddProperty(Properties, Constants.XmppProperties.OrgAddress, this.OrgAddress);
			AddProperty(Properties, Constants.XmppProperties.OrgAddress2, this.OrgAddress2);
			AddProperty(Properties, Constants.XmppProperties.OrgZipCode, this.OrgZipCode);
			AddProperty(Properties, Constants.XmppProperties.OrgArea, this.OrgArea);
			AddProperty(Properties, Constants.XmppProperties.OrgCity, this.OrgCity);
			AddProperty(Properties, Constants.XmppProperties.OrgRegion, this.OrgRegion);
			AddProperty(Properties, Constants.XmppProperties.OrgCountry, ISO_3166_1.ToCode(this.OrgCountryCode));
			AddProperty(Properties, Constants.XmppProperties.Phone, this.PhoneNr);
			AddProperty(Properties, Constants.XmppProperties.EMail, this.EMail);
			AddProperty(Properties, Constants.XmppProperties.DeviceId, this.DeviceId);
			AddProperty(Properties, Constants.XmppProperties.Jid, XmppService.BareJid);

			return [.. Properties];
		}

		private static void AddProperty(List<Property> Properties, string PropertyName, string? PropertyValue)
		{
			string? s = PropertyValue?.Trim();

			if (!string.IsNullOrWhiteSpace(s))
				Properties.Add(new Property(PropertyName, s));
		}

		/// <summary>
		/// Sets the properties of the view model.
		/// </summary>
		/// <param name="Properties">Array of properties to set.</param>
		/// <param name="ClearPropertiesNotFound">If properties should be cleared if they are not found in <paramref name="Properties"/>.</param>
		public void SetProperties(Property[] Properties, bool ClearPropertiesNotFound)
		{
			if (ClearPropertiesNotFound)
			{
				this.FirstName = string.Empty;
				this.MiddleNames = string.Empty;
				this.LastNames = string.Empty;
				this.PersonalNumber = string.Empty;
				this.Address = string.Empty;
				this.Address2 = string.Empty;
				this.ZipCode = string.Empty;
				this.Area = string.Empty;
				this.City = string.Empty;
				this.Region = string.Empty;
				this.CountryCode = string.Empty;
				this.CountryName = string.Empty;
				this.OrgName = string.Empty;
				this.OrgNumber = string.Empty;
				this.OrgAddress = string.Empty;
				this.OrgAddress2 = string.Empty;
				this.OrgZipCode = string.Empty;
				this.OrgArea = string.Empty;
				this.OrgCity = string.Empty;
				this.OrgRegion = string.Empty;
				this.OrgCountryCode = string.Empty;
				this.OrgCountryName = string.Empty;
				this.OrgDepartment = string.Empty;
				this.OrgRole = string.Empty;
				this.PhoneNr = string.Empty;
				this.EMail = string.Empty;
				this.DeviceId = string.Empty;
				this.Jid = string.Empty;
			}

			foreach (Property P in Properties)
			{
				switch (P.Name)
				{
					case Constants.XmppProperties.FirstName:
						this.FirstName = P.Value;
						break;

					case Constants.XmppProperties.MiddleNames:
						this.MiddleNames = P.Value;
						break;

					case Constants.XmppProperties.LastNames:
						this.LastNames = P.Value;
						break;

					case Constants.XmppProperties.PersonalNumber:
						this.PersonalNumber = P.Value;
						break;

					case Constants.XmppProperties.Address:
						this.Address = P.Value;
						break;

					case Constants.XmppProperties.Address2:
						this.Address2 = P.Value;
						break;

					case Constants.XmppProperties.ZipCode:
						this.ZipCode = P.Value;
						break;

					case Constants.XmppProperties.Area:
						this.Area = P.Value;
						break;

					case Constants.XmppProperties.City:
						this.City = P.Value;
						break;

					case Constants.XmppProperties.Region:
						this.Region = P.Value;
						break;

					case Constants.XmppProperties.Country:
						this.CountryCode = P.Value;
						this.CountryName = ISO_3166_1.ToName(P.Value);
						break;

					case Constants.XmppProperties.OrgName:
						this.OrgName = P.Value;
						break;

					case Constants.XmppProperties.OrgNumber:
						this.OrgNumber = P.Value;
						break;

					case Constants.XmppProperties.OrgRole:
						this.OrgRole = P.Value;
						break;

					case Constants.XmppProperties.OrgDepartment:
						this.OrgDepartment = P.Value;
						break;

					case Constants.XmppProperties.OrgAddress:
						this.OrgAddress = P.Value;
						break;

					case Constants.XmppProperties.OrgAddress2:
						this.OrgAddress2 = P.Value;
						break;

					case Constants.XmppProperties.OrgZipCode:
						this.OrgZipCode = P.Value;
						break;

					case Constants.XmppProperties.OrgArea:
						this.OrgArea = P.Value;
						break;

					case Constants.XmppProperties.OrgCity:
						this.OrgCity = P.Value;
						break;

					case Constants.XmppProperties.OrgRegion:
						this.OrgRegion = P.Value;
						break;

					case Constants.XmppProperties.OrgCountry:
						this.OrgCountryCode = P.Value;
						this.OrgCountryName = ISO_3166_1.ToName(P.Value);
						break;
				}
			}

			this.Jid = ServiceRef.XmppService.BareJid;
			this.PhoneNr = ServiceRef.TagProfile.PhoneNumber;
			this.EMail = ServiceRef.TagProfile.EMail;
		}

		/// <summary>
		/// Command for closing current view and go back to previous view.
		/// </summary>
		[RelayCommand]
		protected virtual async Task GoBack()
		{
			await ServiceRef.NavigationService.GoBackAsync();
		}


		/// <summary>
		/// Used to find out if an ICommand can execute
		/// </summary>
		public virtual bool CanExecuteCommands => !this.IsBusy && this.IsConnected;

		/// <summary>
		/// Used to find out if an ICommand can execute
		/// </summary>
		public virtual bool CanApply => false;

		/// <summary>
		/// Executes the application command.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanApply))]
		protected virtual Task Apply()
		{
			return Task.CompletedTask; // Do nothing by default.
		}

	}
}
