using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contracts;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.Services.Notification.Contracts;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.UI.MVVM;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels;
using NeuroAccessMaui.UI.Popups;
using NeuroAccessMaui.UI.Popups.QR;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Persistence.Filters;
using Waher.Script;

namespace NeuroAccessMaui.UI.Pages.Contracts.MyContracts
{
	/// <summary>
	/// Presents a paged, locally summarized collection of contracts or contract templates.
	/// </summary>
	public partial class MyContractsViewModel : BaseViewModel, IDisposable
	{
		private const int contractBatchSize = 24;
		private const int remainingItemsThreshold = 5;
		private const int searchDebounceMilliseconds = 250;

		private readonly ContractsListMode contractsListMode;
		private readonly TaskCompletionSource<Contract?>? selection;
		private readonly Dictionary<string, SelectableTag> tagMap = new(StringComparer.OrdinalIgnoreCase);
		private Dictionary<string, NotificationEvent[]> legacyNotificationsByContractId =
			new(StringComparer.OrdinalIgnoreCase);
		private Dictionary<string, NotificationRecord[]> notificationsByContractId =
			new(StringComparer.OrdinalIgnoreCase);
		private CancellationTokenSource? searchDebounceCancellation;
		private Contract? selectedContract;
		private bool refreshOnNextAppearance;
		private string currentCategory = string.Empty;
		private string searchText = string.Empty;
		private int loadedContracts;
		private long queryGeneration;
		private bool tokenTemplatesEnsured;
		private bool isDisposed;

