using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
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
using NeuroAccessMaui.UI.Pages.Applications.ApplyId;
using NeuroAccessMaui.UI.Pages.Identity.ViewIdentity;

namespace NeuroAccessMaui.UI.Pages.Kyc
{
	/// <summary>
	/// Orchestrates the interactive KYC flow, covering navigation, validation, summary projection, persistence scheduling, peer review, and submission.
	/// </summary>
	public partial class KycProcessViewModel : BaseViewModel, IDisposable
	{
		private readonly IKycService kycService = ServiceRef.KycService;
		private bool disposedValue;
		private readonly KycProcessNavigationArgs? navigationArguments;
		private KycProcess? process;
		private KycReference? kycReference;
		private int currentPageIndex = 0;
		private KycNavigationSnapshot navigation = new KycNavigationSnapshot(0, -1, KycFlowState.Form);
		private bool applicationSent;
		private string? applicationId;

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

		[ObservableProperty] private string? errorDescription;
		[ObservableProperty] private bool hasErrorDescription;

		[ObservableProperty] private bool hasPersonalInformation;
		[ObservableProperty] private bool hasAddressInformation;
		[ObservableProperty] private bool hasAttachments;
		[ObservableProperty] private bool hasCompanyInformation;
		[ObservableProperty] private bool hasCompanyAddress;
		[ObservableProperty] private bool hasCompanyRepresentative;

		// Throttling support for page refresh (avoid per-keystroke SetCurrentPage work)
		private static readonly TimeSpan PageRefreshThrottle = TimeSpan.FromMilliseconds(250);
		private readonly ObservableTask<int> pageRefreshTask;

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

