using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.UI.Pages.Wallet.MyTokens.ObjectModels;
using NeuroFeatures;
using NeuroFeatures.EventArguments;
using NeuroFeatures.Tags;
using Waher.Persistence;

namespace NeuroAccessMaui.UI.Pages.Wallet.MyTokens
{
	/// <summary>
	/// Provides the searchable, pageable token portfolio experience.
	/// </summary>
	public partial class MyTokensViewModel : XmppViewModel
	{
		private const int openingFeedbackMilliseconds = 50;

		private readonly MyTokensNavigationArgs? navigationArgs;
		private readonly List<Token> loadedTokens = [];
		private readonly HashSet<string> loadedTokenIds = new(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, NotificationEvent[]> notificationEventsByTokenId =
			new(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, TokenSummaryItem> summaryByTokenId =
			new(StringComparer.OrdinalIgnoreCase);
		private readonly HashSet<string> pendingTokenRefreshIds = new(StringComparer.OrdinalIgnoreCase);
		private readonly HashSet<string> activeTokenRefreshIds = new(StringComparer.OrdinalIgnoreCase);
		private readonly object loadedTokensSync = new();
		private readonly SemaphoreSlim loadGate = new(1, 1);
		private CancellationTokenSource? queryCancellationTokenSource;
		private int nextOffset;
		private long queryGeneration;
		private bool serverHasMoreTokens;
		private bool initialLoadFailed;
		private bool suppressQueryChanges;
		private bool navigationMessageConsumed;
		private bool hasAppeared;
		private bool isDisposed;

		private string RelatedContractId =>
			this.navigationArgs?.RelatedContractId?.Trim() ?? string.Empty;

		/// <summary>
		/// Initializes a new instance of the <see cref="MyTokensViewModel"/> class.
		/// </summary>
		/// <param name="Args">Optional navigation and token-selection context.</param>
		public MyTokensViewModel(MyTokensNavigationArgs? Args)
		{
			this.navigationArgs = Args;
			this.LifecycleOptions =
			[
				ServiceRef.Localizer[nameof(AppResources.All)],
				ServiceRef.Localizer[nameof(AppResources.FilterActive)],
				ServiceRef.Localizer[nameof(AppResources.FilterExpired)]
			];
			this.AttentionOptions =
			[
				ServiceRef.Localizer[nameof(AppResources.All)],
				ServiceRef.Localizer[nameof(AppResources.FilterNeedsAttention)]
			];
			this.OwnershipOptions =
			[
				ServiceRef.Localizer[nameof(AppResources.All)],
				ServiceRef.Localizer[nameof(AppResources.OwnedByYou)],
				ServiceRef.Localizer[nameof(AppResources.Other)]
			];
			this.SortOptions =
			[
				ServiceRef.Localizer[nameof(AppResources.RecentlyUpdated)],
				ServiceRef.Localizer[nameof(AppResources.Name)],
				ServiceRef.Localizer[nameof(AppResources.SortByExpiry)],
				ServiceRef.Localizer[nameof(AppResources.SortByValue)]
			];
			this.CategoryOptions.Add(ServiceRef.Localizer[nameof(AppResources.All)]);
		}

		private TaskCompletionSource<Token?>? SelectionProvider =>
			this.navigationArgs?.IsSelectionMode == true
				? this.navigationArgs.TokenProvider
				: null;

		/// <summary>
		/// Gets the filtered token summaries shown in the collection.
		/// </summary>
		public ObservableCollection<TokenSummaryItem> Tokens { get; } = [];

		/// <summary>
		/// Gets the collection subtitle for the current navigation context.
		/// </summary>
		public string CollectionSubtitle => string.IsNullOrEmpty(this.RelatedContractId)
			? ServiceRef.Localizer[nameof(AppResources.TokensSubtitle)]
			: ServiceRef.Localizer[nameof(AppResources.TokensCreatedByAgreementDescription)];

		/// <summary>
		/// Gets the localized lifecycle filter choices.
		/// </summary>
		public IReadOnlyList<string> LifecycleOptions { get; }

		/// <summary>
		/// Gets the localized attention filter choices.
		/// </summary>
		public IReadOnlyList<string> AttentionOptions { get; }

		/// <summary>
		/// Gets the localized ownership filter choices.
		/// </summary>
		public IReadOnlyList<string> OwnershipOptions { get; }

		/// <summary>
		/// Gets the localized sorting choices.
		/// </summary>
		public IReadOnlyList<string> SortOptions { get; }

		/// <summary>
		/// Gets the localized category choices discovered from loaded tokens.
		/// </summary>
		public ObservableCollection<string> CategoryOptions { get; } = [];

		/// <inheritdoc/>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			ServiceRef.XmppService.NeuroFeatureAdded += this.Wallet_TokenAdded;
			ServiceRef.XmppService.NeuroFeatureRemoved += this.Wallet_TokenRemoved;
			ServiceRef.XmppService.NeuroFeatureStateUpdated += this.Wallet_TokenStateUpdated;
			ServiceRef.XmppService.NeuroFeatureVariablesUpdated += this.Wallet_TokenVariablesUpdated;

			await this.RefreshTokens();
		}

		/// <inheritdoc/>
		public override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();

			if (this.hasAppeared)
				await this.RefreshTokens();
			else
				this.hasAppeared = true;
		}

