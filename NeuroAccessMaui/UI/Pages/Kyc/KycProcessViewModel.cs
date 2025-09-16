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
using NeuroAccessMaui.Services.UI.Photos;
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
	public partial class KycProcessViewModel : BaseViewModel, IDisposable
	{
		private bool disposedValue;
		private readonly KycProcessNavigationArgs? navigationArguments;
		private KycProcess? process;
		private KycReference? kycReference;
		private int currentPageIndex = 0;
		private bool applicationSent;
		private string? applicationId;

		private List<Property> mappedValues;
		private List<LegalIdentityAttachment> attachments;
		private ServiceProviderWithLegalId[]? peerReviewServices = null;

		[ObservableProperty] private bool shouldViewSummary;
		[ObservableProperty] private bool shouldReturnToSummary;
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
		private CancellationTokenSource? pageRefreshCts;
		private static readonly TimeSpan PageRefreshThrottle = TimeSpan.FromMilliseconds(250);

		public string BannerUriLight => ServiceRef.ThemeService.GetImageUri(Constants.Branding.BannerSmallLight);
		public string BannerUriDark => ServiceRef.ThemeService.GetImageUri(Constants.Branding.BannerSmallDark);

		public string BannerUri => (Application.Current?.RequestedTheme ?? AppTheme.Light) switch
		{
			AppTheme.Dark => this.BannerUriDark,
			AppTheme.Light => this.BannerUriLight,
			_ => this.BannerUriLight
		};

		public ObservableCollection<KycPage> Pages => this.process is not null ? this.process.Pages : [];

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
				if (this.ShouldViewSummary)
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
				this.ApplyInvalidationsToFieldsFromReference();
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
					// There are invalid fields -> go to first invalid page but mark that user should return to summary when fixed
					this.ShouldReturnToSummary = true;
					this.ShouldViewSummary = false;
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
					this.ShouldViewSummary = true;
					this.ShouldReturnToSummary = false;
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
				this.ShouldViewSummary = true;
				this.ShouldReturnToSummary = false;
				this.NextButtonText = ServiceRef.Localizer["Kyc_Apply"].Value;
			}
			else
			{
				// Normal form resume
				this.currentPageIndex = resumeIndex >= 0 ? resumeIndex : this.GetFirstVisibleIndex();
				this.CurrentPagePosition = this.currentPageIndex;
				this.SetCurrentPage(this.currentPageIndex);
			}

			this.NextButtonText = this.ShouldViewSummary ? ServiceRef.Localizer["Kyc_Apply"].Value : ServiceRef.Localizer["Kyc_Next"].Value;
			MainThread.BeginInvokeOnMainThread(this.NextCommand.NotifyCanExecuteChanged);
			if (pending)
			{
				await this.BuildMappedValuesAsync();
				this.ShouldViewSummary = true;
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
			if (this.ShouldViewSummary && this.summaryAnchorPageIndex < 0)
				this.summaryAnchorPageIndex = this.currentPageIndex; // currentPageIndex already set to anchor when summary built

			this.ShouldViewSummary = false;
			this.ShouldReturnToSummary = true; // Allow quick return to summary
			this.currentPageIndex = targetIndex;
			this.CurrentPagePosition = targetIndex;
			this.SetCurrentPage(targetIndex);
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
				this.ShouldViewSummary = true;
				this.OnPropertyChanged(nameof(this.Progress));
			}
			this.OnPropertyChanged(nameof(this.CanRequestFeaturedPeerReviewer));
			this.OnPropertyChanged(nameof(this.FeaturedPeerReviewers));
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
					if (byName.TryGetValue(map.Key, out string value) && !string.IsNullOrWhiteSpace(value))
					{
						field.StringValue = value;
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

		private void SchedulePageRefresh()
		{
			CancellationTokenSource? old;
			lock (this)
			{
				old = this.pageRefreshCts;
				this.pageRefreshCts = new CancellationTokenSource();
			}
			try { old?.Cancel(); } catch { }
			CancellationToken token = this.pageRefreshCts.Token;
			_ = Task.Run(async () =>
			{
				try
				{
					await Task.Delay(PageRefreshThrottle, token);
					if (token.IsCancellationRequested) return;
					await MainThread.InvokeOnMainThreadAsync(() =>
					{
						this.SetCurrentPage(this.currentPageIndex);
						this.NextCommand.NotifyCanExecuteChanged();
					});
				}
				catch (TaskCanceledException) { }
				catch (Exception ex) { ServiceRef.LogService.LogException(ex); }
			});
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
			if (!this.ShouldViewSummary && this.kycReference is not null)
			{
				this.kycReference.LastVisitedPageId = page.Id;
				this.kycReference.LastVisitedMode = "Form";
				this.UpdateReference();
				_ = this.SaveReferenceToStorageAsync();
			}
			this.OnPropertyChanged(nameof(this.Progress));
			this.NextCommand.NotifyCanExecuteChanged();
		}

		private void UpdateReference()
		{
			if (this.process is null || this.kycReference is null) return;
			this.kycReference.Fields = [.. this.process.Values.Select(p => new KycFieldValue(p.Key, p.Value))];
			this.kycReference.Progress = this.Progress;
			this.kycReference.UpdatedUtc = DateTime.UtcNow;
		}

		private async Task SaveReferenceToStorageAsync()
		{
			if (this.kycReference is null) return;
			await ServiceRef.KycService.SaveKycReferenceAsync(this.kycReference);
		}

		private int GetNextIndex()
		{
			if (this.process is null) return -1;
			int start = this.currentPageIndex + 1;
			while (start < this.Pages.Count && !this.Pages[start].IsVisible(this.process.Values)) start++;
			return start;
		}

		private int GetPreviousIndex()
		{
			if (this.process is null) return -1;
			int start = this.currentPageIndex - 1;
			while (start >= 0 && !this.Pages[start].IsVisible(this.process.Values)) start--;
			return start;
		}

		private bool CanExecuteNext()
		{
			// Allow Apply button on summary if application not yet sent
			if (this.ShouldViewSummary)
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
			bool ok = await this.ValidateCurrentPageAsync();
			if (!ok) return;
			if (this.kycReference is not null)
			{
				this.kycReference.LastVisitedPageId = this.CurrentPage?.Id;
				this.kycReference.LastVisitedMode = "Form";
			}
			this.UpdateReference();
			await this.SaveReferenceToStorageAsync();
			if (this.ShouldReturnToSummary)
			{
				int firstInvalid = await this.GetFirstInvalidVisiblePageIndexAsync();
				if (firstInvalid >= 0)
				{
					this.currentPageIndex = firstInvalid;
					this.CurrentPagePosition = firstInvalid;
					this.SetCurrentPage(firstInvalid);
					this.NextButtonText = ServiceRef.Localizer["Kyc_Next"].Value;
					this.ScrollUp();
					return;
				}
				await this.BuildMappedValuesAsync();
				this.ScrollUp();
				this.ShouldViewSummary = true;
				this.ShouldReturnToSummary = false;
				this.OnPropertyChanged(nameof(this.Progress));
				this.NextButtonText = ServiceRef.Localizer["Kyc_Apply"].Value;
				this.UpdateReference();
				await this.SaveReferenceToStorageAsync();
				return;
			}
			int next = this.GetNextIndex();
			if (next < this.Pages.Count)
			{
				this.currentPageIndex = next;
				this.CurrentPagePosition = next;
				this.SetCurrentPage(this.currentPageIndex);
				this.ScrollUp();
			}
			else
			{
				if (this.ShouldViewSummary)
					await this.ExecuteApplyAsync();
				else
				{
					await this.BuildMappedValuesAsync();
					this.ScrollUp();
					this.ShouldViewSummary = true;
					this.OnPropertyChanged(nameof(this.Progress));
					if (this.kycReference is not null)
						this.kycReference.LastVisitedMode = "Summary";
					this.UpdateReference();
					await this.SaveReferenceToStorageAsync();
					this.NextButtonText = ServiceRef.Localizer["Kyc_Apply"].Value;
				}
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
			if (!this.disposedValue)
			{
				if (disposing)
				{
					try { this.pageRefreshCts?.Cancel(); this.pageRefreshCts?.Dispose(); } catch { }
					ServiceRef.XmppService.IdentityApplicationChanged -= this.XmppService_IdentityApplicationChanged;
				}
				this.disposedValue = true;
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		[RelayCommand]
		private async Task GoToSummaryAsync()
		{
			bool ok = await this.ValidateCurrentPageAsync();
			if (!ok) return;
			this.UpdateReference();
			await this.SaveReferenceToStorageAsync();
			await this.BuildMappedValuesAsync();

			// If returning from editing a field selected from summary, restore anchor index
			if (this.ShouldReturnToSummary && this.summaryAnchorPageIndex >= 0 && this.summaryAnchorPageIndex < this.Pages.Count)
			{
				this.currentPageIndex = this.summaryAnchorPageIndex;
			}
			else
			{
				// First time entering summary (store anchor)
				if (this.summaryAnchorPageIndex < 0)
					this.summaryAnchorPageIndex = this.currentPageIndex;
			}

			if (this.currentPageIndex >= 0)
			{
				this.CurrentPagePosition = this.currentPageIndex;
				this.SetCurrentPage(this.currentPageIndex);
			}
			this.ScrollUp();
			this.ShouldViewSummary = true;
			this.ShouldReturnToSummary = false;
			if (this.kycReference is not null)
			{
				this.kycReference.LastVisitedMode = "Summary";
				this.UpdateReference();
				await this.SaveReferenceToStorageAsync();
			}
			this.OnPropertyChanged(nameof(this.Progress));
			this.NextButtonText = ServiceRef.Localizer["Kyc_Apply"].Value;
		}

		private async Task ExecutePrevious()
		{
			if (this.ShouldViewSummary)
			{
				this.ScrollUp();
				this.ShouldViewSummary = false;
				// Restore anchor page when leaving summary via back button
				if (this.summaryAnchorPageIndex >= 0 && this.summaryAnchorPageIndex < this.Pages.Count)
				{
					this.currentPageIndex = this.summaryAnchorPageIndex;
					this.CurrentPagePosition = this.currentPageIndex;
					this.SetCurrentPage(this.currentPageIndex);
				}
				this.NextButtonText = ServiceRef.Localizer["Kyc_Next"].Value;
				this.OnPropertyChanged(nameof(this.Progress));
				return;
			}
			this.UpdateReference();
			await this.SaveReferenceToStorageAsync();
			int prev = this.GetPreviousIndex();
			if (prev >= 0)
			{
				this.ScrollUp();
				this.currentPageIndex = prev;
				this.CurrentPagePosition = prev;
				this.SetCurrentPage(this.currentPageIndex);
				await this.ValidateCurrentPageAsync();
			}
			else
			{
				await base.GoBack();
			}
		}

		public override async Task GoBack()
		{
			if (this.ShouldReturnToSummary)
			{
				int firstInvalid = await this.GetFirstInvalidVisiblePageIndexAsync();
				if (firstInvalid >= 0)
				{
					this.ShouldViewSummary = false;
					this.currentPageIndex = firstInvalid;
					this.CurrentPagePosition = firstInvalid;
					this.SetCurrentPage(firstInvalid);
					this.NextButtonText = ServiceRef.Localizer["Kyc_Next"].Value;
				}
				else
				{
					await this.GoToSummaryAsync();
				}
				return;
			}
			await this.ExecutePrevious();
		}

		[RelayCommand]
		public async Task Exit()
		{
			this.UpdateReference();
			await this.SaveReferenceToStorageAsync();
			if (!await AreYouSure(ServiceRef.Localizer[nameof(AppResources.Kyc_Exit)])) return;
			await base.GoBack();
		}

		private async Task<bool> ValidateCurrentPageAsync()
		{
			if (this.CurrentPage is null) return false;
			bool ok = true;
			IEnumerable<ObservableKycField> fields = this.CurrentPage.VisibleFields;
			if (this.CurrentPageSections is not null)
				fields = fields.Concat(this.CurrentPageSections.SelectMany(s => s.VisibleFields));
			List<Task> tasks = new();
			foreach (ObservableKycField f in fields)
			{
				// Ensure immediate sync validation (bypassing debounce) before async part
				f.ForceSynchronousValidation();
				f.ValidationTask.Run();
				Task t = MainThread.InvokeOnMainThreadAsync(async () =>
				{
					await f.ValidationTask.WaitAllAsync();
					if (!f.IsValid) ok = false;
				});
				tasks.Add(t);
			}
			await Task.WhenAll(tasks);
			if (ok && this.process is not null)
			{
				foreach (ObservableKycField f in fields)
					this.process.Values[f.Id] = f.StringValue;
			}
			MainThread.BeginInvokeOnMainThread(this.NextCommand.NotifyCanExecuteChanged);
			return ok;
		}

		private async Task<int> GetFirstInvalidVisiblePageIndexAsync()
		{
			if (this.process is null) return -1;
			for (int i = 0; i < this.Pages.Count; i++)
			{
				KycPage page = this.Pages[i];
				if (!page.IsVisible(this.process.Values)) continue;
				IEnumerable<ObservableKycField> fields = page.VisibleFields;
				ReadOnlyObservableCollection<KycSection> sections = page.VisibleSections;
				if (sections is not null)
					fields = fields.Concat(sections.SelectMany(s => s.VisibleFields));
				bool ok = true;
				List<Task> tasks = new();
				foreach (ObservableKycField f in fields)
				{
					f.ForceSynchronousValidation();
					f.ValidationTask.Run();
					Task t = MainThread.InvokeOnMainThreadAsync(async () =>
					{
						await f.ValidationTask.WaitAllAsync();
						if (!f.IsValid) ok = false;
					});
					tasks.Add(t);
				}
				await Task.WhenAll(tasks);
				if (!ok) return i;
			}
			return -1;
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
				try
				{
					if (this.kycReference is not null)
					{
						this.kycReference.CreatedIdentityId = added.Id;
						this.kycReference.CreatedIdentityState = added.State;
						this.kycReference.UpdatedUtc = DateTime.UtcNow;
						await ServiceRef.KycService.SaveKycReferenceAsync(this.kycReference);
					}
				}
				catch (Exception ex) { ServiceRef.LogService.LogException(ex); }
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
				this.ShouldViewSummary = true;
				this.OnPropertyChanged(nameof(this.Progress));
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
					this.kycReference.CreatedIdentityState = e.Identity.State;
					try { await ServiceRef.KycService.SaveKycReferenceAsync(this.kycReference); } catch (Exception ex) { ServiceRef.LogService.LogException(ex); }
					if (e.Identity.State == IdentityState.Approved)
					{
						await base.GoBack();
						return;
					}
					else if (e.Identity.State == IdentityState.Rejected)
					{
						this.ApplicationSentPublic = false;
						await this.BuildMappedValuesAsync();
						this.ShouldViewSummary = true;
						this.ShouldReturnToSummary = false;
						if (this.kycReference is not null)
						{
							this.kycReference.LastVisitedMode = "Summary";
							this.UpdateReference();
							await this.SaveReferenceToStorageAsync();
						}
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
				{
					this.kycReference.CreatedIdentityId = null;
					this.kycReference.CreatedIdentityState = null;
					this.kycReference.UpdatedUtc = DateTime.UtcNow;
					await ServiceRef.KycService.SaveKycReferenceAsync(this.kycReference);
				}
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
				{
					this.kycReference.CreatedIdentityId = null;
					this.kycReference.CreatedIdentityState = null;
					this.kycReference.UpdatedUtc = DateTime.UtcNow;
					await ServiceRef.KycService.SaveKycReferenceAsync(this.kycReference);
				}
				await this.BuildMappedValuesAsync();
				this.ShouldViewSummary = true;
				this.OnPropertyChanged(nameof(this.Progress));
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		private Task BuildMappedValuesAsync()
		{
			this.mappedValues = new();
			this.attachments = new();
			if (this.process is null) return Task.CompletedTask;
			return this.BuildMappedValuesInternalAsync();
		}

		private async Task BuildMappedValuesInternalAsync()
		{
			if (this.process is null) return;
			CancellationToken ct = CancellationToken.None;
			foreach (KycPage page in this.process.Pages)
			{
				if (!page.IsVisible(this.process.Values)) continue;
				foreach (ObservableKycField field in page.VisibleFields)
				{
					if (this.CheckAndHandleFile(field, this.attachments)) continue;
					foreach (Property prop in await this.BuildPropertiesFromFieldAsync(field, ct))
						this.mappedValues.Add(prop);
				}
				foreach (KycSection section in page.AllSections)
				{
					foreach (ObservableKycField field in section.VisibleFields)
					{
						if (this.CheckAndHandleFile(field, this.attachments)) continue;
						foreach (Property prop in await this.BuildPropertiesFromFieldAsync(field, ct))
							this.mappedValues.Add(prop);
					}
				}
			}
			string? jid = ServiceRef.TagProfile.LegalIdentity?.Properties.FirstOrDefault(p => p.Name == Constants.XmppProperties.Jid)?.Value ?? ServiceRef.XmppService.BareJid ?? string.Empty;
			string? phone = ServiceRef.TagProfile.LegalIdentity?.Properties.FirstOrDefault(p => p.Name == Constants.XmppProperties.Phone)?.Value ?? ServiceRef.TagProfile.PhoneNumber ?? string.Empty;
			string? email = ServiceRef.TagProfile.LegalIdentity?.Properties.FirstOrDefault(p => p.Name == Constants.XmppProperties.EMail)?.Value ?? ServiceRef.TagProfile.EMail ?? string.Empty;
			this.mappedValues.Add(new Property(Constants.XmppProperties.DeviceId, ServiceRef.PlatformSpecific.GetDeviceId()));
			if (!this.process.HasMapping(Constants.XmppProperties.Jid)) this.mappedValues.Add(new Property(Constants.XmppProperties.Jid, jid));
			if (!this.process.HasMapping(Constants.XmppProperties.Phone)) this.mappedValues.Add(new Property(Constants.XmppProperties.Phone, phone));
			if (!this.process.HasMapping(Constants.XmppProperties.EMail)) this.mappedValues.Add(new Property(Constants.XmppProperties.EMail, email));
			if (!this.process.HasMapping(Constants.XmppProperties.Country) && !string.IsNullOrEmpty(ServiceRef.TagProfile.SelectedCountry))
				this.mappedValues.Add(new Property(Constants.XmppProperties.Country, ServiceRef.TagProfile.SelectedCountry));
			this.GenerateSummaryCollection();
		}

		private async Task<List<Property>> BuildPropertiesFromFieldAsync(ObservableKycField field, CancellationToken ct)
		{
			List<Property> result = new();
			if (field is null || this.process is null || field.Mappings.Count == 0) return result;
			if (field.Condition is not null && !field.Condition.Evaluate(this.process.Values)) return result;
			string baseValue = field.StringValue?.Trim() ?? string.Empty;
			if (string.IsNullOrEmpty(baseValue)) return result;
			foreach (KycMapping map in field.Mappings)
			{
				if (string.IsNullOrEmpty(map.Key)) continue;
				string current = baseValue;
				foreach (string name in map.TransformNames)
				{
					if (string.IsNullOrWhiteSpace(name)) continue;
					if (NeuroAccessMaui.Services.Kyc.Transforms.KycTransformRegistry.TryGet(name, out NeuroAccessMaui.Services.Kyc.Transforms.IKycTransform t))
					{
						try { current = await t.ApplyAsync(field, this.process, current, ct); }
						catch (Exception ex) { ServiceRef.LogService.LogException(ex); }
					}
					if (string.IsNullOrEmpty(current)) break;
				}
				if (!string.IsNullOrEmpty(current)) result.Add(new Property(map.Key, current));
			}
			return result;
		}

		private bool CheckAndHandleFile(ObservableKycField field, List<LegalIdentityAttachment> list)
		{
			if (field.Condition is not null && (field.Mappings.Count == 0 || !field.Condition.Evaluate(this.process!.Values)))
				return false;
			if (!string.IsNullOrEmpty(field.StringValue) && field is ObservableImageField imageField)
			{
				list.Add(new LegalIdentityAttachment(imageField.Mappings.First().Key + ".jpg", Constants.MimeTypes.Jpeg, CompressImage(new MemoryStream(Convert.FromBase64String(imageField.StringValue)))!));
				return true;
			}
			return false;
		}

		private static byte[]? CompressImage(Stream inputStream)
		{
			try
			{
				using SKManagedStream ms = new(inputStream);
				using SKData imgData = SKData.Create(ms);
				using SKCodec codec = SKCodec.Create(imgData);
				SKBitmap bmp = SKBitmap.Decode(imgData);
				bmp = HandleOrientation(bmp, codec.EncodedOrigin);
				bool resize = false;
				int h = bmp.Height; int w = bmp.Width;
				if (w >= h && w > 1920) { h = (int)(h * (1920.0 / w) + 0.5); w = 1920; resize = true; }
				else if (h > w && h > 1920) { w = (int)(w * (1920.0 / h) + 0.5); h = 1920; resize = true; }
				if (resize)
				{
					SKImageInfo info = bmp.Info;
					SKImageInfo ni = new(w, h, info.ColorType, info.AlphaType, info.ColorSpace);
					SKBitmap? resized = bmp.Resize(ni, SKFilterQuality.High);
					if (resized is not null) { bmp.Dispose(); bmp = resized; }
				}
				byte[] bytes;
				using (SKData encoded = bmp.Encode(SKEncodedImageFormat.Jpeg, 80)) bytes = encoded.ToArray();
				bmp.Dispose();
				return bytes;
			}
			catch (Exception ex) { ServiceRef.LogService.LogException(ex); return null; }
		}

		private static SKBitmap HandleOrientation(SKBitmap bmp, SKEncodedOrigin o)
		{
			SKBitmap rotated;
			switch (o)
			{
				case SKEncodedOrigin.BottomRight:
					rotated = new SKBitmap(bmp.Width, bmp.Height);
					using (SKCanvas s = new(rotated)) { s.RotateDegrees(180, bmp.Width / 2, bmp.Height / 2); s.DrawBitmap(bmp, 0, 0); }
					break;
				case SKEncodedOrigin.RightTop:
					rotated = new SKBitmap(bmp.Height, bmp.Width);
					using (SKCanvas s = new(rotated)) { s.Translate(rotated.Width, 0); s.RotateDegrees(90); s.DrawBitmap(bmp, 0, 0); }
					break;
				case SKEncodedOrigin.LeftBottom:
					rotated = new SKBitmap(bmp.Height, bmp.Width);
					using (SKCanvas s = new(rotated)) { s.Translate(0, rotated.Height); s.RotateDegrees(270); s.DrawBitmap(bmp, 0, 0); }
					break;
				default: return bmp;
			}
			return rotated;
		}

		private void GenerateSummaryCollection()
		{
			this.PersonalInformationSummary = new ObservableCollection<DisplayQuad>();
			this.AddressInformationSummary = new ObservableCollection<DisplayQuad>();
			this.AttachmentInformationSummary = new ObservableCollection<DisplayQuad>();
			this.CompanyInformationSummary = new ObservableCollection<DisplayQuad>();
			this.CompanyAddressSummary = new ObservableCollection<DisplayQuad>();
			this.CompanyRepresentativeSummary = new ObservableCollection<DisplayQuad>();
			if (this.process is null) return;
			ISet<string> invalid = this.BuildInvalidMappingSetFromReference();
			IdentitySummaryFormatter.KycSummaryResult summary = IdentitySummaryFormatter.BuildKycSummaryFromProperties(this.mappedValues, this.process, this.attachments.Select(a => new IdentitySummaryFormatter.AttachmentInfo(a.FileName ?? string.Empty, a.ContentType)), CultureInfo.CurrentCulture, invalid);
			foreach (DisplayQuad q in summary.Personal) this.PersonalInformationSummary.Add(q);
			this.HasPersonalInformation = this.PersonalInformationSummary.Count > 0;
			foreach (DisplayQuad q in summary.Address) this.AddressInformationSummary.Add(q);
			this.HasAddressInformation = this.AddressInformationSummary.Count > 0;
			foreach (DisplayQuad q in summary.Attachments) this.AttachmentInformationSummary.Add(q);
			this.HasAttachments = this.AttachmentInformationSummary.Count > 0;
			foreach (DisplayQuad q in summary.CompanyInfo) this.CompanyInformationSummary.Add(q);
			this.HasCompanyInformation = this.CompanyInformationSummary.Count > 0;
			foreach (DisplayQuad q in summary.CompanyAddress) this.CompanyAddressSummary.Add(q);
			this.HasCompanyAddress = this.CompanyAddressSummary.Count > 0;
			foreach (DisplayQuad q in summary.CompanyRepresentative) this.CompanyRepresentativeSummary.Add(q);
			this.HasCompanyRepresentative = this.CompanyRepresentativeSummary.Count > 0;
		}

		private ISet<string> BuildInvalidMappingSetFromReference()
		{
			HashSet<string> invalid = new(StringComparer.OrdinalIgnoreCase);
			if (this.kycReference is null) return invalid;
			foreach (string claim in this.kycReference.InvalidClaims ?? Array.Empty<string>())
			{
				if (string.IsNullOrWhiteSpace(claim)) continue; string key = claim.Trim(); invalid.Add(key);
				if (key.Equals(Constants.XmppProperties.BirthDay, StringComparison.OrdinalIgnoreCase) || key.Equals(Constants.XmppProperties.BirthMonth, StringComparison.OrdinalIgnoreCase) || key.Equals(Constants.XmppProperties.BirthYear, StringComparison.OrdinalIgnoreCase) || key.Equals(Constants.CustomXmppProperties.BirthDate, StringComparison.OrdinalIgnoreCase)) invalid.Add(Constants.CustomXmppProperties.BirthDate);
				if (key.StartsWith("ORGREP", StringComparison.OrdinalIgnoreCase)) invalid.Add("ORGREPBDATE");
			}
			foreach (string photo in this.kycReference.InvalidPhotos ?? Array.Empty<string>())
			{
				if (string.IsNullOrWhiteSpace(photo)) continue; invalid.Add(photo.Trim());
			}
			return invalid;
		}

		private void ApplyInvalidationsToFieldsFromReference()
		{
			if (this.process is null || this.kycReference is null) return;
			IEnumerable<string> invalidClaims = this.kycReference.InvalidClaims ?? Array.Empty<string>();
			if (!invalidClaims.Any()) return;
			HashSet<string> invalidSet = new(invalidClaims, StringComparer.OrdinalIgnoreCase);
			Dictionary<string, string> reasons = this.BuildInvalidReasonsByMappingFromReference();
			foreach (KycPage page in this.process.Pages)
			{
				foreach (ObservableKycField field in page.AllFields)
				{
					if (field.Mappings.Any(m => invalidSet.Contains(m.Key) || this.IsGroupedDateMatch(m.Key, invalidSet)))
					{
						field.IsValid = false;
						string? reason = field.Mappings.Select(m => reasons.TryGetValue(m.Key, out string r) ? r : null).FirstOrDefault(r => r is not null);
						field.ValidationText = !string.IsNullOrWhiteSpace(reason) ? reason : (this.ErrorDescription ?? string.Empty);
					}
				}
				foreach (KycSection section in page.AllSections)
				{
					foreach (ObservableKycField field in section.AllFields)
					{
						if (field.Mappings.Any(m => invalidSet.Contains(m.Key) || this.IsGroupedDateMatch(m.Key, invalidSet)))
						{
							field.IsValid = false;
							string? reason = field.Mappings.Select(m => reasons.TryGetValue(m.Key, out string r) ? r : null).FirstOrDefault(r => r is not null);
							field.ValidationText = !string.IsNullOrWhiteSpace(reason) ? reason : (this.ErrorDescription ?? string.Empty);
						}
					}
				}
			}
		}

		private Dictionary<string, string> BuildInvalidReasonsByMappingFromReference()
		{
			Dictionary<string, string> map = new(StringComparer.OrdinalIgnoreCase);
			if (this.kycReference is null) return map;
			foreach (KycInvalidClaim c in this.kycReference.InvalidClaimDetails ?? Array.Empty<KycInvalidClaim>())
			{
				if (c is null || string.IsNullOrWhiteSpace(c.Claim)) continue;
				string key = c.Claim.Trim(); string reason = string.IsNullOrWhiteSpace(c.Reason) ? this.ErrorDescription ?? string.Empty : c.Reason; map[key] = reason;
				if (key.Equals(Constants.XmppProperties.BirthDay, StringComparison.OrdinalIgnoreCase) || key.Equals(Constants.XmppProperties.BirthMonth, StringComparison.OrdinalIgnoreCase) || key.Equals(Constants.XmppProperties.BirthYear, StringComparison.OrdinalIgnoreCase) || key.Equals("BDATE", StringComparison.OrdinalIgnoreCase)) map[Constants.CustomXmppProperties.BirthDate] = reason;
				if (key.StartsWith("ORGREP", StringComparison.OrdinalIgnoreCase)) map["ORGREPBDATE"] = reason;
			}
			foreach (KycInvalidPhoto p in this.kycReference.InvalidPhotoDetails ?? Array.Empty<KycInvalidPhoto>())
			{
				if (p is null) continue; string key = string.IsNullOrWhiteSpace(p.Mapping) ? (p.FileName ?? string.Empty) : p.Mapping; if (string.IsNullOrWhiteSpace(key)) continue; string reason = string.IsNullOrWhiteSpace(p.Reason) ? this.ErrorDescription ?? string.Empty : p.Reason; map[key.Trim()] = reason;
			}
			return map;
		}

		private bool IsGroupedDateMatch(string mappingKey, ISet<string> invalidSet)
		{
			if (mappingKey.Equals(Constants.XmppProperties.BirthDay, StringComparison.OrdinalIgnoreCase) || mappingKey.Equals(Constants.XmppProperties.BirthMonth, StringComparison.OrdinalIgnoreCase) || mappingKey.Equals(Constants.XmppProperties.BirthYear, StringComparison.OrdinalIgnoreCase))
				return invalidSet.Contains("BDATE") || invalidSet.Contains(Constants.XmppProperties.BirthDay) || invalidSet.Contains(Constants.XmppProperties.BirthMonth) || invalidSet.Contains(Constants.XmppProperties.BirthYear);
			if (mappingKey.StartsWith("ORGREP", StringComparison.OrdinalIgnoreCase))
				return invalidSet.Contains("ORGREPBDATE") || invalidSet.Contains("ORGREPBDAY") || invalidSet.Contains("ORGREPBMONTH") || invalidSet.Contains("ORGREPBYEAR");
			return false;
		}

		public async Task ApplyRejectionAsync(string message, string[] invalidClaims, string[] invalidPhotos, string? code)
		{
			this.ErrorDescription = message;
			this.HasErrorDescription = !string.IsNullOrWhiteSpace(message);
			if (this.kycReference is not null)
			{
				this.kycReference.RejectionMessage = message;
				this.kycReference.RejectionCode = code;
				this.kycReference.InvalidClaims = invalidClaims;
				this.kycReference.InvalidPhotos = invalidPhotos;
				this.kycReference.UpdatedUtc = DateTime.UtcNow;
				await ServiceRef.KycService.SaveKycReferenceAsync(this.kycReference);
			}
			this.ApplyInvalidationsToFieldsFromReference();
			await MainThread.InvokeOnMainThreadAsync(this.GenerateSummaryCollection);
		}

		private int summaryAnchorPageIndex = -1; // Page index used when returning to summary & for back navigation
	}
}
