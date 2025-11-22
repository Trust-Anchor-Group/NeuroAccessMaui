using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Waher.Events;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Notification
{
	/// <summary>
	/// Interface for the redesigned notification service.
	/// </summary>
	[DefaultImplementation(typeof(NotificationServiceV2))]
	public interface INotificationServiceV2 : ILoadableService
	{
		/// <summary>
		/// Adds or updates a notification based on the provided intent.
		/// </summary>
		/// <param name="Intent">Notification intent.</param>
		/// <param name="Source">Origin of the notification.</param>
		/// <param name="RawPayload">Raw payload, if any.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		Task AddAsync(NotificationIntent Intent, NotificationSource Source, string? RawPayload, CancellationToken CancellationToken);

		/// <summary>
		/// Marks a notification as consumed and routes it.
		/// </summary>
		/// <param name="Id">Notification identifier.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		Task ConsumeAsync(string Id, CancellationToken CancellationToken);

		/// <summary>
		/// Retrieves notifications matching the query.
		/// </summary>
		/// <param name="Query">Query filter.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		/// <returns>List of notifications.</returns>
		Task<IReadOnlyList<NotificationRecord>> GetAsync(NotificationQuery Query, CancellationToken CancellationToken);

		/// <summary>
		/// Applies retention pruning.
		/// </summary>
		/// <param name="CancellationToken">Cancellation token.</param>
		Task PruneAsync(CancellationToken CancellationToken);

		/// <summary>
		/// Awaits a notification matching the predicate within a timeout.
		/// </summary>
		/// <param name="Predicate">Match predicate.</param>
		/// <param name="Timeout">Timeout.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		/// <returns>The matching notification, if any.</returns>
		Task<NotificationRecord?> WaitForAsync(Func<NotificationRecord, bool> Predicate, TimeSpan Timeout, CancellationToken CancellationToken);

		/// <summary>
		/// Registers an expectation that will auto-route a matching notification when it arrives.
		/// </summary>
		/// <param name="Predicate">Match predicate.</param>
		/// <param name="Timeout">Timeout.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		Task ExpectAsync(Func<NotificationRecord, bool> Predicate, TimeSpan Timeout, CancellationToken CancellationToken);

		/// <summary>
		/// Event raised when a notification is added or updated.
		/// </summary>
		event EventHandlerAsync<NotificationRecordEventArgs>? OnNotificationAdded;

		/// <summary>
		/// Current counts per channel.
		/// </summary>
		IReadOnlyDictionary<string, int> ChannelCounts { get; }

		/// <summary>
		/// Adds a runtime ignore filter. Dispose the handle to remove it.
		/// </summary>
		/// <param name="Predicate">Predicate to decide if an intent should be ignored.</param>
		IDisposable AddIgnoreFilter(Func<NotificationIntent, bool> Predicate);

		/// <summary>
		/// Computes the stable identifier for an intent using the same logic as storage.
		/// </summary>
		/// <param name="Intent">Notification intent.</param>
		/// <returns>Stable identifier.</returns>
		string ComputeId(NotificationIntent Intent);

		/// <summary>
		/// Deletes notifications by identifier.
		/// </summary>
		/// <param name="Ids">Notification identifiers.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		Task DeleteAsync(IEnumerable<string> Ids, CancellationToken CancellationToken);

		/// <summary>
		/// Attempts to route any pending deferred intents.
		/// </summary>
		/// <param name="CancellationToken">Cancellation token.</param>
		Task ProcessPendingAsync(CancellationToken CancellationToken);
	}
}
