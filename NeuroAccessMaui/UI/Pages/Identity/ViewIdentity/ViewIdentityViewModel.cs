using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui;
using NeuroAccessMaui.Services.UI.Photos;
using Waher.Networking.XMPP.Contracts;
using NeuroAccessMaui.UI.Pages.Identity.ObjectModel;
using NeuroAccessMaui.UI.MVVM;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using Microsoft.Extensions.Localization;
using System.Globalization;
using NeuroAccessMaui.Resources.Languages;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.UI.Popups.Image;
using Waher.Content.Markdown.Model;

namespace NeuroAccessMaui.UI.Pages.Identity.ViewIdentity
{


	public partial class ViewIdentityViewModel : QrXmppViewModel
	{
		private readonly PhotosLoader photosLoader;
		private readonly ViewIdentityNavigationArgs? args;
		private readonly IDispatcherTimer? timer;

		private bool hasAppeared;

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
		private IdentityState? identityState = null;

		public bool IsApproved => this.IdentityState is not null && this.IdentityState == Waher.Networking.XMPP.Contracts.IdentityState.Approved;


		[ObservableProperty]
		private DateTime expireDate;
		[ObservableProperty]
		private DateTime issueDate;

		[ObservableProperty]
		private int timerSeconds = 50;

		public ObservableCollection<ObservableFieldItem> PersonalFields { get; } = [];
		public ObservableCollection<ObservableFieldItem> OrganizationFields { get; } = [];
		public ObservableCollection<ObservableFieldItem> TechnicalFields { get; } = [];
		public ObservableCollection<ObservableFieldItem> OtherFields { get; } = [];

		public ObservableTask<bool> LoadIdentityTask { get; }
		public ObservableTask<int> LoadPhotosTask { get; }
		public ObservableCollection<Photo> Photos { get; } = [];

		public ImageSource? ProfilePhoto => this.Photos.Count > 0 ? this.Photos[0].Source : null;

		public bool HasProfilePhoto => this.ProfilePhoto is not null;

		public bool HasPhotos => this.Photos.Count > 0;

		public bool HasTimer => this.timer?.IsRunning ?? false;






		// Define custom field descriptors for any multi-part or special properties for example BDAY BMONTH BYEAR -> B
		private static readonly List<CustomFieldDefinition> customFields =
		[
			new CustomFieldDefinition
			(
				Keys: [Constants.XmppProperties.BirthDay, Constants.XmppProperties.BirthMonth, Constants.XmppProperties.BirthYear],
				NewKey: Constants.CustomXmppProperties.BirthDay,
				GetLabel: () => ServiceRef.Localizer[AppResources.BirthDate, false],
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
				NewKey: nameof(LegalIdentity.Id),
				GetLabel: () => ServiceRef.Localizer[nameof(AppResources.LegalId)],
				GetValue: identity => identity.Id),

			new CustomFieldDefinition(
				Keys: Array.Empty<string>(),
				NewKey: nameof(LegalIdentity.Provider),
				GetLabel: () => ServiceRef.Localizer[nameof(AppResources.Provider)],
				GetValue: identity => identity.Provider),

			new CustomFieldDefinition(
				Keys: Array.Empty<string>(),
				NewKey: nameof(LegalIdentity.State),
				GetLabel: () => ServiceRef.Localizer[nameof(AppResources.State)],
				GetValue: identity => identity.State.ToString()),

			new CustomFieldDefinition(
				Keys: Array.Empty<string>(),
				NewKey: nameof(LegalIdentity.Created),
				GetLabel: () => ServiceRef.Localizer[nameof(AppResources.Created)],
				GetValue: identity => identity.Created.ToString("g", CultureInfo.CurrentCulture)),

			new CustomFieldDefinition(
				Keys: Array.Empty<string>(),
				NewKey: nameof(LegalIdentity.Updated),
				GetLabel: () => ServiceRef.Localizer[nameof(AppResources.Updated)],
				GetValue: identity => identity.Updated.ToString("g", CultureInfo.CurrentCulture)),

			new CustomFieldDefinition(
				Keys: Array.Empty<string>(),
				NewKey: nameof(LegalIdentity.From),
				GetLabel: () => ServiceRef.Localizer[nameof(AppResources.From)],
				GetValue: identity => identity.From.ToString("d", CultureInfo.CurrentCulture)),

			new CustomFieldDefinition(
				Keys: Array.Empty<string>(),
				NewKey: nameof(LegalIdentity.To),
				GetLabel: () => ServiceRef.Localizer[nameof(AppResources.To)],
				GetValue: identity => identity.To.ToString("d", CultureInfo.CurrentCulture)),

			new CustomFieldDefinition(
				Keys: Array.Empty<string>(),
				NewKey: nameof(LegalIdentity.ClientKeyName),
				GetLabel: () => ServiceRef.Localizer[nameof(AppResources.Encrypted)],
				GetValue: identity => identity.ClientKeyName)
		];


