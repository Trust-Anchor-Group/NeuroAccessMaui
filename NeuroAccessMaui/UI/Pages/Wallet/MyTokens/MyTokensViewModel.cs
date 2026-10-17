using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.UI.Toasts;
using NeuroAccessMaui.Services.Wallet.Tokens;
using NeuroAccessMaui.UI.MVVM;
using NeuroAccessMaui.UI.Pages.Wallet.MyWallet.ObjectModels;
using NeuroFeatures;
using NeuroFeatures.EventArguments;

namespace NeuroAccessMaui.UI.Pages.Wallet.MyTokens
{
	/// <summary>
	/// The view model to bind to for when displaying my tokens.
	/// </summary>
	/// <param name="Args">Navigation arguments. When given, the page is a picker that returns the chosen token.</param>
	public partial class MyTokensViewModel(MyTokensNavigationArgs? Args) : XmppViewModel(), IDisposable
	{
		/// <summary>
		/// How many already loaded positions each "load more" request asks for again. The server sorts tokens by when they
		/// were last updated, so tokens can move while the list is open, and the overlap keeps such moves from skipping tokens.
		/// </summary>
		private const int PageOverlap = 5;

		/// <summary>
		/// How many tokens each request asks for while the whole wallet is loaded for a search.
		/// </summary>
		private const int SearchBatchSize = 25;

		/// <summary>
		/// The most tokens loaded for a search. Above this, search covers only the tokens loaded.
		/// </summary>
		private const int SearchLimit = 500;

		/// <summary>
		/// How long typing has to pause before the search runs.
		/// </summary>
		private static readonly TimeSpan searchDelay = TimeSpan.FromMilliseconds(300);

		private readonly MyTokensNavigationArgs? navigationArgs = Args;
		private readonly HashSet<string> loadedTokenIds = new(StringComparer.OrdinalIgnoreCase);
		private readonly List<TokenItem> allTokens = [];
		private readonly SemaphoreSlim pageLock = new(1, 1);
		private int serverOffset;
		private bool reachedEnd;
		private int listGeneration;
		private Task? loadAllTask;
		private CancellationTokenSource? searchDelayCancel;
		private bool disposed;

		/// <inheritdoc/>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			ServiceRef.TokenUnreadTracker.UnreadChanged += this.TokenUnreadTracker_UnreadChanged;
			ServiceRef.XmppService.NeuroFeatureAdded += this.Wallet_TokenAdded;
			ServiceRef.XmppService.NeuroFeatureRemoved += this.Wallet_TokenRemoved;
			this.LoadTask.PropertyChanged += this.LoadTask_PropertyChanged;

			this.LoadTask.Load(this.LoadFirstPageAsync);
		}

		/// <inheritdoc/>
		public override Task OnDisposeAsync()
		{
			ServiceRef.TokenUnreadTracker.UnreadChanged -= this.TokenUnreadTracker_UnreadChanged;
			ServiceRef.XmppService.NeuroFeatureAdded -= this.Wallet_TokenAdded;
			ServiceRef.XmppService.NeuroFeatureRemoved -= this.Wallet_TokenRemoved;
			this.LoadTask.PropertyChanged -= this.LoadTask_PropertyChanged;

			this.LoadTask.Cancel();
			this.searchDelayCancel?.Cancel();

			if (this.navigationArgs?.TokenItemProvider is TaskCompletionSource<TokenItem?> TaskSource)
				TaskSource.TrySetResult(null);

			return base.OnDisposeAsync();
		}

		#region Loading

		/// <summary>
		/// Loads the first page of tokens. Used by <see cref="LoadTask"/>, so failures show the error state.
		/// </summary>
		/// <param name="Context">Task context.</param>
		private async Task LoadFirstPageAsync(TaskContext<int> Context)
		{
			try
			{
				await this.ReloadFromStartAsync();
			}
			catch (Exception Ex)
			{
				await MainThread.InvokeOnMainThreadAsync(() => this.LoadErrorText = GetLoadErrorText(Ex));
				throw;
			}
		}

