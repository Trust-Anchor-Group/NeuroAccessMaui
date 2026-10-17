using NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels;
using NeuroAccessMaui.Services.Contracts;
using NeuroAccessMaui.Services.Notification;
using Waher.Networking.XMPP.Contracts;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Resources.Languages;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Services.UI;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.UI.Popups.QR;
using NeuroAccessMaui.UI.MVVM;
using CommunityToolkit.Maui.Core.Extensions;
using NeuroAccessMaui.Models;
using NeuroAccessMaui.Services.UI.QR;
using NeuroAccessMaui.Services.Wallet.Tokens;

namespace NeuroAccessMaui.UI.Pages.Contracts.MyContracts
{
	/// <summary>
	/// View model responsible for presenting and filtering a list of contracts and templates.
	/// In the contracts list, contracts are grouped as an inbox: what needs the user's action, what is in progress, what is
	/// signed and what has ended. Templates are listed newest first, one row per title, with older versions folded under the
	/// newest.
	/// </summary>
	public partial class MyContractsViewModel : BaseViewModel, IDisposable
	{
		private const int ReloadDelayMs = 300;
		private const int GroupPageSize = 10;
		private const int PickerBatchSize = 50;
		private const int MaxTemplates = 500;
		private const int TemplatePlaceholderDelayMs = 200;
		private const int UpToDateDisplayMs = 1500;

		private static readonly ContractListGroup[] groupOrder =
		[
			ContractListGroup.NeedsAction,
			ContractListGroup.InProgress,
			ContractListGroup.Signed,
			ContractListGroup.Ended
		];

		private readonly ContractsListMode contractsListMode;
		private readonly TaskCompletionSource<Contract?>? selection;
		private readonly ContractPickerFilter? pickerFilter;
		private readonly bool isInbox;
		private readonly Dictionary<ContractListGroup, ContractGroupModel> groups = [];
		private Contract? selectedContract = null;
		private readonly int contractBatchSize = 10;
		private readonly List<TemplateGroupModel> allTemplates = [];
		private readonly HashSet<string> knownTemplateIds = new(StringComparer.OrdinalIgnoreCase);

		private readonly SemaphoreSlim loadLock = new(1, 1);
		private CancellationTokenSource? reloadCancellation;
		private CancellationTokenSource? petitionReloadCancellation;
		private INotificationServiceV2? notificationService;
		private bool isSyncRefreshRunning;
		private bool isWaitingForSync;
		private bool isLoaded;
		private bool isSubscribed;
		private bool disposed;
		private bool templatesLoaded;
		private bool isTemplatePlaceholderDue;
		private bool showTemplatesUpToDate;

		private int loadedContracts;
		private string searchText = string.Empty;

		/// <summary>
		/// Remaining items threshold for incremental loading. 0 enables loading, -1 disables.
		/// </summary>
		[ObservableProperty]
		private int hasMore = 0; // 0 means collectionView will load more when scrolles. -1 means event wont be fired.

		/// <summary>
		/// Gets or sets whether template sharing via QR is available in the current mode.
		/// </summary>
		[ObservableProperty]
		private bool canShareTemplate;

		/// <summary>
		/// If the list is being refreshed with a pull to refresh.
		/// </summary>
		[ObservableProperty]
		private bool isRefreshing;

		/// <summary>
		/// Creates an instance of the <see cref="MyContractsViewModel"/> class.
		/// </summary>
		/// <param name="Args">Navigation arguments.</param>
		public MyContractsViewModel(MyContractsNavigationArgs? Args)
		{
			this.IsBusy = true;
			this.Action = SelectContractAction.ViewContract;

			this.loadedContracts = 0;
			this.hasMore = 0;

			if (Args is not null)
			{
				this.contractsListMode = Args.Mode;
				this.Action = Args.Action;
				this.selection = Args.Selection;
				this.pickerFilter = Args.Filter;

				switch (this.contractsListMode)
				{
					case ContractsListMode.Contracts:
						this.Title = ServiceRef.Localizer[nameof(AppResources.Contracts)];
						this.Description = ServiceRef.Localizer[nameof(AppResources.ContractsInfoText)];
						this.CanShareTemplate = false;
						break;

					case ContractsListMode.ContractTemplates:
						this.Title = ServiceRef.Localizer[nameof(AppResources.ContractTemplates)];
						this.Description = ServiceRef.Localizer[nameof(AppResources.ContractTemplatesInfoText)];
						this.CanShareTemplate = true;
						break;

					case ContractsListMode.TokenCreationTemplates:
						this.Title = ServiceRef.Localizer[nameof(AppResources.TokenCreationTemplates)];
						this.Description = ServiceRef.Localizer[nameof(AppResources.TokenCreationTemplatesInfoText)];
						this.CanShareTemplate = true;
						break;
				}
			}

			this.isInbox = this.contractsListMode == ContractsListMode.Contracts && this.Action == SelectContractAction.ViewContract;

			this.groups[ContractListGroup.NeedsAction] = new ContractGroupModel(ContractListGroup.NeedsAction, ServiceRef.Localizer[nameof(AppResources.ContractGroupNeedsAction)]);
			this.groups[ContractListGroup.InProgress] = new ContractGroupModel(ContractListGroup.InProgress, ServiceRef.Localizer[nameof(AppResources.ContractGroupInProgress)]);
			this.groups[ContractListGroup.Signed] = new ContractGroupModel(ContractListGroup.Signed, ServiceRef.Localizer[nameof(AppResources.ContractGroupSigned)]);
			this.groups[ContractListGroup.Ended] = new ContractGroupModel(ContractListGroup.Ended, ServiceRef.Localizer[nameof(AppResources.ContractGroupEnded)]);
		}

		/// <inheritdoc/>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			this.IsBusy = true;
			this.ShowContractsMissing = false;

