using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Waher.Events;
using Waher.Persistence;

namespace NeuroAccessMaui.Services.Notification
{
	/// <summary>
	/// Redesigned notification service with Waher persistence.
	/// </summary>
	public sealed class NotificationServiceV2 : LoadableService, INotificationServiceV2
	{
		private const int DefaultSchemaVersion = 1;
		private const int MaxPerChannel = 100;
		private const int MaxTotal = 1000;
		private static readonly TimeSpan IdBucket = TimeSpan.FromMinutes(1);
		private readonly INotificationIntentRouter intentRouter;
		private readonly INotificationFilterRegistry filterRegistry;
		private readonly List<Expectation> expectations = [];
		private readonly Dictionary<string, int> channelCounts = new(StringComparer.OrdinalIgnoreCase);
		private readonly Queue<NotificationIntent> pendingRoutes = new();

		/// <summary>
		/// Initializes a new instance of the <see cref="NotificationServiceV2"/> class.
		/// </summary>
		/// <param name="IntentRouter">Router used to navigate notification intents.</param>
		/// <param name="FilterRegistry">Registry for runtime ignore filters.</param>
		public NotificationServiceV2(INotificationIntentRouter IntentRouter, INotificationFilterRegistry FilterRegistry)
		{
			this.intentRouter = IntentRouter;
			this.filterRegistry = FilterRegistry;
		}

		/// <summary>
		/// Event raised when a notification is added or updated.
		/// </summary>
		public event EventHandlerAsync<NotificationRecordEventArgs>? OnNotificationAdded;

		/// <summary>
		/// Current counts per channel.
		/// </summary>
		public IReadOnlyDictionary<string, int> ChannelCounts
		{
			get
			{
				lock (this.channelCounts)
				{
					return new Dictionary<string, int>(this.channelCounts);
				}
			}
		}

		/// <summary>
		/// Adds a runtime ignore filter. Dispose the handle to remove it.
		/// </summary>
		/// <param name="Predicate">Predicate to decide if an intent should be ignored.</param>
		public IDisposable AddIgnoreFilter(Func<NotificationIntent, bool> Predicate)
		{
			return this.filterRegistry.AddFilter(Predicate);
		}

		/// <inheritdoc/>
		public string ComputeId(NotificationIntent Intent)
		{
			return this.ResolveId(Intent, Intent.Channel ?? string.Empty);
		}

		/// <summary>
		/// Loads the service.
		/// </summary>
		/// <param name="IsResuming">If the app is resuming.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		public override Task Load(bool IsResuming, CancellationToken CancellationToken)
		{
			return base.Load(IsResuming, CancellationToken);
		}

		/// <summary>
		/// Adds or updates a notification based on the provided intent.
		/// </summary>
		/// <param name="Intent">Notification intent.</param>
		/// <param name="Source">Origin of the notification.</param>
		/// <param name="RawPayload">Raw payload if available.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		public async Task AddAsync(NotificationIntent Intent, NotificationSource Source, string? RawPayload, CancellationToken CancellationToken)
		{
			NotificationRecord Record = this.CreateRecord(Intent, Source, RawPayload);

			NotificationRecord? ExistingRecord = await this.LoadByIdAsync(Record.Id);
			if (ExistingRecord is not null)
			{
				ExistingRecord.Title = Record.Title;
				ExistingRecord.Body = Record.Body;
				ExistingRecord.Action = Record.Action;
				ExistingRecord.EntityId = Record.EntityId;
				ExistingRecord.ExtrasJson = Record.ExtrasJson;
				ExistingRecord.RawPayload = Record.RawPayload ?? ExistingRecord.RawPayload;
				ExistingRecord.SchemaVersion = Record.SchemaVersion;
				ExistingRecord.Source = Record.Source;
				ExistingRecord.State = NotificationState.Delivered;
				ExistingRecord.DeliveredAt = ExistingRecord.DeliveredAt ?? DateTime.UtcNow;

				await Database.Update(ExistingRecord);
				await this.RaiseAdded(ExistingRecord);
				return;
			}

			Record.State = NotificationState.Delivered;
			Record.DeliveredAt = DateTime.UtcNow;

			await Database.Insert(Record);
			this.IncrementChannelCount(Record.Channel);
			await this.RaiseAdded(Record);
			await this.PruneAsync(CancellationToken);
		}