				ObservableCollection<KycPage> visible = [.. this.Pages.Where(p => p.IsVisible(this.process.Values))];
				if (visible.Count == 0)
				{
					this.ProgressPercent = "0%";
					return 0;
				}
				if (this.IsInSummary)
				{
					this.ProgressPercent = "100%";
					return 1;
				}
				int index = visible.IndexOf(this.CurrentPage);
				double progress = Math.Clamp((double)index / visible.Count, 0, 1);
				this.ProgressPercent = $"{(progress * 100):0}%";
				return progress;
			}
		}

		[ObservableProperty] private string progressPercent = "0%";

		public IAsyncRelayCommand NextCommand { get; }
		public IRelayCommand PreviousCommand { get; }

		public KycProcessViewModel(KycProcessNavigationArgs? Args)
		{
			this.navigationArguments = Args;
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
				.WithPolicy(Policies.Debounce(PageRefreshThrottle))
				.Run(async context =>
				{
					CancellationToken cancellationToken = context.CancellationToken;
					if (cancellationToken.IsCancellationRequested)
					{
						return;
					}

					try
					{
						await MainThread.InvokeOnMainThreadAsync(() =>
						{
							if (cancellationToken.IsCancellationRequested)
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
					catch (Exception ex)
					{
						ServiceRef.LogService.LogException(ex);
					}
				})
				.Build();
		}

		protected override async Task OnInitialize()
		{
			this.IsLoading = true;
			await base.OnInitialize();

			string lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
			this.kycReference = this.navigationArguments?.Reference;
			if (this.kycReference is null)
			{
				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], "Missing KYC reference.", ServiceRef.Localizer[nameof(AppResources.Ok)]);
				await this.GoBack();
				return;
			}

			this.process = await this.kycReference.ToProcess(lang);
			this.applicationId = this.kycReference.CreatedIdentityId;
			this.OnPropertyChanged(nameof(this.Pages));
			if (this.process is null)
			{
				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], "Unable to load KYC process.", ServiceRef.Localizer[nameof(AppResources.Ok)]);
				await this.GoBack();
				return;
			}
			this.process.Initialize();

			bool pending = this.kycReference.CreatedIdentityState == IdentityState.Created && !string.IsNullOrEmpty(this.kycReference.CreatedIdentityId);
			bool rejected = this.kycReference.CreatedIdentityState == IdentityState.Rejected && !string.IsNullOrEmpty(this.kycReference.CreatedIdentityId);
			this.ApplicationSentPublic = pending;
			if (!pending && !rejected)
				this.PrefillFieldsFromProfile();

			ServiceRef.XmppService.IdentityApplicationChanged += this.XmppService_IdentityApplicationChanged;
			if (ServiceRef.TagProfile.IdentityApplication is not null)
			{
				await this.LoadApplicationAttributes();
				await this.LoadFeaturedPeerReviewers();
			}

			if (this.kycReference.RejectionMessage is not null)
			{
				this.ErrorDescription = this.kycReference.RejectionMessage;
				this.HasErrorDescription = !string.IsNullOrWhiteSpace(this.ErrorDescription);
				KycInvalidation.ApplyInvalidations(this.process, this.kycReference, this.ErrorDescription);
			}

			this.process.ClearValidation();
			foreach (KycPage page in this.process.Pages)
			{
				page.PropertyChanged += this.Page_PropertyChanged;
				foreach (ObservableKycField field in page.AllFields)
					field.PropertyChanged += this.Field_PropertyChanged;
				foreach (KycSection section in page.AllSections)
				{
					section.PropertyChanged += this.Section_PropertyChanged;
					foreach (ObservableKycField field in section.AllFields)
						field.PropertyChanged += this.Field_PropertyChanged;
				}
			}
			foreach (KycPage page in this.process.Pages)
				page.UpdateVisibilities(this.process.Values);

			int resumeIndex = -1;
			if (!string.IsNullOrEmpty(this.kycReference.LastVisitedPageId))
			{
				for (int i = 0; i < this.Pages.Count; i++)
				{
					KycPage p = this.Pages[i];
					if (p.Id == this.kycReference.LastVisitedPageId && p.IsVisible(this.process.Values))
					{
						resumeIndex = i;
						break;
					}
				}
			}

			bool lastWasSummary = string.Equals(this.kycReference.LastVisitedMode, "Summary", StringComparison.OrdinalIgnoreCase);

			if (lastWasSummary)
			{
				int firstInvalid = await this.GetFirstInvalidVisiblePageIndexAsync();
				if (firstInvalid >= 0)
				{
					// There are invalid fields -> go to first invalid page but mark editing from summary
					this.SetEditingFromSummary(true);
					this.currentPageIndex = firstInvalid;
					this.CurrentPagePosition = firstInvalid;
					this.SetCurrentPage(firstInvalid);
					this.NextButtonText = ServiceRef.Localizer["Kyc_Next"].Value;
				}
				else
				{
					await this.BuildMappedValuesAsync();
					// Keep a valid page index for navigation (use last visited if available else first visible)
					this.currentPageIndex = resumeIndex >= 0 ? resumeIndex : this.GetFirstVisibleIndex();
					if (this.currentPageIndex >= 0)
					{
						this.CurrentPagePosition = this.currentPageIndex;
						this.SetCurrentPage(this.currentPageIndex);
					}
					int anchorIndex = this.currentPageIndex >= 0 ? this.currentPageIndex : this.navigation.AnchorPageIndex;
					this.navigation = this.navigation with { State = KycFlowState.Summary, AnchorPageIndex = anchorIndex, CurrentPageIndex = anchorIndex >= 0 ? anchorIndex : this.navigation.CurrentPageIndex };
					this.SetEditingFromSummary(false);
					this.NotifyNavigationChanged();
					this.NextButtonText = ServiceRef.Localizer["Kyc_Apply"].Value;
				}
			}
			else if (rejected)
			{
				await this.BuildMappedValuesAsync();
				// For rejected applications, allow re-application: show summary but keep a valid page index
				int firstInvalid = await this.GetFirstInvalidVisiblePageIndexAsync();
				this.currentPageIndex = firstInvalid >= 0 ? firstInvalid : this.GetFirstVisibleIndex();
				if (this.currentPageIndex >= 0)
				{
					this.CurrentPagePosition = this.currentPageIndex;
					this.SetCurrentPage(this.currentPageIndex);
				}
			int anchorIndexRejected = this.currentPageIndex >= 0 ? this.currentPageIndex : this.navigation.AnchorPageIndex;
			this.navigation = this.navigation with { State = KycFlowState.Summary, AnchorPageIndex = anchorIndexRejected, CurrentPageIndex = anchorIndexRejected >= 0 ? anchorIndexRejected : this.navigation.CurrentPageIndex };
				this.SetEditingFromSummary(false);
				this.NotifyNavigationChanged();
				this.NextButtonText = ServiceRef.Localizer["Kyc_Apply"].Value;
			}
			else
			{
				// Normal form resume
				this.currentPageIndex = resumeIndex >= 0 ? resumeIndex : this.GetFirstVisibleIndex();
				// Synchronize navigation snapshot so Back() logic and snapshot persistence have correct page index.
				if (this.currentPageIndex >= 0)
					this.navigation = this.navigation with { CurrentPageIndex = this.currentPageIndex };
				this.CurrentPagePosition = this.currentPageIndex; // Triggers SetCurrentPage via partial method
				// Explicit call retained for clarity (idempotent) but could be removed since setter already calls it.
				this.SetCurrentPage(this.currentPageIndex);
			}

			this.NextButtonText = this.IsInSummary ? ServiceRef.Localizer["Kyc_Apply"].Value : ServiceRef.Localizer["Kyc_Next"].Value;
			MainThread.BeginInvokeOnMainThread(this.NextCommand.NotifyCanExecuteChanged);
			if (pending)
			{
				await this.BuildMappedValuesAsync();
			int anchorIndexPending = this.currentPageIndex >= 0 ? this.currentPageIndex : this.navigation.AnchorPageIndex;
			this.navigation = this.navigation with { State = KycFlowState.Summary, AnchorPageIndex = anchorIndexPending, CurrentPageIndex = anchorIndexPending >= 0 ? anchorIndexPending : this.navigation.CurrentPageIndex };
				this.SetEditingFromSummary(false);
				this.NotifyNavigationChanged();
				this.NextButtonText = ServiceRef.Localizer["Kyc_Apply"].Value;
			}
			this.IsLoading = false;
		}

		private int GetFirstVisibleIndex()
		{
			if (this.process is null) return -1;
			for (int i = 0; i < this.Pages.Count; i++)
			{
				if (this.Pages[i].IsVisible(this.process.Values)) return i;
			}
			return -1;
		}

		[RelayCommand]
		private async Task GoToPageWithMapping(string? mapping)
		{
			if (string.IsNullOrWhiteSpace(mapping)) return;
			if (this.process is null) return;
			if (this.ApplicationSentPublic) return; // Cannot edit after application sent

			int targetIndex = this.FindPageIndexByMapping(mapping);
			if (targetIndex < 0) return;

			// Preserve original anchor if coming from summary
			// Anchor now maintained via navigation.AnchorPageIndex (legacy summaryAnchorPageIndex removed)

			// Transition from summary to edit mode
			this.navigation = this.navigation with { State = KycFlowState.Form };
			this.SetEditingFromSummary(true); // Allow quick return to summary
			this.currentPageIndex = targetIndex;
			this.CurrentPagePosition = targetIndex;
			this.SetCurrentPage(targetIndex);
			this.NotifyNavigationChanged();
			this.NextButtonText = ServiceRef.Localizer[nameof(AppResources.Kyc_Return), false];
			await MainThread.InvokeOnMainThreadAsync(this.ScrollUp);
		}

		private int FindPageIndexByMapping(string mapping)
		{
			if (this.process is null) return -1;
			string m = mapping.Trim();
			for (int i = 0; i < this.Pages.Count; i++)
			{
				KycPage page = this.Pages[i];
				if (!page.IsVisible(this.process.Values)) continue;
				// Direct fields
				foreach (ObservableKycField field in page.AllFields)
				{
					if (FieldMatches(field, m)) return i;
				}
				// Sections
				foreach (KycSection section in page.AllSections)
				{
					foreach (ObservableKycField field in section.AllFields)
					{
						if (FieldMatches(field, m)) return i;
					}
				}
			}
			return -1;
			static bool FieldMatches(ObservableKycField field, string mappingKey)
			{
				foreach (KycMapping map in field.Mappings)
				{
					if (string.Equals(map.Key, mappingKey, StringComparison.OrdinalIgnoreCase)) return true;
					// Handle grouped date mapping equivalences (BDATE vs components, ORGREP...)
					if (mappingKey.Equals("BDATE", StringComparison.OrdinalIgnoreCase) &&
						(string.Equals(map.Key, Constants.XmppProperties.BirthDay, StringComparison.OrdinalIgnoreCase) ||
						 string.Equals(map.Key, Constants.XmppProperties.BirthMonth, StringComparison.OrdinalIgnoreCase) ||
						 string.Equals(map.Key, Constants.XmppProperties.BirthYear, StringComparison.OrdinalIgnoreCase))) return true;
					if (mappingKey.StartsWith("ORGREP", StringComparison.OrdinalIgnoreCase) && map.Key.StartsWith("ORGREP", StringComparison.OrdinalIgnoreCase)) return true;
				}
				return false;
			}
		}

		// Add derived properties required by XAML
		public bool CanRequestFeaturedPeerReviewer => this.ApplicationSentPublic && this.HasFeaturedPeerReviewers;
		public bool FeaturedPeerReviewers => this.CanRequestFeaturedPeerReviewer && this.PeerReview;

		partial void OnApplicationSentPublicChanged(bool value)
		{
			if (value)
			{
				int anchor = this.currentPageIndex >= 0 ? this.currentPageIndex : this.navigation.AnchorPageIndex;
				this.navigation = this.navigation with { State = KycFlowState.Summary, AnchorPageIndex = anchor, CurrentPageIndex = anchor >= 0 ? anchor : this.navigation.CurrentPageIndex };
				this.SetEditingFromSummary(false);
				this.NotifyNavigationChanged();
			}
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
			LegalIdentity? identity = ServiceRef.TagProfile.LegalIdentity;
			if (identity?.Properties is null) return;
			Dictionary<string, string> byName = identity.Properties
				.Where(p => p is not null && !string.IsNullOrWhiteSpace(p.Name))
				.GroupBy(p => p.Name!, StringComparer.OrdinalIgnoreCase)
				.Select(g => g.OrderByDescending(p => p?.Value?.Length ?? 0).First())
				.ToDictionary(p => p.Name!, p => p.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);
			IEnumerable<ObservableKycField> allFields = this.process.Pages.SelectMany(p => p.AllFields)
				.Concat(this.process.Pages.SelectMany(p => p.AllSections).SelectMany(s => s.AllFields));
			foreach (ObservableKycField field in allFields)
			{
				if (!string.IsNullOrWhiteSpace(field.StringValue)) continue;
				if (field.Mappings.Count == 0) continue;
				foreach (KycMapping map in field.Mappings)
				{
					if (string.IsNullOrWhiteSpace(map.Key)) continue;
					if (byName.TryGetValue(map.Key, out string? Value) && !string.IsNullOrWhiteSpace(Value))
					{
						field.StringValue = Value;
						this.process.Values[field.Id] = field.StringValue;
						break;
					}
				}
			}
		}

		partial void OnCurrentPagePositionChanged(int value)
		{
			if (value >= 0 && value < this.Pages.Count)
			{
				this.currentPageIndex = value;
				this.SetCurrentPage(this.currentPageIndex);
			}
		}

		private void Field_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ObservableKycField.RawValue))
			{
				this.SchedulePageRefresh();
			}
			if (e.PropertyName == nameof(ObservableKycField.IsValid))
			{
				MainThread.BeginInvokeOnMainThread(this.NextCommand.NotifyCanExecuteChanged);
			}
		}

		/// <summary>
		/// Queues a debounced refresh of the current page metadata on the UI thread.
		/// </summary>
		private void SchedulePageRefresh()
		{
			this.pageRefreshTask.Run();
		}

		private void Section_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(KycSection.IsVisible))
				this.SchedulePageRefresh();
		}

		private void Page_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(KycPage.IsVisible))
				this.SchedulePageRefresh();
		}

		private void SetCurrentPage(int index)
		{
			if (this.process is null) return;
			if (index < 0 || index >= this.Pages.Count) return;
			this.currentPageIndex = index;
			this.CurrentPagePosition = index;
			KycPage page = this.Pages[index];
			this.CurrentPage = page;
			this.CurrentPageTitle = page.Title?.Text ?? page.Id;
			this.CurrentPageDescription = page.Description?.Text;
			this.HasCurrentPageDescription = !string.IsNullOrWhiteSpace(this.CurrentPageDescription);
			this.CurrentPageSections = page.VisibleSections;
			this.HasSections = this.CurrentPageSections is not null && this.CurrentPageSections.Count > 0;
			// Persist last visited page (only when not in summary mode)
			if (!this.IsInSummary && this.kycReference is not null && this.process is not null)
				_ = this.kycService.ScheduleSnapshotAsync(this.kycReference, this.process, this.navigation, this.Progress, page.Id);
			this.OnPropertyChanged(nameof(this.Progress));
			this.NextCommand.NotifyCanExecuteChanged();
		}

		private bool CanExecuteNext()
		{
			// Allow Apply button on summary if application not yet sent
			if (this.IsInSummary)
				return !this.ApplicationSentPublic;
			if (this.CurrentPage is null) return false;
			IEnumerable<ObservableKycField> fields = this.CurrentPage.VisibleFields;
			if (this.CurrentPageSections is not null)
				fields = fields.Concat(this.CurrentPageSections.SelectMany(s => s.VisibleFields));
			return fields.All(f => f.IsValid);
		}

		private async Task ExecuteNextAsync()
		{
			ServiceRef.PlatformSpecific.HideKeyboard();
			if (this.IsInSummary)
			{
				await this.ExecuteApplyAsync();
				return;
			}
			if (this.editingFromSummary)
			{
				bool okEditing = await this.ValidateCurrentPageAsync();
				if (!okEditing)
				{
					return;
				}
				int firstInvalidFromSummary = await this.GetFirstInvalidVisiblePageIndexAsync();
				if (firstInvalidFromSummary >= 0)
				{
					this.currentPageIndex = firstInvalidFromSummary;
					this.CurrentPagePosition = firstInvalidFromSummary;
					this.SetCurrentPage(firstInvalidFromSummary);
					this.NextButtonText = ServiceRef.Localizer["Kyc_Next"].Value;
					return;
				}
				await this.GoToSummaryAsync();
				this.SetEditingFromSummary(false);
				return;
			}
			bool ok = await this.ValidateCurrentPageAsync();
			if (!ok) return;
			if (this.kycReference is not null && this.process is not null)
				await this.kycService.FlushSnapshotAsync(this.kycReference, this.process, this.navigation, this.Progress, this.CurrentPage?.Id);
			if (this.process is null) return;
			// Build list of visible page indices
			List<int> VisibleIndices = new List<int>();
			for (int i = 0; i < this.Pages.Count; i++)
			{
				if (this.Pages[i].IsVisible(this.process.Values)) VisibleIndices.Add(i);
			}
			// Build current process state including page + field validity snapshots
			KycProcessState ProcessState = this.BuildProcessState();
			KycNavigationSnapshot NextSnap = KycTransitions.Advance(ProcessState, VisibleIndices);
			if (NextSnap.State == KycFlowState.Summary || NextSnap.State == KycFlowState.PendingSummary)
			{
				await this.BuildMappedValuesAsync();
				this.ScrollUp();
				this.SetEditingFromSummary(false);
				this.navigation = NextSnap with { AnchorPageIndex = NextSnap.AnchorPageIndex >= 0 ? NextSnap.AnchorPageIndex : this.navigation.AnchorPageIndex >= 0 ? this.navigation.AnchorPageIndex : this.currentPageIndex };
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
		}

		public event EventHandler? ScrollToTop;
		private void ScrollUp() => this.ScrollToTop?.Invoke(this, EventArgs.Empty);

		protected override Task OnDispose()
		{
			this.Dispose(true);
			return base.OnDispose();
		}

		protected virtual void Dispose(bool disposing)
		{
			if (this.disposedValue)
			{
				return;
			}
			if (disposing)
			{
				this.pageRefreshTask.Dispose();
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
			int anchor = this.navigation.AnchorPageIndex >= 0 ? this.navigation.AnchorPageIndex : this.currentPageIndex;
			if (anchor >= 0)
			{
				this.currentPageIndex = anchor;
				this.CurrentPagePosition = anchor;
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
			for (int i = 0; i < this.Pages.Count; i++)
			{
				if (this.Pages[i].IsVisible(this.process.Values)) VisibleIndices.Add(i);
			}
			KycProcessState ProcessState = this.BuildProcessState();
			KycNavigationSnapshot PrevSnap = KycTransitions.Back(ProcessState, VisibleIndices);
			if (this.IsInSummary)
			{
				// Leave summary going back
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
			if (PrevSnap.CurrentPageIndex == 0)
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

		/// <summary>
		/// Synchronizes UI boolean flags from current navigation state (temporary bridging method until full refactor removes legacy flags).
		/// </summary>
		private void NotifyNavigationChanged()
		{
			this.OnPropertyChanged(nameof(this.IsInSummary));
			this.OnPropertyChanged(nameof(this.Progress));
		}

		public override async Task GoBack()
		{
			if (this.editingFromSummary)
			{
				int FirstInvalid = await this.GetFirstInvalidVisiblePageIndexAsync();
				if (FirstInvalid >= 0)
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
			// Delegates to unified IKycService
			if (this.CurrentPage is null) return false;
			bool ok = await this.kycService.ValidatePageAsync(this.CurrentPage);
			if (ok && this.process is not null)
			{
				IEnumerable<ObservableKycField> fields = this.CurrentPage.VisibleFields;
				if (this.CurrentPageSections is not null)
					fields = fields.Concat(this.CurrentPageSections.SelectMany(s => s.VisibleFields));
				foreach (ObservableKycField f in fields)
					this.process.Values[f.Id] = f.StringValue;
			}
			MainThread.BeginInvokeOnMainThread(this.NextCommand.NotifyCanExecuteChanged);
			return ok;
		}

		private async Task<int> GetFirstInvalidVisiblePageIndexAsync()
		{
			// Delegates to unified IKycService
			if (this.process is null) return -1;
			return await this.kycService.GetFirstInvalidVisiblePageIndexAsync(this.process);
		}

		[RelayCommand]
		private async Task ExecuteApplyAsync()
		{
			if (this.applicationSent) return;
			if (!await AreYouSure(ServiceRef.Localizer[nameof(AppResources.AreYouSureYouWantToSendThisIdApplication)])) return;
			if (!await App.AuthenticateUserAsync(AuthenticationPurpose.SignApplication, true)) return;
			if (this.attachments is null || this.mappedValues is null) return;
			bool hasIdWithKey = ServiceRef.TagProfile.LegalIdentity is not null && await ServiceRef.XmppService.HasPrivateKey(ServiceRef.TagProfile.LegalIdentity.Id);
			(bool succeeded, LegalIdentity? added) = await ServiceRef.NetworkService.TryRequest(() => ServiceRef.XmppService.AddLegalIdentity(this.mappedValues.ToArray(), !hasIdWithKey, this.attachments.ToArray()));
			if (succeeded && added is not null)
			{
				await ServiceRef.TagProfile.SetIdentityApplication(added, true);
				this.applicationSent = true;
				if (this.kycReference is not null)
				{
					try { await this.kycService.ApplySubmissionAsync(this.kycReference, added); }
					catch (Exception ex) { ServiceRef.LogService.LogException(ex); }
				}
				foreach (LegalIdentityAttachment localAttachment in this.attachments)
				{
					Attachment? match = added.Attachments.FirstOrDefault(a => string.Equals(a.FileName, localAttachment.FileName, StringComparison.OrdinalIgnoreCase));
					if (match != null && localAttachment.Data is not null && localAttachment.ContentType is not null)
					{
						await ServiceRef.AttachmentCacheService.Add(match.Url, added.Id, true, localAttachment.Data, localAttachment.ContentType);
					}
				}
				this.applicationId = added.Id;
				this.ApplicationSentPublic = true;
				this.NrReviews = ServiceRef.TagProfile.NrReviews;
				await this.LoadApplicationAttributes();
				await this.LoadFeaturedPeerReviewers();
				int anchorAfterApply = this.currentPageIndex >= 0 ? this.currentPageIndex : this.navigation.AnchorPageIndex;
				this.navigation = this.navigation with { State = KycFlowState.Summary, AnchorPageIndex = anchorAfterApply, CurrentPageIndex = anchorAfterApply >= 0 ? anchorAfterApply : this.navigation.CurrentPageIndex };
				this.NotifyNavigationChanged();
			}
		}

		private async Task LoadApplicationAttributes()
		{
			try
			{
				IdApplicationAttributesEventArgs e = await ServiceRef.XmppService.GetIdApplicationAttributes();
				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.PeerReview = e.PeerReview;
					this.NrReviewers = e.NrReviewers;
				});
			}
			catch (Exception ex) { ServiceRef.LogService.LogException(ex); }
		}

		private async Task LoadFeaturedPeerReviewers()
		{
			await ServiceRef.NetworkService.TryRequest(async () =>
			{
				this.peerReviewServices = await ServiceRef.XmppService.GetServiceProvidersForPeerReviewAsync();
				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.HasFeaturedPeerReviewers = (this.peerReviewServices?.Length ?? 0) > 0;
				});
			});
		}

		private async Task XmppService_IdentityApplicationChanged(object? sender, LegalIdentityEventArgs e)
		{
			await MainThread.InvokeOnMainThreadAsync(async () =>
			{
				this.ApplicationSentPublic = ServiceRef.TagProfile.IdentityApplication is not null;
				this.NrReviews = ServiceRef.TagProfile.NrReviews;
				if (this.kycReference is not null && this.kycReference.CreatedIdentityId == e.Identity.Id)
				{
					try { await this.kycService.ApplySubmissionAsync(this.kycReference, e.Identity); } catch (Exception ex) { ServiceRef.LogService.LogException(ex); }
					if (e.Identity.State == IdentityState.Approved)
					{
						await base.GoBack();
						return;
					}
					else if (e.Identity.State == IdentityState.Rejected)
					{
						this.ApplicationSentPublic = false;
						await this.BuildMappedValuesAsync();
						int anchorRejected = this.currentPageIndex >= 0 ? this.currentPageIndex : this.navigation.AnchorPageIndex;
						this.navigation = this.navigation with { State = KycFlowState.Summary, AnchorPageIndex = anchorRejected, CurrentPageIndex = anchorRejected >= 0 ? anchorRejected : this.navigation.CurrentPageIndex };
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
			});
		}

		[RelayCommand(CanExecute = nameof(ApplicationSentPublic))]
		private async Task ScanQrCode()
		{
			string? url = await Services.UI.QR.QrCode.ScanQrCode(nameof(AppResources.QrPageTitleScanPeerId), [Constants.UriSchemes.IotId]);
			if (string.IsNullOrEmpty(url) || !Constants.UriSchemes.StartsWithIdScheme(url)) return;
			await this.SendPeerReviewRequest(Constants.UriSchemes.RemoveScheme(url));
		}

		private async Task SendPeerReviewRequest(string? reviewerId)
		{
			LegalIdentity? toReview = ServiceRef.TagProfile.IdentityApplication;
			if (toReview is null || string.IsNullOrEmpty(reviewerId)) return;
			try
			{
				await ServiceRef.XmppService.PetitionPeerReviewId(reviewerId, toReview, Guid.NewGuid().ToString(), ServiceRef.Localizer[nameof(AppResources.CouldYouPleaseReviewMyIdentityInformation)]);
				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.PetitionSent)], ServiceRef.Localizer[nameof(AppResources.APetitionHasBeenSentToYourPeer)]);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		[RelayCommand(CanExecute = nameof(CanRequestFeaturedPeerReviewer))]
		private async Task RequestReview()
		{
			if (this.peerReviewServices is null)
				await this.LoadFeaturedPeerReviewers();
			if ((this.peerReviewServices?.Length ?? 0) > 0)
			{
				List<ServiceProviderWithLegalId> list = [.. this.peerReviewServices, new RequestFromPeer()];
				ServiceProvidersNavigationArgs e = new(list.ToArray(), ServiceRef.Localizer[nameof(AppResources.RequestReview)], ServiceRef.Localizer[nameof(AppResources.SelectServiceProviderPeerReview)]);
				await ServiceRef.UiService.GoToAsync(nameof(ServiceProvidersPage), e, Services.UI.BackMethod.Pop);
				if (e.ServiceProvider is not null)
				{
					IServiceProvider? sp = await e.ServiceProvider.Task;
					if (sp is ServiceProviderWithLegalId spwl && !string.IsNullOrEmpty(spwl.LegalId))
					{
						if (!spwl.External)
						{
							if (!await ServiceRef.NetworkService.TryRequest(async () => await ServiceRef.XmppService.SelectPeerReviewService(sp.Id, sp.Type)))
								return;
						}
						await this.SendPeerReviewRequest(spwl.LegalId);
						return;
					}
					else if (sp is null) return;
				}
			}
			await this.ScanQrCode();
		}

		[RelayCommand(CanExecute = nameof(ApplicationSentPublic))]
		private async Task RevokeApplication()
		{
			LegalIdentity? app = ServiceRef.TagProfile.IdentityApplication;
			if (app is null)
			{
				this.ApplicationSentPublic = false;
				this.peerReviewServices = null;
				this.HasFeaturedPeerReviewers = false;
				if (this.kycReference is not null)
					await this.kycService.ClearSubmissionAsync(this.kycReference);
				return;
			}
			if (!await AreYouSure(ServiceRef.Localizer[nameof(AppResources.AreYouSureYouWantToRevokeTheCurrentIdApplication)])) return;
			if (!await App.AuthenticateUserAsync(AuthenticationPurpose.RevokeApplication, true)) return;
			try
			{
				await ServiceRef.XmppService.ObsoleteLegalIdentity(app.Id);
				await ServiceRef.TagProfile.SetIdentityApplication(null, true);
				this.ApplicationSentPublic = false;
				this.peerReviewServices = null;
				this.HasFeaturedPeerReviewers = false;
				if (this.kycReference is not null)
					await this.kycService.ClearSubmissionAsync(this.kycReference);
				await this.BuildMappedValuesAsync();
				int anchorAfterRevoke = this.currentPageIndex >= 0 ? this.currentPageIndex : this.navigation.AnchorPageIndex;
				this.navigation = this.navigation with { State = KycFlowState.Summary, AnchorPageIndex = anchorAfterRevoke, CurrentPageIndex = anchorAfterRevoke >= 0 ? anchorAfterRevoke : this.navigation.CurrentPageIndex };
				this.SetEditingFromSummary(false);
				this.NotifyNavigationChanged();
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		private Task BuildMappedValuesAsync()
		{
			// Delegates to unified IKycService
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
				// Preserve legacy additions (Jid, Phone, Email, Country, DeviceId)
				string? jid = ServiceRef.TagProfile.LegalIdentity?.Properties.FirstOrDefault(p => p.Name == Constants.XmppProperties.Jid)?.Value ?? ServiceRef.XmppService.BareJid ?? string.Empty;
				string? phone = ServiceRef.TagProfile.LegalIdentity?.Properties.FirstOrDefault(p => p.Name == Constants.XmppProperties.Phone)?.Value ?? ServiceRef.TagProfile.PhoneNumber ?? string.Empty;
				string? email = ServiceRef.TagProfile.LegalIdentity?.Properties.FirstOrDefault(p => p.Name == Constants.XmppProperties.EMail)?.Value ?? ServiceRef.TagProfile.EMail ?? string.Empty;
				this.mappedValues.Add(new Property(Constants.XmppProperties.DeviceId, ServiceRef.PlatformSpecific.GetDeviceId()));
				if (!this.process.HasMapping(Constants.XmppProperties.Jid)) this.mappedValues.Add(new Property(Constants.XmppProperties.Jid, jid));
				if (!this.process.HasMapping(Constants.XmppProperties.Phone)) this.mappedValues.Add(new Property(Constants.XmppProperties.Phone, phone));
				if (!this.process.HasMapping(Constants.XmppProperties.EMail)) this.mappedValues.Add(new Property(Constants.XmppProperties.EMail, email));
				if (!this.process.HasMapping(Constants.XmppProperties.Country) && !string.IsNullOrEmpty(ServiceRef.TagProfile.SelectedCountry))
					this.mappedValues.Add(new Property(Constants.XmppProperties.Country, ServiceRef.TagProfile.SelectedCountry));
				// New summary assembly via domain model
				ISet<string> invalid = KycSummary.BuildInvalidMappingSet(this.kycReference);
				KycSummaryModel model = KycSummary.Generate(this.process, this.mappedValues, this.attachments, invalid);
				MainThread.BeginInvokeOnMainThread(() => this.ApplySummaryModel(model));
			});
		}

		private void ApplySummaryModel(KycSummaryModel model)
		{
			// Rebuild observable collections from model (use Array.Empty to satisfy IReadOnlyList type expectations)
			this.PersonalInformationSummary = new ObservableCollection<DisplayQuad>(model.Get(KycSummary.Personal)?.Items ?? Array.Empty<DisplayQuad>());
			this.AddressInformationSummary = new ObservableCollection<DisplayQuad>(model.Get(KycSummary.Address)?.Items ?? Array.Empty<DisplayQuad>());
			this.AttachmentInformationSummary = new ObservableCollection<DisplayQuad>(model.Get(KycSummary.Attachments)?.Items ?? Array.Empty<DisplayQuad>());
			this.CompanyInformationSummary = new ObservableCollection<DisplayQuad>(model.Get(KycSummary.CompanyInfo)?.Items ?? Array.Empty<DisplayQuad>());
			this.CompanyAddressSummary = new ObservableCollection<DisplayQuad>(model.Get(KycSummary.CompanyAddress)?.Items ?? Array.Empty<DisplayQuad>());
			this.CompanyRepresentativeSummary = new ObservableCollection<DisplayQuad>(model.Get(KycSummary.CompanyRepresentative)?.Items ?? Array.Empty<DisplayQuad>());
			this.HasPersonalInformation = this.PersonalInformationSummary.Count > 0;
			this.HasAddressInformation = this.AddressInformationSummary.Count > 0;
			this.HasAttachments = this.AttachmentInformationSummary.Count > 0;
			this.HasCompanyInformation = this.CompanyInformationSummary.Count > 0;
			this.HasCompanyAddress = this.CompanyAddressSummary.Count > 0;
			this.HasCompanyRepresentative = this.CompanyRepresentativeSummary.Count > 0;
		}


		/// <summary>
		/// Builds a snapshot of the current process state including per-page and per-field validity.
		/// </summary>
		private KycProcessState BuildProcessState()
		{
			if (this.process is null)
				return new KycProcessState(Array.Empty<KycPageState>(), this.navigation, this.applicationSent);

			List<KycPageState> PageStates = new List<KycPageState>(this.process.Pages.Count);
			foreach (KycPage Page in this.process.Pages)
			{
				bool IsVisible = Page.IsVisible(this.process.Values);
				List<KycFieldState> FieldStates = new List<KycFieldState>();
				// Top-level fields
				foreach (ObservableKycField Field in Page.AllFields)
				{
					if (!Field.IsVisible) continue;
					string? Value = string.IsNullOrWhiteSpace(Field.StringValue) ? null : Field.StringValue;
					FieldStates.Add(new KycFieldState(Field.Id, Field.IsValid, Value));
				}
				// Section fields
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

		public async Task ApplyRejectionAsync(string message, string[] invalidClaims, string[] invalidPhotos, string? code)
		{
			this.ErrorDescription = message;
			this.HasErrorDescription = !string.IsNullOrWhiteSpace(message);
			if (this.kycReference is not null)
				await this.kycService.ApplyRejectionAsync(this.kycReference, message, invalidClaims, invalidPhotos, code);
			KycInvalidation.ApplyInvalidations(this.process, this.kycReference, this.ErrorDescription);
			await this.BuildMappedValuesAsync();
		}

	}
}
