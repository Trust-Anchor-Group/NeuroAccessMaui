using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Data;
using NeuroAccessMaui.Services.Identity;
using NeuroAccessMaui.Services.Kyc;
using NeuroAccessMaui.Services.Kyc.Models;
using NeuroAccessMaui.Services.Kyc.ViewModels;
using NeuroAccessMaui.Services.Kyc.Domain;
using NeuroAccessMaui.Services.UI.Photos;
using NeuroAccessMaui.UI.MVVM;
using NeuroAccessMaui.UI.MVVM.Building;
using NeuroAccessMaui.UI.MVVM.Policies;
using SkiaSharp;
using Waher.Content.Html.Elements;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Contracts.EventArguments;
using IServiceProvider = Waher.Networking.XMPP.Contracts.IServiceProvider;
using NeuroAccessMaui.UI.Pages.Wallet.ServiceProviders;
using NeuroAccessMaui.UI.Pages.Identity.ViewIdentity;
using NeuroAccessMaui.UI.MVVM.Reentrancy;
using NeuroAccessMaui.UI.Pages.Applications.ApplyId;
using NeuroAccessMaui.UI; // For IKeyboardInsetAware
using NeuroAccessMaui.Services.Authentication;

namespace NeuroAccessMaui.UI.Pages.Kyc
{
	/// <summary>
	/// Orchestrates the interactive KYC flow, covering navigation, validation, summary projection, persistence scheduling, peer review, and submission.
	/// </summary>
	public partial class KycProcessViewModel : BaseViewModel, IDisposable, IKeyboardInsetAware
	{
		private readonly IKycService kycService = ServiceRef.KycService;
		private bool disposedValue; // Disposal flag
		private readonly KycProcessNavigationArgs? navigationArguments;
		private KycProcess? process;
		private KycReference? kycReference;
		private int currentPageIndex =0;
		private KycNavigationSnapshot navigation = new KycNavigationSnapshot(0, -1, KycFlowState.Form);
		private bool applicationSent;
		private string? applicationId;
		private readonly ReentrancyGuard navigationGuard = new();
		private readonly ReentrancyGuard applyGuard = new();

		private List<Property> mappedValues;
		private List<LegalIdentityAttachment> attachments;
		private ServiceProviderWithLegalId[]? peerReviewServices = null;