			if (!this.isSubscribed)
			{
				ServiceRef.ContractStore.ContractChanged += this.ContractStore_ContractChanged;

				if (this.isInbox)
				{
					this.notificationService = ServiceRef.Provider.GetRequiredService<INotificationServiceV2>();
					this.notificationService.OnNotificationAdded += this.NotificationService_OnNotificationAdded;
				}

				this.isSubscribed = true;
			}

			if (this.isInbox)
			{
				bool IsEmpty = true;

				try
				{
					await this.LoadInboxAsync();
					IsEmpty = await MainThread.InvokeOnMainThreadAsync(() => this.Groups.Count == 0);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}

				// A list that is empty waits for the sync before it says there are no contracts.
				_ = this.SyncOnOpenAsync(IsEmpty);

				if (!IsEmpty)
					await this.ShowLoadedAsync();
			}
			else if (this.IsTemplateList)
			{
				// Templates on the phone show at once. Placeholder rows only show if they take a moment, and templates for
				// creating tokens that are missing are looked for afterwards, while the list can be used.
				bool LooksForTemplates = this.contractsListMode == ContractsListMode.TokenCreationTemplates;

				await MainThread.InvokeOnMainThreadAsync(() => { this.IsCheckingTemplates = LooksForTemplates; });
				_ = this.ShowTemplatePlaceholderWhenSlowAsync();

				try
				{
					await this.LoadTemplatesAsync();
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.templatesLoaded = true;
					this.UpdateTemplateState();
				});

				await this.ShowLoadedAsync();