		/// <summary>
		/// Updates the page when the first page of tokens starts loading, has loaded, or failed to load.
		/// </summary>
		private void LoadTask_PropertyChanged(object? Sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName != nameof(ObservableTask.State))
				return;

			this.OnPropertyChanged(nameof(this.IsLoadingTokens));
			this.OnPropertyChanged(nameof(this.IsLoaded));
			this.OnPropertyChanged(nameof(this.ShowLoadError));
			this.OnPropertyChanged(nameof(this.TokenStatusText));
			this.OnPropertyChanged(nameof(this.ShowTokenStatus));
			this.NotifyTokenCountChanged();
		}

		/// <summary>
		/// Updates what depends on how many tokens are loaded. Called on the main thread.
		/// </summary>
		private void NotifyTokenCountChanged()
		{
			this.OnPropertyChanged(nameof(this.HasTokens));
			this.OnPropertyChanged(nameof(this.ShowNoTokens));
			this.OnPropertyChanged(nameof(this.ShowNoMatches));
			this.OnPropertyChanged(nameof(this.ShowSearch));
			this.OnPropertyChanged(nameof(this.ShowTokenList));
		}

		/// <summary>
		/// Reloads the list from the start, keeping the current search and category filter.
		/// </summary>
		private async Task ReloadFromStartAsync()
		{
			await ServiceRef.TokenUnreadTracker.EnsureLoadedAsync();

			await this.pageLock.WaitAsync();
			try
			{
				TokensEventArgs e = await ServiceRef.XmppService.GetNeuroFeatures(0, Constants.BatchSizes.TokenBatchSize);
				Token[] Tokens = EnsureOk(e);

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.listGeneration++;
					this.loadAllTask = null;
					this.allTokens.Clear();
					this.loadedTokenIds.Clear();
					this.serverOffset = 0;

					this.AddTokens(Tokens, 0);

					this.reachedEnd = Tokens.Length < Constants.BatchSizes.TokenBatchSize;
					this.HasMoreTokens = !this.reachedEnd;
					this.IsSearchLimited = false;

					this.UpdateCategories();
					this.ApplyFilter();
				});
			}
			finally
			{
				this.pageLock.Release();
			}

			MainThread.BeginInvokeOnMainThread(() =>
			{
				if (this.IsFiltering && !this.reachedEnd)
					this.EnsureAllLoaded();
			});
		}

		/// <summary>
		/// Loads the next page of tokens from the server and adds it to the list.
		/// </summary>
		/// <param name="BatchSize">How many new tokens to ask for.</param>
		/// <returns>If the server may have more tokens.</returns>
		private async Task<bool> LoadNextPageAsync(int BatchSize)
		{
			await this.pageLock.WaitAsync();
			try
			{
				if (this.reachedEnd)
					return false;

				int Generation = this.listGeneration;
				int Offset = Math.Max(0, this.serverOffset - PageOverlap);
				int MaxCount = this.serverOffset - Offset + BatchSize;

				TokensEventArgs e = await ServiceRef.XmppService.GetNeuroFeatures(Offset, MaxCount);
				Token[] Tokens = EnsureOk(e);

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					if (Generation != this.listGeneration)
						return;

					this.AddTokens(Tokens, Offset);

					this.reachedEnd = Tokens.Length < MaxCount;
					this.HasMoreTokens = !this.reachedEnd;

					this.UpdateCategories();
				});

				return !this.reachedEnd;
			}
			finally
			{
				this.pageLock.Release();
			}
		}

		/// <summary>
		/// Starts loading the whole wallet for a search, unless it is already loading or loaded. Called on the main thread.
		/// </summary>
		private void EnsureAllLoaded()
		{
			this.loadAllTask ??= this.LoadAllAsync();
		}

		/// <summary>
		/// Loads the rest of the wallet in batches, up to <see cref="SearchLimit"/> tokens, so a search covers every token.
		/// Started on the main thread. When loading fails, the next search tries again.
		/// </summary>
		private async Task LoadAllAsync()
		{
			int Generation = this.listGeneration;

			this.IsLoadingAll = true;
			try
			{
				while (!this.reachedEnd && this.allTokens.Count < SearchLimit && Generation == this.listGeneration)
				{
					if (!await this.LoadNextPageAsync(SearchBatchSize))
						break;

					if (this.IsFiltering)
						await MainThread.InvokeOnMainThreadAsync(this.ApplyFilter);
				}

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					if (Generation != this.listGeneration)
						return;

					this.IsSearchLimited = !this.reachedEnd;
					this.ApplyFilter();
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				MainThread.BeginInvokeOnMainThread(() => this.loadAllTask = null);
				await ServiceRef.ToastService.ShowTextAsync(GetLoadErrorText(Ex));
			}
			finally
			{
				MainThread.BeginInvokeOnMainThread(() => this.IsLoadingAll = false);
			}
		}

		/// <summary>
		/// Gets the tokens from a server response, or throws the error the server returned.
		/// </summary>
		/// <param name="e">Server response.</param>
		/// <returns>The tokens in the response.</returns>
		private static Token[] EnsureOk(TokensEventArgs e)
		{
			if (!e.Ok)
				throw e.StanzaError ?? new Exception(e.ErrorText);

			return e.Tokens ?? [];
		}

		/// <summary>
		/// Gets the text shown when tokens couldn't be loaded.
		/// </summary>
		/// <param name="Error">The error.</param>
		/// <returns>A localized text.</returns>
		private static string GetLoadErrorText(Exception Error)
		{
			return TokenErrors.ToMessage(Error) ?? ServiceRef.Localizer[nameof(AppResources.TokensLoadFailed)];
		}

		/// <summary>
		/// Adds tokens received from the server to the end of the list, skipping tokens that are already in it.
		/// Called on the main thread.
		/// </summary>
		/// <param name="Tokens">Tokens received from the server.</param>
		/// <param name="Offset">The offset the tokens were requested from.</param>
		private void AddTokens(Token[] Tokens, int Offset)
		{
			this.serverOffset = Offset + Tokens.Length;

			foreach (Token Token in Tokens)
			{
				if (!this.loadedTokenIds.Add(Token.TokenId))
					continue;

				TokenItem Item = this.CreateItem(Token);
				this.allTokens.Add(Item);

				if (!this.IsFiltering)
					this.Tokens.Add(Item);
			}

			this.NotifyTokenCountChanged();
		}

		/// <summary>
		/// Creates a list item for a token. In picker mode, every item completes the picker when tapped.
		/// </summary>
		/// <param name="Token">The token.</param>
		/// <returns>The list item.</returns>
		private TokenItem CreateItem(Token Token)
		{
			ServiceRef.TokenNameCache.Remember(Token);

			return new TokenItem(Token, this.navigationArgs?.TokenItemProvider)
			{
				UnreadCount = ServiceRef.TokenUnreadTracker.GetUnreadCount(Token.TokenId)
			};
		}

		#endregion

		#region Search and filter

		partial void OnSearchTextChanged(string? Value)
		{
			this.searchDelayCancel?.Cancel();
			this.searchDelayCancel = new CancellationTokenSource();

			_ = this.SearchAfterDelayAsync(this.searchDelayCancel.Token);
		}

		/// <summary>
		/// Runs the search when typing has paused.
		/// </summary>
		/// <param name="Cancel">Cancelled when the user types again.</param>
		private async Task SearchAfterDelayAsync(CancellationToken Cancel)
		{
			try
			{
				await Task.Delay(searchDelay, Cancel);
				await MainThread.InvokeOnMainThreadAsync(this.FilterChanged);
			}
			catch (TaskCanceledException)
			{
				// The user typed again.
			}
		}

		/// <summary>
		/// Applies a changed search or category filter, and loads the whole wallet the first time one is used.
		/// Called on the main thread.
		/// </summary>
		private void FilterChanged()
		{
			this.OnPropertyChanged(nameof(this.IsFiltering));
			this.ApplyFilter();

			if (this.IsFiltering && !this.reachedEnd)
				this.EnsureAllLoaded();
		}

		/// <summary>
		/// Shows the loaded tokens that match the search and category filter. Called on the main thread.
		/// </summary>
		private void ApplyFilter()
		{
			ObservableCollection<TokenItem> Matches = [];

			foreach (TokenItem Item in this.allTokens)
			{
				if (this.Matches(Item))
					Matches.Add(Item);
			}

			this.Tokens = Matches;
		}

		/// <summary>
		/// Checks if a token matches the search and category filter.
		/// </summary>
		/// <param name="Item">The token.</param>
		/// <returns>If the token matches.</returns>
		private bool Matches(TokenItem Item)
		{
			if (this.SelectedCategory is string Category &&
				!string.Equals(Item.Category, Category, StringComparison.CurrentCultureIgnoreCase))
			{
				return false;
			}

			string Query = this.SearchText?.Trim() ?? string.Empty;
			if (Query.Length == 0)
				return true;

			return Contains(Item.FriendlyName, Query) ||
				Contains(Item.Category, Query) ||
				Contains(Item.ShortTokenId, Query) ||
				Contains(Item.TokenId, Query);
		}

		private static bool Contains(string? Value, string Query)
		{
			return Value is not null && Value.Contains(Query, StringComparison.CurrentCultureIgnoreCase);
		}

		/// <summary>
		/// Updates the category chips from the loaded tokens. Called on the main thread.
		/// </summary>
		private void UpdateCategories()
		{
			SortedSet<string> Names = new(StringComparer.CurrentCultureIgnoreCase);

			foreach (TokenItem Item in this.allTokens)
			{
				if (Item.HasCategory)
					Names.Add(Item.Category);
			}

			if (Names.Count == this.Categories.Count && this.Categories.All(Chip => Names.Contains(Chip.Name)))
				return;

			this.Categories.Clear();

			foreach (string Name in Names)
			{
				this.Categories.Add(new TokenCategoryChip(Name)
				{
					IsSelected = string.Equals(Name, this.SelectedCategory, StringComparison.CurrentCultureIgnoreCase)
				});
			}

			this.OnPropertyChanged(nameof(this.HasCategories));
		}

		#endregion

		#region Live updates

		private Task Wallet_TokenAdded(object? Sender, TokenEventArgs e)
		{
			if (e.Token is null)
				return Task.CompletedTask;

			MainThread.BeginInvokeOnMainThread(() =>
			{
				if (!this.loadedTokenIds.Add(e.Token.TokenId))
					return;

				// The server lists the newest token first, so every loaded token moves one position down.
				this.serverOffset++;

				TokenItem Item = this.CreateItem(e.Token);
				this.allTokens.Insert(0, Item);

				if (this.Matches(Item))
					this.Tokens.Insert(0, Item);

				this.UpdateCategories();
				this.NotifyTokenCountChanged();
			});

			return Task.CompletedTask;
		}

		private Task Wallet_TokenRemoved(object? Sender, TokenEventArgs e)
		{
			if (e.Token is null)
				return Task.CompletedTask;

			MainThread.BeginInvokeOnMainThread(() =>
			{
				if (!this.loadedTokenIds.Remove(e.Token.TokenId))
					return;

				this.serverOffset = Math.Max(0, this.serverOffset - 1);

				RemoveToken(this.allTokens, e.Token.TokenId);
				RemoveToken(this.Tokens, e.Token.TokenId);

				this.UpdateCategories();
				this.NotifyTokenCountChanged();
			});

			return Task.CompletedTask;
		}

		private static void RemoveToken(IList<TokenItem> Items, string TokenId)
		{
			int i, c = Items.Count;

			for (i = 0; i < c; i++)
			{
				if (string.Equals(Items[i].TokenId, TokenId, StringComparison.OrdinalIgnoreCase))
				{
					Items.RemoveAt(i);
					break;
				}
			}
		}

		private void TokenUnreadTracker_UnreadChanged(object? Sender, string TokenId)
		{
			int Count = ServiceRef.TokenUnreadTracker.GetUnreadCount(TokenId);

			MainThread.BeginInvokeOnMainThread(() =>
			{
				foreach (TokenItem Item in this.allTokens)
				{
					if (string.Equals(Item.TokenId, TokenId, StringComparison.OrdinalIgnoreCase))
					{
						Item.UnreadCount = Count;
						break;
					}
				}
			});
		}

		#endregion

		#region Properties

		/// <summary>
		/// Loads the first page of tokens, and exposes the loading, error and loaded states.
		/// </summary>
		public ObservableTask LoadTask { get; } = new();

		/// <summary>
		/// The page title. In picker mode, it asks the user to choose a token.
		/// </summary>
		public string PageTitle => this.navigationArgs is null
			? ServiceRef.Localizer[nameof(AppResources.MyTokens)]
			: ServiceRef.Localizer[nameof(AppResources.ChooseAToken)];

		/// <summary>
		/// If the user can create tokens from here. Not while choosing a token.
		/// </summary>
		public bool CanCreateToken => this.navigationArgs is null;

		/// <summary>
		/// Text shown when the tokens couldn't be loaded.
		/// </summary>
		[ObservableProperty]
		private string? loadErrorText;

		/// <summary>
		/// The tokens shown: every loaded token, or those that match the search and category filter.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(ShowNoMatches))]
		private ObservableCollection<TokenItem> tokens = [];

		/// <summary>
		/// Categories of the loaded tokens, to filter by.
		/// </summary>
		public ObservableCollection<TokenCategoryChip> Categories { get; } = [];

		/// <summary>
		/// If the category chips are shown. A single category isn't worth filtering by.
		/// </summary>
		public bool HasCategories => this.Categories.Count > 1;

		/// <summary>
		/// Text to search for, in token names, categories and IDs.
		/// </summary>
		[ObservableProperty]
		private string? searchText;

		/// <summary>
		/// The category the list is filtered by, if any.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(IsAllCategoriesSelected))]
		private string? selectedCategory;

		/// <summary>
		/// If no category filter is applied, so the "All" chip is selected.
		/// </summary>
		public bool IsAllCategoriesSelected => this.SelectedCategory is null;

		/// <summary>
		/// If a search or category filter is applied.
		/// </summary>
		public bool IsFiltering => !string.IsNullOrWhiteSpace(this.SearchText) || this.SelectedCategory is not null;

		/// <summary>
		/// If the wallet has any tokens.
		/// </summary>
		public bool HasTokens => this.allTokens.Count > 0;

		/// <summary>
		/// If the wallet is empty. Shown once the first page has loaded.
		/// </summary>
		public bool ShowNoTokens => this.LoadTask.IsSucceeded && this.allTokens.Count == 0;

		/// <summary>
		/// If the first page of tokens is loading. Placeholder rows show meanwhile.
		/// </summary>
		public bool IsLoadingTokens => this.LoadTask.IsPending || this.LoadTask.IsRunning;

		/// <summary>
		/// If the first page of tokens has loaded.
		/// </summary>
		public bool IsLoaded => this.LoadTask.IsSucceeded;

		/// <summary>
		/// If the tokens couldn't be loaded.
		/// </summary>
		public bool ShowLoadError => this.LoadTask.IsFailed;

		/// <summary>
		/// If the search field is shown: while the tokens load, and once there are tokens to search.
		/// </summary>
		public bool ShowSearch => this.IsLoadingTokens || (this.IsLoaded && this.allTokens.Count > 0);

		/// <summary>
		/// If the card with the tokens is shown.
		/// </summary>
		public bool ShowTokenList => this.IsLoaded && this.allTokens.Count > 0;

		/// <summary>
		/// What the list is doing: loading, searching all tokens, or how many tokens a search covers. Null when there is
		/// nothing to say.
		/// </summary>
		public string? TokenStatusText
		{
			get
			{
				if (this.IsLoadingTokens)
					return ServiceRef.Localizer[nameof(AppResources.TokensLoading)];

				if (this.IsLoadingAll)
					return ServiceRef.Localizer[nameof(AppResources.TokenSearchLoading)];

				if (this.IsSearchLimited)
					return this.SearchLimitedText;

				return null;
			}
		}

		/// <summary>
		/// If <see cref="TokenStatusText"/> is shown.
		/// </summary>
		public bool ShowTokenStatus => this.TokenStatusText is not null;

		/// <summary>
		/// If a search or filter matched none of the tokens.
		/// </summary>
		public bool ShowNoMatches => this.allTokens.Count > 0 && this.Tokens.Count == 0 && this.IsFiltering && !this.IsLoadingAll;

		/// <summary>
		/// If the server may have more tokens to load while scrolling.
		/// </summary>
		[ObservableProperty]
		private bool hasMoreTokens;

		/// <summary>
		/// If the whole wallet is being loaded for a search.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(ShowNoMatches))]
		[NotifyPropertyChangedFor(nameof(TokenStatusText))]
		[NotifyPropertyChangedFor(nameof(ShowTokenStatus))]
		private bool isLoadingAll;

		/// <summary>
		/// If the wallet has more tokens than a search covers.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(TokenStatusText))]
		[NotifyPropertyChangedFor(nameof(ShowTokenStatus))]
		private bool isSearchLimited;

		/// <summary>
		/// Says how many tokens a search covers, when the wallet has more.
		/// </summary>
		public string SearchLimitedText => ServiceRef.Localizer[nameof(AppResources.TokenSearchLimited), SearchLimit];

		/// <summary>
		/// If the list is being reloaded by pulling down.
		/// </summary>
		[ObservableProperty]
		private bool isRefreshing;

		#endregion

		#region Commands

		/// <summary>
		/// The command to bind to for returning to previous view.
		/// </summary>
		[RelayCommand]
		private Task Back()
		{
			return this.GoBack();
		}

		/// <inheritdoc/>
		public override Task GoBack()
		{
			this.navigationArgs?.TokenItemProvider.TrySetResult(null);
			return base.GoBack();
		}

		/// <summary>
		/// Reloads the list from the start, when the user pulls down. The list stays on screen while it reloads.
		/// </summary>
		[RelayCommand]
		private async Task RefreshTokens()
		{
			try
			{
				await this.ReloadFromStartAsync();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.ToastService.ShowTextAsync(GetLoadErrorText(Ex));
			}
			finally
			{
				this.IsRefreshing = false;
			}
		}

		/// <summary>
		/// Loads more tokens, when the user scrolls near the end of the list. While a search or filter is applied, the
		/// whole wallet is loaded instead.
		/// </summary>
		[RelayCommand]
		private async Task LoadMoreTokens()
		{
			if (!this.HasMoreTokens || this.IsFiltering)
				return;

			try
			{
				await this.LoadNextPageAsync(Constants.BatchSizes.TokenBatchSize);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Creates tokens from a token creation template.
		/// </summary>
		[RelayCommand]
		private Task CreateToken()
		{
			return ServiceRef.NeuroWalletOrchestratorService.CreateTokenAsync();
		}

		/// <summary>
		/// Filters the list by a category, or removes the filter when the selected category is tapped again.
		/// </summary>
		/// <param name="Chip">The tapped category.</param>
		[RelayCommand]
		private void SelectCategory(TokenCategoryChip? Chip)
		{
			if (Chip is null)
				return;

			bool Select = !Chip.IsSelected;

			foreach (TokenCategoryChip Other in this.Categories)
				Other.IsSelected = false;

			Chip.IsSelected = Select;
			this.SelectedCategory = Select ? Chip.Name : null;

			this.FilterChanged();
		}

		/// <summary>
		/// Removes the category filter, when the "All" chip is tapped.
		/// </summary>
		[RelayCommand]
		private void SelectAllCategories()
		{
			if (this.SelectedCategory is null)
				return;

			foreach (TokenCategoryChip Chip in this.Categories)
				Chip.IsSelected = false;

			this.SelectedCategory = null;

			this.FilterChanged();
		}

		#endregion

		/// <summary>
		/// Releases the resources owned by the view model.
		/// </summary>
		/// <remarks>
		/// Not called from <see cref="OnDisposeAsync"/>, since a page load can still be running when the page closes.
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
				Interlocked.Exchange(ref this.searchDelayCancel, null)?.Dispose();
				this.pageLock.Dispose();
			}

			this.disposed = true;
		}
	}
}