		private bool editingFromSummary;
		[ObservableProperty] private bool isLoading;

		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ScanQrCodeCommand))]
		[NotifyCanExecuteChangedFor(nameof(RequestReviewCommand))]
		[NotifyCanExecuteChangedFor(nameof(RevokeApplicationCommand))]
		private bool applicationSentPublic;

		[ObservableProperty] private bool peerReview;
		[ObservableProperty] private int nrReviews;
		[ObservableProperty] private int nrReviewers;
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(RequestReviewCommand))]
		private bool hasFeaturedPeerReviewers;

		[ObservableProperty] private int currentPagePosition;
		[ObservableProperty] private KycPage? currentPage;
		[ObservableProperty] private string? currentPageTitle;
		[ObservableProperty] private string? currentPageDescription;
		[ObservableProperty] private bool hasCurrentPageDescription;
		[ObservableProperty] private ReadOnlyObservableCollection<KycSection>? currentPageSections;
		[ObservableProperty] private bool hasSections;
		[ObservableProperty] private string nextButtonText = "Next";

		[ObservableProperty] private ObservableCollection<DisplayQuad> personalInformationSummary;
		[ObservableProperty] private ObservableCollection<DisplayQuad> addressInformationSummary;
		[ObservableProperty] private ObservableCollection<DisplayQuad> attachmentInformationSummary;
		[ObservableProperty] private ObservableCollection<DisplayQuad> companyInformationSummary;
		[ObservableProperty] private ObservableCollection<DisplayQuad> companyAddressSummary;
		[ObservableProperty] private ObservableCollection<DisplayQuad> companyRepresentativeSummary;
		[ObservableProperty] private ObservableCollection<string> invalidatedItems = new ObservableCollection<string>();
		[ObservableProperty] private ObservableCollection<string> unvalidatedItems = new ObservableCollection<string>();

		[ObservableProperty] private string? errorDescription;
		[ObservableProperty] private bool hasErrorDescription;
		[ObservableProperty] private string unvalidatedSummaryText = string.Empty;

		[ObservableProperty] private bool hasPersonalInformation;
		[ObservableProperty] private bool hasAddressInformation;
		[ObservableProperty] private bool hasAttachments;
		[ObservableProperty] private bool hasCompanyInformation;
		[ObservableProperty] private bool hasCompanyAddress;
		[ObservableProperty] private bool hasCompanyRepresentative;
		[ObservableProperty] private bool isNavigating; // Combined busy state for navigation/apply

		// Throttling support for page refresh (avoid per-keystroke SetCurrentPage work)
		private static readonly TimeSpan pageRefreshThrottle = TimeSpan.FromMilliseconds(250);
		private static readonly TimeSpan fieldSnapshotThrottle = TimeSpan.FromMilliseconds(500);
		private readonly ObservableTask<int> pageRefreshTask;
		private readonly ObservableTask<int> fieldSnapshotTask;

		public string BannerUriLight => ServiceRef.ThemeService.GetImageUri(Constants.Branding.BannerSmallLight);
		public string BannerUriDark => ServiceRef.ThemeService.GetImageUri(Constants.Branding.BannerSmallDark);

		public string BannerUri => (Application.Current?.RequestedTheme ?? AppTheme.Light) switch
		{
			AppTheme.Dark => this.BannerUriDark,
			AppTheme.Light => this.BannerUriLight,
			_ => this.BannerUriLight
		};

		public ObservableCollection<KycPage> Pages => this.process is not null ? this.process.Pages : [];

		public bool IsInSummary => this.navigation.State == KycFlowState.Summary || this.navigation.State == KycFlowState.PendingSummary;
		public bool IsEditingFromSummary => this.editingFromSummary;
		public bool HasUnvalidatedItems => (this.kycReference?.ApplicationReview?.UnvalidatedClaims?.Length ?? 0) > 0 ||
			(this.kycReference?.ApplicationReview?.UnvalidatedPhotos?.Length ?? 0) > 0;

		public bool HasInvalidatedItems => this.InvalidatedItems.Count > 0;
		public bool ShouldShowUnvalidatedBanner => this.HasUnvalidatedItems && this.InvalidatedItems.Count == 0 && this.kycReference?.CreatedIdentityState == IdentityState.Created;
		public bool ShouldShowRejectionBanner => ((!string.IsNullOrEmpty(this.kycReference?.ApplicationReview?.Code)) && this.kycReference?.ApplicationReview?.Code != "ManualReview") || this.kycReference?.CreatedIdentityState == IdentityState.Rejected;

		private void SetEditingFromSummary(bool value)
		{
			if (this.editingFromSummary == value) return;
			this.editingFromSummary = value;
			this.OnPropertyChanged(nameof(this.IsEditingFromSummary));
		}

		public double Progress
		{
			get
			{
				if (this.process is null || this.CurrentPage is null)
				{
					this.ProgressPercent = "0%";
					return 0;
				}

				ObservableCollection<KycPage> Visible = [.. this.Pages.Where(p => p.IsVisible(this.process.Values))];
				if (Visible.Count ==0)
				{
					this.ProgressPercent = "0%";
					return 0;
				}
				if (this.IsInSummary)
				{
					this.ProgressPercent = "100%";
					return 1;
				}
				int index = Visible.IndexOf(this.CurrentPage);
				if (index <0)
				{
					// Current page disappeared (visibility rule change) -> treat as start until navigation picks another page.
					this.ProgressPercent = "0%";
					return 0;
				}
				double progress = Math.Clamp((double)index / Visible.Count,0,1);
				this.ProgressPercent = $"{(progress *100):0}%";
				return progress;
			}
		}

		[ObservableProperty] private string progressPercent = "0%";

		/// <summary>
		/// Gets a value indicating whether any on-screen keyboard is currently visible.
		/// </summary>
		public bool IsKeyboardVisible => this.isKeyboardVisible;

		/// <summary>
		/// Backing field for keyboard visibility state.
		/// </summary>
		private bool isKeyboardVisible;

		/// <summary>
		/// Gets a value indicating whether the bottom navigation bar should be shown.
		/// Hidden when the application has been sent or when the keyboard is visible.
		/// </summary>
		public bool ShowBottomBar => !this.ApplicationSentPublic && !this.IsKeyboardVisible;

		/// <summary>
		/// Receives keyboard inset change notifications from the shell.
		/// </summary>
		/// <param name="args">Keyboard inset change arguments.</param>
		public void OnKeyboardInsetChanged(Services.UI.KeyboardInsetChangedEventArgs args)
		{
			bool Visible = args?.IsVisible ?? false;
			if (this.isKeyboardVisible == Visible)
				return;
			this.isKeyboardVisible = Visible;
			this.OnPropertyChanged(nameof(this.IsKeyboardVisible));
			this.OnPropertyChanged(nameof(this.ShowBottomBar));
		}


		public IAsyncRelayCommand NextCommand { get; }
		public IRelayCommand PreviousCommand { get; }

		public KycProcessViewModel()
		{
			this.navigationArguments = ServiceRef.NavigationService.PopLatestArgs<KycProcessNavigationArgs>();
			this.NextCommand = new AsyncRelayCommand(this.ExecuteNextAsync, this.CanExecuteNext);
			this.PreviousCommand = new AsyncRelayCommand(this.ExecutePrevious);
			this.PersonalInformationSummary = new ObservableCollection<DisplayQuad>();
			this.AddressInformationSummary = new ObservableCollection<DisplayQuad>();
			this.AttachmentInformationSummary = new ObservableCollection<DisplayQuad>();
			this.CompanyInformationSummary = new ObservableCollection<DisplayQuad>();
			this.CompanyAddressSummary = new ObservableCollection<DisplayQuad>();
			this.CompanyRepresentativeSummary = new ObservableCollection<DisplayQuad>();
			this.mappedValues = new List<Property>();
			this.attachments = new List<LegalIdentityAttachment>();
			this.ApplicationSentPublic = ServiceRef.TagProfile.IdentityApplication is not null;
			this.NrReviews = ServiceRef.TagProfile.NrReviews;
			this.pageRefreshTask = new ObservableTaskBuilder()
				.Named("KYC Page Refresh")
				.AutoStart(false)
				.UseTaskRun(false)
				.WithPolicy(Policies.Debounce(pageRefreshThrottle))
				.Run(async context =>
				{
					CancellationToken CancellationToken = context.CancellationToken;
					if (CancellationToken.IsCancellationRequested || this.disposedValue)
					{
						return;
					}

					try
					{
						await MainThread.InvokeOnMainThreadAsync(() =>
						{
							if (CancellationToken.IsCancellationRequested || this.disposedValue)
							{
								return;
							}
							this.SetCurrentPage(this.currentPageIndex);
							this.NextCommand.NotifyCanExecuteChanged();
						});
					}
					catch (TaskCanceledException)
					{
					}
					catch (Exception Ex)
					{
						ServiceRef.LogService.LogException(Ex);
					}
				})
				.Build();
			this.fieldSnapshotTask = new ObservableTaskBuilder()
				.Named("KYC Field Snapshot")
				.AutoStart(false)
				.UseTaskRun(true)
				.WithPolicy(Policies.Debounce(fieldSnapshotThrottle))
				.Run(async context =>
				{
					if (context.CancellationToken.IsCancellationRequested || this.disposedValue)
					{
						return;
					}

					if (this.kycReference is null || this.process is null)
					{
						return;
					}

					KycReference Reference = this.kycReference;
					KycProcess Process = this.process;
					string? CurrentPageId = this.CurrentPage?.Id;
					KycNavigationSnapshot NavigationSnapshot = this.navigation;
					double ProgressValue = this.Progress;

					await this.kycService.ScheduleSnapshotAsync(Reference, Process, NavigationSnapshot, ProgressValue, CurrentPageId).ConfigureAwait(false);
				})
				.Build();
		}

		public override async Task OnInitializeAsync()
		{
			this.IsLoading = true;
			await base.OnInitializeAsync();

			string Lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
			this.kycReference = this.navigationArguments?.Reference;
			if (this.kycReference is null)
			{
				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], "Missing KYC reference.", ServiceRef.Localizer[nameof(AppResources.Ok)]);
				await this.GoBack();
				return;
			}

			this.process = await this.kycReference.ToProcess(Lang);
			this.applicationId = this.kycReference.CreatedIdentityId;
			this.OnPropertyChanged(nameof(this.Pages));
			if (this.process is null)
			{
				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], "Unable to load KYC process.", ServiceRef.Localizer[nameof(AppResources.Ok)]);
				await this.GoBack();
				return;
			}
			this.process.Initialize();

			bool Pending = this.kycReference.CreatedIdentityState == IdentityState.Created && !string.IsNullOrEmpty(this.kycReference.CreatedIdentityId);
			bool Rejected = this.kycReference.CreatedIdentityState == IdentityState.Rejected && !string.IsNullOrEmpty(this.kycReference.CreatedIdentityId);
			this.ApplicationSentPublic = Pending;
			if (!Pending && !Rejected)
				this.PrefillFieldsFromProfile();

			ServiceRef.XmppService.IdentityApplicationChanged += this.XmppService_IdentityApplicationChanged;
			if (ServiceRef.TagProfile.IdentityApplication is not null)
			{
				await this.LoadApplicationAttributes();
				await this.LoadFeaturedPeerReviewers();
			}

			ApplicationReview? ExistingReview = this.kycReference.ApplicationReview;
			if (ExistingReview is not null)
			{
				this.ErrorDescription = ExistingReview.Message;
				this.HasErrorDescription = !string.IsNullOrWhiteSpace(this.ErrorDescription);
				KycInvalidation.ApplyInvalidations(this.process, ExistingReview, this.ErrorDescription);
				this.UpdateReviewIndicators();
			}

			this.process.ClearValidation();
			foreach (KycPage Page in this.process.Pages)
			{
				Page.PropertyChanged += this.Page_PropertyChanged;
				foreach (ObservableKycField Field in Page.AllFields)
					Field.PropertyChanged += this.Field_PropertyChanged;
				foreach (KycSection Section in Page.AllSections)
				{
					Section.PropertyChanged += this.Section_PropertyChanged;
					foreach (ObservableKycField Field in Section.AllFields)
						Field.PropertyChanged += this.Field_PropertyChanged;
				}
			}
			foreach (KycPage Page in this.process.Pages)
				Page.UpdateVisibilities(this.process.Values);

			int ResumeIndex = -1;
			if (!string.IsNullOrEmpty(this.kycReference.LastVisitedPageId))
			{
				for (int I =0; I < this.Pages.Count; I++)
				{
					KycPage PageItem = this.Pages[I];
					if (PageItem.Id == this.kycReference.LastVisitedPageId && PageItem.IsVisible(this.process.Values))
					{
						ResumeIndex = I;
						break;
					}
				}
			}

			bool LastWasSummary = string.Equals(this.kycReference.LastVisitedMode, "Summary", StringComparison.OrdinalIgnoreCase);

			if (LastWasSummary)
			{
				int FirstInvalid = await this.GetFirstInvalidVisiblePageIndexAsync();
				if (FirstInvalid >=0)
				{
					// There are invalid fields -> go to first invalid page but mark editing from summary
					this.SetEditingFromSummary(true);
					this.currentPageIndex = FirstInvalid;
					this.CurrentPagePosition = FirstInvalid;
					this.SetCurrentPage(FirstInvalid);
					this.NextButtonText = ServiceRef.Localizer["Kyc_Next"].Value;
				}
				else
				{
					await this.BuildMappedValuesAsync();
					// Keep a valid page index for navigation (use last visited if available else first visible)
					this.currentPageIndex = ResumeIndex >=0 ? ResumeIndex : this.GetFirstVisibleIndex();
					if (this.currentPageIndex >=0)
					{
						this.CurrentPagePosition = this.currentPageIndex;
						this.SetCurrentPage(this.currentPageIndex);
					}
					int AnchorIndex = this.currentPageIndex >=0 ? this.currentPageIndex : this.navigation.AnchorPageIndex;
					this.navigation = this.navigation with { State = KycFlowState.Summary, AnchorPageIndex = AnchorIndex, CurrentPageIndex = AnchorIndex >=0 ? AnchorIndex : this.navigation.CurrentPageIndex };
					this.SetEditingFromSummary(false);
					this.NotifyNavigationChanged();
					this.NextButtonText = ServiceRef.Localizer["Kyc_Apply"].Value;
				}
			}
			else if (Rejected)
			{
				await this.BuildMappedValuesAsync();
				// For rejected applications, allow re-application: show summary but keep a valid page index
				int FirstInvalid = await this.GetFirstInvalidVisiblePageIndexAsync();
				this.currentPageIndex = FirstInvalid >=0 ? FirstInvalid : this.GetFirstVisibleIndex();
				if (this.currentPageIndex >=0)
				{
					this.CurrentPagePosition = this.currentPageIndex;
					this.SetCurrentPage(this.currentPageIndex);
				}
				int AnchorIndexRejected = this.currentPageIndex >=0 ? this.currentPageIndex : this.navigation.AnchorPageIndex;
				this.navigation = this.navigation with { State = KycFlowState.Summary, AnchorPageIndex = AnchorIndexRejected, CurrentPageIndex = AnchorIndexRejected >=0 ? AnchorIndexRejected : this.navigation.CurrentPageIndex };
				this.SetEditingFromSummary(false);
				this.NotifyNavigationChanged();
				this.NextButtonText = ServiceRef.Localizer["Kyc_Apply"].Value;
			}
			else
			{
				// Normal form resume
				this.currentPageIndex = ResumeIndex >=0 ? ResumeIndex : this.GetFirstVisibleIndex();
				// Synchronize navigation snapshot so Back() logic and snapshot persistence have correct page index.
				if (this.currentPageIndex >=0)
					this.navigation = this.navigation with { CurrentPageIndex = this.currentPageIndex };
				this.CurrentPagePosition = this.currentPageIndex; // Property change no longer triggers page set.
				this.SetCurrentPage(this.currentPageIndex);
			}

			this.NextButtonText = this.IsInSummary ? ServiceRef.Localizer["Kyc_Apply"].Value : ServiceRef.Localizer["Kyc_Next"].Value;
			MainThread.BeginInvokeOnMainThread(this.NextCommand.NotifyCanExecuteChanged);
			if (Pending)
			{
				await this.BuildMappedValuesAsync();
				int AnchorIndexPending = this.currentPageIndex >=0 ? this.currentPageIndex : this.navigation.AnchorPageIndex;
				this.navigation = this.navigation with { State = KycFlowState.Summary, AnchorPageIndex = AnchorIndexPending, CurrentPageIndex = AnchorIndexPending >=0 ? AnchorIndexPending : this.navigation.CurrentPageIndex };
				this.SetEditingFromSummary(false);
				this.NotifyNavigationChanged();
				this.NextButtonText = ServiceRef.Localizer["Kyc_Apply"].Value;
			}
			this.IsLoading = false;
		}

		private int GetFirstVisibleIndex()
		{
			if (this.process is null) return -1;
			for (int I =0; I < this.Pages.Count; I++)
			{
				if (this.Pages[I].IsVisible(this.process.Values)) return I;
			}
			return -1;
		}

		[RelayCommand]
		private async Task GoToPageWithMapping(string? mapping)
		{
			if (string.IsNullOrWhiteSpace(mapping)) return;
			if (this.process is null) return;
			if (this.ApplicationSentPublic) return; // Cannot edit after application sent

			int TargetIndex = this.FindPageIndexByMapping(mapping);
			if (TargetIndex <0) return;

			// Transition from summary to edit mode
			this.navigation = this.navigation with { State = KycFlowState.Form };
			this.SetEditingFromSummary(true); // Allow quick return to summary
			this.currentPageIndex = TargetIndex;
			this.CurrentPagePosition = TargetIndex;
			this.SetCurrentPage(TargetIndex);
			this.NotifyNavigationChanged();
			this.NextButtonText = ServiceRef.Localizer[nameof(AppResources.Kyc_Return), false];
			await MainThread.InvokeOnMainThreadAsync(this.ScrollUp);
		}

		private int FindPageIndexByMapping(string Mapping)
		{
			if (this.process is null) return -1;
			string MappingKey = Mapping.Trim();
			for (int I =0; I < this.Pages.Count; I++)
			{
				KycPage Page = this.Pages[I];
				if (!Page.IsVisible(this.process.Values)) continue;
				foreach (ObservableKycField Field in Page.AllFields)
				{
					if (FieldMatches(Field, MappingKey)) return I;
				}
				foreach (KycSection Section in Page.AllSections)
				{
					foreach (ObservableKycField Field in Section.AllFields)
					{
						if (FieldMatches(Field, MappingKey)) return I;
					}
				}
			}
			return -1;
			static bool FieldMatches(ObservableKycField field, string mappingKey)
			{
				foreach (KycMapping Map in field.Mappings)
				{
					if (string.Equals(Map.Key, mappingKey, StringComparison.OrdinalIgnoreCase)) return true;
					if (mappingKey.Equals("BDATE", StringComparison.OrdinalIgnoreCase) &&
						(string.Equals(Map.Key, Constants.XmppProperties.BirthDay, StringComparison.OrdinalIgnoreCase) ||
						 string.Equals(Map.Key, Constants.XmppProperties.BirthMonth, StringComparison.OrdinalIgnoreCase) ||
						 string.Equals(Map.Key, Constants.XmppProperties.BirthYear, StringComparison.OrdinalIgnoreCase))) return true;
					if (mappingKey.StartsWith("ORGREP", StringComparison.OrdinalIgnoreCase) && Map.Key.StartsWith("ORGREP", StringComparison.OrdinalIgnoreCase)) return true;
				}
				return false;
			}
		}

		public bool CanRequestFeaturedPeerReviewer => this.ApplicationSentPublic && this.HasFeaturedPeerReviewers;
		public bool FeaturedPeerReviewers => this.CanRequestFeaturedPeerReviewer && this.PeerReview;

		partial void OnApplicationSentPublicChanged(bool value)
		{
			if (value)
			{
				int anchor = this.currentPageIndex >=0 ? this.currentPageIndex : this.navigation.AnchorPageIndex;
				this.navigation = this.navigation with { State = KycFlowState.Summary, AnchorPageIndex = anchor, CurrentPageIndex = anchor >=0 ? anchor : this.navigation.CurrentPageIndex };
				this.SetEditingFromSummary(false);
				this.NotifyNavigationChanged();
			}
			this.OnPropertyChanged(nameof(this.ShowBottomBar));
			this.OnPropertyChanged(nameof(this.CanRequestFeaturedPeerReviewer));
			this.OnPropertyChanged(nameof(this.FeaturedPeerReviewers));
			this.NextCommand.NotifyCanExecuteChanged();
		}

		partial void OnPeerReviewChanged(bool value)
		{
			this.OnPropertyChanged(nameof(this.FeaturedPeerReviewers));
		}

		partial void OnHasFeaturedPeerReviewersChanged(bool value)
		{
			this.OnPropertyChanged(nameof(this.CanRequestFeaturedPeerReviewer));
			this.OnPropertyChanged(nameof(this.FeaturedPeerReviewers));
		}

		private void PrefillFieldsFromProfile()
		{
			if (this.process is null) return;
			LegalIdentity? Identity = ServiceRef.TagProfile.LegalIdentity;
			if (Identity?.Properties is null) return;
			Dictionary<string, string> ByName = Identity.Properties
				.Where(p => p is not null && !string.IsNullOrWhiteSpace(p.Name))
				.GroupBy(p => p.Name!, StringComparer.OrdinalIgnoreCase)
				.Select(g => g.OrderByDescending(p => p?.Value?.Length ??0).First())
				.ToDictionary(p => p.Name!, p => p.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);
			IEnumerable<ObservableKycField> AllFields = this.process.Pages.SelectMany(p => p.AllFields)
				.Concat(this.process.Pages.SelectMany(p => p.AllSections).SelectMany(s => s.AllFields));
			foreach (ObservableKycField Field in AllFields)
			{
				if (!string.IsNullOrWhiteSpace(Field.StringValue)) continue;
				if (Field.Mappings.Count ==0) continue;
				foreach (KycMapping Map in Field.Mappings)
				{
					if (string.IsNullOrWhiteSpace(Map.Key)) continue;
					if (ByName.TryGetValue(Map.Key, out string? Value) && !string.IsNullOrWhiteSpace(Value))
					{
						Field.StringValue = Value;
						this.process.Values[Field.Id] = Field.StringValue;
						break;
					}
				}
			}
		}

		partial void OnCurrentPagePositionChanged(int value)
		{
			// Removed recursive SetCurrentPage call to avoid double work.
			if (value >=0 && value < this.Pages.Count)
			{
				this.currentPageIndex = value;
			}
		}

		private void Field_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (this.disposedValue) return;
			if (e.PropertyName == nameof(ObservableKycField.RawValue))
			{
				this.SchedulePageRefresh();
				this.ScheduleFieldSnapshot();
			}
			if (e.PropertyName == nameof(ObservableKycField.IsValid))
			{
				MainThread.BeginInvokeOnMainThread(this.NextCommand.NotifyCanExecuteChanged);
			}
		}

		private void ScheduleFieldSnapshot()
		{
			if (this.disposedValue)
				return;
			if (this.kycReference is null || this.process is null)
				return;
			this.fieldSnapshotTask.Run();
		}

		private void SchedulePageRefresh()
		{
			if (this.disposedValue) return;
			this.pageRefreshTask.Run();
		}

		private void Section_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (this.disposedValue) return;
			if (e.PropertyName == nameof(KycSection.IsVisible))
				this.SchedulePageRefresh();
		}

		private void Page_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (this.disposedValue) return;
			if (e.PropertyName == nameof(KycPage.IsVisible))
				this.SchedulePageRefresh();
		}

		private void SetCurrentPage(int index)
		{
			if (this.disposedValue) return;
			if (this.process is null) return;
			if (index <0 || index >= this.Pages.Count) return;
			// If target page is not visible (visibility changed), pick first visible page instead (unless in summary where we keep current page context).
			if (!this.IsInSummary && !this.Pages[index].IsVisible(this.process.Values))
			{
				int firstVisible = this.GetFirstVisibleIndex();
				if (firstVisible <0) return; // No visible pages -> nothing to show.
				index = firstVisible;
			}

			// Clamp stale index if needed (visibility could have changed since last schedule)
			if (!this.IsInSummary && (this.currentPageIndex >= this.Pages.Count || (this.currentPageIndex >=0 && !this.Pages[this.currentPageIndex].IsVisible(this.process.Values))))
			{
				int firstVisible = this.GetFirstVisibleIndex();
				if (firstVisible >=0) index = firstVisible;
			}

			KycPage Page = this.Pages[index];
			bool pageChanged = !object.ReferenceEquals(this.CurrentPage, Page) || this.currentPageIndex != index;

			this.currentPageIndex = index;
			if (this.CurrentPagePosition != index)
			{
				this.CurrentPagePosition = index;
			}
			this.CurrentPage = Page;
			this.CurrentPageTitle = Page.Title?.Text ?? Page.Id;
			this.CurrentPageDescription = Page.Description?.Text;
			this.HasCurrentPageDescription = !string.IsNullOrWhiteSpace(this.CurrentPageDescription);
			this.CurrentPageSections = Page.VisibleSections;
			this.HasSections = this.CurrentPageSections is not null && this.CurrentPageSections.Count >0;
			if (!this.IsInSummary && this.kycReference is not null && this.process is not null && pageChanged)
				_ = this.kycService.ScheduleSnapshotAsync(this.kycReference, this.process, this.navigation, this.Progress, Page.Id);
			this.OnPropertyChanged(nameof(this.Progress));
			this.NextCommand.NotifyCanExecuteChanged();
		}

		private bool CanExecuteNext()
		{
			if (this.IsInSummary)
				return !this.ApplicationSentPublic;
			if (this.CurrentPage is null) return false;
			IEnumerable<ObservableKycField> Fields = this.CurrentPage.VisibleFields;
			if (this.CurrentPageSections is not null)
				Fields = Fields.Concat(this.CurrentPageSections.SelectMany(s => s.VisibleFields));
			return Fields.All(f => f.IsValid);
		}

		private async Task ExecuteNextAsync()
		{
			if (!await this.navigationGuard.RunIfNotBusy(async () =>
			{
				this.IsNavigating = true;
				ServiceRef.PlatformSpecific.HideKeyboard();
				if (this.IsInSummary)
				{
					await this.ExecuteApplyAsync();
					return;
				}
				if (this.editingFromSummary)
				{
					bool OkEditing = await this.ValidateCurrentPageAsync();
					if (!OkEditing)
					{
						return;
					}
					int FirstInvalidFromSummary = await this.GetFirstInvalidVisiblePageIndexAsync();
					if (FirstInvalidFromSummary >=0)
					{
						this.currentPageIndex = FirstInvalidFromSummary;
						this.CurrentPagePosition = FirstInvalidFromSummary;
						this.SetCurrentPage(FirstInvalidFromSummary);
						this.NextButtonText = ServiceRef.Localizer["Kyc_Next"].Value;
						return;
					}
					await this.GoToSummaryAsync();
					this.SetEditingFromSummary(false);
					return;
				}
				bool Ok = await this.ValidateCurrentPageAsync();
				if (!Ok) return;
				if (this.kycReference is not null && this.process is not null)
					await this.kycService.FlushSnapshotAsync(this.kycReference, this.process, this.navigation, this.Progress, this.CurrentPage?.Id);
				if (this.process is null) return;
				List<int> VisibleIndices = new List<int>();
				for (int I =0; I < this.Pages.Count; I++)
				{
					if (this.Pages[I].IsVisible(this.process.Values)) VisibleIndices.Add(I);
				}
				KycProcessState ProcessState = this.BuildProcessState();
				KycNavigationSnapshot NextSnap = KycTransitions.Advance(ProcessState, VisibleIndices);
				if (NextSnap.State == KycFlowState.Summary || NextSnap.State == KycFlowState.PendingSummary)
				{
					await this.BuildMappedValuesAsync();
					this.ScrollUp();
					this.SetEditingFromSummary(false);
					this.navigation = NextSnap with { AnchorPageIndex = NextSnap.AnchorPageIndex >=0 ? NextSnap.AnchorPageIndex : this.navigation.AnchorPageIndex >=0 ? this.navigation.AnchorPageIndex : this.currentPageIndex };
					this.NotifyNavigationChanged();
					this.OnPropertyChanged(nameof(this.Progress));
					this.NextButtonText = ServiceRef.Localizer["Kyc_Apply"].Value;
					if (this.kycReference is not null && this.process is not null)
						await this.kycService.FlushSnapshotAsync(this.kycReference, this.process, this.navigation, this.Progress, this.CurrentPage?.Id);
				}
				else
				{
					this.navigation = NextSnap;
					this.currentPageIndex = NextSnap.CurrentPageIndex;
					this.CurrentPagePosition = this.currentPageIndex;
					this.SetCurrentPage(this.currentPageIndex);
					this.ScrollUp();
					this.NotifyNavigationChanged();
				}
			}).ConfigureAwait(false))
			{
				return; // Skipped due to ongoing navigation
			}
			this.IsNavigating = false;
		}

		public event EventHandler? ScrollToTop;
		private void ScrollUp() => this.ScrollToTop?.Invoke(this, EventArgs.Empty);

		public override Task OnDisposeAsync()
		{
			this.Dispose(true);
			return base.OnDisposeAsync();
		}

		private void UnsubscribeProcessHandlers()
		{
			if (this.process is null) return;
			foreach (KycPage Page in this.process.Pages)
			{
				Page.PropertyChanged -= this.Page_PropertyChanged;
				foreach (ObservableKycField Field in Page.AllFields)
					Field.PropertyChanged -= this.Field_PropertyChanged;
				foreach (KycSection Section in Page.AllSections)
				{
					Section.PropertyChanged -= this.Section_PropertyChanged;
					foreach (ObservableKycField Field in Section.AllFields)
						Field.PropertyChanged -= this.Field_PropertyChanged;
				}
			}
		}

		protected virtual void Dispose(bool disposing)
		{
			if (this.disposedValue)
			{
				return;
			}
			if (disposing)
			{
				try { this.UnsubscribeProcessHandlers(); } catch { }
				try { this.pageRefreshTask.Dispose(); } catch { }
				ServiceRef.XmppService.IdentityApplicationChanged -= this.XmppService_IdentityApplicationChanged;
			}
			this.disposedValue = true;
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		[RelayCommand]
		private async Task GoToSummaryAsync()
		{
			if (this.process is null) return;
			bool Ok = await this.ValidateCurrentPageAsync();
			if (!Ok) return;
			if (this.kycReference is not null && this.process is not null)
				await this.kycService.FlushSnapshotAsync(this.kycReference, this.process, this.navigation, this.Progress, this.CurrentPage?.Id);
			await this.BuildMappedValuesAsync();
			this.ScrollUp();
			KycProcessState ProcessState = this.BuildProcessState();
			this.navigation = KycTransitions.EnterSummary(ProcessState);
			int Anchor = this.navigation.AnchorPageIndex >=0 ? this.navigation.AnchorPageIndex : this.currentPageIndex;
			if (Anchor >=0)
			{
				this.currentPageIndex = Anchor;
				this.CurrentPagePosition = Anchor;
			}
			this.SetEditingFromSummary(false);
			this.NotifyNavigationChanged();
			if (this.kycReference is not null && this.process is not null)
				await this.kycService.FlushSnapshotAsync(this.kycReference, this.process, this.navigation, this.Progress, this.CurrentPage?.Id);
			this.OnPropertyChanged(nameof(this.Progress));
			this.NextButtonText = ServiceRef.Localizer["Kyc_Apply"].Value;
		}

		private async Task ExecutePrevious()
		{
			if (this.process is null)
			{
				await base.GoBack();
				return;
			}
			List<int> VisibleIndices = new List<int>();
			for (int I =0; I < this.Pages.Count; I++)
			{
				if (this.Pages[I].IsVisible(this.process.Values)) VisibleIndices.Add(I);
			}
			KycProcessState ProcessState = this.BuildProcessState();
			KycNavigationSnapshot PrevSnap = KycTransitions.Back(ProcessState, VisibleIndices);
			if (this.IsInSummary)
			{
				this.navigation = PrevSnap;
				this.currentPageIndex = PrevSnap.CurrentPageIndex;
				this.CurrentPagePosition = this.currentPageIndex;
				this.SetCurrentPage(this.currentPageIndex);
				this.NextButtonText = ServiceRef.Localizer["Kyc_Next"].Value;
				this.ScrollUp();
				this.OnPropertyChanged(nameof(this.Progress));
				this.NotifyNavigationChanged();
				return;
			}
			if (PrevSnap.CurrentPageIndex == this.currentPageIndex)
			{
				// Already at first visible page -> exit
				await base.GoBack();
				return;
			}
			this.navigation = PrevSnap;
			this.currentPageIndex = PrevSnap.CurrentPageIndex;
			this.CurrentPagePosition = this.currentPageIndex;
			this.SetCurrentPage(this.currentPageIndex);
			this.ScrollUp();
			this.NotifyNavigationChanged();
			await this.ValidateCurrentPageAsync();
		}

		private void NotifyNavigationChanged()
		{
			this.OnPropertyChanged(nameof(this.IsInSummary));
			this.OnPropertyChanged(nameof(this.Progress));
		}

		public override async Task GoBack()
		{
			if(this.applicationSent)
				return;
			if (this.editingFromSummary)
			{
				int FirstInvalid = await this.GetFirstInvalidVisiblePageIndexAsync();
				if (FirstInvalid >=0)
				{
					this.currentPageIndex = FirstInvalid;
					this.CurrentPagePosition = FirstInvalid;
					this.SetCurrentPage(FirstInvalid);
					this.NextButtonText = ServiceRef.Localizer["Kyc_Next"].Value;
				}
				else
				{
					await this.GoToSummaryAsync();
					this.SetEditingFromSummary(false);
				}
				return;
			}
			await this.ExecutePrevious();
		}

		[RelayCommand]
		public async Task Exit()
		{
			if (this.kycReference is not null && this.process is not null)
				await this.kycService.FlushSnapshotAsync(this.kycReference, this.process, this.navigation, this.Progress, this.CurrentPage?.Id);
			if (!await AreYouSure(ServiceRef.Localizer[nameof(AppResources.Kyc_Exit)])) return;
			await base.GoBack();
		}

		private async Task<bool> ValidateCurrentPageAsync()
		{
			if (this.CurrentPage is null) return false;
			bool Ok = await this.kycService.ValidatePageAsync(this.CurrentPage);
			if (Ok && this.process is not null)
			{
				IEnumerable<ObservableKycField> Fields = this.CurrentPage.VisibleFields;
				if (this.CurrentPageSections is not null)
					Fields = Fields.Concat(this.CurrentPageSections.SelectMany(s => s.VisibleFields));
				foreach (ObservableKycField Field in Fields)
					this.process.Values[Field.Id] = Field.StringValue;
			}
			MainThread.BeginInvokeOnMainThread(this.NextCommand.NotifyCanExecuteChanged);
			return Ok;
		}

		private async Task<int> GetFirstInvalidVisiblePageIndexAsync()
		{
			if (this.process is null) return -1;
			return await this.kycService.GetFirstInvalidVisiblePageIndexAsync(this.process);
		}

		[RelayCommand]
		private async Task ExecuteApplyAsync()
		{
			if (this.applicationSent) return;
			if (!await this.applyGuard.RunIfNotBusy(async () =>
			{
				if (!await AreYouSure(ServiceRef.Localizer[nameof(AppResources.AreYouSureYouWantToSendThisIdApplication)]))
					return;

				IAuthenticationService Auth = ServiceRef.Provider.GetRequiredService<IAuthenticationService>();
				if (!await Auth.AuthenticateUserAsync(AuthenticationPurpose.SignApplication, true))
					return;
				if (this.attachments is null || this.mappedValues is null) return;
				bool HasIdWithKey = false;
				try
				{ 
					HasIdWithKey = ServiceRef.TagProfile.LegalIdentity is not null && await ServiceRef.XmppService.HasPrivateKey(ServiceRef.TagProfile.LegalIdentity.Id);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogWarning("Error checking for existing identity private key, genereting new keys...: " + Ex.Message);
				}
				(bool Succeeded, LegalIdentity? Added) = await ServiceRef.NetworkService.TryRequest(() => ServiceRef.XmppService.AddLegalIdentity(this.mappedValues.ToArray(), !HasIdWithKey, this.attachments.ToArray()));
				if (Succeeded && Added is not null)
				{
					await ServiceRef.TagProfile.SetIdentityApplication(Added, true);
					this.applicationSent = true;
					if (this.kycReference is not null)
					{
						try { await this.kycService.ApplySubmissionAsync(this.kycReference, Added); }
						catch (Exception Ex) { ServiceRef.LogService.LogException(Ex); }
					}
					this.ErrorDescription = null;
					this.HasErrorDescription = false;
					this.InvalidatedItems.Clear();
					this.UnvalidatedItems.Clear();
					this.UnvalidatedSummaryText = string.Empty;
					this.OnPropertyChanged(nameof(this.HasUnvalidatedItems));
					this.OnPropertyChanged(nameof(this.ShouldShowUnvalidatedBanner));
					this.OnPropertyChanged(nameof(this.ShouldShowRejectionBanner));
					this.RemovePendingAndResubmitCommand?.NotifyCanExecuteChanged();
					foreach (LegalIdentityAttachment LocalAttachment in this.attachments)
					{
						Attachment? Match = Added.Attachments.FirstOrDefault(a => string.Equals(a.FileName, LocalAttachment.FileName, StringComparison.OrdinalIgnoreCase));
						if (Match != null && LocalAttachment.Data is not null && LocalAttachment.ContentType is not null)
						{
							await ServiceRef.AttachmentCacheService.Add(Match.Url, Added.Id, true, LocalAttachment.Data, LocalAttachment.ContentType);
						}
					}
					this.applicationId = Added.Id;
					this.ApplicationSentPublic = true;
					this.NrReviews = ServiceRef.TagProfile.NrReviews;
					await this.LoadApplicationAttributes();
					await this.LoadFeaturedPeerReviewers();
					int AnchorAfterApply = this.currentPageIndex >=0 ? this.currentPageIndex : this.navigation.AnchorPageIndex;
					this.navigation = this.navigation with { State = KycFlowState.Summary, AnchorPageIndex = AnchorAfterApply, CurrentPageIndex = AnchorAfterApply >=0 ? AnchorAfterApply : this.navigation.CurrentPageIndex };
					this.NotifyNavigationChanged();
				}
			}).ConfigureAwait(false)) return;
		}

		private async Task LoadApplicationAttributes()
		{
			try
			{
				IdApplicationAttributesEventArgs AttributesEventArgs = await ServiceRef.XmppService.GetIdApplicationAttributes();
				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.PeerReview = AttributesEventArgs.PeerReview;
					this.NrReviewers = AttributesEventArgs.NrReviewers;
				});
			}
			catch (Exception Ex) { ServiceRef.LogService.LogException(Ex); }
		}

		private async Task LoadFeaturedPeerReviewers()
		{
			await ServiceRef.NetworkService.TryRequest(async () =>
			{
				this.peerReviewServices = await ServiceRef.XmppService.GetServiceProvidersForPeerReviewAsync();
				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.HasFeaturedPeerReviewers = (this.peerReviewServices?.Length ??0) >0;
				});
			});
		}

		private async Task XmppService_IdentityApplicationChanged(object? sender, LegalIdentityEventArgs E)
		{
			await MainThread.InvokeOnMainThreadAsync(async () =>
			{
				this.ApplicationSentPublic = ServiceRef.TagProfile.IdentityApplication is not null;
				this.NrReviews = ServiceRef.TagProfile.NrReviews;
				if (this.kycReference is not null && this.kycReference.CreatedIdentityId == E.Identity.Id)
				{
					try { await this.kycService.ApplySubmissionAsync(this.kycReference, E.Identity); } catch (Exception Ex) { ServiceRef.LogService.LogException(Ex); }
					if (E.Identity.State == IdentityState.Approved)
					{
						await base.GoBack();
						return;
					}
					else if (E.Identity.State == IdentityState.Rejected)
					{
						this.ApplicationSentPublic = false;
						await this.BuildMappedValuesAsync();
						int AnchorRejected = this.currentPageIndex >=0 ? this.currentPageIndex : this.navigation.AnchorPageIndex;
						this.navigation = this.navigation with { State = KycFlowState.Summary, AnchorPageIndex = AnchorRejected, CurrentPageIndex = AnchorRejected >=0 ? AnchorRejected : this.navigation.CurrentPageIndex };
						this.SetEditingFromSummary(false);
						if (this.kycReference is not null && this.process is not null)
							await this.kycService.FlushSnapshotAsync(this.kycReference, this.process, this.navigation, this.Progress, this.CurrentPage?.Id);
						this.NextButtonText = ServiceRef.Localizer["Kyc_Apply"].Value;
						this.OnPropertyChanged(nameof(this.Progress));
						await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.Rejected)], ServiceRef.Localizer[nameof(AppResources.YourApplicationWasRejected)]);
					}
				}
				if (this.ApplicationSentPublic && this.peerReviewServices is null)
					await this.LoadFeaturedPeerReviewers();
				
				this.UpdateReviewIndicators();
			});
		}

		[RelayCommand(CanExecute = nameof(ApplicationSentPublic))]
		private async Task ScanQrCode()
		{
			string? Url = await Services.UI.QR.QrCode.ScanQrCode(nameof(AppResources.QrPageTitleScanPeerId), [Constants.UriSchemes.IotId]);
			if (string.IsNullOrEmpty(Url) || !Constants.UriSchemes.StartsWithIdScheme(Url)) return;
			await this.SendPeerReviewRequest(Constants.UriSchemes.RemoveScheme(Url));
		}

		private async Task SendPeerReviewRequest(string? reviewerId)
		{
			LegalIdentity? ToReview = ServiceRef.TagProfile.IdentityApplication;
			if (ToReview is null || string.IsNullOrEmpty(reviewerId)) return;
			try
			{
				await ServiceRef.XmppService.PetitionPeerReviewId(reviewerId, ToReview, Guid.NewGuid().ToString(), ServiceRef.Localizer[nameof(AppResources.CouldYouPleaseReviewMyIdentityInformation)]);
				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.PetitionSent)], ServiceRef.Localizer[nameof(AppResources.APetitionHasBeenSentToYourPeer)]);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		[RelayCommand(CanExecute = nameof(CanRequestFeaturedPeerReviewer))]
		private async Task RequestReview()
		{
			if (this.peerReviewServices is null)
				await this.LoadFeaturedPeerReviewers();
			if ((this.peerReviewServices?.Length ??0) >0)
			{
				ServiceProviderWithLegalId[] LocalPeerReview = this.peerReviewServices ?? Array.Empty<ServiceProviderWithLegalId>();
				List<ServiceProviderWithLegalId> List = [.. LocalPeerReview, new RequestFromPeer()];
				ServiceProvidersNavigationArgs NavigationArgs = new(List.ToArray(), ServiceRef.Localizer[nameof(AppResources.RequestReview)], ServiceRef.Localizer[nameof(AppResources.SelectServiceProviderPeerReview)]);
				await ServiceRef.NavigationService.GoToAsync(nameof(ServiceProvidersPage), NavigationArgs, Services.UI.BackMethod.Pop);
				if (NavigationArgs.ServiceProvider is not null)
				{
					IServiceProvider? ServiceProvider = await NavigationArgs.ServiceProvider.Task;
					if (ServiceProvider is ServiceProviderWithLegalId SPWL && !string.IsNullOrEmpty(SPWL.LegalId))
					{
						if (!SPWL.External)
						{
							if (!await ServiceRef.NetworkService.TryRequest(async () => await ServiceRef.XmppService.SelectPeerReviewService(ServiceProvider.Id, ServiceProvider.Type)))
								return;
						}
						await this.SendPeerReviewRequest(SPWL.LegalId);
						return;
					}
					else if (ServiceProvider is null) return;
				}
			}
			await this.ScanQrCode();
		}

		[RelayCommand(CanExecute = nameof(ApplicationSentPublic))]
		private async Task RevokeApplication()
		{
			await this.RevokeCurrentApplicationAsync(true);
		}

		private Task BuildMappedValuesAsync()
		{
			if (this.process is null)
			{
				this.mappedValues = new();
				this.attachments = new();
				return Task.CompletedTask;
			}
			return Task.Run(async () =>
			{
				(IReadOnlyList<Property> Props, IReadOnlyList<LegalIdentityAttachment> Atts) = await this.kycService.PreparePropertiesAndAttachmentsAsync(this.process, CancellationToken.None);
				this.mappedValues = Props.ToList();
				this.attachments = Atts.ToList();
				string? Jid = ServiceRef.TagProfile.LegalIdentity?.Properties.FirstOrDefault(p => p.Name == Constants.XmppProperties.Jid)?.Value ?? ServiceRef.XmppService.BareJid ?? string.Empty;
				string? Phone = ServiceRef.TagProfile.LegalIdentity?.Properties.FirstOrDefault(p => p.Name == Constants.XmppProperties.Phone)?.Value ?? ServiceRef.TagProfile.PhoneNumber ?? string.Empty;
				string? Email = ServiceRef.TagProfile.LegalIdentity?.Properties.FirstOrDefault(p => p.Name == Constants.XmppProperties.EMail)?.Value ?? ServiceRef.TagProfile.EMail ?? string.Empty;
				this.mappedValues.Add(new Property(Constants.XmppProperties.DeviceId, ServiceRef.PlatformSpecific.GetDeviceId()));
				if (!this.process.HasMapping(Constants.XmppProperties.Jid)) this.mappedValues.Add(new Property(Constants.XmppProperties.Jid, Jid));
				if (!this.process.HasMapping(Constants.XmppProperties.Phone)) this.mappedValues.Add(new Property(Constants.XmppProperties.Phone, Phone));
				if (!this.process.HasMapping(Constants.XmppProperties.EMail)) this.mappedValues.Add(new Property(Constants.XmppProperties.EMail, Email));
				if (!this.process.HasMapping(Constants.XmppProperties.Country) && !string.IsNullOrEmpty(ServiceRef.TagProfile.SelectedCountry))
					this.mappedValues.Add(new Property(Constants.XmppProperties.Country, ServiceRef.TagProfile.SelectedCountry));
				ISet<string> Invalid = KycSummary.BuildInvalidMappingSet(this.kycReference);
				KycSummaryModel Model = KycSummary.Generate(this.process, this.mappedValues, this.attachments, Invalid);
				MainThread.BeginInvokeOnMainThread(() => this.ApplySummaryModel(Model));
			});
		}

		private void ApplySummaryModel(KycSummaryModel model)
		{
			this.PersonalInformationSummary = new ObservableCollection<DisplayQuad>(model.Get(KycSummary.Personal)?.Items ?? Array.Empty<DisplayQuad>());
			this.AddressInformationSummary = new ObservableCollection<DisplayQuad>(model.Get(KycSummary.Address)?.Items ?? Array.Empty<DisplayQuad>());
			this.AttachmentInformationSummary = new ObservableCollection<DisplayQuad>(model.Get(KycSummary.Attachments)?.Items ?? Array.Empty<DisplayQuad>());
			this.CompanyInformationSummary = new ObservableCollection<DisplayQuad>(model.Get(KycSummary.CompanyInfo)?.Items ?? Array.Empty<DisplayQuad>());
			this.CompanyAddressSummary = new ObservableCollection<DisplayQuad>(model.Get(KycSummary.CompanyAddress)?.Items ?? Array.Empty<DisplayQuad>());
			this.CompanyRepresentativeSummary = new ObservableCollection<DisplayQuad>(model.Get(KycSummary.CompanyRepresentative)?.Items ?? Array.Empty<DisplayQuad>());
			this.HasPersonalInformation = this.PersonalInformationSummary.Count >0;
			this.HasAddressInformation = this.AddressInformationSummary.Count >0;
			this.HasAttachments = this.AttachmentInformationSummary.Count >0;
			this.HasCompanyInformation = this.CompanyInformationSummary.Count >0;
			this.HasCompanyAddress = this.CompanyAddressSummary.Count >0;
			this.HasCompanyRepresentative = this.CompanyRepresentativeSummary.Count >0;
		}

		private KycProcessState BuildProcessState()
		{
			if (this.process is null)
				return new KycProcessState(Array.Empty<KycPageState>(), this.navigation, this.applicationSent);

			List<KycPageState> PageStates = new List<KycPageState>(this.process.Pages.Count);
			foreach (KycPage Page in this.process.Pages)
			{
				bool IsVisible = Page.IsVisible(this.process.Values);
				List<KycFieldState> FieldStates = new List<KycFieldState>();
				foreach (ObservableKycField Field in Page.AllFields)
				{
					if (!Field.IsVisible) continue;
					string? Value = string.IsNullOrWhiteSpace(Field.StringValue) ? null : Field.StringValue;
					FieldStates.Add(new KycFieldState(Field.Id, Field.IsValid, Value));
				}
				foreach (KycSection Section in Page.AllSections)
				{
					if (!Section.IsVisible) continue;
					foreach (ObservableKycField Field in Section.AllFields)
					{
						if (!Field.IsVisible) continue;
						string? Value = string.IsNullOrWhiteSpace(Field.StringValue) ? null : Field.StringValue;
						FieldStates.Add(new KycFieldState(Field.Id, Field.IsValid, Value));
					}
				}
				PageStates.Add(new KycPageState(Page.Id, IsVisible, FieldStates));
			}
			return new KycProcessState(PageStates, this.navigation, this.applicationSent);
		}

		public async Task ApplyApplicationReviewAsync(ApplicationReview review)
		{
			this.ErrorDescription = review.Message;
			this.HasErrorDescription = !string.IsNullOrWhiteSpace(review.Message);
			if (this.kycReference is not null)
				await this.kycService.ApplyApplicationReviewAsync(this.kycReference, review);
			KycInvalidation.ApplyInvalidations(this.process, review, this.ErrorDescription);
			this.UpdateReviewIndicators();
			await this.BuildMappedValuesAsync();
		}

		[RelayCommand(CanExecute = nameof(ShouldShowUnvalidatedBanner))]
		private async Task RemovePendingAndResubmitAsync()
		{
			string Confirmation = ServiceRef.Localizer[nameof(AppResources.KycConfirmRemovePending), false];
			if (!await AreYouSure(Confirmation))
				return;

			if (!await this.RevokeCurrentApplicationAsync(false))
				return;

			if (!await this.ClearUnvalidatedFieldsAsync())
				return;

			if (this.process is not null && this.kycReference is not null)
			{
				try
				{
					await this.kycService.FlushSnapshotAsync(this.kycReference, this.process, this.navigation, this.Progress, this.CurrentPage?.Id);
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
				}
			}

			await this.BuildMappedValuesAsync();
			await this.ExecuteApplyAsync();
		}

		private async Task<bool> ClearUnvalidatedFieldsAsync()
		{
			if (this.process is null || this.kycReference?.ApplicationReview is null)
				return false;

			ApplicationReview review = this.kycReference.ApplicationReview;
			HashSet<string> mappingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (string Claim in review.UnvalidatedClaims ?? Array.Empty<string>())
			{
				if (!string.IsNullOrWhiteSpace(Claim))
					mappingKeys.Add(Claim.Trim());
			}
			foreach (string Photo in review.UnvalidatedPhotos ?? Array.Empty<string>())
			{
				if (!string.IsNullOrWhiteSpace(Photo))
				{
					string trimmed = Photo.Trim();
					mappingKeys.Add(trimmed);
					string? baseName = Path.GetFileNameWithoutExtension(trimmed);
					if (!string.IsNullOrWhiteSpace(baseName))
						mappingKeys.Add(baseName.Trim());
				}
			}

			if (mappingKeys.Count == 0)
				return false;

			bool cleared = this.ClearFieldsForMappings(mappingKeys);

			if (!cleared)
				return false;

			this.mappedValues = this.mappedValues
				.Where(p => !mappingKeys.Contains(p.Name ?? string.Empty))
				.ToList();
			this.attachments = this.attachments
				.Where(a =>
				{
					string fileName = a.FileName ?? string.Empty;
					string baseName = Path.GetFileNameWithoutExtension(fileName) ?? fileName;
					return !mappingKeys.Contains(fileName.Trim()) && !mappingKeys.Contains(baseName.Trim());
				})
				.ToList();

			review.UnvalidatedClaims = Array.Empty<string>();
			review.UnvalidatedPhotos = Array.Empty<string>();

			await this.kycService.ApplyApplicationReviewAsync(this.kycReference, review);
			this.UpdateReviewIndicators();
			return true;
		}

		private bool ClearFieldsForMappings(HashSet<string> mappingKeys)
		{
			if (this.process is null || mappingKeys.Count == 0)
				return false;

			bool cleared = false;
			foreach (KycPage Page in this.process.Pages)
			{
				cleared |= this.ClearFields(Page.AllFields, mappingKeys);

				foreach (KycSection Section in Page.AllSections)
				{
					cleared |= this.ClearFields(Section.AllFields, mappingKeys);
				}
			}
			return cleared;
		}

		private async Task<bool> RevokeCurrentApplicationAsync(bool requireConfirmation)
		{
			LegalIdentity? Application = ServiceRef.TagProfile.IdentityApplication;
			if (Application is null)
			{
				this.ApplicationSentPublic = false;
				this.applicationSent = false;
				this.peerReviewServices = null;
				this.HasFeaturedPeerReviewers = false;
				if (this.kycReference is not null)
					await this.kycService.ClearSubmissionAsync(this.kycReference);
				return true;
			}

			if (requireConfirmation && !await AreYouSure(ServiceRef.Localizer[nameof(AppResources.AreYouSureYouWantToRevokeTheCurrentIdApplication)]))
				return false;

			IAuthenticationService Auth = ServiceRef.Provider.GetRequiredService<IAuthenticationService>();
			if (!await Auth.AuthenticateUserAsync(AuthenticationPurpose.RevokeApplication, true))
				return false;

			try
			{
				await ServiceRef.XmppService.ObsoleteLegalIdentity(Application.Id);
				await ServiceRef.TagProfile.SetIdentityApplication(null, true);
				this.ApplicationSentPublic = false;
				this.applicationSent = false;
				this.peerReviewServices = null;
				this.HasFeaturedPeerReviewers = false;
				if (this.kycReference is not null)
					await this.kycService.ClearSubmissionAsync(this.kycReference);
				await this.BuildMappedValuesAsync();
				int AnchorAfterRevoke = this.currentPageIndex >= 0 ? this.currentPageIndex : this.navigation.AnchorPageIndex;
				this.navigation = this.navigation with { State = KycFlowState.Summary, AnchorPageIndex = AnchorAfterRevoke, CurrentPageIndex = AnchorAfterRevoke >= 0 ? AnchorAfterRevoke : this.navigation.CurrentPageIndex };
				this.SetEditingFromSummary(false);
				this.NotifyNavigationChanged();
				return true;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
				return false;
			}
		}

		private bool ClearFields(IEnumerable<ObservableKycField> fields, HashSet<string> mappingKeys)
		{
			bool cleared = false;
			foreach (ObservableKycField Field in fields)
			{
				if (!Field.Mappings.Any(m => mappingKeys.Contains(m.Key)))
					continue;

				if (!string.IsNullOrWhiteSpace(Field.StringValue))
				{
					Field.StringValue = null;
					if (this.process is not null && this.process.Values.ContainsKey(Field.Id))
						this.process.Values[Field.Id] = null;
					cleared = true;
				}
			}
			return cleared;
		}

		private void UpdateReviewIndicators()
		{
			ApplicationReview? review = this.kycReference?.ApplicationReview;
			if (review is null)
			{
				this.UnvalidatedSummaryText = string.Empty;
				this.InvalidatedItems.Clear();
				this.UnvalidatedItems.Clear();
			}
			else
			{
				int claimCount = review.UnvalidatedClaims?.Length ?? 0;
				int photoCount = review.UnvalidatedPhotos?.Length ?? 0;
				if (claimCount == 0 && photoCount == 0)
				{
					this.UnvalidatedSummaryText = string.Empty;
				}
				else
				{
					this.UnvalidatedSummaryText = ServiceRef.Localizer["KycUnvalidatedWarningDescription", false, claimCount, photoCount];
				}

				this.InvalidatedItems.Clear();
				foreach (ApplicationReviewClaimDetail detail in review.InvalidClaimDetails ?? Array.Empty<ApplicationReviewClaimDetail>())
				{
					if (detail is null)
						continue;
					string label = this.ResolveDisplayLabel(detail.Claim, detail.DisplayName);
					string reason = string.IsNullOrWhiteSpace(detail.Reason) ? string.Empty : $" — {detail.Reason}";
					this.InvalidatedItems.Add($"{label}{reason}");
				}
				foreach (ApplicationReviewPhotoDetail Detail in review.InvalidPhotoDetails ?? Array.Empty<ApplicationReviewPhotoDetail>())
				{
					if (Detail is null)
						continue;
					string Label = this.ResolveDisplayLabel(null, Detail.DisplayName);
					string Reason = string.IsNullOrWhiteSpace(Detail.Reason) ? string.Empty : $" — {Detail.Reason}";
					this.InvalidatedItems.Add($"{Label}{Reason}");
				}

				this.UnvalidatedItems.Clear();
				foreach (string claim in review.UnvalidatedClaims ?? Array.Empty<string>())
				{
					if (string.IsNullOrWhiteSpace(claim))
						continue;
					string label = this.ResolveDisplayLabel(claim.Trim(), claim.Trim());
					this.UnvalidatedItems.Add(label);
				}
				foreach (string photo in review.UnvalidatedPhotos ?? Array.Empty<string>())
				{
					if (string.IsNullOrWhiteSpace(photo))
						continue;
					string label = this.ResolveDisplayLabel(photo.Trim(), photo.Trim());
					this.UnvalidatedItems.Add(label);
				}
			}

			this.OnPropertyChanged(nameof(this.HasUnvalidatedItems));
			this.OnPropertyChanged(nameof(this.ShouldShowUnvalidatedBanner));
			this.OnPropertyChanged(nameof(this.ShouldShowRejectionBanner));
			this.RemovePendingAndResubmitCommand?.NotifyCanExecuteChanged();
		}

		private string ResolveDisplayLabel(string? MappingKey, string? Fallback)
		{
			string? FromMapping = string.IsNullOrWhiteSpace(MappingKey) ? null : this.FindLabelByMapping(MappingKey);
			if (!string.IsNullOrWhiteSpace(FromMapping))
				return FromMapping;

			if (!string.IsNullOrWhiteSpace(Fallback))
			{
				string? FromLabel = this.FindLabelByDisplayName(Fallback);
				if (!string.IsNullOrWhiteSpace(FromLabel))
					return FromLabel;
			}

			return Fallback ?? string.Empty;
		}

		private string? FindLabelByMapping(string mappingKey)
		{
			foreach ((DisplayQuad Item, bool PreferValue) entry in this.IterateSummaryItems())
			{
				if (!string.IsNullOrWhiteSpace(entry.Item.Mapping) && entry.Item.Mapping.Equals(mappingKey, StringComparison.OrdinalIgnoreCase))
					return this.GetDisplayLabel(entry.Item, entry.PreferValue);
			}

			return null;
		}

		private string? FindLabelByDisplayName(string label)
		{
			foreach ((DisplayQuad Item, bool PreferValue) entry in this.IterateSummaryItems())
			{
				string candidate = this.GetDisplayLabel(entry.Item, entry.PreferValue);
				if (!string.IsNullOrWhiteSpace(candidate) && candidate.Equals(label, StringComparison.OrdinalIgnoreCase))
					return candidate;
			}

			return null;
		}

		private IEnumerable<(DisplayQuad Item, bool PreferValue)> IterateSummaryItems()
		{
			IEnumerable<(ObservableCollection<DisplayQuad>? Collection, bool PreferValue)> collections = new (ObservableCollection<DisplayQuad>?, bool)[]
			{
				(this.PersonalInformationSummary, false),
				(this.AddressInformationSummary, false),
				(this.AttachmentInformationSummary, true),
				(this.CompanyInformationSummary, false),
				(this.CompanyAddressSummary, false),
				(this.CompanyRepresentativeSummary, false)
			};

			foreach ((ObservableCollection<DisplayQuad>? Collection, bool PreferValue) entry in collections)
			{
				if (entry.Collection is null)
					continue;

				foreach (DisplayQuad item in entry.Collection)
					yield return (item, entry.PreferValue);
			}
		}

		private string GetDisplayLabel(DisplayQuad item, bool preferValue)
		{
			if (item is null)
				return string.Empty;

			if (preferValue && !string.IsNullOrWhiteSpace(item.Value))
				return item.Value;

			if (!string.IsNullOrWhiteSpace(item.Label))
				return item.Label;

			return item.Value ?? string.Empty;
		}

	}
}