				if (LooksForTemplates)
					_ = this.DownloadMissingTemplatesAsync();
			}
			else
			{
				await this.LoadContracts();
				await this.ShowLoadedAsync();
			}
		}

		/// <inheritdoc/>
		public override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();

			if (this.selection is not null && this.selection.Task.IsCompleted)
			{
				await this.GoBack();
				return;
			}

			if (this.isInbox && this.isLoaded)
				this.SchedulePetitionReload();   // A request may have been answered on its own page.
		}

		/// <inheritdoc/>
		public override async Task OnDisposeAsync()
		{
			if (this.isSubscribed)
			{
				ServiceRef.ContractStore.ContractChanged -= this.ContractStore_ContractChanged;

				if (this.notificationService is not null)
					this.notificationService.OnNotificationAdded -= this.NotificationService_OnNotificationAdded;

				this.isSubscribed = false;
			}

			this.reloadCancellation?.Cancel();
			this.petitionReloadCancellation?.Cancel();

			if (this.Action != SelectContractAction.Select)
			{
				this.ShowContractsMissing = false;
			}

			this.selection?.TrySetResult(this.selectedContract);

			await base.OnDisposeAsync();
		}

		/// <summary>
		/// Gets or sets the title for the view displaying contracts.
		/// </summary>
		[ObservableProperty]
		private string? title;

		/// <summary>
		/// Gets or sets the introductory text for the view displaying contracts.
		/// </summary>
		[ObservableProperty]
		private string? description;

		/// <summary>
		/// The action to take when contact has been selected.
		/// </summary>
		[ObservableProperty]
		private SelectContractAction action;

		/// <summary>
		/// Gets or sets whether to show a contracts missing alert or not.
		/// </summary>
		[ObservableProperty]
		private bool showContractsMissing;

		/// <summary>
		/// If a search found no contracts.
		/// </summary>
		[ObservableProperty]
		private bool showNoMatches;

		/// <summary>
		/// If the list is the contracts inbox, grouped by what the contracts need.
		/// </summary>
		public bool IsInbox => this.isInbox;

		/// <summary>
		/// If the list is a flat list: templates, or contracts to pick from.
		/// </summary>
		public bool IsFlatList => !this.isInbox;

		/// <summary>
		/// If the list is a template picker: contract templates, or templates for creating tokens.
		/// </summary>
		public bool IsTemplateList => this.contractsListMode != ContractsListMode.Contracts;

		/// <summary>
		/// What the list says when it is empty. An empty list of token creation templates says how to get one.
		/// </summary>
		public string EmptyText => this.contractsListMode == ContractsListMode.TokenCreationTemplates
			? ServiceRef.Localizer[nameof(AppResources.NoCreationTemplates)]
			: ServiceRef.Localizer[nameof(AppResources.NoContractsFound)];

		/// <summary>
		/// Holds the flat list of contracts to display.
		/// </summary>
		public ObservableCollection<ContractModel> Contracts { get; } = [];

		/// <summary>
		/// The groups of the contracts inbox that have rows, in the order they are shown.
		/// </summary>
		public ObservableCollection<ContractGroupModel> Groups { get; } = [];

		/// <summary>
		/// Executed when a row is tapped. A contract is opened or selected, based on <see cref="Action"/>. A contract request
		/// opens the request.
		/// </summary>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task ContractSelected(object? parameter)
		{
			if (parameter is ContractPetitionModel Petition)
			{
				await this.PetitionSelectedAsync(Petition);
				return;
			}

			if (parameter is not ContractModel model)
				return;

			try
			{
				this.IsBusy = true;
				await Task.Yield();
				await this.ContractSelectedAsync(model);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
			finally
			{
				this.IsBusy = false;
			}
		}

		/// <summary>
		/// Should be called by page on text change. Contracts are searched by text over every group, after a short pause in
		/// typing.
		/// </summary>
		public void UpdateSearch(string? text)
		{
			this.searchText = text?.Trim() ?? string.Empty;
			this.ScheduleReload();
		}

		/// <summary>
		/// Clears and repopulates the visible Contracts collection based on search.
		/// </summary>
		private async Task ApplySearchFilter()
		{
			await this.loadLock.WaitAsync();
			try
			{
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.Contracts.Clear();
					this.loadedContracts = 0;
					this.HasMore = 0;
				});

				await this.LoadContractsLockedAsync();
			}
			finally
			{
				this.loadLock.Release();
			}
		}

		/// <summary>
		/// Updates the list when a stored contract changes. In the inbox, a single change updates its row in place. A finished
		/// sync, a search, and changes to the flat lists reload the list.
		/// </summary>
		private Task ContractStore_ContractChanged(object? Sender, ContractChangedEventArgs e)
		{
			if (e.Kind == ContractChangeKind.SyncCompleted)
			{
				if (!this.isSyncRefreshRunning)   // A pull to refresh reloads the list itself.
					this.ScheduleReload();

				return Task.CompletedTask;
			}

			if (e.Reference is not null && e.Reference.IsTemplate != (this.contractsListMode != ContractsListMode.Contracts))
				return Task.CompletedTask;

			if (this.isInbox)
			{
				if (string.IsNullOrEmpty(this.searchText) || e.Kind == ContractChangeKind.Viewed)
					MainThread.BeginInvokeOnMainThread(() => this.ApplyChange(e));
				else
					this.ScheduleReload();

				return Task.CompletedTask;
			}

			if (e.Kind == ContractChangeKind.Viewed)
				return Task.CompletedTask;

			this.ScheduleReload();
			return Task.CompletedTask;
		}

		/// <summary>
		/// Updates the petition rows when a petition notification is added or changes state.
		/// </summary>
		private Task NotificationService_OnNotificationAdded(object? Sender, NotificationRecordEventArgs e)
		{
			if (string.Equals(e.Record.Channel, Constants.PushChannels.Petitions, StringComparison.Ordinal))
				this.SchedulePetitionReload();

			return Task.CompletedTask;
		}

		/// <summary>
		/// Applies a single change to the inbox: moves, updates, adds or removes the contract's row. Called on the main thread.
		/// </summary>
		/// <param name="e">The change.</param>
		private void ApplyChange(ContractChangedEventArgs e)
		{
			try
			{
				ContractModel? Row = this.FindRow(e.ContractId?.Value, out ContractGroupModel? OldGroup);
				ContractReference? Ref = e.Reference;

				if (Ref is null)
				{
					// Removed, or deleted on the server without a kept copy.
					if (Row is not null && OldGroup is not null)
					{
						OldGroup.Remove(Row);
						OldGroup.LoadedCount = Math.Max(0, OldGroup.LoadedCount - 1);

						if (OldGroup.Count == 0 && OldGroup.HasMore)
							this.ScheduleReload();
					}
				}
				else
				{
					if (!this.groups.TryGetValue(Ref.ListGroup, out ContractGroupModel? NewGroup))
					{
						this.ScheduleReload();
						return;
					}

					if (e.Kind == ContractChangeKind.Viewed)
					{
						// Viewing only clears the unread dot; the row keeps its place.
						Row?.Update(Ref);
						return;
					}

					if (Row is not null && OldGroup is not null)
					{
						OldGroup.Remove(Row);
						OldGroup.LoadedCount = Math.Max(0, OldGroup.LoadedCount - 1);
						Row.Update(Ref);
					}
					else
						Row = new ContractModel(Ref);

					if (NewGroup.InsertContract(Row))
						NewGroup.LoadedCount++;
				}

				this.UpdateVisibleGroups();
				this.UpdateEmptyState();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Finds the inbox row of a contract.
		/// </summary>
		/// <param name="ContractId">The contract ID.</param>
		/// <param name="Group">The group the row is in, if found.</param>
		/// <returns>The row, or null if the contract isn't listed.</returns>
		private ContractModel? FindRow(string? ContractId, out ContractGroupModel? Group)
		{
			foreach (ContractGroupModel Model in this.groups.Values)
			{
				ContractModel? Row = Model.FindContract(ContractId);
				if (Row is not null)
				{
					Group = Model;
					return Row;
				}
			}

			Group = null;
			return null;
		}

		/// <summary>
		/// Shows the groups that have rows, in order, and hides empty ones. Called on the main thread.
		/// </summary>
		private void UpdateVisibleGroups()
		{
			int Index = 0;

			foreach (ContractListGroup Group in groupOrder)
			{
				ContractGroupModel Model = this.groups[Group];
				int Current = this.Groups.IndexOf(Model);

				if (Model.Count > 0)
				{
					if (Current < 0)
						this.Groups.Insert(Index, Model);

					Index++;
				}
				else if (Current >= 0)
					this.Groups.RemoveAt(Current);
			}
		}

		/// <summary>
		/// Shows "No contracts" or "No matches" once the list has loaded. Called on the main thread.
		/// </summary>
		private void UpdateEmptyState()
		{
			if (!this.isLoaded)
				return;

			bool IsEmpty = this.isInbox ? this.Groups.Count == 0 : this.Contracts.Count == 0;
			bool IsSearching = !string.IsNullOrEmpty(this.searchText);

			this.ShowContractsMissing = IsEmpty && !IsSearching;
			this.ShowNoMatches = IsEmpty && IsSearching;
		}

		/// <summary>
		/// Ends the loading state and shows the empty state if there are no rows. While the list waits for a sync, it keeps
		/// loading instead.
		/// </summary>
		private Task ShowLoadedAsync()
		{
			return MainThread.InvokeOnMainThreadAsync(() =>
			{
				if (this.isWaitingForSync)
					return;

				this.isLoaded = true;
				this.IsBusy = false;
				this.UpdateEmptyState();
			});
		}

		/// <summary>
		/// Runs a sync when the inbox opens. A sync that ran recently is skipped. When the list is empty, it keeps loading
		/// until the sync has finished, so "No contracts" doesn't show before the contracts arrive.
		/// </summary>
		/// <param name="IsEmpty">If the list was empty when it opened.</param>
		private async Task SyncOnOpenAsync(bool IsEmpty)
		{
			if (IsEmpty)
			{
				this.isWaitingForSync = true;
				this.isSyncRefreshRunning = true;
			}

			try
			{
				await ServiceRef.ContractSyncService.SyncAsync(false, CancellationToken.None);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
			finally
			{
				if (IsEmpty)
				{
					this.isSyncRefreshRunning = false;
					this.isWaitingForSync = false;
				}
			}

			if (IsEmpty)
			{
				try
				{
					await this.LoadInboxAsync();
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}

				await this.ShowLoadedAsync();
			}
		}

		/// <summary>
		/// Reloads the list after a short delay. Changes that arrive close together, and typing in the search field, cause
		/// one reload.
		/// </summary>
		private void ScheduleReload()
		{
			CancellationTokenSource Cancellation = new();
			CancellationTokenSource? Previous = Interlocked.Exchange(ref this.reloadCancellation, Cancellation);
			Previous?.Cancel();

			_ = this.ReloadAfterDelayAsync(Cancellation.Token);
		}

		/// <summary>
		/// Waits briefly, so changes that arrive close together cause one reload, and then reloads the list.
		/// </summary>
		/// <param name="CancellationToken">Cancelled when a newer change arrives.</param>
		private async Task ReloadAfterDelayAsync(CancellationToken CancellationToken)
		{
			try
			{
				await Task.Delay(ReloadDelayMs, CancellationToken);
				await this.ReloadAsync();
			}
			catch (OperationCanceledException)
			{
				// A newer change will reload the list.
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Reloads the petition rows after a short delay.
		/// </summary>
		private void SchedulePetitionReload()
		{
			CancellationTokenSource Cancellation = new();
			CancellationTokenSource? Previous = Interlocked.Exchange(ref this.petitionReloadCancellation, Cancellation);
			Previous?.Cancel();

			_ = this.ReloadPetitionsAfterDelayAsync(Cancellation.Token);
		}

		/// <summary>
		/// Waits briefly, and then reloads the petition rows under "Needs your action".
		/// </summary>
		/// <param name="CancellationToken">Cancelled when a newer change arrives.</param>
		private async Task ReloadPetitionsAfterDelayAsync(CancellationToken CancellationToken)
		{
			try
			{
				await Task.Delay(ReloadDelayMs, CancellationToken);

				IReadOnlyList<ContractPetitionModel> Petitions = await LoadPetitionsAsync(this.searchText);
				CancellationToken.ThrowIfCancellationRequested();

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.groups[ContractListGroup.NeedsAction].SetPetitions(Petitions);
					this.UpdateVisibleGroups();
					this.UpdateEmptyState();
				});
			}
			catch (OperationCanceledException)
			{
				// A newer change will reload the petitions.
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Reloads the list from the stored contracts.
		/// </summary>
		private async Task ReloadAsync()
		{
			if (this.isInbox)
				await this.LoadInboxAsync();
			else if (this.IsTemplateList)
				await this.LoadTemplatesAsync();
			else
				await this.ApplySearchFilter();

			await this.ShowLoadedAsync();
		}

		/// <summary>
		/// Loads the first page of every inbox group, and the pending contract requests.
		/// </summary>
		private async Task LoadInboxAsync()
		{
			await this.loadLock.WaitAsync();
			try
			{
				string Search = this.searchText;
				Dictionary<ContractListGroup, IReadOnlyList<ContractReference>> Pages = [];

				foreach (ContractListGroup Group in groupOrder)
				{
					Pages[Group] = await ServiceRef.ContractStore.QueryAsync(new ContractListQuery()
					{
						IsTemplate = false,
						Group = Group,
						SearchText = Search,
						Offset = 0,
						MaxCount = GroupPageSize + 1
					});
				}

				IReadOnlyList<ContractPetitionModel> Petitions = await LoadPetitionsAsync(Search);

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					foreach (ContractListGroup Group in groupOrder)
					{
						ContractGroupModel Model = this.groups[Group];
						IReadOnlyList<ContractReference> Rows = Pages[Group];
						List<ContractReference> Page = [.. Rows.Take(GroupPageSize)];

						Model.SetContracts(Page);
						Model.LoadedCount = Page.Count;
						Model.HasMore = Rows.Count > GroupPageSize;
					}

					this.groups[ContractListGroup.NeedsAction].SetPetitions(Petitions);
					this.UpdateVisibleGroups();
				});
			}
			finally
			{
				this.loadLock.Release();
			}
		}

		/// <summary>
		/// Loads the next page of an inbox group.
		/// </summary>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task LoadMoreInGroupAsync(object? parameter)
		{
			if (parameter is not ContractGroupModel Group)
				return;

			await this.loadLock.WaitAsync();
			try
			{
				int Offset = await MainThread.InvokeOnMainThreadAsync(() => Group.LoadedCount);

				IReadOnlyList<ContractReference> Rows = await ServiceRef.ContractStore.QueryAsync(new ContractListQuery()
				{
					IsTemplate = false,
					Group = Group.Group,
					SearchText = this.searchText,
					Offset = Offset,
					MaxCount = GroupPageSize + 1
				});

				List<ContractReference> Page = [.. Rows.Take(GroupPageSize)];

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					Group.AddContracts(Page);
					Group.LoadedCount += Page.Count;
					Group.HasMore = Rows.Count > GroupPageSize;
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
			finally
			{
				this.loadLock.Release();
			}
		}

		/// <summary>
		/// Loads the pending requests to see the user's contracts: petition notifications with a contract ID that haven't
		/// been answered.
		/// </summary>
		/// <param name="SearchText">The search text, or an empty string.</param>
		/// <returns>The requests, newest first.</returns>
		private static async Task<IReadOnlyList<ContractPetitionModel>> LoadPetitionsAsync(string SearchText)
		{
			try
			{
				INotificationServiceV2 NotificationService = ServiceRef.Provider.GetRequiredService<INotificationServiceV2>();
				IReadOnlyList<NotificationRecord> Records = await NotificationService.GetAsync(new NotificationQuery()
				{
					Channels = [Constants.PushChannels.Petitions],
					States = [NotificationState.New, NotificationState.Delivered, NotificationState.Read]
				}, CancellationToken.None);

				List<ContractPetitionModel> Result = [];
				HashSet<string> Seen = new(StringComparer.Ordinal);

				foreach (NotificationRecord Record in Records)
				{
					ContractPetitionModel? Model = ContractPetitionModel.TryCreate(Record);
					if (Model is null)
						continue;

					if (!string.IsNullOrEmpty(Model.PetitionId) && !Seen.Add(Model.PetitionId))
						continue;

					if (!string.IsNullOrEmpty(SearchText) && !Model.Matches(SearchText))
						continue;

					Result.Add(Model);
				}

				return Result;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				return [];
			}
		}

		/// <summary>
		/// Opens a contract request, as a tap on its notification would.
		/// </summary>
		/// <param name="Petition">The request.</param>
		private async Task PetitionSelectedAsync(ContractPetitionModel Petition)
		{
			try
			{
				INotificationIntentRouter Router = ServiceRef.Provider.GetRequiredService<INotificationIntentRouter>();
				await Router.RouteAsync(Petition.ToIntent(), true, CancellationToken.None);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Pull to refresh. In the contracts list, reads every contract from the server first.
		/// </summary>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task RefreshContractsAsync()
		{
			try
			{
				if (this.contractsListMode == ContractsListMode.Contracts)
				{
					this.isSyncRefreshRunning = true;
					try
					{
						await ServiceRef.ContractSyncService.SyncAsync(true, CancellationToken.None);
					}
					finally
					{
						this.isSyncRefreshRunning = false;
					}
				}

				await this.ReloadAsync();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
			finally
			{
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.IsRefreshing = false;
				});
			}
		}

		/// <summary>
		/// Handle contract selection.
		/// </summary>
		public async Task ContractSelectedAsync(ContractModel Model)
		{
			try
			{
				ContractReference Ref = Model.ContractRef;

				if (string.IsNullOrEmpty(Ref.ContractId?.Value))
				{
					bool Remove = await MainThread.InvokeOnMainThreadAsync(async () =>
						await ServiceRef.UiService.DisplayAlert(
							ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
							ServiceRef.Localizer[nameof(AppResources.ContractCouldNotBeFound)],
							ServiceRef.Localizer[nameof(AppResources.Yes)],
							ServiceRef.Localizer[nameof(AppResources.No)]));

					if (Remove)
					{
						await ServiceRef.ContractStore.RemoveAsync(Ref.ContractId!);	// Removes the rows with the same missing ID.
						await this.ReloadAsync();
					}

					return;
				}

				switch (this.Action)
				{
					case SelectContractAction.ViewContract:
						await ServiceRef.ContractOrchestratorService.OpenContractAsync(Ref.ContractId!, null);
						break;

					case SelectContractAction.Select:
						Contract? Contract = await Ref.GetContract().ConfigureAwait(false);
						this.selectedContract = Contract;
						await MainThread.InvokeOnMainThreadAsync(async () =>
						{
							await this.GoBack();
						});
						this.selection?.TrySetResult(Contract);
						break;
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				return;
			}
		}

		/// <summary>
		/// Legacy helper maintained for compatibility with existing callers.
		/// </summary>
		public void ContractSelected(ContractModel Model)
		{
			_ = this.ContractSelectedAsync(Model);
		}

		/// <summary>
		/// Shares a template reference as a QR code in a popup. A row of the template picker shares its newest version.
		/// </summary>
		[RelayCommand]
		private async Task ShareTemplateQR(object? parameter)
		{
			try
			{
				ContractModel? Model = parameter switch
				{
					TemplateGroupModel Group => Group.Newest,
					ContractModel Contract => Contract,
					_ => null
				};

				if (Model is null)
					return;

				string ContractUri = Model.ContractIdUriString;
				string ContractName = parameter is TemplateGroupModel Row ? Row.Title : Model.Category;

				if (string.IsNullOrEmpty(ContractUri))
					return;

				int Width = Constants.QrCode.DefaultImageWidth;
				int Height = Constants.QrCode.DefaultImageHeight;
				byte[] QrBytes = Services.UI.QR.QrCode.GeneratePng(ContractUri, Width, Height);

				ShowQRPopup QrPopup = new ShowQRPopup(QrBytes, ContractUri, ContractName);
				await ServiceRef.PopupService.PushAsync(QrPopup);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Command handler that loads additional contracts when the user scrolls near the end of the list.
		/// </summary>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task LoadMoreContracts()
		{
			if (this.isInbox || this.IsTemplateList)
				return;

			await this.LoadContracts();
		}

		/// <summary>
		/// Loads a batch of contracts from the database and adds them to the visible list.
		/// </summary>
		private async Task LoadContracts()
		{
			await this.loadLock.WaitAsync();
			try
			{
				await this.LoadContractsLockedAsync();
			}
			finally
			{
				this.loadLock.Release();
			}
		}

		/// <summary>
		/// Loads a batch of contracts from the database and adds them to the visible list. The caller holds the load lock.
		/// </summary>
		private async Task LoadContractsLockedAsync()
		{
			try
			{
				IEnumerable<ContractReference>? ContractReferences = await this.LoadFromDatabase();

				if (ContractReferences is null)
				{
					this.HasMore = -1;
					return;
				}

				List<ContractModel> Items = [.. ContractReferences.Select(Ref => new ContractModel(Ref))];

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					foreach (ContractModel Item in Items)
						this.Contracts.Add(Item);
				});
			}
			finally
			{
				this.loadedContracts += this.contractBatchSize;
			}
		}

		/// <summary>
		/// Queries the database for a batch of contracts consistent with the current search. Also updates
		/// <see cref="HasMore"/> based on result count. Templates are loaded by <see cref="LoadTemplatesAsync"/> instead.
		/// </summary>
		private async Task<IEnumerable<ContractReference>?> LoadFromDatabase()
		{
			if (this.contractsListMode != ContractsListMode.Contracts)
				return null;

			IEnumerable<ContractReference> ContractReferences;

			if (this.pickerFilter is not null)
			{
				// Picking for a contract-reference parameter: only signed contracts that meet its rules, in one list.
				ContractReferences = await this.LoadPickerCandidatesAsync();
				this.HasMore = -1;
			}
			else
			{
				ContractReferences = await ServiceRef.ContractStore.QueryAsync(new ContractListQuery()
				{
					IsTemplate = false,
					SearchText = this.searchText,
					Offset = this.loadedContracts,
					MaxCount = this.contractBatchSize
				});

				// If fetched amount is less than batch size, tell collectionview to not fire load more event.
				this.HasMore = (ContractReferences.Count() < this.contractBatchSize) ? -1 : 0;
			}

			return ContractReferences;
		}

		/// <summary>
		/// Loads the contracts that can be picked for a contract-reference parameter: signed contracts that meet the
		/// parameter's rules for template, provider, machine-readable name and validity dates.
		/// </summary>
		/// <returns>The matching contracts, sorted by last change.</returns>
		private async Task<IEnumerable<ContractReference>> LoadPickerCandidatesAsync()
		{
			List<ContractReference> Result = [];

			if (this.pickerFilter is null || this.loadedContracts > 0)
				return Result;

			DateTime UtcNow = DateTime.UtcNow;
			int Offset = 0;

			while (true)
			{
				IReadOnlyList<ContractReference> Rows = await ServiceRef.ContractStore.QueryAsync(new ContractListQuery()
				{
					IsTemplate = false,
					Group = ContractListGroup.Signed,
					SearchText = this.searchText,
					Offset = Offset,
					MaxCount = PickerBatchSize
				});

				foreach (ContractReference Ref in Rows)
				{
					if (Ref.IsDeletedOnServer)
						continue;

					try
					{
						Contract? Contract = await Ref.GetContract();
						if (Contract is not null && this.pickerFilter.Matches(Contract, UtcNow))
							Result.Add(Ref);
					}
					catch (Exception Ex)
					{
						ServiceRef.LogService.LogException(Ex);
					}
				}

				if (Rows.Count < PickerBatchSize)
					break;

				Offset += Rows.Count;
			}

			return Result;
		}

		#region Template picker

		/// <summary>
		/// The rows of the template picker that match the search: one row per template title, newest first.
		/// </summary>
		public ObservableCollection<TemplateGroupModel> Templates { get; } = [];

		/// <summary>
		/// Text to search the templates' titles for.
		/// </summary>
		[ObservableProperty]
		private string? templateSearchText;

		/// <summary>
		/// If templates that are missing from the phone are being looked for and downloaded.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(ShowTemplateCheckLine))]
		private bool isCheckingTemplates;

		/// <summary>
		/// The line under the search field that says what the template list is doing, if anything.
		/// </summary>
		[ObservableProperty]
		private string? templateStatusText;

		/// <summary>
		/// If <see cref="TemplateStatusText"/> is shown.
		/// </summary>
		[ObservableProperty]
		private bool showTemplateStatus;

		/// <summary>
		/// If placeholder rows are shown while the first templates load.
		/// </summary>
		[ObservableProperty]
		private bool showTemplatePlaceholder;

		/// <summary>
		/// If the template list has rows to show.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(ShowTemplateCheckLine))]
		private bool showTemplateList;

		/// <summary>
		/// If there are no templates at all, once loading and looking for templates have finished.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(TemplateHeadingText))]
		[NotifyPropertyChangedFor(nameof(TemplateDescriptionText))]
		[NotifyPropertyChangedFor(nameof(ShowTemplateSearch))]
		[NotifyPropertyChangedFor(nameof(CanSearchTemplatesAgain))]
		private bool showNoTemplates;

		/// <summary>
		/// If a search matched none of the templates.
		/// </summary>
		[ObservableProperty]
		private bool showNoTemplateMatches;

		/// <summary>
		/// If the line along the top of the template list runs, while templates are looked for.
		/// </summary>
		public bool ShowTemplateCheckLine => this.IsCheckingTemplates && this.ShowTemplateList;

		/// <summary>
		/// The template picker's heading.
		/// </summary>
		public string TemplateHeadingText => this.ShowNoTemplates
			? ServiceRef.Localizer[nameof(AppResources.TemplatesNoneHeading)]
			: ServiceRef.Localizer[nameof(AppResources.TemplatePickerHeading)];

		/// <summary>
		/// The text under the template picker's heading.
		/// </summary>
		public string TemplateDescriptionText => this.ShowNoTemplates
			? ServiceRef.Localizer[nameof(AppResources.TemplatesNoneDescription)]
			: ServiceRef.Localizer[nameof(AppResources.TemplatePickerDescription)];

		/// <summary>
		/// If the template search field is shown. There is nothing to search when there are no templates.
		/// </summary>
		public bool ShowTemplateSearch => !this.ShowNoTemplates;

		/// <summary>
		/// If templates can be looked for again. Only templates for creating tokens are looked for.
		/// </summary>
		public bool CanSearchTemplatesAgain => this.ShowNoTemplates && this.contractsListMode == ContractsListMode.TokenCreationTemplates;

		partial void OnTemplateSearchTextChanged(string? Value)
		{
			this.ApplyTemplateFilter();
		}

		/// <summary>
		/// Shows the placeholder rows if the templates haven't loaded after a moment, so a quick load doesn't flash them.
		/// </summary>
		private async Task ShowTemplatePlaceholderWhenSlowAsync()
		{
			try
			{
				await Task.Delay(TemplatePlaceholderDelayMs);

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.isTemplatePlaceholderDue = true;
					this.UpdateTemplateState();
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Loads the stored templates, groups the versions of each template under its title, and shows the rows that match
		/// the search. Templates that weren't on the phone when the picker opened are marked as new.
		/// </summary>
		private async Task LoadTemplatesAsync()
		{
			await this.loadLock.WaitAsync();
			try
			{
				IReadOnlyList<ContractReference> Rows = await ServiceRef.ContractStore.QueryAsync(new ContractListQuery()
				{
					IsTemplate = true,
					IsTokenCreationTemplate = this.contractsListMode == ContractsListMode.TokenCreationTemplates ? true : null,
					Offset = 0,
					MaxCount = MaxTemplates
				});

				List<TemplateGroupModel> Groups = GroupTemplates(Rows);

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					HashSet<string> ExpandedKeys = new(StringComparer.Ordinal);

					foreach (TemplateGroupModel Group in this.allTemplates)
					{
						if (Group.IsExpanded)
							ExpandedKeys.Add(Group.Key);
					}

					bool IsFirstLoad = !this.templatesLoaded;

					foreach (TemplateGroupModel Group in Groups)
					{
						if (IsFirstLoad)
						{
							foreach (TemplateVersionModel Version in Group.Versions)
								this.knownTemplateIds.Add(Version.Contract.ContractId);
						}
						else
							Group.IsNew = !this.knownTemplateIds.Contains(Group.Newest.ContractId);

						Group.IsExpanded = ExpandedKeys.Contains(Group.Key);
					}

					this.allTemplates.Clear();
					this.allTemplates.AddRange(Groups);
					this.templatesLoaded = true;

					this.ApplyTemplateFilter();
				});
			}
			finally
			{
				this.loadLock.Release();
			}
		}

		/// <summary>
		/// Groups templates by title. Each group lists its versions newest first, and the groups are sorted by their newest
		/// version, newest first.
		/// </summary>
		/// <param name="Rows">The stored templates.</param>
		/// <returns>The rows of the template picker.</returns>
		private static List<TemplateGroupModel> GroupTemplates(IEnumerable<ContractReference> Rows)
		{
			Dictionary<string, List<ContractModel>> ByKey = new(StringComparer.Ordinal);

			foreach (ContractReference Ref in Rows)
			{
				if (string.IsNullOrEmpty(Ref.ContractId?.Value))
					continue;

				ContractModel Model = new(Ref);
				string Key = TemplateGroupModel.GetKey(GetTemplateTitle(Model), Model.ContractId);

				if (!ByKey.TryGetValue(Key, out List<ContractModel>? Versions))
				{
					Versions = [];
					ByKey[Key] = Versions;
				}

				Versions.Add(Model);
			}

			List<TemplateGroupModel> Result = [];

			foreach (List<ContractModel> Versions in ByKey.Values)
			{
				Versions.Sort((A, B) => B.Timestamp.CompareTo(A.Timestamp));
				Result.Add(new TemplateGroupModel(GetTemplateTitle(Versions[0]), Versions));
			}

			Result.Sort((A, B) => B.Newest.Timestamp.CompareTo(A.Newest.Timestamp));

			return Result;
		}

		/// <summary>
		/// Gets the title of a template: its category, or else its name, or else its ID.
		/// </summary>
		/// <param name="Model">The template.</param>
		/// <returns>The title.</returns>
		private static string GetTemplateTitle(ContractModel Model)
		{
			if (!string.IsNullOrWhiteSpace(Model.Category))
				return Model.Category.Trim();

			if (!string.IsNullOrWhiteSpace(Model.Name))
				return Model.Name.Trim();

			return Model.ContractId;
		}

		/// <summary>
		/// Shows the template rows whose titles match the search. Called on the main thread.
		/// </summary>
		private void ApplyTemplateFilter()
		{
			string Query = this.TemplateSearchText?.Trim() ?? string.Empty;

			this.Templates.Clear();

			foreach (TemplateGroupModel Group in this.allTemplates)
			{
				if (Query.Length == 0 || Group.Title.Contains(Query, StringComparison.CurrentCultureIgnoreCase))
					this.Templates.Add(Group);
			}

			this.UpdateTemplateState();
		}

		/// <summary>
		/// Updates what the template picker shows: placeholder rows while the first templates load, the list, the empty
		/// state, and the status line. Called on the main thread.
		/// </summary>
		private void UpdateTemplateState()
		{
			bool HasTemplates = this.allTemplates.Count > 0;
			bool IsWaiting = !this.templatesLoaded || (!HasTemplates && this.IsCheckingTemplates);
			string? Status = null;

			this.ShowTemplatePlaceholder = IsWaiting && this.isTemplatePlaceholderDue;
			this.ShowTemplateList = this.Templates.Count > 0;
			this.ShowNoTemplates = !IsWaiting && !HasTemplates;
			this.ShowNoTemplateMatches = HasTemplates && this.Templates.Count == 0;

			if (IsWaiting)
			{
				if (this.isTemplatePlaceholderDue)
					Status = ServiceRef.Localizer[nameof(AppResources.TemplatesLooking)];
			}
			else if (this.IsCheckingTemplates && HasTemplates)
				Status = ServiceRef.Localizer[nameof(AppResources.TemplatesChecking)];
			else if (this.showTemplatesUpToDate && HasTemplates)
				Status = ServiceRef.Localizer[nameof(AppResources.TemplatesUpToDate)];

			this.TemplateStatusText = Status;
			this.ShowTemplateStatus = Status is not null;
		}

		/// <summary>
		/// Looks for templates for creating tokens that aren't on the phone, and downloads them. The list stays usable
		/// meanwhile, and templates that arrive are added to it and marked as new.
		/// </summary>
		private async Task CheckForNewTemplatesAsync()
		{
			bool IsAlreadyChecking = await MainThread.InvokeOnMainThreadAsync(() =>
			{
				if (this.IsCheckingTemplates)
					return true;

				this.IsCheckingTemplates = true;
				this.showTemplatesUpToDate = false;
				this.UpdateTemplateState();
				return false;
			});

			if (!IsAlreadyChecking)
				await this.DownloadMissingTemplatesAsync();
		}

		/// <summary>
		/// Downloads the templates for creating tokens that aren't on the phone, reloads the list, and ends the check. The
		/// caller has set <see cref="IsCheckingTemplates"/>.
		/// </summary>
		private async Task DownloadMissingTemplatesAsync()
		{
			bool Completed = false;

			try
			{
				foreach (string TemplateId in await ServiceRef.TokenTemplateService.GetCreationTemplatesAsync())
				{
					try
					{
						ContractReference? Existing = await ServiceRef.ContractStore.GetAsync(TemplateId);

						if (Existing is null || !Existing.IsTemplate || !Existing.ContractLoaded)
						{
							Contract? Template = await ServiceRef.XmppService.GetContract(TemplateId);
							if (Template is not null)
								await ServiceRef.ContractStore.UpsertAsync(Template, ContractOrigin.Opened);
						}
					}
					catch (Exception Ex)
					{
						ServiceRef.LogService.LogException(Ex);
					}
				}

				Completed = true;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}

			try
			{
				// The templates that arrived are listed before the check ends, so the picker never says it's empty in between.
				await this.LoadTemplatesAsync();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}

			bool ShowsUpToDate = await MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.IsCheckingTemplates = false;
				this.showTemplatesUpToDate = Completed && this.allTemplates.Count > 0;
				this.UpdateTemplateState();
				return this.showTemplatesUpToDate;
			});

			if (!ShowsUpToDate)
				return;

			await Task.Delay(UpToDateDisplayMs);

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.showTemplatesUpToDate = false;
				this.UpdateTemplateState();
			});
		}

		/// <summary>
		/// Uses a template: the newest version when a row is tapped, or the version that was tapped.
		/// </summary>
		/// <param name="Parameter">The row, or the version.</param>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task TemplateSelectedAsync(object? Parameter)
		{
			ContractModel? Model = Parameter switch
			{
				TemplateGroupModel Group => Group.Newest,
				TemplateVersionModel Version => Version.Contract,
				_ => null
			};

			if (Model is null)
				return;

			try
			{
				await this.ContractSelectedAsync(Model);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Shows or hides the versions of a template.
		/// </summary>
		/// <param name="Parameter">The row.</param>
		[RelayCommand]
		private void ToggleVersions(object? Parameter)
		{
			if (Parameter is TemplateGroupModel Group)
				Group.IsExpanded = !Group.IsExpanded;
		}

		/// <summary>
		/// Scans a template's QR code, saves the template, and uses it as if its row was tapped.
		/// </summary>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task ScanTemplateQrAsync()
		{
			try
			{
				ContractLinkData? Scan = await QrCode.GetQrService().ScanContractAsync(nameof(AppResources.ScanQRCode));
				Contract? Template = Scan?.Contract;

				if (Template is null)
					return;

				if (!this.IsUsableTemplate(Template))
				{
					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.TemplateQrNotUsable)],
						ServiceRef.Localizer[nameof(AppResources.Ok)]);
					return;
				}

				ContractReference Ref = await ServiceRef.ContractStore.UpsertAsync(Template, ContractOrigin.Opened);
				await this.ContractSelectedAsync(new ContractModel(Ref));
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Looks for templates again, after nothing was found.
		/// </summary>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private Task SearchTemplatesAgainAsync()
		{
			return this.CheckForNewTemplatesAsync();
		}

		/// <summary>
		/// Checks if a scanned contract can be used in this picker: it must be a template, and in the token picker, a
		/// template for creating tokens.
		/// </summary>
		/// <param name="Template">The scanned contract.</param>
		/// <returns>If the contract can be used.</returns>
		private bool IsUsableTemplate(Contract Template)
		{
			if (Template.PartsMode != ContractParts.TemplateOnly)
				return false;

			if (this.contractsListMode != ContractsListMode.TokenCreationTemplates)
				return true;

			return Template.ForMachinesNamespace == TokenTemplateService.GetNamespace(TokenAction.Create) &&
				Template.ForMachinesLocalName == TokenTemplateService.GetLocalName(TokenAction.Create);
		}

		#endregion

		/// <summary>
		/// Releases the resources owned by the view model.
		/// </summary>
		/// <remarks>
		/// Not called from <see cref="OnDisposeAsync"/>, since a load can still be running when the page closes.
		/// </remarks>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Releases the resources owned by the view model.
		/// </summary>
		/// <param name="Disposing">If managed resources should be released.</param>
		protected virtual void Dispose(bool Disposing)
		{
			if (this.disposed)
				return;

			if (Disposing)
			{
				Interlocked.Exchange(ref this.reloadCancellation, null)?.Dispose();
				Interlocked.Exchange(ref this.petitionReloadCancellation, null)?.Dispose();
				this.loadLock.Dispose();
			}

			this.disposed = true;
		}

		/// <summary>
		/// Container class representing a selectable category tag.
		/// </summary>
		public partial class SelectableTag : ObservableObject
		{
			/// <summary>
			/// Category name.
			/// </summary>
			[ObservableProperty]
			private string category;
			/// <summary>
			/// True if the tag is currently selected.
			/// </summary>
			[ObservableProperty]
			private bool isSelected;

			/// <summary>
			/// Creates a new <see cref="SelectableTag"/>.
			/// </summary>
			/// <param name="Category">Category name.</param>
			/// <param name="IsSelected">Initial selection state.</param>
			public SelectableTag(string Category, bool IsSelected)
			{
				this.category = Category;
				this.isSelected = IsSelected;
			}

			/// <summary>
			/// Toggles the selection state of the tag.
			/// </summary>
			public void ToggleSelection() => this.IsSelected = !this.IsSelected;
		}
	}
}
