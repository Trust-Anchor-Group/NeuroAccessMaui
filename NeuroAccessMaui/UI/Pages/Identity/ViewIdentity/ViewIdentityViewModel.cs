using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Localization;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.UI.Photos;
using NeuroAccessMaui.UI.MVVM;
using NeuroAccessMaui.UI.Pages.Applications.ApplyId;
using NeuroAccessMaui.UI.Pages.Contacts.Chat;
using NeuroAccessMaui.UI.Pages.Identity.ObjectModel;
using NeuroAccessMaui.UI.Popups.Image;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Script.Constants;

namespace NeuroAccessMaui.UI.Pages.Identity.ViewIdentity
{


	public partial class ViewIdentityViewModel : QrXmppViewModel
	{
		private readonly PhotosLoader photosLoader;
		private readonly ViewIdentityNavigationArgs? args;
		private readonly IDispatcherTimer? timer;
		private readonly IDispatcherTimer? qrTimer;

		private LegalIdentity? identity = null;

		private bool hasAppeared;

		/// <summary>
		/// You'll find out on your birthday
		/// </summary>
		[ObservableProperty]
		private bool shouldCelebrate = false;

		[ObservableProperty]
		private bool canAddContact = false;
		[ObservableProperty]
		private bool canRemoveContact = false;

