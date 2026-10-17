using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.UI.Toasts;
using NeuroAccessMaui.Services.Wallet.Tokens;
using NeuroAccessMaui.UI.MVVM;
using NeuroAccessMaui.UI.Pages.Wallet.TokenEvents.Events;
using NeuroAccessMaui.UI.Popups.Tokens.AddTextNote;
using NeuroFeatures.EventArguments;
using NeuroFeatures.Events;
using System.Collections.ObjectModel;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenEvents
{
	/// <summary>
	/// The view model to bind to for when displaying the notes and history of a token. Events are loaded page by page,
	/// newest first, and new events are merged in at the top.
	/// </summary>
	public partial class TokenEventsViewModel : BaseViewModel, IDisposable
	{
		/// <summary>
		/// Number of events fetched at a time.
		/// </summary>
		private const int PageSize = 20;

		private static readonly TimeSpan refreshDelay = TimeSpan.FromSeconds(1);

		private readonly TokenEventsNavigationArgs? navigationArguments;
		private readonly HashSet<(DateTime Timestamp, EventType Type)> eventKeys = [];
		private readonly List<NoteTextItem> localNotes = [];
		private readonly SemaphoreSlim pageLock = new(1, 1);
		private TokenEvent[]? preloadedEvents;
		private int serverOffset;
		private bool reachedEnd;
		private CancellationTokenSource? refreshCancel;
		private bool disposed;

		/// <summary>
		/// Creates an instance of the <see cref="TokenEventsViewModel"/> class.
		/// </summary>
		/// <param name="Args">Navigation arguments</param>
		public TokenEventsViewModel(TokenEventsNavigationArgs? Args)
			: base()
		{
			this.navigationArguments = Args;
			this.preloadedEvents = Args?.Events;
			this.TokenId = Args?.TokenId;
			this.IsMyToken = Args?.IsMyToken ?? false;

			this.Events = [];
		}

		/// <inheritdoc/>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			ServiceRef.XmppService.NeuroFeatureStateUpdated += this.Wallet_StateUpdated;
			ServiceRef.TokenUnreadTracker.UnreadChanged += this.TokenUnreadTracker_UnreadChanged;

			this.LoadTask.Load(this.LoadFirstPageAsync);
		}

		/// <inheritdoc/>
		public override async Task OnDisposeAsync()
		{
			ServiceRef.XmppService.NeuroFeatureStateUpdated -= this.Wallet_StateUpdated;
			ServiceRef.TokenUnreadTracker.UnreadChanged -= this.TokenUnreadTracker_UnreadChanged;

			this.refreshCancel?.Cancel();
			this.LoadTask.Cancel();
			this.Events.Clear();

			await base.OnDisposeAsync();
		}

		#region Loading

		/// <summary>
		/// Loads the newest events. Events passed in the navigation arguments are shown first, without fetching them again.
		/// </summary>
		/// <param name="Context">Task context.</param>
		private async Task LoadFirstPageAsync(TaskContext<int> Context)
		{
			string? TokenId = this.TokenId;
			if (string.IsNullOrEmpty(TokenId))
				return;

			await this.pageLock.WaitAsync();
			try
			{
				TokenEvent[] Page = Interlocked.Exchange(ref this.preloadedEvents, null)
					?? await ServiceRef.XmppService.GetNeuroFeatureEvents(TokenId, 0, PageSize);
				List<EventItem> Items = await CreateItemsAsync(Page);

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.eventKeys.Clear();
					this.localNotes.Clear();
					this.Events.Clear();

					foreach (EventItem Item in Items)
					{
						if (this.eventKeys.Add(GetKey(Item)))
							this.Events.Add(Item);
					}

					this.serverOffset = Page.Length;
					this.reachedEnd = Page.Length < PageSize;
					this.HasMoreEvents = !this.reachedEnd;
					this.ShowNoEvents = this.Events.Count == 0;
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);

				await MainThread.InvokeOnMainThreadAsync(() =>
					this.LoadErrorText = TokenErrors.ToMessage(Ex) ?? ServiceRef.Localizer[nameof(AppResources.SomethingWentWrong)]);
				throw;
			}
			finally
			{
				this.pageLock.Release();
			}
		}

		/// <summary>
		/// Fetches the newest events again, and adds the ones not shown yet at the top. Notes added on this device are
		/// replaced by the copies the token's Neuron stored.
		/// </summary>
		private async Task MergeFirstPageAsync()
		{
			string? TokenId = this.TokenId;
			if (string.IsNullOrEmpty(TokenId) || !this.LoadTask.IsSucceeded)
				return;

			await this.pageLock.WaitAsync();
			try
			{
				TokenEvent[] Page = await ServiceRef.XmppService.GetNeuroFeatureEvents(TokenId, 0, PageSize);
				List<EventItem> Items = await CreateItemsAsync(Page);

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					foreach (EventItem Item in Items)
					{
						if (Item is NoteTextItem Note)
							this.RemoveLocalNote(Note);
					}

					int Index = 0;

					foreach (EventItem Item in Items)
					{
						if (this.eventKeys.Add(GetKey(Item)))
							this.Events.Insert(Index++, Item);
					}

					// Events added since the first page was loaded push the older ones further down on the server.
					this.serverOffset += Index;
					this.ShowNoEvents = this.Events.Count == 0;
				});
			}
			finally
			{
				this.pageLock.Release();
			}
		}

		/// <summary>
		/// Removes a note added on this device, once the token's Neuron returns it.
		/// </summary>
		/// <param name="Stored">The note as stored.</param>
		private void RemoveLocalNote(NoteTextItem Stored)
		{
			foreach (NoteTextItem Local in this.localNotes)
			{
				if (Local.Personal == Stored.Personal && string.Equals(Local.Note, Stored.Note, StringComparison.Ordinal))
				{
					this.localNotes.Remove(Local);
					this.Events.Remove(Local);
					return;
				}
			}
		}

		/// <summary>
		/// Refreshes the newest events shortly. Several changes close together refresh once.
		/// </summary>
		private void ScheduleRefresh()
		{
			CancellationTokenSource Cancel = new();
			Interlocked.Exchange(ref this.refreshCancel, Cancel)?.Cancel();

			_ = this.RefreshQuietlyAsync(Cancel.Token);
		}

		private async Task RefreshQuietlyAsync(CancellationToken Cancel)
		{
			try
			{
				await Task.Delay(refreshDelay, Cancel);
				await this.MergeFirstPageAsync();
			}
			catch (OperationCanceledException)
			{
				// A newer refresh replaced this one.
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private static async Task<List<EventItem>> CreateItemsAsync(TokenEvent[] Events)
		{
			List<EventItem> Items = new(Events.Length);

			foreach (TokenEvent Event in Events)
			{
				EventItem Item = EventItem.Create(Event);
				await Item.DoBind();
				Items.Add(Item);
			}

			return Items;
		}

		private static (DateTime Timestamp, EventType Type) GetKey(EventItem Item)
		{
			return (Item.Timestamp, Item.Type);
		}

		private bool IsThisToken(string? TokenId)
		{
			if (string.IsNullOrEmpty(TokenId))
				return false;

			return string.Equals(TokenId, this.TokenId, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(TokenId, this.navigationArguments?.MachineId, StringComparison.OrdinalIgnoreCase);
		}

		private Task Wallet_StateUpdated(object? Sender, NewStateEventArgs e)
		{
			if (this.IsThisToken(e.TokenId))
				this.ScheduleRefresh();

			return Task.CompletedTask;
		}

		private void TokenUnreadTracker_UnreadChanged(object? Sender, string TokenId)
		{
			// A new notification about the token means a new event, such as a note or a transfer.
			if (this.IsThisToken(TokenId) && ServiceRef.TokenUnreadTracker.GetUnreadCount(TokenId) > 0)
				this.ScheduleRefresh();
		}

		#endregion

		#region Properties

		/// <summary>
		/// Events, newest first.
		/// </summary>
		public ObservableCollection<EventItem> Events { get; }

		/// <summary>
		/// Loads the newest events.
		/// </summary>
		public ObservableTask LoadTask { get; } = new();

		/// <summary>
		/// Token ID
		/// </summary>
		[ObservableProperty]
		private string? tokenId;

		/// <summary>
		/// If the token belongs to the user, who can then add notes.
		/// </summary>
		[ObservableProperty]
		private bool isMyToken;

		/// <summary>
		/// Why the events couldn't be loaded.
		/// </summary>
		[ObservableProperty]
		private string? loadErrorText;

		/// <summary>
		/// If there are older events to load.
		/// </summary>
		[ObservableProperty]
		private bool hasMoreEvents;

		/// <summary>
		/// If older events are being loaded.
		/// </summary>
		[ObservableProperty]
		private bool isLoadingMore;

		/// <summary>
		/// If the events are being refreshed by pulling down.
		/// </summary>
		[ObservableProperty]
		private bool isRefreshing;

		/// <summary>
		/// If the token has no events to show.
		/// </summary>
		[ObservableProperty]
		private bool showNoEvents;

		#endregion

		#region Commands

		/// <summary>
		/// Loads the next page of older events.
		/// </summary>
		[RelayCommand]
		private async Task LoadMoreEvents()
		{
			string? TokenId = this.TokenId;
			if (string.IsNullOrEmpty(TokenId) || this.reachedEnd || !this.LoadTask.IsSucceeded)
				return;

			await this.pageLock.WaitAsync();
			try
			{
				if (this.reachedEnd)
					return;

				await MainThread.InvokeOnMainThreadAsync(() => this.IsLoadingMore = true);

				TokenEvent[] Page = await ServiceRef.XmppService.GetNeuroFeatureEvents(TokenId, this.serverOffset, PageSize);
				List<EventItem> Items = await CreateItemsAsync(Page);

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					foreach (EventItem Item in Items)
					{
						if (this.eventKeys.Add(GetKey(Item)))
							this.Events.Add(Item);
					}

					this.serverOffset += Page.Length;
					this.reachedEnd = Page.Length < PageSize;
					this.HasMoreEvents = !this.reachedEnd;
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.ToastService.ShowTextAsync(TokenErrors.ToMessage(Ex) ?? ServiceRef.Localizer[nameof(AppResources.SomethingWentWrong)]);
			}
			finally
			{
				await MainThread.InvokeOnMainThreadAsync(() => this.IsLoadingMore = false);
				this.pageLock.Release();
			}
		}

		/// <summary>
		/// Fetches the newest events again, when pulled down.
		/// </summary>
		[RelayCommand]
		private async Task RefreshEvents()
		{
			try
			{
				if (this.LoadTask.IsSucceeded)
					await this.MergeFirstPageAsync();
				else
					this.LoadTask.Load(this.LoadFirstPageAsync);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.ToastService.ShowTextAsync(TokenErrors.ToMessage(Ex) ?? ServiceRef.Localizer[nameof(AppResources.SomethingWentWrong)]);
			}
			finally
			{
				this.IsRefreshing = false;
			}
		}

		/// <summary>
		/// Command to add a text note to the token. Notes are always sent as text, as written. The note is shown at the
		/// top at once, and replaced by the stored copy when the events are refreshed.
		/// </summary>
		[RelayCommand]
		private async Task AddNote()
		{
			string? TokenId = this.TokenId;
			if (string.IsNullOrEmpty(TokenId))
				return;

			try
			{
				AddTextNoteViewModel AddTextNoteViewModel = new((Note, Personal) =>
					ServiceRef.XmppService.AddNeuroFeatureTextNote(TokenId, Note, Personal));
				AddTextNotePopup AddTextNotePopup = new(AddTextNoteViewModel);

				await ServiceRef.PopupService.PushAsync(AddTextNotePopup);
				bool? Result = await AddTextNoteViewModel.Result;

				if (Result.HasValue && Result.Value)
				{
					NoteTextItem NewEvent = new(new NoteText()
					{
						Note = AddTextNoteViewModel.TextNote,
						Personal = AddTextNoteViewModel.Personal,
						TokenId = TokenId,
						Timestamp = DateTime.Now
					});

					await NewEvent.DoBind();

					this.localNotes.Add(NewEvent);
					this.Events.Insert(0, NewEvent);
					this.ShowNoEvents = false;

					this.ScheduleRefresh();
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
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
				Interlocked.Exchange(ref this.refreshCancel, null)?.Dispose();
				this.pageLock.Dispose();
			}

			this.disposed = true;
		}
	}
}