		/// <inheritdoc/>
		public override Task OnDisposeAsync()
		{
			this.isDisposed = true;
			ServiceRef.XmppService.NeuroFeatureAdded -= this.Wallet_TokenAdded;
			ServiceRef.XmppService.NeuroFeatureRemoved -= this.Wallet_TokenRemoved;
			ServiceRef.XmppService.NeuroFeatureStateUpdated -= this.Wallet_TokenStateUpdated;
			ServiceRef.XmppService.NeuroFeatureVariablesUpdated -= this.Wallet_TokenVariablesUpdated;

			this.queryCancellationTokenSource?.Cancel();
			this.queryCancellationTokenSource?.Dispose();
			this.queryCancellationTokenSource = null;
			this.SelectionProvider?.TrySetResult(null);

			return base.OnDisposeAsync();
		}

		/// <inheritdoc/>
		public override Task GoBack()
		{
			this.SelectionProvider?.TrySetResult(null);
			return base.GoBack();
		}

		#region Observable properties

		/// <summary>
		/// Gets or sets the in-memory token search query.
		/// </summary>
		[ObservableProperty]
		private string? searchText;

		/// <summary>
		/// Gets or sets the selected lifecycle filter index.
		/// </summary>
		[ObservableProperty]
		private int selectedLifecycleIndex;

		/// <summary>
		/// Gets or sets the selected attention filter index.
		/// </summary>
		[ObservableProperty]
		private int selectedAttentionIndex;

		/// <summary>
		/// Gets or sets the selected category filter index.
		/// </summary>
		[ObservableProperty]
		private int selectedCategoryIndex;

		/// <summary>
		/// Gets or sets the selected ownership filter index.
		/// </summary>
		[ObservableProperty]
		private int selectedOwnershipIndex;

		/// <summary>
		/// Gets or sets the selected sorting index.
		/// </summary>
		[ObservableProperty]
		private int selectedSortIndex;

		/// <summary>
		/// Gets or sets a value indicating whether the filter panel is visible.
		/// </summary>
		[ObservableProperty]
		private bool isFilterPanelVisible;

		/// <summary>
		/// Gets or sets a value indicating whether the first page is loading.
		/// </summary>
		[ObservableProperty]
		private bool isInitialLoading;

		/// <summary>
		/// Gets or sets a value indicating whether a manual refresh is running.
		/// </summary>
		[ObservableProperty]
		private bool isRefreshing;

		/// <summary>
		/// Gets or sets a value indicating whether another server page is loading.
		/// </summary>
		[ObservableProperty]
		private bool isLoadingMore;

		/// <summary>
		/// Gets or sets a value indicating whether remaining pages are being scanned for the active query.
		/// </summary>
		[ObservableProperty]
		private bool isScanning;

		/// <summary>
		/// Gets or sets a value indicating whether filtered token content is visible.
		/// </summary>
		[ObservableProperty]
		private bool showContent;

		/// <summary>
		/// Gets or sets a value indicating whether the initial loading state is visible.
		/// </summary>
		[ObservableProperty]
		private bool showInitialLoading;

		/// <summary>
		/// Gets or sets a value indicating whether the true empty-wallet state is visible.
		/// </summary>
		[ObservableProperty]
		private bool showEmptyState;

		/// <summary>
		/// Gets or sets a value indicating whether the no-matching-results state is visible.
		/// </summary>
		[ObservableProperty]
		private bool showNoResultsState;

		/// <summary>
		/// Gets or sets a value indicating whether the initial error state is visible.
		/// </summary>
		[ObservableProperty]
		private bool showErrorState;

		/// <summary>
		/// Gets or sets a value indicating whether a non-blocking partial-results warning is visible.
		/// </summary>
		[ObservableProperty]
		private bool showPartialState;

		/// <summary>
		/// Gets or sets the localized blocking error message.
		/// </summary>
		[ObservableProperty]
		private string errorMessage = string.Empty;

		/// <summary>
		/// Gets or sets the localized partial-results message.
		/// </summary>
		[ObservableProperty]
		private string partialMessage = string.Empty;