		[ObservableProperty]
		private bool isThirdPartyIdentity = false;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasPersonalFields))]
		private bool hasDomainProperty = false;

		[ObservableProperty]
		private string friendlyName = string.Empty;
		[ObservableProperty]
		private string subText = string.Empty;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasAge))]
		private string ageText = string.Empty;
		public bool HasAge => !string.IsNullOrEmpty(this.AgeText);

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(IsApproved))]
		[NotifyPropertyChangedFor(nameof(ShowBackground))]
		private IdentityState? identityState = Waher.Networking.XMPP.Contracts.IdentityState.Created;

		public bool IsApproved => this.IdentityState is not null && this.IdentityState == Waher.Networking.XMPP.Contracts.IdentityState.Approved;


		[ObservableProperty]
		private DateTime? expireDate = null;
		[ObservableProperty]
		private DateTime? issueDate = null;

		[ObservableProperty]
		private int timerSeconds = Convert.ToInt32(Constants.Timeouts.IdentityAllowedWatch.TotalSeconds);

		public ObservableCollection<ObservableFieldItem> PersonalFields { get; } = [];
		public ObservableCollection<ObservableFieldItem> OrganizationFields { get; } = [];
		public ObservableCollection<ObservableFieldItem> TechnicalFields { get; } = [];
		public ObservableCollection<ObservableFieldItem> OtherFields { get; } = [];

		public bool HasPersonalFields => this.PersonalFields.Count > 0 && !this.HasDomainProperty;
		public bool HasOrganizationFields => this.OrganizationFields.Count > 0;
		public bool HasTechnicalFields => this.TechnicalFields.Count > 0;
		public bool HasOtherFields => this.OtherFields.Count > 0;

		public ObservableTask<bool> LoadIdentityTask { get; }
		public ObservableTask<int> LoadPhotosTask { get; }
		public ObservableCollection<Photo> Photos { get; } = [];

		public ImageSource? ProfilePhoto
		{
			get
			{
				// Look for a photo named "ProfilePhoto" (adjust property as needed)
				Photo? Profile = this.Photos.FirstOrDefault(p => p.Attachment?.FileName.StartsWith("ProfilePhoto", StringComparison.OrdinalIgnoreCase) ?? false);

				// If not found, fallback to first photo
				return (Profile ?? this.Photos.FirstOrDefault())?.Source;
			}
		}

		public bool HasProfilePhoto => this.ProfilePhoto is not null;

		public bool HasPhotos => this.Photos.Count > 0;

		public bool HasTimer => this.timer?.IsRunning ?? false;

		// Get the length of the profile image side in DIPs (device independent pixels).
		public double ProfileImageSideLength
		{
			get
			{
				// Width and Height are in pixels
				double Width = DeviceDisplay.Current.MainDisplayInfo.Width;
				double Height = DeviceDisplay.Current.MainDisplayInfo.Height;

				// Aspect ratio (width / height)
				double AspectRatio = Height / Width;

				double Length = AspectRatio * AspectRatio * 45;

				if (Length > 250) Length = 250;
				if (Length < 125) Length = 125;

				return Length;
			}
		}

		// Define custom field descriptors for any multi-part or special properties for example BDAY BMONTH BYEAR -> B
		private static readonly List<CustomFieldDefinition> customFields =
		[
			new CustomFieldDefinition
			(
				Keys: [Constants.XmppProperties.BirthDay, Constants.XmppProperties.BirthMonth, Constants.XmppProperties.BirthYear],
				NewKey: Constants.CustomXmppProperties.BirthDate,
				GetLabel: (_) => ServiceRef.Localizer[nameof(AppResources.BirthDate), false],
				GetValue: static identity =>
				{
					string D = identity[Constants.XmppProperties.BirthDay];
					string M = identity[Constants.XmppProperties.BirthMonth];
					string y = identity[Constants.XmppProperties.BirthYear];
					if (int.TryParse(D, out int Day) && int.TryParse(M, out int Month) && int.TryParse(y, out int Year))
					{
						try
						{
							return new DateTime(Year, Month, Day).ToString("d", CultureInfo.CurrentCulture.DateTimeFormat);
						}
						catch(Exception Ex)
						{
							ServiceRef.LogService.LogException(Ex);
						}
					}
					return null;
			}),
			new CustomFieldDefinition(
				Keys: Array.Empty<string>(),
				NewKey: Constants.CustomXmppProperties.Neuro_Id,
				GetLabel: _ => ServiceRef.Localizer[nameof(AppResources.NeuroID)],
				GetValue: identity => identity.Id),

			new CustomFieldDefinition(
				Keys: Array.Empty<string>(),
				NewKey: Constants.CustomXmppProperties.Provider,
				GetLabel: _ => ServiceRef.Localizer[nameof(AppResources.Provider)],
				GetValue: identity => identity.Provider),

			new CustomFieldDefinition(
				Keys: Array.Empty<string>(),
				NewKey: Constants.CustomXmppProperties.State,
				GetLabel: identity => ServiceRef.Localizer[nameof(AppResources.Status)],
				GetValue: identity => ServiceRef.Localizer["IdentityState_" + identity.State.ToString()]),

			new CustomFieldDefinition(
				Keys: Array.Empty<string>(),
				NewKey: Constants.CustomXmppProperties.Created,
				GetLabel: (_) => ServiceRef.Localizer[nameof(AppResources.Created)],
				GetValue: identity => identity.Created.ToString("g", CultureInfo.CurrentCulture)),

			new CustomFieldDefinition(
				Keys: Array.Empty<string>(),
				NewKey: Constants.CustomXmppProperties.Updated,
				GetLabel: (_) => ServiceRef.Localizer[nameof(AppResources.Updated)],
				GetValue: identity => identity.Updated.ToString("g", CultureInfo.CurrentCulture)),

			new CustomFieldDefinition(
				Keys: Array.Empty<string>(),
				NewKey: Constants.CustomXmppProperties.From,
				GetLabel: (_) => ServiceRef.Localizer[nameof(AppResources.Issued)],
				GetValue: identity => identity.From.ToString("d", CultureInfo.CurrentCulture)),

			new CustomFieldDefinition(
				Keys: Array.Empty<string>(),
				NewKey: Constants.CustomXmppProperties.To,
				GetLabel: (_) => ServiceRef.Localizer[nameof(AppResources.Expires)],
				GetValue: identity => identity.To.ToString("d", CultureInfo.CurrentCulture)),
		];

		public string BannerUriLight => ServiceRef.ThemeService.GetImageUri(Constants.Branding.BannerSmallLight);
		public string BannerUriDark => ServiceRef.ThemeService.GetImageUri(Constants.Branding.BannerSmallDark);

		public string BannerUri =>
			Application.Current.RequestedTheme switch
			{
				AppTheme.Dark => this.BannerUriDark,
				AppTheme.Light => this.BannerUriLight,
				_ => this.BannerUriLight
			};


		public ViewIdentityViewModel(ViewIdentityNavigationArgs? args)
			: base()
		{
			this.args = args;
			this.photosLoader = new PhotosLoader();

			this.LoadIdentityTask = new ObservableTask<bool>();
			this.LoadPhotosTask = new ObservableTask<int>();

			Application.Current.RequestedThemeChanged += (_, __) =>
				OnPropertyChanged(nameof(BannerUri));

			this.timer = Application.Current?.Dispatcher.CreateTimer();
			if (this.timer is null)
				return;
			this.timer.Interval = TimeSpan.FromSeconds(1);
			this.timer.Tick += this.OnTimerTick;

			this.qrTimer = Application.Current?.Dispatcher.CreateTimer();
			if (this.qrTimer is null)
				return;
			this.qrTimer.Interval = Constants.Intervals.Qr;
			this.qrTimer.Tick += this.OnQrTimerTick;
		}



		protected override async Task OnAppearing()
		{
			await base.OnAppearing();

			bool IsRefresh = this.hasAppeared;
			this.hasAppeared = true;

			// Determine identity source
			LegalIdentity Identity = this.args?.Identity ?? ServiceRef.TagProfile.LegalIdentity!;
			this.identity = Identity;

			if (IsRefresh)
			{
				// TODO: Refresh identity from server
				// identity = await ServiceRef.XmppService.GetLegalIdentity(identity.Id);
				ServiceRef.LogService.LogWarning("Refreshing identity...");
			}

			this.IdentityState = Identity.State;

			string Domain = Identity.GetDomain();

			string FullJid = Identity.GetJid();
			string[]? Jid = null;


			if (!string.IsNullOrEmpty(FullJid))
			{
				Jid =  FullJid.Split('@');
				Jid[1] = "@" + Jid[1];
				this.FriendlyName = Jid.Length > 0 ? Jid[0] : FullJid;
				this.SubText = Jid.Length > 1 ? Jid[1] : string.Empty;
			}
			else
			{
				this.FriendlyName = Identity.Id;
			}

			if(!string.IsNullOrEmpty(Domain))
			{
				this.FriendlyName = Domain;
				this.SubText = Identity.Id;

				this.HasDomainProperty = true;
			}

			// Friendly name
			PersonalInformation? PInfo = null;
			try
			{
				if (!this.HasDomainProperty)
				{
					PInfo = Identity.GetPersonalInfo();
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			} 
			
			if (PInfo is not null)
			{
				if (!string.IsNullOrEmpty(PInfo.FullName))
				{
					this.FriendlyName = PInfo.FullName;
					if (PInfo.HasBirthDate)
						this.SubText = PInfo.BirthDate!.Value.ToShortDateString();
					else
						this.SubText = Jid is not null ? Jid[1] : string.Empty;
				}
				if (PInfo.HasBirthDate && PInfo.Age > 0)
					this.AgeText = PInfo.Age.ToString(CultureInfo.InvariantCulture);
			}


			this.IssueDate = Identity.From;
			this.ExpireDate = Identity.To;

			// Load fields
			this.LoadIdentityTask.Load(async ctx =>
			{
				List<ObservableFieldItem> PersonalList = [];
				List<ObservableFieldItem> OrganizationList = [];
				List<ObservableFieldItem> TechnicalList = [];
				List<ObservableFieldItem> OtherList = [];

				// Reviewable keys set
				HashSet<string> ReviewableKeys = new(StringComparer.OrdinalIgnoreCase)
				{
					Constants.XmppProperties.FirstName,
					Constants.XmppProperties.MiddleNames,
					Constants.XmppProperties.LastNames,
					Constants.XmppProperties.PersonalNumber,
					Constants.XmppProperties.Address,
					Constants.XmppProperties.Address2,
					Constants.XmppProperties.ZipCode,
					Constants.XmppProperties.Area,
					Constants.XmppProperties.City,
					Constants.XmppProperties.Region,
					Constants.XmppProperties.Country,
					Constants.XmppProperties.BirthDay,
					Constants.XmppProperties.BirthMonth,
					Constants.XmppProperties.BirthYear,
					Constants.XmppProperties.OrgName,
					Constants.XmppProperties.OrgDepartment,
					Constants.XmppProperties.OrgRole,
					Constants.XmppProperties.OrgAddress,
					Constants.XmppProperties.OrgAddress2,
					Constants.XmppProperties.OrgZipCode,
					Constants.XmppProperties.OrgArea,
					Constants.XmppProperties.OrgCity,
					Constants.XmppProperties.OrgRegion,
					Constants.XmppProperties.OrgCountry,
					Constants.XmppProperties.OrgNumber
				};

				// Classification sets using Constants
				HashSet<string> PersonalKeys = new(StringComparer.OrdinalIgnoreCase)
				{
					Constants.XmppProperties.FirstName,
					Constants.XmppProperties.MiddleNames,
					Constants.XmppProperties.LastNames,
					Constants.CustomXmppProperties.BirthDate,
					Constants.XmppProperties.BirthDay,
					Constants.XmppProperties.BirthMonth,
					Constants.XmppProperties.BirthYear,
					Constants.XmppProperties.Address,
					Constants.XmppProperties.Address2,
					Constants.XmppProperties.ZipCode,
					Constants.XmppProperties.Area,
					Constants.XmppProperties.City,
					Constants.XmppProperties.Region,
					Constants.XmppProperties.Country,
					Constants.XmppProperties.PersonalNumber,
					Constants.XmppProperties.Nationality,
					Constants.XmppProperties.Gender,
					Constants.XmppProperties.Phone,
					Constants.XmppProperties.EMail

				};

				HashSet<string> OrgKeys = new(StringComparer.OrdinalIgnoreCase)
				{
					Constants.XmppProperties.OrgName,
					Constants.XmppProperties.OrgDepartment,
					Constants.XmppProperties.OrgRole,
					Constants.XmppProperties.OrgAddress,
					Constants.XmppProperties.OrgAddress2,
					Constants.XmppProperties.OrgZipCode,
					Constants.XmppProperties.OrgArea,
					Constants.XmppProperties.OrgCity,
					Constants.XmppProperties.OrgRegion,
					Constants.XmppProperties.OrgCountry,
					Constants.XmppProperties.OrgNumber
				};

				HashSet<string> TechnicalKeys = new(StringComparer.OrdinalIgnoreCase)
				{
					Constants.XmppProperties.Jid,
					Constants.CustomXmppProperties.Neuro_Id,
					Constants.CustomXmppProperties.Provider,
					Constants.CustomXmppProperties.State,
					Constants.CustomXmppProperties.Created,
					Constants.CustomXmppProperties.Updated,
					Constants.CustomXmppProperties.From,
					Constants.CustomXmppProperties.To,
					Constants.XmppProperties.DeviceId
				};

				// Label map for display names (We should localize based on the key, but this is done so we don't need to refactor the localization fornow)
				// TODO: Localize based on the key
				Dictionary<string, LocalizedString> LabelMap = new(StringComparer.OrdinalIgnoreCase)
				{
				   {Constants.XmppProperties.FirstName,   ServiceRef.Localizer[nameof(AppResources.FirstName)]},
				   {Constants.XmppProperties.MiddleNames, ServiceRef.Localizer[nameof(AppResources.MiddleNames)]},
				   {Constants.XmppProperties.LastNames,   ServiceRef.Localizer[nameof(AppResources.LastNames)]},
				   {Constants.CustomXmppProperties.BirthDate, ServiceRef.Localizer[nameof(AppResources.BirthDate)]},
				   {Constants.XmppProperties.Address,     ServiceRef.Localizer[nameof(AppResources.Address)]},
				   {Constants.XmppProperties.Address2,    ServiceRef.Localizer[nameof(AppResources.Address2)]},
				   {Constants.XmppProperties.ZipCode,     ServiceRef.Localizer[nameof(AppResources.ZipCode)]},
				   {Constants.XmppProperties.Area,        ServiceRef.Localizer[nameof(AppResources.Area)]},
				   {Constants.XmppProperties.City,        ServiceRef.Localizer[nameof(AppResources.City)]},
				   {Constants.XmppProperties.Region,      ServiceRef.Localizer[nameof(AppResources.Region)]},
				   {Constants.XmppProperties.Country,     ServiceRef.Localizer[nameof(AppResources.Country)]},
				   {Constants.XmppProperties.Nationality,     ServiceRef.Localizer[nameof(AppResources.Nationality)]},
				   {Constants.XmppProperties.PersonalNumber, ServiceRef.Localizer[nameof(AppResources.PersonalNumber)]},
				   {Constants.XmppProperties.Gender, ServiceRef.Localizer[nameof(AppResources.Gender)]},
				   {Constants.XmppProperties.Phone, ServiceRef.Localizer[nameof(AppResources.PhoneNr)]},
				   {Constants.XmppProperties.EMail, ServiceRef.Localizer[nameof(AppResources.EMail)]},
				   {Constants.XmppProperties.OrgName,    ServiceRef.Localizer[nameof(AppResources.OrgName)]},
				   {Constants.XmppProperties.OrgDepartment, ServiceRef.Localizer[nameof(AppResources.OrgDepartment)]},
				   {Constants.XmppProperties.OrgRole,     ServiceRef.Localizer[nameof(AppResources.OrgRole)]},
				   {Constants.XmppProperties.OrgAddress,  ServiceRef.Localizer[nameof(AppResources.OrgAddress)]},
				   {Constants.XmppProperties.OrgAddress2, ServiceRef.Localizer[nameof(AppResources.OrgAddress2)]},
				   {Constants.XmppProperties.OrgZipCode,  ServiceRef.Localizer[nameof(AppResources.OrgZipCode)]},
				   {Constants.XmppProperties.OrgArea,     ServiceRef.Localizer[nameof(AppResources.OrgArea)]},
				   {Constants.XmppProperties.OrgCity,     ServiceRef.Localizer[nameof(AppResources.OrgCity)]},
				   {Constants.XmppProperties.OrgRegion,   ServiceRef.Localizer[nameof(AppResources.OrgRegion)]},
				   {Constants.XmppProperties.OrgCountry,  ServiceRef.Localizer[nameof(AppResources.OrgCountry)]},
				   {Constants.XmppProperties.OrgNumber,   ServiceRef.Localizer[nameof(AppResources.OrgNumber)]},
				   {Constants.XmppProperties.Jid,   ServiceRef.Localizer[nameof(AppResources.NetworkID)]},
				   {Constants.XmppProperties.DeviceId,   ServiceRef.Localizer[nameof(AppResources.DeviceID)]}
				};




				// Handle custom fields first
				HashSet<string> UsedKeys = new(StringComparer.OrdinalIgnoreCase);
				// Now iterate raw properties
				foreach (Property? Prop in Identity.Properties ?? [])
				{
					if (UsedKeys.Contains(Prop.Name))
						continue;

					string? Key = null;
					LocalizedString? Label = null;
					string? ValueOverride = null;

					// Handle custom definitions
					CustomFieldDefinition? CustomDef = customFields.Find(CustomFieldDefinition => CustomFieldDefinition.Keys.Contains(Prop.Name, StringComparer.OrdinalIgnoreCase));
					if (CustomDef is not null)
					{
						if (!CustomDef.Keys.Any(k => UsedKeys.Contains(k)))
						{
							ValueOverride = CustomDef.GetValue(Identity);
							//	if (val is null)
							//		continue;
							if (!string.IsNullOrEmpty(ValueOverride))
							{
								Key = CustomDef.NewKey;
								Label = CustomDef.GetLabel(Identity);
								foreach (string Keys in CustomDef.Keys)
									UsedKeys.Add(Keys);
							}
							else
								ValueOverride = null;
						}
					}

					// Create a new field item
					Key ??= Prop.Name;
					Label ??= LabelMap.TryGetValue(Prop.Name, out LocalizedString? L) ? L : new LocalizedString(Prop.Name, Prop.Name);
					if (Label.ResourceNotFound)
					{
						Label = new LocalizedString(Prop.Name, Prop.Name);
					}
					bool IsReviewable = ReviewableKeys.Contains(Prop.Name);
					ObservableFieldItem Item = new(Key, Label, Identity, IsReviewable, ValueOverride);

					if (PersonalKeys.Contains(Prop.Name)) PersonalList.Add(Item);
					else if (OrgKeys.Contains(Prop.Name)) OrganizationList.Add(Item);
					else if (TechnicalKeys.Contains(Prop.Name)) TechnicalList.Add(Item);
					else OtherList.Add(Item);

					UsedKeys.Add(Key);

				}

				// Handle custom fields that are not part of the identity model
				customFields
					.Where(CustomFieldDefinition => CustomFieldDefinition.Keys.Length == 0)
					.ToList()
					.ForEach(CustomFieldDefinition =>
					{
						string? ValueOverride = CustomFieldDefinition.GetValue(Identity);
						if (string.IsNullOrEmpty(ValueOverride))
							return;
						// Create a new field item
						ObservableFieldItem Item = new(CustomFieldDefinition.NewKey, CustomFieldDefinition.GetLabel(Identity), Identity, false, ValueOverride);
						if (PersonalKeys.Contains(CustomFieldDefinition.NewKey)) PersonalList.Add(Item);
						else if (OrgKeys.Contains(CustomFieldDefinition.NewKey)) OrganizationList.Add(Item);
						else if (TechnicalKeys.Contains(CustomFieldDefinition.NewKey)) TechnicalList.Add(Item);
						else OtherList.Add(Item);
					});


				bool ShouldCelebrate = PersonalList.Any(Item => Item.Key == Constants.CustomXmppProperties.BirthDate &&
																!string.IsNullOrEmpty(Item.Value) &&
																DateTime.TryParse(Item.Value, out DateTime BirthDate) &&
																BirthDate == DateTime.Today);

				// Check if we can add or remove contact and update contact info

				bool CanAddContact = false;
				bool CanRemoveContact = false;
				bool IsThirdPartyIdentity = false;

				string MyJid = ServiceRef.TagProfile.Account + "@" + ServiceRef.TagProfile.Domain;
				string Jid = this.identity.GetJid();
				if (!Jid.Equals(MyJid, StringComparison.OrdinalIgnoreCase))
				{
					try
					{
						ContactInfo? Info = await ContactInfo.FindByBareJid(Jid);
						if ((Info is not null) &&
							(Info.LegalIdentity is null ||
							(Info.LegalId != this.identity.Id &&
							Info.LegalIdentity.Created < this.identity!.Created &&
							this.identity.State == Waher.Networking.XMPP.Contracts.IdentityState.Approved)))
						{
							Info.LegalId = this.identity.Id;
							Info.LegalIdentity = this.identity;
							Info.FriendlyName = ContactInfo.GetFriendlyName(this.identity);

							await Database.Update(Info);
							await Database.Provider.Flush();
						}

						CanAddContact = Info is null;
						CanRemoveContact = Info is not null;

						IsThirdPartyIdentity = true;
					}
					catch (Exception Ex)
					{
						ServiceRef.LogService.LogException(Ex);
					}

				}

				// Apply to UI on main thread
				await MainThread.InvokeOnMainThreadAsync(async () =>
				{
					this.PersonalFields.Clear(); PersonalList.ForEach(this.PersonalFields.Add);
					await Task.Yield(); // Sacrifice performance for UI responsiveness. In the future, we might encounter identities with a ridiculous amount of fields.
					this.OrganizationFields.Clear(); OrganizationList.ForEach(this.OrganizationFields.Add);
					await Task.Yield();
					this.TechnicalFields.Clear(); TechnicalList.ForEach(this.TechnicalFields.Add);
					await Task.Yield();
					this.OtherFields.Clear(); OtherList.ForEach(this.OtherFields.Add);

					this.ShouldCelebrate = ShouldCelebrate;
					this.CanAddContact = CanAddContact;
					this.CanRemoveContact = CanRemoveContact;

					this.IsThirdPartyIdentity = IsThirdPartyIdentity;

					this.OnPropertyChanged(nameof(this.HasPersonalFields));
					this.OnPropertyChanged(nameof(this.HasOrganizationFields));
					this.OnPropertyChanged(nameof(this.HasTechnicalFields));
					this.OnPropertyChanged(nameof(this.HasOtherFields));
					this.OnPropertyChanged(nameof(this.HasAge));


					this.timer?.Start();
					this.OnPropertyChanged(nameof(this.HasTimer));

					this.OnQrTimerTick(this, EventArgs.Empty); // Generate the QR code for the first time
					//this.qrTimer?.Start(); //Currently the qr is not random, so no need to set time for refresh

				});

			});

			// Load photos
			this.LoadPhotosTask.Load(async ctx =>
			{
				this.photosLoader.CancelLoadPhotos();

				List<Photo> Buffer = [];
				Attachment[] Atts = Identity.Attachments ?? [];
				string[] AllowedContentTypes = new[]
				{
					Waher.Content.Images.ImageCodec.ContentTypePng,
					Waher.Content.Images.ImageCodec.ContentTypeJpg,
				};

				for (int i = 0; i < Atts.Length; i++)
				{
					if (!AllowedContentTypes.Contains(Atts[i].ContentType))
						continue;

					if (ctx.CancellationToken.IsCancellationRequested)
						break;

					ctx.Progress.Report(i * 100 / Math.Max(Atts.Length, 1));
					(byte[]? Bin, string _, int Rot) = await this.photosLoader.LoadOnePhoto(Atts[i], SignWith.LatestApprovedIdOrCurrentKeys);
					if (Bin is not null)
						Buffer.Add(new Photo(Bin, Rot, Atts[i]));
				}

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.Photos.Clear();
					Buffer.ForEach(this.Photos.Add);

					this.OnPropertyChanged(nameof(this.ProfilePhoto));
					this.OnPropertyChanged(nameof(this.HasProfilePhoto));
					this.OnPropertyChanged(nameof(this.HasPhotos));

				});

				ctx.Progress.Report(100);
			});
		}

		protected override Task OnDisappearing()
		{
			try
			{
				this.timer?.Stop();
			}
			catch
			{
				//Ignore, timer might already been stopped (not sure if it throws when already stopped)
			}
			return base.OnDisappearing();
		}

		private void OnTimerTick(object? sender, EventArgs e)
		{
			MainThread.BeginInvokeOnMainThread(async () =>
			{
				if (this.TimerSeconds > 0)
				{
					this.TimerSeconds--;
				}
				else
				{
					try
					{
						this.timer?.Stop();
						await this.GoBack();
					}
					catch (Exception Ex)
					{
						ServiceRef.LogService.LogException(Ex);
					}
				}
			});
		}

		private void OnQrTimerTick(object? sender, EventArgs e)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{

				try
				{
					if (this.identity is null)
						return;
					this.GenerateQrCode(Constants.UriSchemes.CreateIdUri(this.identity.Id));
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}
			});
		}

		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task ImageTappedAsync(Attachment ClickedAttachment)
		{
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.timer?.Stop();
			});

			try
			{
				ImagesPopup ImagesPopup = new();
				ImagesViewModel ImagesViewModel = new([ClickedAttachment]);
				await ServiceRef.UiService.PushAsync(ImagesPopup, ImagesViewModel);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}

			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.timer?.Start();
			});
		}

		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task QrTappedAsync()
		{
			if (this.QrCodeBin is null || string.IsNullOrEmpty(this.QrCodeUri))
				return;

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.timer?.Stop();
			});

			try
			{
				await Clipboard.SetTextAsync(this.QrCodeUri);
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
					ServiceRef.Localizer[nameof(AppResources.IdCopiedSuccessfully)]);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}

			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.timer?.Start();
			});
		}

		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task FieldTappedAsync(string Value)
		{
			if (this.QrCodeBin is null || string.IsNullOrEmpty(this.QrCodeUri))
				return;

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.timer?.Stop();
			});

			try
			{
				await Clipboard.SetTextAsync(Value);
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
					ServiceRef.Localizer[nameof(AppResources.TagValueCopiedToClipboard)]);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}

			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.timer?.Start();
			});
		}

		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task ShareAsync()
		{
			if (this.identity is null && this.LoadIdentityTask.IsSucceeded)
				return;

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.timer?.Stop();
			});

			try
			{
				await this.OpenQrPopup();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}

			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.timer?.Start();
			});

		}

		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task RemoveContact()
		{
			if (this.identity is null)
				return;
			try
			{
				if (!await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer["Confirm"], ServiceRef.Localizer["AreYouSureYouWantToRemoveContact"], ServiceRef.Localizer["Yes"], ServiceRef.Localizer["Cancel"]))
					return;

				string BareJid = this.identity.GetJid();

				ContactInfo Info = await ContactInfo.FindByBareJid(BareJid);
				if (Info is not null)
				{
					await Database.Delete(Info);
					await ServiceRef.AttachmentCacheService.MakeTemporary(Info.LegalId);
					await Database.Provider.Flush();
				}

				RosterItem? Item = ServiceRef.XmppService.GetRosterItem(BareJid);
				if (Item is not null)
					ServiceRef.XmppService.RemoveRosterItem(BareJid);

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.CanAddContact = true;
					this.CanRemoveContact = false;
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task AddContact()
		{
			if (this.identity is null)
				return;

			try
			{

				string FriendlyName = ContactInfo.GetFriendlyName(this.identity);
				string BareJid = this.identity.GetJid();

				RosterItem? Item = ServiceRef.XmppService.GetRosterItem(BareJid);
				if (Item is null)
					ServiceRef.XmppService.AddRosterItem(new RosterItem(BareJid, FriendlyName));

				ContactInfo Info = await ContactInfo.FindByBareJid(BareJid);
				if (Info is null)
				{
					Info = new ContactInfo()
					{
						BareJid = BareJid,
						LegalId = this.identity.Id,
						LegalIdentity = this.identity,
						FriendlyName = FriendlyName,
						IsThing = false
					};

					await Database.Insert(Info);
				}
				else
				{
					Info.LegalId = this.identity.Id;
					Info.LegalIdentity = this.identity;
					Info.FriendlyName = FriendlyName;

					await Database.Update(Info);
				}
				await ServiceRef.AttachmentCacheService.MakePermanent(this.identity.Id!);
				await Database.Provider.Flush();

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.CanAddContact = false;
					this.CanRemoveContact = true;
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task OpenChat()
		{
			if (this.identity is null)
				return;
			try
			{
				string? Jid = this.identity.GetJid();
				PersonalInformation? PersonalInfo = this.identity.GetPersonalInfo();

				if (string.IsNullOrEmpty(Jid))
					return;

				ChatNavigationArgs ChatArgs = new(this.identity.Id, Jid, PersonalInfo.FullName);
				await ServiceRef.UiService.GoToAsync(nameof(ChatPage), ChatArgs, BackMethod.Inherited, Jid);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		public bool HasLegalIdentity => ServiceRef.TagProfile.LegalIdentity.HasApprovedPersonalInformation();

		public bool ShowBackground => this.HasLegalIdentity && this.IsApproved;

		public string CurrentState => this.HasLegalIdentity ? States.HasID : States.NoID;

		static class States
		{
			public const string HasID = "HasID";
			public const string NoID = "NoID";
		}

		[RelayCommand(AllowConcurrentExecutions = false)]
		public async Task GoToApplyIdentity()
		{
			try
			{
				await ServiceRef.UiService.GoToAsync(nameof(ApplyIdPage));
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		#region ILinkableView

		/// <summary>
		/// Title of the current view
		/// </summary>
		///
		public override Task<string> Title => Task.FromResult("Test");//Task.FromResult<string>(ContactInfo.GetFriendlyName(this.LegalIdentity!));

		#endregion

		// Simple holder for custom field metadata
		private record CustomFieldDefinition(string[] Keys,
											 string NewKey,
											 Func<LegalIdentity, LocalizedString> GetLabel,
											 Func<LegalIdentity, string?> GetValue);
	}


}
