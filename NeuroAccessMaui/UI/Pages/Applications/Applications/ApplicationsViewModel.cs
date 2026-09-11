using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EDaler;
using Microsoft.Maui.ApplicationModel;
using NeuroAccessMaui;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Authentication;
using NeuroAccessMaui.Services.Kyc;
using NeuroAccessMaui.Services.Kyc.Models;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.Wallet;
using NeuroAccessMaui.Telemetry;
using NeuroAccessMaui.UI;
using NeuroAccessMaui.UI.MVVM; // ObservableTask
using NeuroAccessMaui.UI.MVVM.Building;
using NeuroAccessMaui.UI.MVVM.Policies;
using NeuroAccessMaui.UI.Pages.Applications.ApplyId;
using NeuroAccessMaui.UI.Pages.Contracts;
using NeuroAccessMaui.UI.Pages.Identity.ViewIdentity;
using NeuroAccessMaui.UI.Pages.Kyc;
using NeuroAccessMaui.UI.Pages.Main;
using NeuroAccessMaui.UI.Pages.Main.Apps;
using NeuroAccessMaui.UI.Pages.Wallet;
using NeuroAccessMaui.UI.Pages.Wallet.BuyEDaler;
using NeuroAccessMaui.UI.Pages.Wallet.MyWallet;
using NeuroAccessMaui.UI.Pages.Wallet.RequestPayment;
using NeuroAccessMaui.UI.Pages.Wallet.ServiceProviders;
using NeuroFeatures;
using NeuroFeatures.EventArguments;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Contracts.EventArguments;
using Waher.Persistence;

namespace NeuroAccessMaui.UI.Pages.Applications.Applications
{
	/// <summary>
	/// The view model to bind to for when displaying the applications page.
	/// </summary>
	public partial class ApplicationsViewModel : XmppViewModel
	{
		private const int availableTemplatesPageSize = 10; // Unified page size for available applications pagination
		private KycApplicationPage? availableApplicationsPage;

		/// <summary>
		/// Gets the locally persisted KYC application references displayed on the page.
		/// </summary>
		public ObservableCollection<KycReference> Applications { get; } = new ObservableCollection<KycReference>();

		/// <summary>
		/// Gets the available KYC application templates the user can start.
		/// </summary>
		public ObservableCollection<KycApplicationTemplate> AvailableApplications { get; } = new ObservableCollection<KycApplicationTemplate>();

		/// <summary>
		/// Gets the light theme banner image URI.
		/// </summary>
		public string BannerUriLight => ServiceRef.ThemeService.GetImageUri(Constants.Branding.BannerSmallLight);

		/// <summary>
		/// Gets the dark theme banner image URI.
		/// </summary>
		public string BannerUriDark => ServiceRef.ThemeService.GetImageUri(Constants.Branding.BannerSmallDark);

		/// <summary>
		/// Gets the banner image URI for the current application theme.
		/// </summary>
		public string BannerUri =>
			Application.Current?.UserAppTheme switch
			{
				AppTheme.Dark => this.BannerUriDark,
				AppTheme.Light => this.BannerUriLight,
				_ => this.BannerUriLight
			} ?? this.BannerUriLight;
		// Single current application (0 or 1)
		[ObservableProperty]
		private KycReference? currentApplication;

		[ObservableProperty]
		private bool hasMoreAvailableTemplates;

		/// <summary>
		/// Gets a value indicating whether there is a current KYC application.
		/// </summary>
		public bool HasCurrentApplication => this.CurrentApplication is not null;

		/// <summary>
		/// Gets a value indicating whether another page of application templates can be loaded.
		/// </summary>
		public bool CanLoadMoreAvailableApplications => this.CanExecuteCommands && this.HasMoreAvailableTemplates;

		/// <summary>
		/// Gets the loader for locally persisted applications.
		/// </summary>
		public ObservableTask<int> Loader { get; init; }

		/// <summary>
		/// Gets the loader for available remote or fallback application templates.
		/// </summary>
		public ObservableTask<int> AvailableLoader { get; init; }