		/// <summary>
		/// Gets or sets the number of active narrowing filters.
		/// </summary>
		[ObservableProperty]
		private int activeFilterCount;

		/// <summary>
		/// Gets or sets a value indicating whether any narrowing filter is active.
		/// </summary>
		[ObservableProperty]
		private bool hasActiveFilters;

		#endregion

		partial void OnSearchTextChanged(string? Value)
		{
			this.QueryChanged();
		}

		partial void OnSelectedLifecycleIndexChanged(int Value)
		{
			this.QueryChanged();
		}

		partial void OnSelectedAttentionIndexChanged(int Value)
		{
			this.QueryChanged();
		}

		partial void OnSelectedCategoryIndexChanged(int Value)
		{
			this.QueryChanged();
		}

		partial void OnSelectedOwnershipIndexChanged(int Value)
		{
			this.QueryChanged();
		}

		partial void OnSelectedSortIndexChanged(int Value)
		{
			this.QueryChanged();
		}

		/// <summary>
		/// Refreshes the portfolio from its authoritative server source.
		/// </summary>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task RefreshTokens()
		{
			this.CancelQueryScan();
			Interlocked.Increment(ref this.queryGeneration);

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.IsRefreshing = true;
				this.IsInitialLoading = this.Tokens.Count == 0;
				this.InitialLoadFailed = false;
				this.ErrorMessage = string.Empty;
				this.UpdatePageState();
			});

