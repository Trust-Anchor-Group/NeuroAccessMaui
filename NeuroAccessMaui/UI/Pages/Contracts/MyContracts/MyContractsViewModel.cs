using NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels;
using NeuroAccessMaui.Services.Contracts;
using NeuroAccessMaui.Services.Notification;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Persistence.Filters;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Resources.Languages;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Services.UI;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.UI.Popups.QR;
using Waher.Script;
using NeuroAccessMaui.UI.MVVM;
using CommunityToolkit.Maui.Core.Extensions;
using NeuroAccessMaui.UI.Popups;

namespace NeuroAccessMaui.UI.Pages.Contracts.MyContracts
{
	/// <summary>
	/// View model responsible for presenting and filtering a list of contracts and templates.
	/// In the contracts list, contracts are grouped as an inbox: what needs the user's action, what is in progress, what is
	/// signed and what has ended. Templates are listed flat, filtered by category.
	/// </summary>
	public partial class MyContractsViewModel : BaseViewModel, IDisposable
	{
		private const int ReloadDelayMs = 300;
		private const int GroupPageSize = 10;
		private const int PickerBatchSize = 50;

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
		private readonly Dictionary<string, SelectableTag> tagMap = new(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// Event raised when a filter tag becomes selected. Consumers can react (e.g., scroll into view).
		/// </summary>
		public event Action<SelectableTag>? TagSelected;

		private readonly string AllCategory = ServiceRef.Localizer[nameof(AppResources.All)];
		private readonly int contractBatchSize = 10;

		private readonly SemaphoreSlim loadLock = new(1, 1);
		private CancellationTokenSource? reloadCancellation;
		private CancellationTokenSource? petitionReloadCancellation;
		private INotificationServiceV2? notificationService;
		private bool isSyncRefreshRunning;
		private bool isWaitingForSync;
		private bool isLoaded;
		private bool isSubscribed;
		private bool disposed;

		private int loadedContracts;
		private string currentCategory;
		private string searchText = string.Empty;

		/// <summary>
		/// Collection of available filter tags for categories. One tag can be selected at a time.
		/// </summary>
		public ObservableCollection<SelectableTag> FilterTags { get; set; } = new();

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
			this.currentCategory = this.AllCategory;

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

			if (this.UsesCategoryTags)
			{
				await this.LoadCategories();

				// Ensure an "All" tag exists showing total count, selected by default
				MainThread.BeginInvokeOnMainThread(() =>
				{
					if (this.tagMap.TryGetValue(this.AllCategory, out SelectableTag? allTag))
					{
						allTag.IsSelected = true;
					}
					else
					{
						SelectableTag all = new(this.AllCategory, true);
						this.tagMap[this.AllCategory] = all;
						this.FilterTags.Insert(0, all);
					}
				});

				this.currentCategory = this.AllCategory;
			}

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
		/// If the list is filtered by category tags. Templates are; contracts are searched by text.
		/// </summary>
		public bool UsesCategoryTags => this.contractsListMode != ContractsListMode.Contracts;

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
		/// Opens the filter popup and applies the selected tag when the popup returns.
		/// </summary>
		[RelayCommand]
		private async Task OpenFilterPopup()
		{
			FilterContractsPopupViewModel vm = new FilterContractsPopupViewModel(this.FilterTags);
			SelectableTag? selectedTag = await ServiceRef.PopupService.PushAsync<FilterContractsPopup, FilterContractsPopupViewModel, MyContractsViewModel.SelectableTag>(vm);
			if (selectedTag is not null && !string.Equals(selectedTag.Category, this.currentCategory, StringComparison.OrdinalIgnoreCase))
				await this.FilterChanged(selectedTag);
		}

		/// <summary>
		/// Changes the selected filter tag and refreshes the contract list accordingly.
		/// </summary>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task FilterChanged(object? parameter)
		{
			if (parameter is SelectableTag Tag)
			{
				bool wasSelected = Tag.IsSelected;
				if (wasSelected)
				{
					foreach (SelectableTag t in this.FilterTags)
					{
						t.IsSelected = t.Category == this.AllCategory;
						if (string.Equals(t.Category, this.AllCategory, StringComparison.OrdinalIgnoreCase))
							this.TagSelected?.Invoke(t);
					}

					this.currentCategory = this.AllCategory;
				}
				else
				{
					foreach (SelectableTag t in this.FilterTags)
						t.IsSelected = (t == Tag);

					this.currentCategory = Tag.Category;
					this.TagSelected?.Invoke(Tag);
				}

				await this.ApplySearchFilter();
			}
		}

		/// <summary>
		/// Should be called by page on text change.
		/// </summary>
		public void UpdateSearch(string? text)
		{
			if (!this.UsesCategoryTags)
			{
				// Contracts are searched by text over every group, after a short pause in typing.
				this.searchText = text?.Trim() ?? string.Empty;
				this.ScheduleReload();
				return;
			}

			string? previousCategory = this.currentCategory;

			SelectableTag? selectedTag = null;

			if (string.IsNullOrEmpty(text))
			{
				foreach (SelectableTag Tag in this.FilterTags)
				{
					Tag.IsSelected = false;
				}
				this.FilterTags[0].IsSelected = true; // Select "All"
				selectedTag = this.FilterTags[0];
				this.currentCategory = this.AllCategory;
			}
			else
			{
				bool found = false; // To ensure only one tag is selected
				foreach (SelectableTag Tag in this.FilterTags)
				{
					if (Tag.Category.Contains(text ?? string.Empty, StringComparison.OrdinalIgnoreCase) && !found)
					{
						found = true;
						Tag.IsSelected = true;
						selectedTag = Tag;
						this.currentCategory = Tag.Category;
					}
					else
					{
						Tag.IsSelected = false;
					}
				}
			}

			if (!string.Equals(previousCategory, this.currentCategory, StringComparison.OrdinalIgnoreCase))
			{
				if (selectedTag is not null)
					this.TagSelected?.Invoke(selectedTag);

				this.ApplySearchFilter().ConfigureAwait(false);
			}
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
			else
			{
				if (this.UsesCategoryTags)
					await this.LoadCategories();

				await this.ApplySearchFilter();
			}

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
		/// Shares a template reference as a QR code in a popup.
		/// </summary>
		[RelayCommand]
		private async Task ShareTemplateQR(object? parameter)
		{
			try
			{
				ContractModel? Model = parameter as ContractModel;
				if (Model is null)
					return;

				string ContractUri = Model.ContractIdUriString;
				string ContractName = Model.Category;

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
		/// Loads available categories and populates <see cref="FilterTags"/>.
		/// </summary>
		private async Task LoadCategories()
		{
			try
			{
				string Filter = this.contractsListMode == ContractsListMode.TokenCreationTemplates
					? "IsTemplate=true and IsTokenCreationTemplate=true"
					: $"IsTemplate={this.contractsListMode != ContractsListMode.Contracts}";

				object Categories = await Expression.EvalAsync("select distinct Category from NeuroAccessMaui.Services.Contracts.ContractReference where " + Filter);

				if (Categories is not object[] Items)
					return;

				foreach (object Item in Items)
				{
					if (Item is string category && !string.IsNullOrWhiteSpace(category))
					{
						MainThread.BeginInvokeOnMainThread(() =>
						{
							if (!this.tagMap.ContainsKey(category))
							{
								SelectableTag NewTag = new(category, false);
								this.tagMap[category] = NewTag;
								this.FilterTags.Add(NewTag);
							}
						});
					}
				}
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
			if (this.isInbox)
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

				if (this.HasMore == -1 && this.contractsListMode == ContractsListMode.TokenCreationTemplates)
				{
					// Templates found on the Neuron, or known for its domain, are added to the list when they are missing.
					foreach (string TokenTemplateId in await ServiceRef.TokenTemplateService.GetCreationTemplatesAsync())
					{
						try
						{
							ContractReference? Existing = await ServiceRef.ContractStore.GetAsync(TokenTemplateId);

							if (Existing is null || !Existing.IsTemplate || !Existing.ContractLoaded)
							{
								// The store raises a change for the new row, and the list reloads with it.
								Contract? Contract = await ServiceRef.XmppService.GetContract(TokenTemplateId);
								if (Contract is not null)
									await ServiceRef.ContractStore.UpsertAsync(Contract, ContractOrigin.Opened);
							}
						}
						catch (Exception Ex)
						{
							ServiceRef.LogService.LogException(Ex);
						}
					}
				}
			}
		}

		/// <summary>
		/// Queries the database for a batch of <see cref="ContractReference"/> records consistent with the current filter.
		/// Also updates <see cref="HasMore"/> based on result count.
		/// </summary>
		private async Task<IEnumerable<ContractReference>?> LoadFromDatabase()
		{
			IEnumerable<ContractReference> ContractReferences;

			if (this.contractsListMode == ContractsListMode.Contracts)
			{
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

			if (this.currentCategory == this.AllCategory)
			{
				switch (this.contractsListMode)
				{
					case ContractsListMode.ContractTemplates:
						ContractReferences = await Database.Find<ContractReference>(this.loadedContracts, this.contractBatchSize, new FilterAnd(
							new FilterFieldEqualTo("IsTemplate", true),
							new FilterFieldEqualTo("ContractLoaded", true)));

						// If fetched amount is less than batch size, tell collectionview to not fire load more event.
						this.HasMore = (ContractReferences.Count() < this.contractBatchSize) ? -1 : 0;
						break;

					case ContractsListMode.TokenCreationTemplates:
						ContractReferences = await Database.Find<ContractReference>(this.loadedContracts, this.contractBatchSize, new FilterAnd(
							new FilterFieldEqualTo("IsTemplate", true),
							new FilterFieldEqualTo("IsTokenCreationTemplate", true),
							new FilterFieldEqualTo("ContractLoaded", true)));

						// If fetched amount is less than batch size, tell collectionview to not fire load more event.
						this.HasMore = (ContractReferences.Count() < this.contractBatchSize) ? -1 : 0;
						break;

					default:
						return null;
				}
			}
			else
			{
				switch (this.contractsListMode)
				{
					case ContractsListMode.ContractTemplates:
						ContractReferences = await Database.Find<ContractReference>(this.loadedContracts, this.contractBatchSize, new FilterAnd(
							new FilterFieldEqualTo("IsTemplate", true),
							new FilterFieldEqualTo("ContractLoaded", true),
							new FilterFieldEqualTo("Category", this.currentCategory)));

						// If fetched amount is less than batch size, tell collectionview to not fire load more event.
						this.HasMore = (ContractReferences.Count() < this.contractBatchSize) ? -1 : 0;
						break;

					case ContractsListMode.TokenCreationTemplates:
						ContractReferences = await Database.Find<ContractReference>(this.loadedContracts, this.contractBatchSize, new FilterAnd(
							new FilterFieldEqualTo("IsTemplate", true),
							new FilterFieldEqualTo("IsTokenCreationTemplate", true),
							new FilterFieldEqualTo("ContractLoaded", true),
							new FilterFieldEqualTo("Category", this.currentCategory)));

						// If fetched amount is less than batch size, tell collectionview to not fire load more event.
					this.HasMore = (ContractReferences.Count() < this.contractBatchSize) ? -1 : 0;
						break;

					default:
						return null;
				}
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