		public ViewIdentityViewModel(ViewIdentityNavigationArgs? args)
			: base()
		{
			this.args = args;
			this.photosLoader = new PhotosLoader();

			this.LoadIdentityTask = new ObservableTask<bool>();
			this.LoadPhotosTask = new ObservableTask<int>();

			this.timer = Application.Current?.Dispatcher.CreateTimer();
			if (this.timer is null)
				return;
			this.timer.Interval = TimeSpan.FromSeconds(1);
			this.timer.Tick += this.OnTimerTick;
		}



		protected override async Task OnAppearing()
		{
			await base.OnAppearing();

			bool IsRefresh = this.hasAppeared;
			this.hasAppeared = true;

			// Determine identity source
			LegalIdentity Identity = this.args?.Identity ?? ServiceRef.TagProfile.LegalIdentity!;

			if (IsRefresh)
			{
				// TODO: Refresh identity from server
				// identity = await ServiceRef.XmppService.GetLegalIdentity(identity.Id);
				ServiceRef.LogService.LogWarning("Refreshing identity...");
			}

			this.IdentityState = Identity.State;

			// Friendly name
			PersonalInformation PInfo = Identity.GetPersonalInformation();
			string[] Jid = PInfo.Jid.Split('@');
			Jid[1] = "@" + Jid[1];

			if (string.IsNullOrEmpty(PInfo.FullName))
			{
				this.FriendlyName = Jid[0];
				this.SubText = Jid[1];
			}
			else
			{
				this.FriendlyName = PInfo.FullName;
				if (PInfo.HasBirthDate)
					this.SubText = PInfo.BirthDate!.Value.ToShortDateString();
				else
					this.SubText = Jid[1];
			}

			if (PInfo.Age > 0)
				this.AgeText = PInfo.Age.ToString(CultureInfo.InvariantCulture);


			this.IssueDate = Identity.From;
			this.ExpireDate = Identity.To;

			this.GenerateQrCode(Constants.UriSchemes.CreateIdUri(Identity.Id));


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
					Constants.CustomXmppProperties.BirthDay,
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
					Constants.XmppProperties.PersonalNumber
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

				// Label map for display names
				Dictionary<string, LocalizedString> LabelMap = new(StringComparer.OrdinalIgnoreCase)
				{
				   {Constants.XmppProperties.FirstName,   ServiceRef.Localizer[nameof(AppResources.FirstName)]},
				   {Constants.XmppProperties.MiddleNames, ServiceRef.Localizer[nameof(AppResources.MiddleNames)]},
				   {Constants.XmppProperties.LastNames,   ServiceRef.Localizer[nameof(AppResources.LastNames)]},
				   {Constants.CustomXmppProperties.BirthDay, ServiceRef.Localizer[nameof(AppResources.BirthDate)]},
				   {Constants.XmppProperties.Address,     ServiceRef.Localizer[nameof(AppResources.Address)]},
				   {Constants.XmppProperties.Address2,    ServiceRef.Localizer[nameof(AppResources.Address2)]},
				   {Constants.XmppProperties.ZipCode,     ServiceRef.Localizer[nameof(AppResources.ZipCode)]},
				   {Constants.XmppProperties.Area,        ServiceRef.Localizer[nameof(AppResources.Area)]},
				   {Constants.XmppProperties.City,        ServiceRef.Localizer[nameof(AppResources.City)]},
				   {Constants.XmppProperties.Region,      ServiceRef.Localizer[nameof(AppResources.Region)]},
				   {Constants.XmppProperties.Country,     ServiceRef.Localizer[nameof(AppResources.Country)]},
				   {Constants.XmppProperties.PersonalNumber, ServiceRef.Localizer[nameof(AppResources.PersonalNumber)]},
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
				   {Constants.XmppProperties.OrgNumber,   ServiceRef.Localizer[nameof(AppResources.OrgNumber)]}
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
								Label = CustomDef.GetLabel();
								foreach (string Keys in CustomDef.Keys)
									UsedKeys.Add(Keys);
							}
							continue;
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
						ObservableFieldItem Item = new(CustomFieldDefinition.NewKey, CustomFieldDefinition.GetLabel(), Identity, false, ValueOverride);
						TechnicalList.Add(Item);
					});

				// Apply to UI on main thread
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.PersonalFields.Clear(); PersonalList.ForEach(this.PersonalFields.Add);
					this.OrganizationFields.Clear(); OrganizationList.ForEach(this.OrganizationFields.Add);
					this.TechnicalFields.Clear(); TechnicalList.ForEach(this.TechnicalFields.Add);
					this.OtherFields.Clear(); OtherList.ForEach(this.OtherFields.Add);

					this.timer?.Start();
					this.OnPropertyChanged(nameof(this.HasTimer));
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
					for (int i = 0; i < 25; ++i)
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
			catch ( Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}

			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.timer?.Start();
			});
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
											 Func<LocalizedString> GetLabel,
											 Func<LegalIdentity, string?> GetValue);
	}


}