		/// <summary>
		/// Gets a value indicating whether local application loading is running.
		/// </summary>
		public bool IsLoading => this.Loader.IsRunning;

		/// <summary>
		/// Gets the local application loading error message, if any.
		/// </summary>
		public string? LoadError => this.Loader.ErrorMessage;

		/// <summary>
		/// Gets a value indicating whether locally persisted applications are available.
		/// </summary>
		public bool HasApplications => this.Applications.Count > 0; // legacy, not used by current UI

		/// <summary>
		/// Gets a value indicating whether current application progress should be shown.
		/// </summary>
		public bool ShowProgressBar => this.CurrentApplication is not null
									&& (this.CurrentApplication.GetEffectiveApplicationIdentityState() is null
									|| this.CurrentApplication.GetEffectiveApplicationIdentityState() == IdentityState.Created);

		partial void OnCurrentApplicationChanged(KycReference? Value)
		{
			this.OnPropertyChanged(nameof(this.HasCurrentApplication));
			this.OnPropertyChanged(nameof(this.ShowProgressBar));
		}

		partial void OnHasMoreAvailableTemplatesChanged(bool Value)
		{
			this.LoadMoreAvailableApplicationsCommand.NotifyCanExecuteChanged();
		}

		/// <summary>
		/// Creates an instance of the <see cref="ApplicationsViewModel"/> class.
		/// </summary>
		public ApplicationsViewModel()
			: base()
		{
			// Disable auto-start to avoid immediate generation superseding reload in OnAppearing.
			this.Loader = new ObservableTaskBuilder()
				.Named("LoadApplications")
				.AutoStart(false)
				.WithPolicy(Policies.Retry(3, (attempt, ex) => TimeSpan.FromMilliseconds(250 * attempt * attempt)))
				.WithTelemetry(new LoggerTelemetry())
				.UseTaskRun(false)
				.Run(this.LoadApplicationsAsync)
				.Build(this.CreateNewApplicationCommand, this.OpenApplicationCommand);

			this.AvailableLoader = new ObservableTaskBuilder()
				.Named("LoadAvailableApplications")
				.AutoStart(false)
				.WithPolicy(Policies.Retry(3, (attempt, ex) => TimeSpan.FromMilliseconds(250 * attempt * attempt)))
				.WithTelemetry(new LoggerTelemetry())
				.UseTaskRun(false)
				.Run(this.LoadAvailableApplicationsAsync)
				.Build(this.CreateNewApplicationCommand, this.LoadMoreAvailableApplicationsCommand);
		}

		public override async Task OnInitializeAsync()
		{
			this.IdentityApplicationSent = ServiceRef.TagProfile.IdentityApplication is not null;

			this.HasLegalIdentity = ServiceRef.TagProfile.LegalIdentity is not null &&
				ServiceRef.TagProfile.LegalIdentity.State == IdentityState.Approved;

			ServiceRef.XmppService.IdentityApplicationChanged += this.XmppService_IdentityApplicationChanged;
			ServiceRef.XmppService.LegalIdentityChanged += this.XmppService_LegalIdentityChanged;
			ServiceRef.TagProfile.OnPropertiesChanged += this.TagProfile_OnPropertiesChanged;

			await base.OnInitializeAsync();

			this.NotifyCommandsCanExecuteChanged();
		}

		public override Task OnDisposeAsync()
		{
			ServiceRef.XmppService.IdentityApplicationChanged -= this.XmppService_IdentityApplicationChanged;
			ServiceRef.XmppService.LegalIdentityChanged -= this.XmppService_LegalIdentityChanged;
			ServiceRef.TagProfile.OnPropertiesChanged -= this.TagProfile_OnPropertiesChanged;

			// If desired, cancel any in-flight load.
			this.Loader.Cancel();

			return base.OnDisposeAsync();
		}