		/// <summary>
		/// Marks a notification as consumed and updates state.
		/// </summary>
		/// <param name="Id">Notification identifier.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		public async Task ConsumeAsync(string Id, CancellationToken CancellationToken)
		{
			NotificationRecord? Record = await this.LoadByIdAsync(Id);
			if (Record is null)
				return;

			Record.State = NotificationState.Consumed;
			Record.ConsumedAt = DateTime.UtcNow;
			await Database.Update(Record);
			await this.RaiseAdded(Record);

			NotificationIntent Intent = this.ToIntent(Record);
			NotificationRouteResult result = await this.intentRouter.RouteAsync(Intent, true, CancellationToken);
			if (result == NotificationRouteResult.Deferred)
			{
				lock (this.pendingRoutes)
				{
					this.pendingRoutes.Enqueue(Intent);
				}
			}
		}

		/// <summary>
		/// Retrieves notifications that match the query.
		/// </summary>
		/// <param name="Query">Query filter.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		/// <returns>Matching notifications.</returns>
		public async Task<IReadOnlyList<NotificationRecord>> GetAsync(NotificationQuery Query, CancellationToken CancellationToken)
		{
			List<NotificationRecord> Results = new();

			Filter filter = new(Query.Channels, Query.States);
			IEnumerable<NotificationRecord> FromDb = await Database.Find<NotificationRecord>(nameof(NotificationRecord.TimestampCreated));

			foreach (NotificationRecord Record in FromDb)
			{
				if (!filter.Matches(Record))
					continue;

				Results.Add(Record);

				if (Query.Limit is not null && Results.Count >= Query.Limit.Value)
					break;
			}

			return Results;
		}

		/// <summary>
		/// Applies retention pruning to stored notifications.
		/// </summary>
		/// <param name="CancellationToken">Cancellation token.</param>
		public async Task PruneAsync(CancellationToken CancellationToken)
		{
			List<NotificationRecord> All = new();
			Dictionary<string, List<NotificationRecord>> PerChannel = new(StringComparer.OrdinalIgnoreCase);

			IEnumerable<NotificationRecord> Records = await Database.Find<NotificationRecord>(nameof(NotificationRecord.TimestampCreated));
			foreach (NotificationRecord Record in Records)
			{
				All.Add(Record);
				if (!PerChannel.TryGetValue(Record.Channel, out List<NotificationRecord>? List))
				{
					List = new List<NotificationRecord>();
					PerChannel[Record.Channel] = List;
				}
				List.Add(Record);
			}

			List<NotificationRecord> ToDelete = new();

			foreach (KeyValuePair<string, List<NotificationRecord>> Pair in PerChannel)
			{
				List<NotificationRecord> Ordered = Pair.Value.OrderBy(R => R.TimestampCreated).ToList();
				int Excess = Ordered.Count - MaxPerChannel;
				if (Excess > 0)
					ToDelete.AddRange(Ordered.Take(Excess));
			}

			if (All.Count - ToDelete.Count > MaxTotal)
			{
				List<NotificationRecord> Ordered = All.OrderBy(R => R.TimestampCreated).ToList();
				int Need = (All.Count - MaxTotal) - ToDelete.Count;
				if (Need > 0)
					ToDelete.AddRange(Ordered.Take(Need));
			}

			foreach (NotificationRecord Record in ToDelete)
			{
				await Database.Delete(Record);
				this.DecrementChannelCount(Record.Channel);
			}
		}

		/// <summary>
		/// Deletes notifications by identifier.
		/// </summary>
		/// <param name="Ids">Identifiers to delete.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		public async Task DeleteAsync(IEnumerable<string> Ids, CancellationToken CancellationToken)
		{
			if (Ids is null)
				return;

			foreach (string Id in Ids)
			{
				NotificationRecord? Record = await this.LoadByIdAsync(Id);
				if (Record is null)
					continue;

				await Database.Delete(Record);
				this.DecrementChannelCount(Record.Channel);
				await this.RaiseAdded(Record);
			}
		}

