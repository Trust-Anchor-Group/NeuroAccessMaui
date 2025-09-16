using System.Collections.ObjectModel;
using System.Globalization;
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
		public ObservableCollection<KycReference> Applications { get; } = new();
		public ObservableCollection<KycReference> AvailableApplications { get; } = new();

		public string BannerUriLight => ServiceRef.ThemeService.GetImageUri(Constants.Branding.BannerSmallLight);
		public string BannerUriDark => ServiceRef.ThemeService.GetImageUri(Constants.Branding.BannerSmallDark);
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

		public bool HasCurrentApplication => this.CurrentApplication is not null;

		// Expose loader state if you want to bind spinners/errors in XAML
		public ObservableTask<int> Loader { get; init; }
		public ObservableTask<int> AvailableLoader { get; init; }
		public bool IsLoading => this.Loader.IsRunning;
		public string? LoadError => this.Loader.ErrorMessage;

		public bool HasApplications => this.Applications.Count > 0; // legacy, not used by current UI

		public bool ShowProgressBar => this.CurrentApplication is not null
										&& (this.CurrentApplication.CreatedIdentityState is null
										|| this.CurrentApplication.CreatedIdentityState == IdentityState.Created);

		/// <summary>
		/// Creates an instance of the <see cref="ApplicationsViewModel"/> class.
		/// </summary>
		public ApplicationsViewModel()
			: base()
		{
			// Kick off first load. We also pass commands so they get NotifyCanExecuteChanged when state changes.
			this.Loader = new ObservableTaskBuilder()
				.Named("LoadApplications")
				.WithPolicy(Policies.Retry(3, (attempt, ex) => TimeSpan.FromMilliseconds(250 * attempt * attempt)))
				.WithTelemetry(new LoggerTelemetry()) // when you have a telemetry sink
				.UseTaskRun(false) // IO-bound work; keep default path
				.Run(this.LoadApplicationsAsync)
				.Build(this.CreateNewApplicationCommand, this.OpenApplicationCommand);

			this.AvailableLoader = new ObservableTaskBuilder()
				.Named("LoadAvailableApplications")
				.WithPolicy(Policies.Retry(3, (attempt, ex) => TimeSpan.FromMilliseconds(250 * attempt * attempt)))
				.WithTelemetry(new LoggerTelemetry())
				.UseTaskRun(false)
				.Run(this.LoadAvailableApplicationsAsync)
				.Build(this.CreateNewApplicationCommand);
		}

		protected override async Task OnInitialize()
		{
			if (ServiceRef.TagProfile.IdentityApplication is not null)
			{
				if (ServiceRef.TagProfile.IdentityApplication.IsDiscarded())
					await ServiceRef.TagProfile.SetIdentityApplication(null, true);
			}

			this.IdentityApplicationSent = ServiceRef.TagProfile.IdentityApplication is not null;

			this.HasLegalIdentity = ServiceRef.TagProfile.LegalIdentity is not null &&
				ServiceRef.TagProfile.LegalIdentity.State == IdentityState.Approved;

			ServiceRef.XmppService.IdentityApplicationChanged += this.XmppService_IdentityApplicationChanged;
			ServiceRef.XmppService.LegalIdentityChanged += this.XmppService_LegalIdentityChanged;
			ServiceRef.TagProfile.OnPropertiesChanged += this.TagProfile_OnPropertiesChanged;

			await base.OnInitialize();



			this.NotifyCommandsCanExecuteChanged();
		}

		protected override Task OnDispose()
		{
			ServiceRef.XmppService.IdentityApplicationChanged -= this.XmppService_IdentityApplicationChanged;
			ServiceRef.XmppService.LegalIdentityChanged -= this.XmppService_LegalIdentityChanged;
			ServiceRef.TagProfile.OnPropertiesChanged -= this.TagProfile_OnPropertiesChanged;

			// If desired, cancel any in-flight load.
			this.Loader.Cancel();

			return base.OnDispose();
		}

		private Task XmppService_IdentityApplicationChanged(object? Sender, LegalIdentityEventArgs e)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.IdentityApplicationSent = ServiceRef.TagProfile.IdentityApplication is not null;
			});

			return Task.CompletedTask;
		}

		private Task XmppService_LegalIdentityChanged(object Sender, LegalIdentityEventArgs e)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.HasLegalIdentity = ServiceRef.TagProfile.LegalIdentity is not null &&
					ServiceRef.TagProfile.LegalIdentity.State == IdentityState.Approved;
			});

			return Task.CompletedTask;
		}

		private void TagProfile_OnPropertiesChanged(object? sender, EventArgs e)
		{
			// no-op for now
		}

		protected override async Task OnAppearing()
		{
			await base.OnAppearing();

			// Ensure data reflects latest storage state each time page appears
			this.Loader.Reload();
			this.AvailableLoader.Reload();

			// Page is not correctly updated if changes has happened when viewing a sub-view. Fix by resending notification.
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

			// Optionally also surface loader commands in UI, so keep them fresh:
	//		this.Loader.CancelCommand.NotifyCanExecuteChanged();
	//		this.Loader.ReloadCommand.NotifyCanExecuteChanged();
	//		this.Loader.RefreshCommand.NotifyCanExecuteChanged();
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
		private async Task CreateNewApplication()
		{
			try
			{
				// Capture fields from the most recent application (current or latest previous)
				KycFieldValue[]? PreviousFields = null;

				if (this.CurrentApplication is not null)
				{
					try
					{
						if (this.CurrentApplication.Fields is not null)
						{
							PreviousFields = this.CurrentApplication.Fields
								.Select(F => new KycFieldValue(F.FieldId, F.Value))
								.ToArray();
						}

						if (!string.IsNullOrEmpty(this.CurrentApplication.CreatedIdentityId))
						{
							// Fetch latest state of the created identity
							LegalIdentity Identity = await ServiceRef.XmppService.GetLegalIdentity(this.CurrentApplication.CreatedIdentityId);
							if (Identity.State == IdentityState.Created)
							{
								// Confirm and authenticate for revoking/obsoleting application
								if (!await App.AuthenticateUserAsync(AuthenticationPurpose.RevokeApplication, true))
									return;

								await ServiceRef.XmppService.ObsoleteLegalIdentity(Identity.Id);
							}
						}

						await ServiceRef.TagProfile.SetIdentityApplication(null, true);
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
				KycReference Ref = await ServiceRef.KycService.LoadKycReferenceAsync(Language);

				// Ensure a clean start: reset resume and sent-application flags on the new reference
				Ref.LastVisitedMode = "Form";
				Ref.LastVisitedPageId = null;
				Ref.RejectionMessage = null;
				Ref.RejectionCode = null;
				Ref.InvalidClaims = null;
				Ref.InvalidPhotos = null;
				Ref.InvalidClaimDetails = null;
				Ref.InvalidPhotoDetails = null;
				Ref.CreatedIdentityId = null;
				Ref.CreatedIdentityState = null;
				Ref.UpdatedUtc = DateTime.UtcNow;
				try { await ServiceRef.KycService.SaveKycReferenceAsync(Ref); } catch (Exception Ex) { ServiceRef.LogService.LogException(Ex); }

				// Apply previous field values to the new reference, if any
				if (PreviousFields is not null && PreviousFields.Length > 0)
				{
					Ref.Fields = PreviousFields;
					Ref.UpdatedUtc = DateTime.UtcNow;
					try
					{
						await ServiceRef.KycService.SaveKycReferenceAsync(Ref);
						// Ensure cached process instance reflects the newly assigned fields
						await Ref.ApplyFieldsToProcessAsync(Language);
					}
					catch (Exception Ex)
					{
						ServiceRef.LogService.LogException(Ex);
					}
				}

				await ServiceRef.UiService.GoToAsync(nameof(KycProcessPage), new KycProcessNavigationArgs(Ref));
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

				if (!string.IsNullOrEmpty(Item.CreatedIdentityId))
				{
					LegalIdentity Identity = await ServiceRef.XmppService.GetLegalIdentity(Item.CreatedIdentityId);
					if (Identity.State == IdentityState.Approved)
					{
						// Preview in review/approved identity
						await ServiceRef.UiService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(Identity));
					}
					else
					{
						// Rejected or other states: allow editing in KYC
						await ServiceRef.UiService.GoToAsync(nameof(KycProcessPage), new KycProcessNavigationArgs(Item));
					}
				}
				else
				{
					// Open KYC process to resume
					await ServiceRef.UiService.GoToAsync(nameof(KycProcessPage), new KycProcessNavigationArgs(Item));
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

		#endregion

		#region Loading (refactored to ObservableTask)

		/// <summary>
		/// Factory for the loader. Uses generation & cancellation from TaskContext,
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

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.Applications.Clear();
					this.CurrentApplication = Latest;
					if (Latest is not null)
						this.Applications.Add(Latest);
				});


				Progress.Report(100);

				// Final notify
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.OnPropertyChanged(nameof(this.HasCurrentApplication));
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

				IReadOnlyList<KycReference> Refs = Array.Empty<KycReference>();
				try
				{
					string Language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
					Ct.ThrowIfCancellationRequested();
					Refs = await ServiceRef.KycService.LoadAvailableKycReferencesAsync(Language);
				}
				catch (OperationCanceledException) { throw; }
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}

				Ct.ThrowIfCancellationRequested();

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.AvailableApplications.Clear();
					foreach (KycReference r in Refs)
						this.AvailableApplications.Add(r);
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