			await this.loadGate.WaitAsync();
			try
			{
				TokensEventArgs Result = await this.GetTokenPageAsync(
					0,
					Constants.BatchSizes.TokenBatchSize);

				if (!Result.Ok)
					throw new InvalidOperationException(ServiceRef.Localizer[nameof(AppResources.TokensUnavailableDescription)]);

				Token[] FreshTokens = Result.Tokens ?? [];
				SortedDictionary<CaseInsensitiveString, NotificationEvent[]> EventsByCategory =
					ServiceRef.NotificationService.GetEventsByCategory(NotificationEventType.Wallet);

				lock (this.loadedTokensSync)
				{
					this.loadedTokens.Clear();
					this.loadedTokenIds.Clear();
					this.notificationEventsByTokenId.Clear();
					this.summaryByTokenId.Clear();

					foreach (Token Token in FreshTokens)
					{
						if (string.IsNullOrWhiteSpace(Token.TokenId) || !this.loadedTokenIds.Add(Token.TokenId))
							continue;

						this.loadedTokens.Add(Token);
						this.notificationEventsByTokenId[Token.TokenId] =
							EventsByCategory.TryGetValue(Token.TokenId, out NotificationEvent[]? Events)
								? Events
								: [];
					}

					this.nextOffset = FreshTokens.Length;
					this.serverHasMoreTokens = FreshTokens.Length == Constants.BatchSizes.TokenBatchSize;
				}

				long CurrentGeneration = Volatile.Read(ref this.queryGeneration);
				await this.ApplyCurrentQueryAsync(CurrentGeneration);

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.PartialMessage = this.GetInitialNavigationMessage();
					this.UpdatePageState();
				});
			}
			catch (Exception ex)
			{
				LogRedactedFailure("Token collection refresh failed.", ex);
				lock (this.loadedTokensSync)
					this.serverHasMoreTokens = false;

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					bool HasLoadedTokens;
					lock (this.loadedTokensSync)
						HasLoadedTokens = this.loadedTokens.Count > 0;

					if (!HasLoadedTokens)
					{
						this.InitialLoadFailed = true;
						this.ErrorMessage = ServiceRef.Localizer[nameof(AppResources.TokensUnavailableDescription)];
					}
					else
					{
						this.PartialMessage = ServiceRef.Localizer[nameof(AppResources.PartialTokenResults)];
					}

					this.UpdatePageState();
				});
			}
			finally
			{
				this.loadGate.Release();

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.IsInitialLoading = false;
					this.IsRefreshing = false;
					this.UpdatePageState();
				});
			}

			long LatestGeneration = Volatile.Read(ref this.queryGeneration);
			if (this.RequiresFullCollectionScan())
				this.StartQueryScan(LatestGeneration);
		}

		/// <summary>
		/// Loads the next server page while preserving the current query.
		/// </summary>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task LoadMoreTokens()
		{
			if (this.IsLoadingMore || this.IsScanning)
				return;

			lock (this.loadedTokensSync)
			{
				if (!this.serverHasMoreTokens)
					return;
			}

			this.IsLoadingMore = true;

			try
			{
				await this.LoadNextPageAsync(CancellationToken.None);
				long Generation = Volatile.Read(ref this.queryGeneration);
				await this.ApplyCurrentQueryAsync(Generation);
			}
			catch (Exception ex)
			{
				LogRedactedFailure("Token collection page load failed.", ex);
				lock (this.loadedTokensSync)
					this.serverHasMoreTokens = false;
				this.PartialMessage = ServiceRef.Localizer[nameof(AppResources.PartialTokenResults)];
				this.UpdatePageState();
			}
			finally
			{
				this.IsLoadingMore = false;
			}
		}

		/// <summary>
		/// Shows or hides the token filter panel.
		/// </summary>
		[RelayCommand]
		private void ToggleFilterPanel()
		{
			this.IsFilterPanelVisible = !this.IsFilterPanelVisible;
		}

		/// <summary>
		/// Closes the token filter panel.
		/// </summary>
		[RelayCommand]
		private void CloseFilterPanel()
		{
			this.IsFilterPanelVisible = false;
		}

		/// <summary>
		/// Clears search and all narrowing filters without changing the selected sort order.
		/// </summary>
		[RelayCommand]
		private void ClearFilters()
		{
			this.suppressQueryChanges = true;
			this.SearchText = string.Empty;
			this.SelectedLifecycleIndex = 0;
			this.SelectedAttentionIndex = 0;
			this.SelectedCategoryIndex = 0;
			this.SelectedOwnershipIndex = 0;
			this.suppressQueryChanges = false;
			this.QueryChanged();
		}

		private bool InitialLoadFailed
		{
			get => this.initialLoadFailed;
			set => this.initialLoadFailed = value;
		}

		private async Task ActivateTokenAsync(TokenSummaryItem Item)
		{
			if (this.SelectionProvider is TaskCompletionSource<Token?> Selection)
			{
				Selection.TrySetResult(Item.Token);
				await ServiceRef.UiService.GoBackAsync();
				return;
			}

			await Task.Delay(openingFeedbackMilliseconds);
			await ServiceRef.NeuroWalletOrchestratorService.OpenTokenAsync(Item.TokenId, Item.Token);
		}

		private async Task LoadNextPageAsync(CancellationToken CancellationToken)
		{
			await this.loadGate.WaitAsync(CancellationToken);
			try
			{
				int Offset;
				lock (this.loadedTokensSync)
				{
					if (!this.serverHasMoreTokens)
						return;

					Offset = this.nextOffset;
				}

				TokensEventArgs Result = await this.GetTokenPageAsync(
					Offset,
					Constants.BatchSizes.TokenBatchSize);
				CancellationToken.ThrowIfCancellationRequested();

				if (!Result.Ok)
					throw new InvalidOperationException(ServiceRef.Localizer[nameof(AppResources.TokensUnavailableDescription)]);

				Token[] Page = Result.Tokens ?? [];
				SortedDictionary<CaseInsensitiveString, NotificationEvent[]> EventsByCategory =
					ServiceRef.NotificationService.GetEventsByCategory(NotificationEventType.Wallet);

				lock (this.loadedTokensSync)
				{
					foreach (Token Token in Page)
					{
						if (string.IsNullOrWhiteSpace(Token.TokenId) || !this.loadedTokenIds.Add(Token.TokenId))
							continue;

						this.loadedTokens.Add(Token);
						this.notificationEventsByTokenId[Token.TokenId] =
							EventsByCategory.TryGetValue(Token.TokenId, out NotificationEvent[]? Events)
								? Events
								: [];
					}

					this.nextOffset += Page.Length;
					this.serverHasMoreTokens = Page.Length == Constants.BatchSizes.TokenBatchSize;
				}
			}
			finally
			{
				this.loadGate.Release();
			}
		}

		private void QueryChanged()
		{
			if (this.suppressQueryChanges)
				return;

			this.UpdateFilterCount();
			this.CancelQueryScan();

			long Generation = Interlocked.Increment(ref this.queryGeneration);
			_ = this.RefreshQueryAsync(Generation);
		}

		private async Task RefreshQueryAsync(long Generation)
		{
			try
			{
				await this.ApplyCurrentQueryAsync(Generation);

				if (this.RequiresFullCollectionScan())
					this.StartQueryScan(Generation);
			}
			catch (Exception ex)
			{
				LogRedactedFailure("Token collection query refresh failed.", ex);
			}
		}

		private void StartQueryScan(long Generation)
		{
			lock (this.loadedTokensSync)
			{
				if (!this.serverHasMoreTokens)
					return;
			}

			if (Generation != Volatile.Read(ref this.queryGeneration))
				return;

			if (this.queryCancellationTokenSource is not null)
				return;

			CancellationTokenSource QuerySource = new();
			this.queryCancellationTokenSource = QuerySource;
			_ = this.ScanRemainingPagesAsync(Generation, QuerySource);
		}

		private async Task ScanRemainingPagesAsync(long Generation, CancellationTokenSource QuerySource)
		{
			CancellationToken CancellationToken = QuerySource.Token;
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				if (Generation == Volatile.Read(ref this.queryGeneration))
					this.IsScanning = true;
			});

			try
			{
				while (!CancellationToken.IsCancellationRequested &&
					Generation == Volatile.Read(ref this.queryGeneration))
				{
					bool HasMore;
					lock (this.loadedTokensSync)
						HasMore = this.serverHasMoreTokens;

					if (!HasMore)
						break;

					await this.LoadNextPageAsync(CancellationToken);
					await this.ApplyCurrentQueryAsync(Generation);
				}
			}
			catch (OperationCanceledException)
			{
				// A newer search or filter owns the visible result set.
			}
			catch (Exception ex)
			{
				LogRedactedFailure("Token collection scan failed.", ex);
				lock (this.loadedTokensSync)
					this.serverHasMoreTokens = false;

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					if (Generation != Volatile.Read(ref this.queryGeneration))
						return;

					this.PartialMessage = ServiceRef.Localizer[nameof(AppResources.PartialTokenResults)];
					this.UpdatePageState();
				});
			}
			finally
			{
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					if (Generation == Volatile.Read(ref this.queryGeneration))
					{
						this.IsScanning = false;
						if (ReferenceEquals(this.queryCancellationTokenSource, QuerySource))
							this.queryCancellationTokenSource = null;
					}
				});

				QuerySource.Dispose();
			}
		}

		private Task ApplyCurrentQueryAsync(long Generation)
		{
			return MainThread.InvokeOnMainThreadAsync(() =>
			{
				// Only the latest query generation can replace the visible collection.
				if (Generation != Volatile.Read(ref this.queryGeneration))
					return;

				this.UpdateCategoryOptions();
				QuerySnapshot Query = this.CaptureQuery();
				List<Token> TokenSnapshot;
				Dictionary<string, NotificationEvent[]> EventSnapshot;
				Dictionary<string, TokenSummaryItem> SummarySnapshot;

				lock (this.loadedTokensSync)
				{
					TokenSnapshot = [.. this.loadedTokens];
					EventSnapshot = new Dictionary<string, NotificationEvent[]>(
						this.notificationEventsByTokenId,
						StringComparer.OrdinalIgnoreCase);
					SummarySnapshot = new Dictionary<string, TokenSummaryItem>(
						this.summaryByTokenId,
						StringComparer.OrdinalIgnoreCase);
				}

				List<TokenSummaryItem> Summaries = [];
				foreach (Token Token in TokenSnapshot)
				{
					string TokenId = Token.TokenId ?? string.Empty;
					if (!SummarySnapshot.TryGetValue(TokenId, out TokenSummaryItem? Summary) ||
						!ReferenceEquals(Summary.Token, Token))
					{
						NotificationEvent[] Events =
							EventSnapshot.TryGetValue(TokenId, out NotificationEvent[]? TokenEvents)
								? TokenEvents
								: [];
						Summary = new TokenSummaryItem(
							Token,
							Events,
							ServiceRef.XmppService.BareJid,
							this.ActivateTokenAsync);

						lock (this.loadedTokensSync)
						{
							if (this.loadedTokenIds.Contains(TokenId))
								this.summaryByTokenId[TokenId] = Summary;
						}
					}

					if (MatchesQuery(Summary, Query))
						Summaries.Add(Summary);
				}

				SortSummaries(Summaries, Query.Sort);
				this.ReplaceVisibleTokens(Summaries);
				this.UpdatePageState();
			});
		}

		private void ReplaceVisibleTokens(IReadOnlyList<TokenSummaryItem> DesiredTokens)
		{
			int CommonPrefixLength = 0;
			int MaximumPrefixLength = Math.Min(this.Tokens.Count, DesiredTokens.Count);

			while (CommonPrefixLength < MaximumPrefixLength &&
				string.Equals(
					this.Tokens[CommonPrefixLength].TokenId,
					DesiredTokens[CommonPrefixLength].TokenId,
					StringComparison.OrdinalIgnoreCase))
			{
				CommonPrefixLength++;
			}

			while (this.Tokens.Count > CommonPrefixLength)
				this.Tokens.RemoveAt(this.Tokens.Count - 1);

			for (int i = CommonPrefixLength; i < DesiredTokens.Count; i++)
				this.Tokens.Add(DesiredTokens[i]);
		}

		private QuerySnapshot CaptureQuery()
		{
			string Category = this.SelectedCategoryIndex > 0 &&
				this.SelectedCategoryIndex < this.CategoryOptions.Count
					? this.CategoryOptions[this.SelectedCategoryIndex]
					: string.Empty;

			return new QuerySnapshot(
				this.SearchText?.Trim() ?? string.Empty,
				Category,
				this.SelectedLifecycleIndex switch
				{
					1 => TokenLifecycleFilter.Active,
					2 => TokenLifecycleFilter.Expired,
					_ => TokenLifecycleFilter.All
				},
				this.SelectedAttentionIndex == 1
					? TokenAttentionFilter.NeedsAttention
					: TokenAttentionFilter.All,
				this.SelectedOwnershipIndex switch
				{
					1 => TokenOwnershipFilter.OwnedByYou,
					2 => TokenOwnershipFilter.Other,
					_ => TokenOwnershipFilter.All
				},
				this.SelectedSortIndex switch
				{
					1 => TokenSortOrder.Name,
					2 => TokenSortOrder.Expiry,
					3 => TokenSortOrder.Value,
					_ => TokenSortOrder.RecentlyUpdated
				});
		}

		private void UpdateCategoryOptions()
		{
			string SelectedCategory = this.SelectedCategoryIndex > 0 &&
				this.SelectedCategoryIndex < this.CategoryOptions.Count
					? this.CategoryOptions[this.SelectedCategoryIndex]
					: string.Empty;
			string[] Categories;

			lock (this.loadedTokensSync)
			{
				Categories = this.loadedTokens
					.Select(Token => Token.Category?.Trim() ?? string.Empty)
					.Where(Category => !string.IsNullOrEmpty(Category))
					.Distinct(StringComparer.CurrentCultureIgnoreCase)
					.OrderBy(Category => Category, StringComparer.CurrentCultureIgnoreCase)
					.ToArray();
			}

			this.suppressQueryChanges = true;
			this.CategoryOptions.Clear();
			this.CategoryOptions.Add(ServiceRef.Localizer[nameof(AppResources.All)]);

			foreach (string Category in Categories)
				this.CategoryOptions.Add(Category);

			int NewIndex = string.IsNullOrEmpty(SelectedCategory)
				? 0
				: this.CategoryOptions
					.Select((Category, Index) => new { Category, Index })
					.FirstOrDefault(Item =>
						string.Equals(Item.Category, SelectedCategory, StringComparison.CurrentCultureIgnoreCase))
					?.Index ?? 0;
			this.SelectedCategoryIndex = NewIndex;
			this.suppressQueryChanges = false;
			this.UpdateFilterCount();
		}

		private void UpdateFilterCount()
		{
			this.ActiveFilterCount =
				(this.SelectedLifecycleIndex > 0 ? 1 : 0) +
				(this.SelectedAttentionIndex > 0 ? 1 : 0) +
				(this.SelectedCategoryIndex > 0 ? 1 : 0) +
				(this.SelectedOwnershipIndex > 0 ? 1 : 0);
			this.HasActiveFilters = this.ActiveFilterCount > 0;
		}

		private void UpdatePageState()
		{
			bool HasVisibleTokens = this.Tokens.Count > 0;
			bool IsQueryActive = this.IsCollectionNarrowed();

			this.ShowInitialLoading = this.IsInitialLoading && !HasVisibleTokens;
			this.ShowContent = HasVisibleTokens;
			this.ShowErrorState = !this.IsInitialLoading && this.InitialLoadFailed && !HasVisibleTokens;
			this.ShowEmptyState = !this.IsInitialLoading &&
				!this.InitialLoadFailed &&
				!HasVisibleTokens &&
				!IsQueryActive;
			this.ShowNoResultsState = !this.IsInitialLoading &&
				!this.InitialLoadFailed &&
				!HasVisibleTokens &&
				IsQueryActive;
			this.ShowPartialState = !string.IsNullOrWhiteSpace(this.PartialMessage);
		}

		private bool IsCollectionNarrowed()
		{
			return !string.IsNullOrWhiteSpace(this.SearchText) ||
				this.SelectedLifecycleIndex > 0 ||
				this.SelectedAttentionIndex > 0 ||
				this.SelectedCategoryIndex > 0 ||
				this.SelectedOwnershipIndex > 0;
		}

		private bool RequiresFullCollectionScan()
		{
			return this.IsCollectionNarrowed() || this.SelectedSortIndex > 0;
		}

		private void CancelQueryScan()
		{
			this.queryCancellationTokenSource?.Cancel();
			this.queryCancellationTokenSource?.Dispose();
			this.queryCancellationTokenSource = null;
			this.IsScanning = false;
		}

		private string GetInitialNavigationMessage()
		{
			if (this.navigationMessageConsumed)
				return string.Empty;

			this.navigationMessageConsumed = true;
			return this.navigationArgs?.InitialErrorMessage?.Trim() ?? string.Empty;
		}

		private Task<TokensEventArgs> GetTokenPageAsync(int Offset, int MaxCount)
		{
			if (string.IsNullOrEmpty(this.RelatedContractId))
				return ServiceRef.XmppService.GetNeuroFeatures(Offset, MaxCount);

			return ServiceRef.XmppService.GetNeuroFeaturesForContract(
				this.RelatedContractId,
				Offset,
				MaxCount);
		}

		private NotificationEvent[] GetNotificationEvents(string TokenId)
		{
			lock (this.loadedTokensSync)
			{
				return this.notificationEventsByTokenId.TryGetValue(TokenId, out NotificationEvent[]? Events)
					? Events
					: [];
			}
		}

		private Task Wallet_TokenAdded(object? Sender, TokenEventArgs e)
		{
			if (!string.IsNullOrEmpty(this.RelatedContractId))
				return this.RefreshTokens();

			if (!ServiceRef.NotificationService.TryGetNotificationEvents(
				NotificationEventType.Wallet,
				e.Token.TokenId,
				out NotificationEvent[]? Events))
			{
				Events = [];
			}

			lock (this.loadedTokensSync)
			{
				this.loadedTokens.RemoveAll(Token =>
					string.Equals(Token.TokenId, e.Token.TokenId, StringComparison.OrdinalIgnoreCase));
				this.loadedTokenIds.Add(e.Token.TokenId);
				this.summaryByTokenId.Remove(e.Token.TokenId);
				this.loadedTokens.Insert(0, e.Token);
				this.notificationEventsByTokenId[e.Token.TokenId] = Events;
			}

			long Generation = Volatile.Read(ref this.queryGeneration);
			return this.ApplyCurrentQueryAsync(Generation);
		}

		private Task Wallet_TokenRemoved(object? Sender, TokenEventArgs e)
		{
			lock (this.loadedTokensSync)
			{
				this.loadedTokens.RemoveAll(Token =>
					string.Equals(Token.TokenId, e.Token.TokenId, StringComparison.OrdinalIgnoreCase));
				this.loadedTokenIds.Remove(e.Token.TokenId);
				this.notificationEventsByTokenId.Remove(e.Token.TokenId);
				this.summaryByTokenId.Remove(e.Token.TokenId);
			}

			long Generation = Volatile.Read(ref this.queryGeneration);
			return this.ApplyCurrentQueryAsync(Generation);
		}

		private Task Wallet_TokenStateUpdated(object? Sender, NewStateEventArgs e)
		{
			this.QueueTokenRefresh(e.TokenId);
			return Task.CompletedTask;
		}

		private Task Wallet_TokenVariablesUpdated(object? Sender, VariablesUpdatedEventArgs e)
		{
			this.QueueTokenRefresh(e.TokenId);
			return Task.CompletedTask;
		}

		private void QueueTokenRefresh(string TokenId)
		{
			if (this.isDisposed || string.IsNullOrWhiteSpace(TokenId))
				return;

			lock (this.loadedTokensSync)
			{
				if (!this.loadedTokenIds.Contains(TokenId))
					return;

				this.pendingTokenRefreshIds.Add(TokenId);
				if (!this.activeTokenRefreshIds.Add(TokenId))
					return;
			}

			_ = this.ProcessQueuedTokenRefreshAsync(TokenId);
		}

		private async Task ProcessQueuedTokenRefreshAsync(string TokenId)
		{
			while (!this.isDisposed)
			{
				lock (this.loadedTokensSync)
				{
					if (!this.pendingTokenRefreshIds.Remove(TokenId))
					{
						this.activeTokenRefreshIds.Remove(TokenId);
						return;
					}
				}

				await this.RefreshTokenFromIncomingEventAsync(TokenId).ConfigureAwait(false);
			}

			lock (this.loadedTokensSync)
			{
				this.pendingTokenRefreshIds.Remove(TokenId);
				this.activeTokenRefreshIds.Remove(TokenId);
			}
		}

		private async Task RefreshTokenFromIncomingEventAsync(string TokenId)
		{
			if (this.isDisposed || string.IsNullOrWhiteSpace(TokenId))
				return;

			lock (this.loadedTokensSync)
			{
				if (!this.loadedTokenIds.Contains(TokenId))
					return;
			}

			try
			{
				Token UpdatedToken = await ServiceRef.XmppService.GetNeuroFeature(TokenId)
					.ConfigureAwait(false);
				if (this.isDisposed)
					return;
				if (!string.Equals(
					UpdatedToken.TokenId,
					TokenId,
					StringComparison.OrdinalIgnoreCase))
				{
					return;
				}

				NotificationEvent[] Events = this.GetNotificationEvents(TokenId);
				lock (this.loadedTokensSync)
				{
					int Index = this.loadedTokens.FindIndex(Token =>
						string.Equals(
							Token.TokenId,
							TokenId,
							StringComparison.OrdinalIgnoreCase));
					if (Index < 0)
						return;

					this.loadedTokens[Index] = UpdatedToken;
					this.notificationEventsByTokenId[TokenId] = Events;
					this.summaryByTokenId.Remove(TokenId);
				}

				long Generation = Volatile.Read(ref this.queryGeneration);
				await this.ApplyCurrentQueryAsync(Generation).ConfigureAwait(false);
			}
			catch (Exception Ex)
			{
				LogRedactedFailure("Incoming token refresh failed.", Ex);
			}
		}

		private static void LogRedactedFailure(string Message, Exception Exception)
		{
			// Token identifiers and server payloads are deliberately excluded from collection diagnostics.
			ServiceRef.LogService.LogWarning(
				Message,
				new KeyValuePair<string, object?>(
					"FailureType",
					Exception.GetType().Name));
		}

		private static bool MatchesQuery(TokenSummaryItem Summary, QuerySnapshot Query)
		{
			if (!string.IsNullOrEmpty(Query.Category) &&
				!string.Equals(Summary.Category, Query.Category, StringComparison.CurrentCultureIgnoreCase))
			{
				return false;
			}

			if (Query.Lifecycle == TokenLifecycleFilter.Active && Summary.IsExpired)
				return false;

			if (Query.Lifecycle == TokenLifecycleFilter.Expired && !Summary.IsExpired)
				return false;

			if (Query.Attention == TokenAttentionFilter.NeedsAttention && !Summary.NeedsAttention)
				return false;

			if (Query.Ownership == TokenOwnershipFilter.OwnedByYou && !Summary.IsOwner)
				return false;

			if (Query.Ownership == TokenOwnershipFilter.Other && Summary.IsOwner)
				return false;

			if (string.IsNullOrEmpty(Query.Search))
				return true;

			List<string> SearchableValues =
			[
				Summary.DisplayName,
				Summary.Category,
				Summary.Description,
				Summary.TokenId,
				Summary.ShortTokenId
			];

			try
			{
				// Tag names are searchable; arbitrary tag values stay out of the default index because they can be sensitive.
				foreach (TokenTag Tag in Summary.Token.Tags ?? [])
				{
					if (!string.IsNullOrWhiteSpace(Tag.Name))
						SearchableValues.Add(Tag.Name);
				}
			}
			catch
			{
				// Malformed optional tags must not make the portfolio unusable.
			}

			string[] SearchTerms = Query.Search.Split(
				[' ', '\t', '\r', '\n'],
				StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

			return SearchTerms.All(Term =>
				SearchableValues.Any(Value =>
					Value.Contains(Term, StringComparison.CurrentCultureIgnoreCase)));
		}

		private static void SortSummaries(List<TokenSummaryItem> Summaries, TokenSortOrder Sort)
		{
			IOrderedEnumerable<TokenSummaryItem> Ordered = Sort switch
			{
				TokenSortOrder.Name => Summaries
					.OrderBy(Item => Item.DisplayName, StringComparer.CurrentCultureIgnoreCase),
				TokenSortOrder.Expiry => Summaries
					.OrderBy(Item => Item.HasExpiry ? 0 : 1)
					.ThenBy(Item => Item.Token.Expires),
				TokenSortOrder.Value => Summaries
					.OrderByDescending(Item => Item.Token.Value)
					.ThenBy(Item => Item.Token.Currency, StringComparer.CurrentCultureIgnoreCase),
				_ => Summaries
					.OrderByDescending(Item => Item.Token.Updated)
			};

			List<TokenSummaryItem> Sorted = Ordered
				.ThenBy(Item => Item.TokenId, StringComparer.OrdinalIgnoreCase)
				.ToList();
			Summaries.Clear();
			Summaries.AddRange(Sorted);
		}

		private readonly record struct QuerySnapshot(
			string Search,
			string Category,
			TokenLifecycleFilter Lifecycle,
			TokenAttentionFilter Attention,
			TokenOwnershipFilter Ownership,
			TokenSortOrder Sort);

		private enum TokenLifecycleFilter
		{
			All,
			Active,
			Expired
		}

		private enum TokenAttentionFilter
		{
			All,
			NeedsAttention
		}

		private enum TokenOwnershipFilter
		{
			All,
			OwnedByYou,
			Other
		}

		private enum TokenSortOrder
		{
			RecentlyUpdated,
			Name,
			Expiry,
			Value
		}
	}
}