		/// <inheritdoc/>
		public async Task ProcessPendingAsync(CancellationToken CancellationToken)
		{
			while (true)
			{
				NotificationIntent? intent = null;
				lock (this.pendingRoutes)
				{
					if (this.pendingRoutes.Count > 0)
						intent = this.pendingRoutes.Dequeue();
				}

				if (intent is null)
					break;

				await this.intentRouter.RouteAsync(intent, true, CancellationToken);
			}
		}

		private NotificationRecord CreateRecord(NotificationIntent Intent, NotificationSource Source, string? RawPayload)
		{
			string Channel = Intent.Channel ?? string.Empty;
			string ActionString = Intent.Action.ToString();
			string ExtrasJson = JsonSerializer.Serialize(Intent.Extras);

			string Id = this.ResolveId(Intent, Channel);

			return new NotificationRecord
			{
				Id = Id,
				Channel = Channel,
				Title = Intent.Title,
				Body = Intent.Body,
				Action = ActionString,
				EntityId = Intent.EntityId,
				ExtrasJson = ExtrasJson,
				RawPayload = RawPayload,
				SchemaVersion = Intent.Version > 0 ? Intent.Version : DefaultSchemaVersion,
				TimestampCreated = DateTime.UtcNow,
				State = NotificationState.New,
				Source = Source
			};
		}

		private string ResolveId(NotificationIntent Intent, string Channel)
		{
			StringBuilder builder = new();
			DateTime now = DateTime.UtcNow;
			DateTime bucket;
			if (IdBucket > TimeSpan.Zero)
			{
				long ticks = now.Ticks / IdBucket.Ticks;
				bucket = new DateTime(ticks * IdBucket.Ticks, DateTimeKind.Utc);
			}
			else
				bucket = now;
			string Correlation = Intent.CorrelationId ?? string.Empty;

			builder.Append(Channel);
			builder.Append('|');
			builder.Append(Intent.Action.ToString());
			builder.Append('|');
			builder.Append(Intent.EntityId ?? string.Empty);
			builder.Append('|');
			builder.Append(Correlation);
			builder.Append('|');
			builder.Append(bucket.ToString("o", CultureInfo.InvariantCulture));

			byte[] Input = Encoding.UTF8.GetBytes(builder.ToString());
			byte[] Hash = SHA256.HashData(Input);
			return Convert.ToHexString(Hash);
		}

		private async Task<NotificationRecord?> LoadByIdAsync(string Id)
		{
			try
			{
				return await Database.FindFirstDeleteRest<NotificationRecord>(nameof(NotificationRecord.Id), Id);
			}
			catch (KeyNotFoundException)
			{
				return null;
			}
		}

		private async Task RaiseAdded(NotificationRecord Record)
		{
			EventHandlerAsync<NotificationRecordEventArgs>? Handler = this.OnNotificationAdded;
			if (Handler is not null)
				await Handler.Raise(this, new NotificationRecordEventArgs(Record));

			this.TrySatisfyExpectations(Record);
		}

		/// <summary>
		/// Awaits a notification matching the predicate within a timeout.
		/// </summary>
		/// <param name="Predicate">Predicate to match.</param>
		/// <param name="Timeout">Timeout.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		/// <returns>Notification if found, otherwise null.</returns>
		public async Task<NotificationRecord?> WaitForAsync(Func<NotificationRecord, bool> Predicate, TimeSpan Timeout, CancellationToken CancellationToken)
		{
			IEnumerable<NotificationRecord> FromDb = await Database.Find<NotificationRecord>(nameof(NotificationRecord.TimestampCreated));
			foreach (NotificationRecord Record in FromDb)
			{
				if (Predicate(Record))
					return Record;
			}

			TaskCompletionSource<NotificationRecord?> Tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
			Expectation expectation = new(Predicate, DateTime.UtcNow.Add(Timeout), Tcs, false);

			lock (this.expectations)
			{
				this.expectations.Add(expectation);
			}

			using CancellationTokenRegistration reg = CancellationToken.Register(() => Tcs.TrySetCanceled(CancellationToken));
			Task delayTask = Task.Delay(Timeout, CancellationToken);
			Task finished = await Task.WhenAny(Tcs.Task, delayTask);

			lock (this.expectations)
			{
				this.expectations.Remove(expectation);
			}

			if (finished == Tcs.Task)
				return await Tcs.Task;

			return null;
		}

