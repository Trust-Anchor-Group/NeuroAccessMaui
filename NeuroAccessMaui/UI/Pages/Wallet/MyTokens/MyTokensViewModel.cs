using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages.Wallet.MyWallet.ObjectModels;
using NeuroFeatures;
using NeuroFeatures.EventArguments;

namespace NeuroAccessMaui.UI.Pages.Wallet.MyTokens
{
	/// <summary>
	/// The view model to bind to for when displaying my tokens.
	/// </summary>
	/// <param name="Args">Navigation arguments</param>
	public partial class MyTokensViewModel(MyTokensNavigationArgs? Args) : XmppViewModel()
	{
		/// <summary>
		/// How many already loaded positions each "load more" request asks for again. The server sorts tokens by when they
		/// were last updated, so tokens can move while the list is open, and the overlap keeps such moves from skipping tokens.
		/// </summary>
		private const int PageOverlap = 5;

		private readonly MyTokensNavigationArgs? navigationArgs = Args;
		private readonly HashSet<string> loadedTokenIds = new(StringComparer.OrdinalIgnoreCase);
		private int serverOffset;

		/// <inheritdoc/>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			ServiceRef.TokenUnreadTracker.UnreadChanged += this.TokenUnreadTracker_UnreadChanged;

			try
			{
				await ServiceRef.TokenUnreadTracker.EnsureLoadedAsync();

				TokensEventArgs e = await ServiceRef.XmppService.GetNeuroFeatures(0, Constants.BatchSizes.TokenBatchSize);

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.Tokens.Clear();
					this.loadedTokenIds.Clear();
					this.serverOffset = 0;

					if (e.Ok && e.Tokens is not null)
					{
						this.AddTokens(e.Tokens, 0);
						this.HasMoreTokens = e.Tokens.Length == Constants.BatchSizes.TokenBatchSize;
					}
					else
						this.HasMoreTokens = false;

					this.HasTokens = this.Tokens.Count > 0;
				});

				ServiceRef.XmppService.NeuroFeatureAdded += this.Wallet_TokenAdded;
				ServiceRef.XmppService.NeuroFeatureRemoved += this.Wallet_TokenRemoved;
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		/// <inheritdoc/>
		public override Task OnDisposeAsync()
		{
			ServiceRef.TokenUnreadTracker.UnreadChanged -= this.TokenUnreadTracker_UnreadChanged;
			ServiceRef.XmppService.NeuroFeatureAdded -= this.Wallet_TokenAdded;
			ServiceRef.XmppService.NeuroFeatureRemoved -= this.Wallet_TokenRemoved;

			if (this.navigationArgs?.TokenItemProvider is TaskCompletionSource<TokenItem?> TaskSource)
				TaskSource.TrySetResult(null);

			return base.OnDisposeAsync();
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
				if (this.loadedTokenIds.Add(Token.TokenId))
					this.Tokens.Add(this.CreateItem(Token));
			}
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
				this.Tokens.Insert(0, this.CreateItem(e.Token));
				this.HasTokens = true;
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

				int i, c = this.Tokens.Count;

				for (i = 0; i < c; i++)
				{
					TokenItem Item = this.Tokens[i];

					if (string.Equals(Item.TokenId, e.Token.TokenId, StringComparison.OrdinalIgnoreCase))
					{
						this.Tokens.RemoveAt(i);
						break;
					}
				}

				this.HasTokens = this.Tokens.Count > 0;
			});

			return Task.CompletedTask;
		}

		private void TokenUnreadTracker_UnreadChanged(object? Sender, string TokenId)
		{
			int Count = ServiceRef.TokenUnreadTracker.GetUnreadCount(TokenId);

			MainThread.BeginInvokeOnMainThread(() =>
			{
				foreach (TokenItem Item in this.Tokens)
				{
					if (string.Equals(Item.TokenId, TokenId, StringComparison.OrdinalIgnoreCase))
					{
						Item.UnreadCount = Count;
						break;
					}
				}
			});
		}

		#region Properties

		/// <summary>
		/// HasTokens of eDaler to process
		/// </summary>
		[ObservableProperty]
		private bool hasTokens;

		/// <summary>
		/// HasMoreTokens of eDaler to process
		/// </summary>
		[ObservableProperty]
		private bool hasMoreTokens;

		/// <summary>
		/// Holds a list of tokens
		/// </summary>
		public ObservableCollection<TokenItem> Tokens { get; } = [];

		#endregion

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
		/// Command executed when more tokens need to be loaded.
		/// </summary>
		[RelayCommand]
		private async Task LoadMoreTokens()
		{
			if (!this.HasMoreTokens)
				return;

			this.HasMoreTokens = false; // So multiple requests are not made while scrolling.

			try
			{
				int Offset = Math.Max(0, this.serverOffset - PageOverlap);
				int MaxCount = this.serverOffset - Offset + Constants.BatchSizes.TokenBatchSize;
				TokensEventArgs e = await ServiceRef.XmppService.GetNeuroFeatures(Offset, MaxCount);

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					if (e.Ok && e.Tokens is not null)
					{
						this.AddTokens(e.Tokens, Offset);
						this.HasMoreTokens = e.Tokens.Length == MaxCount;
					}
					else
						this.HasMoreTokens = true;
				});
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				this.HasMoreTokens = true;
			}
		}
	}
}
