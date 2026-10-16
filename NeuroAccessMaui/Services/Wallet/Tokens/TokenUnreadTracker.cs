using NeuroAccessMaui.Services.Notification;

namespace NeuroAccessMaui.Services.Wallet.Tokens
{
	/// <summary>
	/// Counts unread notifications per token, from the notification records stored in the token channel.
	/// </summary>
	public sealed class TokenUnreadTracker : ITokenUnreadTracker
	{
		private readonly INotificationServiceV2 notificationService;
		private readonly Dictionary<string, HashSet<string>> unreadByToken = new(StringComparer.OrdinalIgnoreCase);
		private readonly object synchObject = new();
		private readonly SemaphoreSlim loadSemaphore = new(1, 1);
		private bool loaded;

		/// <summary>
		/// Counts unread notifications per token, from the notification records stored in the token channel.
		/// </summary>
		/// <param name="NotificationService">The service storing notification records.</param>
		public TokenUnreadTracker(INotificationServiceV2 NotificationService)
		{
			this.notificationService = NotificationService;
			this.notificationService.OnNotificationAdded += this.NotificationService_OnNotificationAdded;
		}

		/// <inheritdoc/>
		public event EventHandler<string>? UnreadChanged;

		/// <inheritdoc/>
		public async Task EnsureLoadedAsync()
		{
			if (this.loaded)
				return;

			await this.loadSemaphore.WaitAsync();
			try
			{
				if (this.loaded)
					return;

				IReadOnlyList<NotificationRecord> Records = await this.notificationService.GetAsync(new NotificationQuery()
				{
					Channels = [Constants.PushChannels.Tokens],
					States = [NotificationState.New, NotificationState.Delivered]
				}, CancellationToken.None);

				lock (this.synchObject)
				{
					foreach (NotificationRecord Record in Records)
					{
						if (!string.IsNullOrEmpty(Record.EntityId))
							this.Update(Record.EntityId, Record.Id, true);
					}
				}

				this.loaded = true;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
			finally
			{
				this.loadSemaphore.Release();
			}
		}

		/// <inheritdoc/>
		public int GetUnreadCount(string TokenId)
		{
			lock (this.synchObject)
			{
				return this.unreadByToken.TryGetValue(TokenId, out HashSet<string>? Ids) ? Ids.Count : 0;
			}
		}

		/// <inheritdoc/>
		public async Task MarkTokenReadAsync(string TokenId)
		{
			await this.EnsureLoadedAsync();

			string[] Ids;

			lock (this.synchObject)
			{
				if (!this.unreadByToken.TryGetValue(TokenId, out HashSet<string>? Unread) || Unread.Count == 0)
					return;

				Ids = [.. Unread];
			}

			foreach (string Id in Ids)
			{
				try
				{
					await this.notificationService.MarkReadAsync(Id, CancellationToken.None);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}
			}

			// Records deleted in the meantime raise no event when marked, so they are removed here as well.
			bool Changed = false;

			lock (this.synchObject)
			{
				foreach (string Id in Ids)
					Changed |= this.Update(TokenId, Id, false);
			}

			if (Changed)
				this.RaiseUnreadChanged(TokenId);
		}

		private Task NotificationService_OnNotificationAdded(object? Sender, NotificationRecordEventArgs e)
		{
			NotificationRecord Record = e.Record;

			if (!string.Equals(Record.Channel, Constants.PushChannels.Tokens, StringComparison.Ordinal) || string.IsNullOrEmpty(Record.EntityId))
				return Task.CompletedTask;

			bool Unread = Record.State == NotificationState.New || Record.State == NotificationState.Delivered;
			bool Changed;

			lock (this.synchObject)
			{
				Changed = this.Update(Record.EntityId, Record.Id, Unread);
			}

			if (Changed)
				this.RaiseUnreadChanged(Record.EntityId);

			return Task.CompletedTask;
		}

		/// <summary>
		/// Adds a record to, or removes it from, the unread records of a token. Called with the lock held.
		/// </summary>
		/// <param name="TokenId">Token ID.</param>
		/// <param name="RecordId">Notification record ID.</param>
		/// <param name="Unread">If the record is unread.</param>
		/// <returns>If the unread records of the token changed.</returns>
		private bool Update(string TokenId, string RecordId, bool Unread)
		{
			HashSet<string>? Ids;

			if (Unread)
			{
				if (!this.unreadByToken.TryGetValue(TokenId, out Ids))
				{
					Ids = new HashSet<string>(StringComparer.Ordinal);
					this.unreadByToken[TokenId] = Ids;
				}

				return Ids.Add(RecordId);
			}

			if (!this.unreadByToken.TryGetValue(TokenId, out Ids) || !Ids.Remove(RecordId))
				return false;

			if (Ids.Count == 0)
				this.unreadByToken.Remove(TokenId);

			return true;
		}

		private void RaiseUnreadChanged(string TokenId)
		{
			try
			{
				this.UnreadChanged?.Invoke(this, TokenId);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}
	}
}