		/// <summary>
		/// Initializes a new contract collection view model.
		/// </summary>
		/// <param name="Args">Navigation arguments, or <see langword="null"/> for the contracts mode.</param>
		public MyContractsViewModel(MyContractsNavigationArgs? Args)
		{
			this.contractsListMode = Args?.Mode ?? ContractsListMode.Contracts;
			this.Action = Args?.Action ?? SelectContractAction.ViewContract;
			this.selection = Args?.Selection;
			this.IsInitialLoading = true;

			switch (this.contractsListMode)
			{
				case ContractsListMode.Contracts:
					this.Title = ServiceRef.Localizer[nameof(AppResources.Contracts)];
					this.Description = ServiceRef.Localizer[nameof(AppResources.ContractsSubtitle)];
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

		/// <summary>
		/// Occurs when a category tag is selected and should be brought into view.
		/// </summary>
		public event Action<SelectableTag>? TagSelected;

		/// <summary>
		/// Gets the available category filters.
		/// </summary>
		public ObservableCollection<SelectableTag> FilterTags { get; } = [];

		/// <summary>
		/// Gets the paged contract summaries currently displayed.
		/// </summary>
		public ObservableCollection<ContractModel> Contracts { get; } = [];

		/// <summary>
		/// Gets whether at least one loaded contract needs the current person's attention.
		/// </summary>
		public bool HasAttentionContracts => this.AttentionContractsCount > 0;

		/// <summary>
		/// Gets whether the empty-state panel should be displayed.
		/// </summary>
		public bool ShowEmptyState => !this.IsInitialLoading && !this.HasLoadError && !this.HasContracts;

		/// <summary>
		/// Gets whether server-backed reference recovery is relevant to the current empty view.
		/// </summary>
		public bool CanRecoverContracts =>
			this.contractsListMode == ContractsListMode.Contracts &&
			string.IsNullOrEmpty(this.searchText) &&
			string.IsNullOrEmpty(this.currentCategory);

		/// <summary>
		/// Gets or sets the collection title.
		/// </summary>
		[ObservableProperty]
		private string title = string.Empty;

		/// <summary>
		/// Gets or sets the plain-language collection description.
		/// </summary>
		[ObservableProperty]
		private string description = string.Empty;

		/// <summary>
		/// Gets or sets the action performed when a contract is selected.
		/// </summary>
		[ObservableProperty]
		private SelectContractAction action;

		/// <summary>
		/// Gets or sets whether template sharing is available in the current mode.
		/// </summary>
		[ObservableProperty]
		private bool canShareTemplate;

		/// <summary>
		/// Gets or sets the collection-view threshold, or -1 when no additional page exists.
		/// </summary>
		[ObservableProperty]
		private int hasMore = -1;

		/// <summary>
		/// Gets or sets whether the first local page is loading.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(ShowEmptyState))]
		private bool isInitialLoading;

		/// <summary>
		/// Gets or sets whether an additional local page is loading.
		/// </summary>
		[ObservableProperty]
		private bool isLoadingMore;

		/// <summary>
		/// Gets or sets whether the latest local database request failed.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(ShowEmptyState))]
		private bool hasLoadError;

		/// <summary>
		/// Gets or sets the localized local-load error message.
		/// </summary>
		[ObservableProperty]
		private string loadErrorMessage = string.Empty;

		/// <summary>
		/// Gets or sets whether missing contract references are being recovered.
		/// </summary>
		[ObservableProperty]
		private bool isRecoveringContracts;

		/// <summary>
		/// Gets or sets whether at least one summary is displayed.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(ShowEmptyState))]
		private bool hasContracts;

		/// <summary>
		/// Gets or sets the number of loaded contracts with a locally derivable action.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasAttentionContracts))]
		private int attentionContractsCount;

		/// <inheritdoc/>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			long Generation = Interlocked.Increment(ref this.queryGeneration);
			await this.LoadCategoriesAsync(Generation).ConfigureAwait(false);
			await this.ReloadAsync(Generation).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();

			if (this.selection is not null && this.selection.Task.IsCompleted)
				await this.GoBack();
			else if (this.refreshOnNextAppearance)
			{
				this.refreshOnNextAppearance = false;
				long Generation = Interlocked.Increment(ref this.queryGeneration);
				await this.LoadCategoriesAsync(Generation).ConfigureAwait(false);
				await this.ReloadAsync(Generation).ConfigureAwait(false);
			}
		}

		/// <inheritdoc/>
		public override async Task OnDisposeAsync()
		{
			this.Dispose();

			await base.OnDisposeAsync();
		}

		/// <summary>
		/// Releases the search debounce cancellation source owned by this view model.
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Releases resources owned by this view model.
		/// </summary>
		/// <param name="Disposing">
		/// <see langword="true"/> when called from <see cref="Dispose()"/>.
		/// </param>
		protected virtual void Dispose(bool Disposing)
		{
			if (this.isDisposed)
				return;

			this.isDisposed = true;
			if (Disposing)
			{
				Interlocked.Increment(ref this.queryGeneration);
				CancellationTokenSource? Cancellation =
					Interlocked.Exchange(ref this.searchDebounceCancellation, null);
				Cancellation?.Cancel();
				Cancellation?.Dispose();
				this.selection?.TrySetResult(this.selectedContract);
			}
		}

		/// <summary>
		/// Gets or sets the contract search text bound by the collection view.
		/// </summary>
		public string SearchText
		{
			get => this.searchText;
			set => this.UpdateSearch(value);
		}

		/// <summary>
		/// Updates the local database search after a short debounce.
		/// </summary>
		/// <param name="Text">Search text.</param>
		public void UpdateSearch(string? Text)
		{
			string NewSearchText = Text?.Trim() ?? string.Empty;
			if (string.Equals(NewSearchText, this.searchText, StringComparison.Ordinal))
				return;

			this.searchText = NewSearchText;
			this.OnPropertyChanged(nameof(this.SearchText));
			this.OnPropertyChanged(nameof(this.CanRecoverContracts));
			long Generation = Interlocked.Increment(ref this.queryGeneration);

			this.searchDebounceCancellation?.Cancel();
			this.searchDebounceCancellation?.Dispose();
			this.searchDebounceCancellation = new CancellationTokenSource();
			_ = this.DebounceSearchAsync(Generation, this.searchDebounceCancellation.Token);
		}

		private async Task DebounceSearchAsync(long Generation, CancellationToken CancellationToken)
		{
			try
			{
				await Task.Delay(searchDebounceMilliseconds, CancellationToken).ConfigureAwait(false);
				if (!CancellationToken.IsCancellationRequested)
					await this.ReloadAsync(Generation).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
			}
		}

		/// <summary>
		/// Opens the category-filter popup.
		/// </summary>
		[RelayCommand]
		private async Task OpenFilterPopup()
		{
			FilterContractsPopupViewModel ViewModel = new(this.FilterTags);
			SelectableTag? SelectedTag =
				await ServiceRef.PopupService.PushAsync<
					FilterContractsPopup,
					FilterContractsPopupViewModel,
					SelectableTag>(ViewModel);

			if (SelectedTag is not null)
				await this.FilterChanged(SelectedTag);
		}

		/// <summary>
		/// Applies a selected category while preserving the current search.
		/// </summary>
		/// <param name="Parameter">Selected category tag.</param>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task FilterChanged(object? Parameter)
		{
			if (Parameter is not SelectableTag Tag)
				return;

			string NewCategory =
				Tag.IsSelected && !string.IsNullOrEmpty(Tag.FilterValue)
					? string.Empty
					: Tag.FilterValue;

			foreach (SelectableTag ExistingTag in this.FilterTags)
				ExistingTag.IsSelected = string.Equals(
					ExistingTag.FilterValue,
					NewCategory,
					StringComparison.OrdinalIgnoreCase);

			this.currentCategory = NewCategory;
			this.OnPropertyChanged(nameof(this.CanRecoverContracts));
			SelectableTag? SelectedTag = this.FilterTags.FirstOrDefault(Item => Item.IsSelected);
			if (SelectedTag is not null)
				this.TagSelected?.Invoke(SelectedTag);

			long Generation = Interlocked.Increment(ref this.queryGeneration);
			await this.ReloadAsync(Generation).ConfigureAwait(false);
		}

		/// <summary>
		/// Retries the current local database query.
		/// </summary>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task Retry()
		{
			long Generation = Interlocked.Increment(ref this.queryGeneration);
			await this.ReloadAsync(Generation).ConfigureAwait(false);
		}

		/// <summary>
		/// Recovers missing server contract IDs without loading contract contents.
		/// </summary>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task RecoverContracts()
		{
			if (!this.CanRecoverContracts)
				return;

			this.IsRecoveringContracts = true;
			try
			{
				await ServiceRef.XmppService.RecoverContractReferences();
				long Generation = Interlocked.Increment(ref this.queryGeneration);
				await this.LoadCategoriesAsync(Generation);
				await this.ReloadAsync(Generation);
			}
			catch
			{
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.Error)],
					ServiceRef.Localizer[nameof(AppResources.SomethingWentWrong)],
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
			}
			finally
			{
				this.IsRecoveringContracts = false;
			}
		}

