using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Data;
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
		private string? firstName;

		/// <summary>
		/// Middle name(s) as one string
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private string? middleNames;

		/// <summary>
		/// Last name(s) as one string
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private string? lastNames;

		/// <summary>
		/// Personal number
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private string? personalNumber;

		/// <summary>
		/// Address, line 1
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private string? address;

		/// <summary>
		/// Address, line 2
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private string? address2;

		/// <summary>
		/// Zip code (postal code)
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private string? zipCode;

		/// <summary>
		/// Area
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private string? area;

		/// <summary>
		/// City
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private string? city;

		/// <summary>
		/// Region
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private string? region;

		/// <summary>
		/// Country (ISO code)
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private string? countryCode;

		/// <summary>
		/// Country Name
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private string? countryName;

		/// <summary>
		/// Organization name
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private string? orgName;

		/// <summary>
		/// Organization number
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private string? orgNumber;

		/// <summary>
		/// Organization Address, line 1
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private string? orgAddress;

		/// <summary>
		/// Organization Address, line 2
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private string? orgAddress2;

		/// <summary>
		/// Organization Zip code (postal code)
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private string? orgZipCode;

		/// <summary>
		/// Organization Area
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private string? orgArea;

		/// <summary>
		/// Organization City
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private string? orgCity;

		/// <summary>
		/// Organization Region
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
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
		private string? orgCountryName;

		/// <summary>
		/// Organization Department
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
		private string? orgDepartment;

		/// <summary>
		/// Organization Role
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
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
			return Task.CompletedTask;	// Do nothing by default.
		}

	}
}