		private Task XmppService_IdentityApplicationChanged(object? Sender, LegalIdentityEventArgs e)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.IdentityApplicationSent = ServiceRef.TagProfile.IdentityApplication is not null;
				this.Loader.Reload();
			});

			return Task.CompletedTask;
		}

		private Task XmppService_LegalIdentityChanged(object Sender, LegalIdentityEventArgs e)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.HasLegalIdentity = ServiceRef.TagProfile.LegalIdentity is not null &&
					ServiceRef.TagProfile.LegalIdentity.State == IdentityState.Approved;
				this.Loader.Reload();
			});

			return Task.CompletedTask;
		}

		private void TagProfile_OnPropertiesChanged(object? sender, EventArgs e)
		{
			// no-op for now
		}

		public override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();

			this.Loader.Run();
			this.AvailableLoader.Run();
			this.HasLegalIdentity = ServiceRef.TagProfile.LegalIdentity?.State == IdentityState.Approved;

			// Page is not correctly updated if changes happened when viewing a sub-view. Fix by resending notification.
			bool IdApplicationSent = ServiceRef.TagProfile.IdentityApplication is not null;
			if (this.IdentityApplicationSent != IdApplicationSent)
				this.IdentityApplicationSent = IdApplicationSent;
			else
				this.OnPropertyChanged(nameof(this.IdentityApplicationSent));
		}

		/// <inheritdoc/>
		protected override Task XmppService_ConnectionStateChanged(object? Sender, XmppState NewState)
		{
			return MainThread.InvokeOnMainThreadAsync(async () =>
			{
				await base.XmppService_ConnectionStateChanged(Sender, NewState);
				this.NotifyCommandsCanExecuteChanged();
			});
		}

		/// <inheritdoc/>
		public override void SetIsBusy(bool IsBusy)
		{
			base.SetIsBusy(IsBusy);
			this.NotifyCommandsCanExecuteChanged();
		}

		private void NotifyCommandsCanExecuteChanged()
		{
			this.CreateNewApplicationCommand.NotifyCanExecuteChanged();
			this.OpenApplicationCommand.NotifyCanExecuteChanged();
			this.LoadMoreAvailableApplicationsCommand.NotifyCanExecuteChanged();
		}

		#region Properties

		/// <summary>
		/// Used to find out if a command can execute
		/// </summary>
		public bool CanExecuteCommands => !this.IsBusy;

		/// <summary>
		/// If an identity application has been sent.
		/// </summary>
		[ObservableProperty]
		private bool identityApplicationSent;

		/// <summary>
		/// If the user has an approved legal identity.
		/// </summary>
		[ObservableProperty]
		private bool hasLegalIdentity;

		#endregion

		#region Commands

		[RelayCommand(CanExecute = nameof(CanExecuteCommands))]
		private async Task CreateNewApplication(KycApplicationTemplate? Template)
		{
			try
			{
				KycFieldValue[]? PreviousFields = null;

				if (this.CurrentApplication is not null)
				{
					try
					{
						bool RemoveApplicationAttachments = true;
						if (this.CurrentApplication.Fields is not null)
						{
							PreviousFields = this.CurrentApplication.Fields
								.Select(F => new KycFieldValue(F.FieldId, F.Value))
								.ToArray();
						}

						string? ActiveApplicationIdentityId = this.CurrentApplication.GetActiveApplicationIdentityId();
						if (!string.IsNullOrEmpty(ActiveApplicationIdentityId))
						{
							LegalIdentity Identity = await ServiceRef.XmppService.GetLegalIdentity(ActiveApplicationIdentityId);
							if (Identity.State == IdentityState.Created)
							{
								IAuthenticationService Auth = ServiceRef.Provider.GetRequiredService<IAuthenticationService>();

								if (!await Auth.AuthenticateUserAsync(AuthenticationPurpose.RevokeApplication, true))
									return;

								await ServiceRef.XmppService.ObsoleteLegalIdentity(Identity.Id);
							}
							else if (Identity.IsApproved())
							{
								LegalIdentity? CurrentLegalIdentity = ServiceRef.TagProfile.LegalIdentity;
								if (CurrentLegalIdentity is null ||
									!CurrentLegalIdentity.IsApproved() ||
									string.Equals(CurrentLegalIdentity.Id, Identity.Id, StringComparison.OrdinalIgnoreCase))
								{
									await ServiceRef.TagProfile.SetLegalIdentity(Identity, true);
								}

								RemoveApplicationAttachments = false;
							}
						}

						await ServiceRef.TagProfile.SetIdentityApplication(null, RemoveApplicationAttachments);
						await Database.Delete(this.CurrentApplication);
						await Database.Provider.Flush();
					}
					catch (Exception Ex)
					{
						ServiceRef.LogService.LogException(Ex);
					}
				}
				else
				{
					// No current application: try to find the latest previous draft and reuse its fields
					try
					{
						IEnumerable<KycReference> All = await Database.Find<KycReference>();
						KycReference? LatestWithFields = All
							.OrderByDescending(r => r.UpdatedUtc)
							.FirstOrDefault(r => r.Fields is not null && r.Fields.Length > 0);

						if (LatestWithFields?.Fields is not null)
						{
							PreviousFields = LatestWithFields.Fields
								.Select(F => new KycFieldValue(F.FieldId, F.Value))
								.ToArray();
						}
					}
					catch (Exception Ex)
					{
						ServiceRef.LogService.LogException(Ex);
					}
				}

				string Language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
				KycApplicationTemplate? TemplateToUse = Template ?? this.AvailableApplications.FirstOrDefault();
				KycReference Ref = await ServiceRef.KycService.LoadKycReferenceAsync(Language, TemplateToUse);

				await ServiceRef.KycService.PrepareReferenceForNewApplicationAsync(Ref, Language, PreviousFields);

				await this.OpenKycEntryAsync(Ref, Language, true);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		[RelayCommand(CanExecute = nameof(CanLoadMoreAvailableApplications))]
		private async Task LoadMoreAvailableApplications()
		{
			if (!this.CanLoadMoreAvailableApplications)
				return;

			try
			{
				if (this.availableApplicationsPage is null || string.IsNullOrWhiteSpace(this.availableApplicationsPage.NextAfter))
					return;

				string Language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
				string After = this.availableApplicationsPage.NextAfter;
				KycApplicationPage Page = await ServiceRef.KycService.LoadKycApplicationsPageAsync(After, null, null, availableTemplatesPageSize, Language).ConfigureAwait(false);

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					foreach (KycApplicationTemplate Template in Page.Templates)
					{
						bool Exists = this.AvailableApplications.Any(existing => TemplatesEqual(existing, Template));
						if (!Exists)
							this.AvailableApplications.Add(Template);
					}

					this.availableApplicationsPage = Page;
					// Only allow more if we received a full page, the page indicates more, and it's not fallback.
					this.HasMoreAvailableTemplates = !Page.UsedFallback && Page.Templates.Count == availableTemplatesPageSize && Page.HasMoreAfter;
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		[RelayCommand]
		private async Task RemoveApplication(KycReference Item)
		{
			try
			{
				if (Item is null)
					return;

				await Database.Delete(Item);
				await Database.Provider.Flush();

				this.Loader.Refresh();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		[RelayCommand(CanExecute = nameof(CanExecuteCommands))]
		private async Task OpenApplication(KycReference Item)
		{
			try
			{
				if (Item is null)
					return;

				IdentityState? EffectiveApplicationState = Item.GetEffectiveApplicationIdentityState();
				if (EffectiveApplicationState is not null)
				{
					IdentityState? State = EffectiveApplicationState;
					if (State == IdentityState.Approved)
					{
						LegalIdentity? CurrentLegalIdentity = ServiceRef.TagProfile.LegalIdentity;
						if (CurrentLegalIdentity?.IsApproved() == true)
						{
							await ServiceRef.NavigationService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(CurrentLegalIdentity));
							return;
						}

						string? ActiveApplicationIdentityId = Item.GetActiveApplicationIdentityId();
						if (string.IsNullOrEmpty(ActiveApplicationIdentityId))
						{
							await ServiceRef.NavigationService.GoToAsync(nameof(KycProcessPage), new KycProcessNavigationArgs(Item));
							return;
						}

						LegalIdentity? Identity = await ServiceRef.XmppService.GetLegalIdentity(ActiveApplicationIdentityId);
						await ServiceRef.NavigationService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(Identity));
					}
					else if (State == IdentityState.Created ||
						State == IdentityState.Rejected ||
						State == IdentityState.Obsoleted ||
						State == IdentityState.Compromised)
					{
						await ServiceRef.NavigationService.GoToAsync(nameof(KycApplicationStatusPage), new KycProcessNavigationArgs(Item));
					}
					else
					{
						// Unknown states: allow editing in KYC.
						await ServiceRef.NavigationService.GoToAsync(nameof(KycProcessPage), new KycProcessNavigationArgs(Item));
					}
				}
				else
				{
					// Open KYC process to resume
					string Language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
					await this.OpenKycEntryAsync(Item, Language, false);
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		// Optional: surface loader controls via your VM if you want to bind to buttons/gestures.
		[RelayCommand]
		private void RefreshApplications() => this.Loader.Refresh();

		[RelayCommand]
		private void ReloadApplications() => this.Loader.Reload();

		[RelayCommand]
		private void CancelLoading() => this.Loader.Cancel();

		private async Task OpenKycEntryAsync(KycReference Reference, string Language, bool PreferDocumentChip)
		{
			if (await this.ShouldOpenDocumentChipFirstAsync(Reference, Language, PreferDocumentChip))
			{
				await ServiceRef.NavigationService.GoToAsync(nameof(KycTravelDocumentPage), new KycProcessNavigationArgs(Reference));
				return;
			}

			await ServiceRef.NavigationService.GoToAsync(nameof(KycProcessPage), new KycProcessNavigationArgs(Reference));
		}

		private async Task<bool> ShouldOpenDocumentChipFirstAsync(KycReference Reference, string Language, bool PreferDocumentChip)
		{
			if (!ServiceRef.Provider.GetRequiredService<NeuroAccessMaui.Services.Nfc.INfcIsoDepSessionService>().IsPlatformSupported)
			{
				return false;
			}

			KycProcess? Process = await Reference.GetProcess(Language);
			bool HasNfcPolicy = Process is not null &&
				Process.ApplicationPolicy.Mode == KycApplicationMode.Preview &&
				Process.EvidencePolicy.TravelDocument.Nfc.Enabled;
			if (!HasNfcPolicy)
				return false;

			if (Reference.HasFullTravelDocumentMrz ||
				!string.IsNullOrWhiteSpace(Reference.NfcReadoutXml))
			{
				return false;
			}

			if (PreferDocumentChip)
				return true;

			return Reference.Fields is null ||
				!Reference.Fields.Any(Field => !string.IsNullOrWhiteSpace(Field.Value));
		}

		private static bool TemplatesEqual(KycApplicationTemplate First, KycApplicationTemplate Second)
		{
			string? FirstId = First.Source?.ItemId;
			string? SecondId = Second.Source?.ItemId;

			if (!string.IsNullOrEmpty(FirstId) && !string.IsNullOrEmpty(SecondId))
				return string.Equals(FirstId, SecondId, System.StringComparison.Ordinal);

			if (First.Source is null && Second.Source is null &&
				!string.IsNullOrEmpty(First.Reference.KycXml) &&
				!string.IsNullOrEmpty(Second.Reference.KycXml))
			{
				return string.Equals(First.Reference.KycXml, Second.Reference.KycXml, System.StringComparison.Ordinal);
			}

			return false;
		}

		#endregion

		#region Loading (refactored to ObservableTask)

		/// <summary>
		/// Factory for the loader. Uses generation &amp; cancellation from TaskContext,
		/// reports simple progress, and performs UI changes on the main thread.
		/// </summary>
		private async Task LoadApplicationsAsync(TaskContext<int> ctx)
		{
			CancellationToken Ct = ctx.CancellationToken;
			IProgress<int> Progress = ctx.Progress;

			try
			{
				Progress.Report(0);

				// Clear existing (UI thread)
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.Applications.Clear();
					this.OnPropertyChanged(nameof(this.HasApplications));
				});

				// 1) Drafts (local). We show at most one (latest) current application.
				IEnumerable<KycReference> Refs = Array.Empty<KycReference>();
				try
				{
					// Database.Find<T>() doesn't accept CT directly; ensure we respect CT around the call.
					Ct.ThrowIfCancellationRequested();
					Refs = await Database.Find<KycReference>();
				}
				catch (OperationCanceledException) { throw; }
				catch (Exception Ex)
				{
					// Non-fatal; log and continue
					ServiceRef.LogService.LogException(Ex);
				}

				Ct.ThrowIfCancellationRequested();

				KycReference? Latest = Refs.OrderByDescending(r => r.UpdatedUtc).FirstOrDefault();
				LegalIdentity? ApprovedIdentity = ServiceRef.TagProfile.LegalIdentity;
				if (Latest is not null &&
					ApprovedIdentity?.State == IdentityState.Approved &&
					Latest.MatchesIdentityId(ApprovedIdentity.Id) &&
					!Latest.IsReservedPreviewIdentity(ApprovedIdentity.Id) &&
					!Latest.IsPreviewIdentity(ApprovedIdentity.Id) &&
					(Latest.GetEffectiveApplicationIdentityState() is null or IdentityState.Created))
				{
					await ServiceRef.KycService.UpdateSubmissionStateAsync(Latest, ApprovedIdentity);
				}

				Ct.ThrowIfCancellationRequested();

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.Applications.Clear();
					this.CurrentApplication = Latest;
					// KycReference does not notify bindings when its identity state changes in place.
					this.OnPropertyChanged(nameof(this.CurrentApplication));
					this.OnPropertyChanged(nameof(this.ShowProgressBar));
					if (Latest is not null)
						this.Applications.Add(Latest);
				});


				Progress.Report(100);

				// Final notify
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.OnPropertyChanged(nameof(this.HasApplications));
					// If you want command states to react to loading completion:
					this.NotifyCommandsCanExecuteChanged();
				});
			}
			catch (OperationCanceledException)
			{
				// Let ObservableTask mark as Canceled; avoid extra UI updates here.
				throw;
			}
			catch (Exception Ex)
			{
				// ObservableTask will capture/log, but keep behavior consistent.
				ServiceRef.LogService.LogException(Ex);
				throw;
			}
		}

		private async Task LoadAvailableApplicationsAsync(TaskContext<int> ctx)
		{
			CancellationToken Ct = ctx.CancellationToken;
			IProgress<int> Progress = ctx.Progress;

			try
			{
				Progress.Report(0);

				string Language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
				Ct.ThrowIfCancellationRequested();
				KycApplicationPage Page = await ServiceRef.KycService.LoadKycApplicationsPageAsync(null, null, 0, availableTemplatesPageSize, Language, Ct).ConfigureAwait(false);

				Ct.ThrowIfCancellationRequested();

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.AvailableApplications.Clear();
					foreach (KycApplicationTemplate Template in Page.Templates)
						this.AvailableApplications.Add(Template);
					this.availableApplicationsPage = Page;
					// Only show Load More if full page, remote (not fallback), and server hints more.
					this.HasMoreAvailableTemplates = !Page.UsedFallback && Page.Templates.Count == availableTemplatesPageSize && Page.HasMoreAfter;
				});

				Progress.Report(100);
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				throw;
			}
		}

		private static string GetTextOrFallback(string key, string fallback)
		{
			Microsoft.Extensions.Localization.LocalizedString L = ServiceRef.Localizer[key, false];
			return L.ResourceNotFound ? fallback : L.Value;
		}

		private static string GetIdentityStateText(IdentityState state)
		{
			Microsoft.Extensions.Localization.LocalizedString L = ServiceRef.Localizer["IdentityState_" + state.ToString(), false];
			return L.ResourceNotFound ? state.ToString() : L.Value;
		}

		#endregion
	}
}
