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
	/// Handles loading, filtering by category, selection/navigation, and popup interactions.
	/// </summary>
	public partial class MyContractsViewModel : BaseViewModel
	{
		private readonly ContractsListMode contractsListMode;
		private readonly TaskCompletionSource<Contract?>? selection;
		private Contract? selectedContract = null;
		private readonly Dictionary<string, SelectableTag> tagMap = new(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// Event raised when a filter tag becomes selected. Consumers can react (e.g., scroll into view).
		/// </summary>
		public event Action<SelectableTag>? TagSelected;

		private const int ReloadDelayMs = 300;

		private readonly string AllCategory = ServiceRef.Localizer[nameof(AppResources.All)];
		private readonly int contractBatchSize = 10;

		private readonly SemaphoreSlim loadLock = new(1, 1);
		private CancellationTokenSource? reloadCancellation;
		private bool isSyncRefreshRunning;
		private bool isSubscribed;

		private int loadedContracts;
		private string currentCategory;

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
		}

		/// <inheritdoc/>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			this.IsBusy = true;
			this.ShowContractsMissing = false;

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

			await this.LoadContracts();

			this.ShowContractsMissing = this.Contracts.Count < 1;

			if (!this.isSubscribed)
			{
				ServiceRef.ContractStore.ContractChanged += this.ContractStore_ContractChanged;
				this.isSubscribed = true;
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
		}

		/// <inheritdoc/>
		public override async Task OnDisposeAsync()
		{
			if (this.isSubscribed)
			{
				ServiceRef.ContractStore.ContractChanged -= this.ContractStore_ContractChanged;
				this.isSubscribed = false;
			}

			this.reloadCancellation?.Cancel();

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
		/// Holds the flat list of contracts to display.
		/// </summary>
		public ObservableCollection<ContractModel> Contracts { get; } = [];

		/// <summary>
		/// Executed when a contract item is tapped; performs navigation or selection based on <see cref="Action"/>.
		/// </summary>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task ContractSelected(object? parameter)
		{
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
		/// Reloads the list when a stored contract changes. Changes that arrive close together cause one reload.
		/// </summary>
		private Task ContractStore_ContractChanged(object? Sender, ContractChangedEventArgs e)
		{
			if (e.Kind == ContractChangeKind.Viewed)
				return Task.CompletedTask;

			if (e.Kind == ContractChangeKind.SyncCompleted && this.isSyncRefreshRunning)
				return Task.CompletedTask;   // The pull to refresh reloads the list itself.

			if (e.Reference is not null && e.Reference.IsTemplate != (this.contractsListMode != ContractsListMode.Contracts))
				return Task.CompletedTask;

			CancellationTokenSource Cancellation = new();
			CancellationTokenSource? Previous = Interlocked.Exchange(ref this.reloadCancellation, Cancellation);
			Previous?.Cancel();

			_ = this.ReloadAfterDelayAsync(Cancellation.Token);

			return Task.CompletedTask;
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
		/// Reloads the categories and the list from the stored contracts.
		/// </summary>
		private async Task ReloadAsync()
		{
			await this.LoadCategories();
			await this.ApplySearchFilter();

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.ShowContractsMissing = this.Contracts.Count < 1;
			});
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
				object Categories = await Expression.EvalAsync($"select distinct Category from NeuroAccessMaui.Services.Contracts.ContractReference where IsTemplate={this.contractsListMode != ContractsListMode.Contracts}");

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

				foreach (ContractReference Ref in ContractReferences)
				{
					ContractModel Item = new(Ref, []);

					MainThread.BeginInvokeOnMainThread(() =>
					{
						this.Contracts.Add(Item);
					});
				}
			}
			finally
			{
				this.loadedContracts += this.contractBatchSize;
				this.IsBusy = false;

				if (this.HasMore == -1 && this.contractsListMode == ContractsListMode.TokenCreationTemplates)
				{
					foreach (string TokenTemplateId in Constants.ContractTemplates.TokenCreationTemplates)
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

			if (this.currentCategory == this.AllCategory)
			{
				switch (this.contractsListMode)
				{
					case ContractsListMode.Contracts:
						// Invitations whose contract can't be read yet are listed too.
						ContractReferences = await ServiceRef.ContractStore.QueryAsync(new ContractListQuery()
						{
							IsTemplate = false,
							Offset = this.loadedContracts,
							MaxCount = this.contractBatchSize
						});

						// If fetched amount is less than batch size, tell collectionview to not fire load more event.
						this.HasMore = (ContractReferences.Count() < this.contractBatchSize) ? -1 : 0;
						break;

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
					case ContractsListMode.Contracts:
						ContractReferences = await Database.Find<ContractReference>(this.loadedContracts, this.contractBatchSize, new FilterAnd(
							new FilterFieldEqualTo("IsTemplate", false),
							new FilterFieldEqualTo("ContractLoaded", true),
							new FilterFieldEqualTo("Category", this.currentCategory)), "-LastChanged");

						// If fetched amount is less than batch size, tell collectionview to not fire load more event.
						this.HasMore = (ContractReferences.Count() < this.contractBatchSize) ? -1 : 0;
						break;

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