		/// <summary>
		/// Registers an expectation that will auto-route matching notifications when they arrive.
		/// </summary>
		/// <param name="Predicate">Predicate to match.</param>
		/// <param name="Timeout">Timeout.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		public Task ExpectAsync(Func<NotificationRecord, bool> Predicate, TimeSpan Timeout, CancellationToken CancellationToken)
		{
			Expectation expectation = new(Predicate, DateTime.UtcNow.Add(Timeout), null, true);

			lock (this.expectations)
			{
				this.expectations.Add(expectation);
			}

			_ = Task.Delay(Timeout, CancellationToken).ContinueWith(_ =>
			{
				lock (this.expectations)
				{
					this.expectations.Remove(expectation);
				}
			}, CancellationToken.None, TaskContinuationOptions.RunContinuationsAsynchronously, TaskScheduler.Default);

			return Task.CompletedTask;
		}

		private void TrySatisfyExpectations(NotificationRecord Record)
		{
			List<Expectation> Matches = new();

			lock (this.expectations)
			{
				DateTime now = DateTime.UtcNow;
				for (int i = this.expectations.Count - 1; i >= 0; i--)
				{
					Expectation exp = this.expectations[i];
					if (exp.ExpiresAt < now)
					{
						this.expectations.RemoveAt(i);
						continue;
					}

					if (exp.Predicate(Record))
					{
						this.expectations.RemoveAt(i);
						Matches.Add(exp);
					}
				}
			}

			foreach (Expectation exp in Matches)
			{
				if (exp.Source is not null)
					exp.Source.TrySetResult(Record);

				if (exp.RouteOnMatch)
					_ = this.ConsumeAsync(Record.Id, CancellationToken.None);
			}
		}

		private NotificationIntent ToIntent(NotificationRecord Record)
		{
			Dictionary<string, string>? extras = JsonSerializer.Deserialize<Dictionary<string, string>>(Record.ExtrasJson);

			return new NotificationIntent
			{
				Action = Enum.TryParse(Record.Action, out NotificationAction action) ? action : NotificationAction.Unknown,
				EntityId = Record.EntityId,
				Channel = Record.Channel,
				Title = Record.Title,
				Body = Record.Body,
				Extras = extras ?? new Dictionary<string, string>(),
				Version = Record.SchemaVersion
			};
		}

		private sealed record Expectation(Func<NotificationRecord, bool> Predicate, DateTime ExpiresAt, TaskCompletionSource<NotificationRecord?>? Source, bool RouteOnMatch);

		private void IncrementChannelCount(string Channel)
		{
			lock (this.channelCounts)
			{
				this.channelCounts.TryGetValue(Channel, out int count);
				this.channelCounts[Channel] = count + 1;
			}
		}

		private void DecrementChannelCount(string Channel)
		{
			lock (this.channelCounts)
			{
				if (this.channelCounts.TryGetValue(Channel, out int count) && count > 0)
					this.channelCounts[Channel] = count - 1;
			}
		}

		private sealed class Filter
		{
			private readonly HashSet<string>? channels;
			private readonly HashSet<NotificationState>? states;

			public Filter(IReadOnlyList<string>? Channels, IReadOnlyList<NotificationState>? States)
			{
				if (Channels is not null && Channels.Count > 0)
					this.channels = new HashSet<string>(Channels, StringComparer.OrdinalIgnoreCase);

				if (States is not null && States.Count > 0)
					this.states = new HashSet<NotificationState>(States);
			}

			public bool Matches(NotificationRecord Record)
			{
				if (this.channels is not null && !this.channels.Contains(Record.Channel))
					return false;

				if (this.states is not null && !this.states.Contains(Record.State))
					return false;

				return true;
			}
		}
	}
}