		/// <summary>
		/// Loads another local database page.
		/// </summary>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task LoadMoreContracts()
		{
			if (this.HasMore < 0 || this.IsInitialLoading)
				return;

			await this.LoadContractsAsync(this.queryGeneration, false).ConfigureAwait(false);
		}

		private async Task ReloadAsync(long Generation)
		{
			if (!this.IsCurrentGeneration(Generation))
				return;

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.Contracts.Clear();
				this.loadedContracts = 0;
				this.HasContracts = false;
				this.AttentionContractsCount = 0;
				this.HasMore = -1;
				this.HasLoadError = false;
				this.LoadErrorMessage = string.Empty;
				this.IsInitialLoading = true;
			});

			await this.LoadNotificationSnapshotAsync(Generation).ConfigureAwait(false);
			await this.LoadContractsAsync(Generation, true).ConfigureAwait(false);
		}

		private async Task LoadContractsAsync(long Generation, bool IsInitialPage)
		{
			if (!this.IsCurrentGeneration(Generation))
				return;

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				if (!IsInitialPage)
					this.IsLoadingMore = true;
			});

			try
			{
				ContractReference[] References = (await this.LoadFromDatabaseAsync().ConfigureAwait(false)).ToArray();
				List<ContractModel> Models = await Task.Run(async () =>
				{
					List<ContractModel> Result = new(References.Length);
					foreach (ContractReference Reference in References)
					{
						string ContractId = Convert.ToString(Reference.ContractId) ?? string.Empty;
						NotificationEvent[] LegacyEvents =
							this.legacyNotificationsByContractId.TryGetValue(
								ContractId,
								out NotificationEvent[]? FoundLegacyEvents)
								? FoundLegacyEvents
								: [];
						NotificationRecord[] NotificationRecords =
							this.notificationsByContractId.TryGetValue(
								ContractId,
								out NotificationRecord[]? FoundNotificationRecords)
								? FoundNotificationRecords
								: [];
						ContractModel Model = await ContractSummaryFactory.CreateAsync(
							Reference,
							LegacyEvents,
							NotificationRecords,
							this.contractsListMode).ConfigureAwait(false);
						Result.Add(Model);
					}

					return Result;
				}).ConfigureAwait(false);

				if (!this.IsCurrentGeneration(Generation))
					return;

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					foreach (ContractModel Model in Models)
						this.InsertContractModel(Model);

					this.loadedContracts += References.Length;
					this.HasContracts = this.Contracts.Count > 0;
					this.HasMore = References.Length < contractBatchSize
						? -1
						: remainingItemsThreshold;
					this.HasLoadError = false;
					this.LoadErrorMessage = string.Empty;
				});

				await this.EnsureRequiredTokenTemplatesAsync(Generation).ConfigureAwait(false);
			}
			catch (Exception Ex)
			{
				if (!this.IsCurrentGeneration(Generation))
					return;

				LogContractCollectionFailure("Load", Ex);
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.HasLoadError = true;
					this.LoadErrorMessage =
						ServiceRef.Localizer[nameof(AppResources.ContractsUnavailableDescription)];
					this.HasMore = -1;
				});
			}
			finally
			{
				if (this.IsCurrentGeneration(Generation))
				{
					await MainThread.InvokeOnMainThreadAsync(() =>
					{
						this.IsInitialLoading = false;
						this.IsLoadingMore = false;
						this.OnPropertyChanged(nameof(this.ShowEmptyState));
					});
				}
			}
		}

		private async Task LoadNotificationSnapshotAsync(long Generation)
		{
			Dictionary<string, List<NotificationEvent>> LegacyGroups =
				new(StringComparer.OrdinalIgnoreCase);
			Dictionary<string, List<NotificationRecord>> CurrentGroups =
				new(StringComparer.OrdinalIgnoreCase);

			if (this.contractsListMode == ContractsListMode.Contracts)
			{
				try
				{
					foreach (NotificationEvent Event in
						ServiceRef.NotificationService.GetEvents(NotificationEventType.Contracts))
					{
						if (Event is not ContractNotificationEvent ContractEvent ||
							string.IsNullOrWhiteSpace(ContractEvent.ContractId))
						{
							continue;
						}

						if (!LegacyGroups.TryGetValue(
							ContractEvent.ContractId,
							out List<NotificationEvent>? Events))
						{
							Events = [];
							LegacyGroups[ContractEvent.ContractId] = Events;
						}

						Events.Add(Event);
					}
				}
				catch (Exception Ex)
				{
					LogContractCollectionFailure("LegacyNotifications", Ex);
				}

				try
				{
					INotificationServiceV2 NotificationService =
						ServiceRef.Provider.GetRequiredService<INotificationServiceV2>();
					IReadOnlyList<NotificationRecord> Records =
						await NotificationService.GetAsync(
							new NotificationQuery
							{
								Channels = [Constants.PushChannels.Contracts],
								States =
								[
									NotificationState.New,
									NotificationState.Delivered,
									NotificationState.Read
								],
								Limit = 100
							},
							CancellationToken.None).ConfigureAwait(false);

					foreach (NotificationRecord Record in Records)
					{
						if (string.IsNullOrWhiteSpace(Record.EntityId))
							continue;

						if (!CurrentGroups.TryGetValue(
							Record.EntityId,
							out List<NotificationRecord>? ContractRecords))
						{
							ContractRecords = [];
							CurrentGroups[Record.EntityId] = ContractRecords;
						}

						ContractRecords.Add(Record);
					}
				}
				catch (Exception Ex)
				{
					LogContractCollectionFailure("Notifications", Ex);
				}
			}

			if (!this.IsCurrentGeneration(Generation))
				return;

			this.legacyNotificationsByContractId = LegacyGroups.ToDictionary(
				Pair => Pair.Key,
				Pair => Pair.Value.ToArray(),
				StringComparer.OrdinalIgnoreCase);
			this.notificationsByContractId = CurrentGroups.ToDictionary(
				Pair => Pair.Key,
				Pair => Pair.Value.ToArray(),
				StringComparer.OrdinalIgnoreCase);
		}

		private void InsertContractModel(ContractModel Model)
		{
			int InsertIndex = this.Contracts.Count;
			for (int i = 0; i < this.Contracts.Count; i++)
			{
				if (this.Contracts[i].SummaryGroup <= Model.SummaryGroup)
					continue;

				InsertIndex = i;
				break;
			}

			this.Contracts.Insert(InsertIndex, Model);
			if (Model.IsNeedsAttention)
				this.AttentionContractsCount++;
		}

		private async Task<IEnumerable<ContractReference>> LoadFromDatabaseAsync()
		{
			Filter Filter = this.CreateDatabaseFilter();
			return await Database.Find<ContractReference>(
				this.loadedContracts,
				contractBatchSize,
				Filter,
				"-Updated",
				"-Created",
				nameof(ContractReference.ContractId)).ConfigureAwait(false);
		}

		private Filter CreateDatabaseFilter()
		{
			List<Filter> Filters =
			[
				new FilterFieldEqualTo(
					nameof(ContractReference.IsTemplate),
					this.contractsListMode != ContractsListMode.Contracts)
			];

			if (this.contractsListMode != ContractsListMode.Contracts)
			{
				Filters.Add(new FilterFieldEqualTo(
					nameof(ContractReference.IsTokenCreationTemplate),
					this.contractsListMode == ContractsListMode.TokenCreationTemplates));
			}

			if (!string.IsNullOrWhiteSpace(this.currentCategory))
			{
				Filters.Add(new FilterFieldEqualTo(
					nameof(ContractReference.Category),
					this.currentCategory));
			}

			if (!string.IsNullOrWhiteSpace(this.searchText))
			{
				string SearchPattern = "(?i).*" + Regex.Escape(this.searchText) + ".*";
				Filters.Add(new FilterOr(
					new FilterFieldLikeRegEx(nameof(ContractReference.Name), SearchPattern),
					new FilterFieldLikeRegEx(nameof(ContractReference.Category), SearchPattern),
					new FilterFieldLikeRegEx(nameof(ContractReference.ContractId), SearchPattern)));
			}

			return Filters.Count == 1 ? Filters[0] : new FilterAnd(Filters.ToArray());
		}

		private async Task LoadCategoriesAsync(long Generation)
		{
			string ModeCondition = this.contractsListMode switch
			{
				ContractsListMode.Contracts =>
					"IsTemplate=false",
				ContractsListMode.ContractTemplates =>
					"IsTemplate=true and IsTokenCreationTemplate=false",
				ContractsListMode.TokenCreationTemplates =>
					"IsTemplate=true and IsTokenCreationTemplate=true",
				_ => "IsTemplate=false"
			};

			List<string> Categories = [];
			try
			{
				object Result = await Expression.EvalAsync(
					$"select distinct Category from NeuroAccessMaui.Services.Contracts.ContractReference where {ModeCondition}")
					.ConfigureAwait(false);

				if (Result is object[] Items)
				{
					foreach (object Item in Items)
					{
						if (Item is string Category && !string.IsNullOrWhiteSpace(Category))
							Categories.Add(Category);
					}
				}
			}
			catch (Exception Ex)
			{
				LogContractCollectionFailure("Categories", Ex);
			}

			if (this.isDisposed)
				return;

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.FilterTags.Clear();
				this.tagMap.Clear();

				SelectableTag AllTag = new(
					ServiceRef.Localizer[nameof(AppResources.All)],
					string.Empty,
					string.IsNullOrEmpty(this.currentCategory));
				this.FilterTags.Add(AllTag);
				this.tagMap[string.Empty] = AllTag;

				foreach (string Category in Categories
					.Distinct(StringComparer.CurrentCultureIgnoreCase)
					.OrderBy(Value => Value, StringComparer.CurrentCultureIgnoreCase))
				{
					SelectableTag Tag = new(
						Category,
						Category,
						string.Equals(
							Category,
							this.currentCategory,
							StringComparison.OrdinalIgnoreCase));
					this.FilterTags.Add(Tag);
					this.tagMap[Category] = Tag;
				}
			});
		}

		private async Task EnsureRequiredTokenTemplatesAsync(long Generation)
		{
			if (this.contractsListMode != ContractsListMode.TokenCreationTemplates ||
				this.tokenTemplatesEnsured ||
				!string.IsNullOrWhiteSpace(this.searchText) ||
				!string.IsNullOrWhiteSpace(this.currentCategory))
			{
				return;
			}

			this.tokenTemplatesEnsured = true;
			bool InsertedTemplate = false;

			foreach (string TokenTemplateId in Constants.ContractTemplates.TokenCreationTemplates)
			{
				if (!this.IsCurrentGeneration(Generation))
					return;

				ContractReference? Existing =
					await Database.FindFirstIgnoreRest<ContractReference>(new FilterAnd(
						new FilterFieldEqualTo(nameof(ContractReference.IsTemplate), true),
						new FilterFieldEqualTo(nameof(ContractReference.ContractId), TokenTemplateId)))
					.ConfigureAwait(false);

				if (Existing is not null)
					continue;

				try
				{
					// This is the only collection-time remote path: a fixed compatibility
					// set of required token templates, never one request per displayed row.
					Contract? Contract = await ServiceRef.XmppService.GetContract(TokenTemplateId)
						.ConfigureAwait(false);
					if (Contract is null)
						continue;

					ContractReference Reference = new()
						{
							ContractId = Contract.ContractId
						};
					await Reference.SetContract(Contract).ConfigureAwait(false);
					await Database.Insert(Reference).ConfigureAwait(false);
					InsertedTemplate = true;
				}
				catch (Exception Ex)
				{
					LogContractCollectionFailure("RequiredTemplate", Ex);
				}
			}

			if (!InsertedTemplate || !this.IsCurrentGeneration(Generation))
				return;

			long NewGeneration = Interlocked.Increment(ref this.queryGeneration);
			await this.LoadCategoriesAsync(NewGeneration).ConfigureAwait(false);
			await this.ReloadAsync(NewGeneration).ConfigureAwait(false);
		}

		/// <summary>
		/// Opens or selects a contract summary.
		/// </summary>
		/// <param name="Parameter">Selected summary.</param>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task ContractSelected(object? Parameter)
		{
			if (Parameter is ContractModel Model)
				await this.ContractSelectedAsync(Model);
		}

		/// <summary>
		/// Opens or selects the supplied contract summary.
		/// </summary>
		/// <param name="Model">Selected contract summary.</param>
		public async Task ContractSelectedAsync(ContractModel Model)
		{
			ContractReference Reference = Model.ContractRef;
			if (!Model.CanOpen)
			{
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.ContractReferenceUnavailable)],
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
				return;
			}

			try
			{
				switch (this.Action)
				{
					case SelectContractAction.ViewContract:
						if (this.contractsListMode == ContractsListMode.Contracts)
						{
							this.refreshOnNextAppearance = true;
							if (Model.LocalContract is null)
							{
								// Present the loading workspace first. Hydration and persistence
								// then happen inside the destination without blocking this collection.
								await ServiceRef.ContractOrchestratorService.OpenContract(
									Reference,
									Model.ProposalRole,
									Model.ProposalMessage,
									Model.ProposalFromJid).ConfigureAwait(false);
							}
							else
							{
								await ServiceRef.ContractOrchestratorService.OpenContract(
									Model.LocalContract,
									ServiceRef.Localizer[nameof(AppResources.RequestToAccessContract)],
									null,
									Reference,
									Model.ProposalRole,
									Model.ProposalMessage,
									Model.ProposalFromJid).ConfigureAwait(false);
							}
						}
						else
						{
							await ServiceRef.ContractOrchestratorService.OpenContract(
								Reference.ContractId,
								ServiceRef.Localizer[nameof(AppResources.ReferencedID)],
								null).ConfigureAwait(false);
						}
						break;

					case SelectContractAction.Select:
						Contract SelectedContract = await this.GetOrRecoverContractAsync(Model)
							.ConfigureAwait(false);
						this.selectedContract = SelectedContract;
						this.selection?.TrySetResult(SelectedContract);
						await MainThread.InvokeOnMainThreadAsync(this.GoBack);
						break;
				}
			}
			catch (Exception Ex)
			{
				LogContractCollectionFailure("Open", Ex);
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.ContractSavedDetailsUnavailable)],
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
			}
		}

		private async Task<Contract> GetOrRecoverContractAsync(ContractModel Model)
		{
			if (Model.LocalContract is not null)
				return Model.LocalContract;

			ContractReference Reference = Model.ContractRef;
			string ContractId = Convert.ToString(Reference.ContractId) ?? string.Empty;
			Contract Contract = await ServiceRef.XmppService.GetContract(ContractId)
				.ConfigureAwait(false);
			if (string.IsNullOrWhiteSpace(Contract.ContractId) ||
				!string.Equals(
					Contract.ContractId,
					ContractId,
					StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException(
					"The downloaded contract did not match the saved reference identifier.");
			}

			// Recovery overwrites only the same persisted reference after an explicit open.
			// Missing or malformed XML is never deleted merely because summary parsing failed.
			await Reference.SetContract(Contract).ConfigureAwait(false);
			await Database.Update(Reference).ConfigureAwait(false);
			return Contract;
		}

		/// <summary>
		/// Compatibility helper for existing direct callers.
		/// </summary>
		/// <param name="Model">Selected contract summary.</param>
		public void ContractSelected(ContractModel Model)
		{
			_ = this.ContractSelectedAsync(Model);
		}

		/// <summary>
		/// Presents a template reference as a QR code.
		/// </summary>
		/// <param name="Parameter">Template summary.</param>
		[RelayCommand]
		private async Task ShareTemplateQR(object? Parameter)
		{
			try
			{
				if (Parameter is not ContractModel Model ||
					string.IsNullOrWhiteSpace(Model.ContractIdUriString))
				{
					return;
				}

				int Width = Constants.QrCode.DefaultImageWidth;
				int Height = Constants.QrCode.DefaultImageHeight;
				byte[] QrBytes = Services.UI.QR.QrCode.GeneratePng(
					Model.ContractIdUriString,
					Width,
					Height);

				ShowQRPopup Popup = new(
					QrBytes,
					Model.ContractIdUriString,
					Model.Title);
				await ServiceRef.PopupService.PushAsync(Popup);
			}
			catch (Exception Ex)
			{
				LogContractCollectionFailure("Share", Ex);
			}
		}

		private static void LogContractCollectionFailure(string Operation, Exception Ex)
		{
			ServiceRef.LogService.LogWarning(
				"Contract collection operation failed.",
				new KeyValuePair<string, object?>("Operation", Operation),
				new KeyValuePair<string, object?>("FailureType", Ex.GetType().Name));
		}

		private bool IsCurrentGeneration(long Generation)
		{
			return !this.isDisposed && Generation == Interlocked.Read(ref this.queryGeneration);
		}

		/// <summary>
		/// Represents one selectable category filter.
		/// </summary>
		public partial class SelectableTag : ObservableObject
		{
			/// <summary>
			/// Initializes a category filter.
			/// </summary>
			/// <param name="Category">Localized display label.</param>
			/// <param name="FilterValue">Persisted category value, or empty for all categories.</param>
			/// <param name="IsSelected">Initial selection state.</param>
			public SelectableTag(string Category, string FilterValue, bool IsSelected)
			{
				this.category = Category;
				this.FilterValue = FilterValue;
				this.isSelected = IsSelected;
			}

			/// <summary>
			/// Initializes a compatibility category filter.
			/// </summary>
			/// <param name="Category">Category label and filter value.</param>
			/// <param name="IsSelected">Initial selection state.</param>
			public SelectableTag(string Category, bool IsSelected)
				: this(Category, Category, IsSelected)
			{
			}

			/// <summary>
			/// Gets or sets the category display label.
			/// </summary>
			[ObservableProperty]
			private string category;

			/// <summary>
			/// Gets the persisted category value used by the database filter.
			/// </summary>
			public string FilterValue { get; }

			/// <summary>
			/// Gets or sets whether the category is selected.
			/// </summary>
			[ObservableProperty]
			private bool isSelected;

			/// <summary>
			/// Toggles the selected state.
			/// </summary>
			public void ToggleSelection()
			{
				this.IsSelected = !this.IsSelected;
			}
		}
	}
}
